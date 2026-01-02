using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;

#nullable enable

namespace LlamaBrainRuntime.Core
{
  /// <summary>
  /// Unity event for world intents with intent type and target.
  /// </summary>
  [Serializable]
  public class WorldIntentEvent : UnityEvent<string, string, Dictionary<string, string>> { }

  /// <summary>
  /// Unity event for specific intent types.
  /// </summary>
  [Serializable]
  public class SpecificIntentEvent : UnityEvent<WorldIntent, string> { }

  /// <summary>
  /// Configuration for a registered intent handler.
  /// </summary>
  [Serializable]
  public class IntentHandlerConfig
  {
    /// <summary>
    /// The intent type to handle (e.g., "follow_player", "give_item").
    /// </summary>
    [Tooltip("The intent type to handle (e.g., 'follow_player', 'give_item')")]
    public string intentType = "";

    /// <summary>
    /// Event fired when this intent type is received.
    /// Parameters: WorldIntent, NpcId
    /// </summary>
    [Tooltip("Event fired when this intent type is received")]
    public SpecificIntentEvent onIntentReceived = new SpecificIntentEvent();
  }

  /// <summary>
  /// Dispatches world intents from the memory mutation controller to game systems.
  /// Acts as a bridge between the LlamaBrain validation/mutation pipeline and Unity game logic.
  ///
  /// Game systems can subscribe to specific intent types to handle NPC desires like
  /// "follow_player", "give_item", "start_quest", etc.
  ///
  /// SINGLETON LIFECYCLE (Phase 10.5 Requirement):
  /// - Lifetime Model: SCENE-LOCAL (Option A)
  /// - Each scene should contain exactly one WorldIntentDispatcher
  /// - Does NOT use DontDestroyOnLoad
  /// - If duplicate detected in Awake(): component is disabled, gameObject is destroyed (end-of-frame)
  /// - OnDestroy() clears Instance only if Instance == this
  /// - Tests must yield return null before asserting duplicate removal (Unity lifecycle)
  /// </summary>
  [AddComponentMenu("LlamaBrain/World Intent Dispatcher")]
  public class WorldIntentDispatcher : MonoBehaviour
  {
    /// <summary>
    /// Singleton instance for global access.
    /// Cleared only when the Instance itself is destroyed, not duplicates.
    /// </summary>
    public static WorldIntentDispatcher? Instance { get; private set; }

    [Header("General Events")]
    [Tooltip("Event fired for all intents. Parameters: intentType, target, parameters")]
    [SerializeField] private WorldIntentEvent onAnyIntent = new WorldIntentEvent();

    [Header("Intent-Specific Handlers")]
    [Tooltip("Handlers for specific intent types")]
    [SerializeField] private List<IntentHandlerConfig> intentHandlers = new List<IntentHandlerConfig>();

    [Header("Debug")]
    [Tooltip("Enable debug logging for dispatched intents")]
    [SerializeField] private bool debugLogging = false;

    [Tooltip("Maximum number of intents to keep in history")]
    [SerializeField] private int maxHistorySize = 100;

    // Runtime state
    private readonly Queue<WorldIntentRecord> intentHistory = new Queue<WorldIntentRecord>();
    private readonly Dictionary<string, List<Action<WorldIntent, string?>>> codeHandlers
      = new Dictionary<string, List<Action<WorldIntent, string?>>>();
    private readonly HashSet<MemoryMutationController> hookedControllers = new HashSet<MemoryMutationController>();

    /// <summary>
    /// Event fired for any dispatched intent.
    /// </summary>
    public WorldIntentEvent OnAnyIntent => onAnyIntent;

    /// <summary>
    /// Gets the intent history (most recent first).
    /// </summary>
    public IEnumerable<WorldIntentRecord> IntentHistory => intentHistory;

    /// <summary>
    /// Total number of intents dispatched.
    /// </summary>
    public int TotalIntentsDispatched { get; private set; }

    /// <summary>
    /// Indicates whether this instance was marked as a duplicate and is pending destruction.
    /// Used by tests to verify singleton enforcement.
    /// </summary>
    public bool IsDuplicateInstance { get; private set; }

    private void Awake()
    {
      // Singleton enforcement: only one Instance allowed per scene
      if (Instance != null && Instance != this)
      {
        Debug.LogWarning($"[WorldIntentDispatcher] Multiple instances detected. " +
          $"Existing: {Instance.gameObject.name}, Duplicate: {gameObject.name}. Destroying duplicate.");

        // Mark as duplicate for test verification
        IsDuplicateInstance = true;

        // Disable the component immediately to prevent any further processing
        enabled = false;

        // Destroy the gameObject at end-of-frame (Unity lifecycle)
        // Note: Destroy is deferred, so tests must yield return null before asserting
        Destroy(gameObject);
        return;
      }

      Instance = this;
      IsDuplicateInstance = false;
    }

    private void OnDestroy()
    {
      // Only clear Instance if WE are the actual singleton instance
      // Duplicates should not affect the Instance reference when destroyed
      if (Instance == this)
      {
        Instance = null;
      }
    }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    /// <summary>
    /// Resets the singleton instance for testing purposes.
    /// Only available in editor and test builds.
    /// </summary>
    internal static void ResetForTests()
    {
      Instance = null;
    }

    /// <summary>
    /// Sets the maximum history size for testing purposes.
    /// Only available in editor and test builds.
    /// </summary>
    internal void SetMaxHistorySizeForTests(int size)
    {
      maxHistorySize = size;
    }
#endif

    /// <summary>
    /// Dispatches a world intent to all registered handlers.
    /// Called by the MemoryMutationController when intents are emitted.
    /// </summary>
    /// <param name="intent">The world intent to dispatch.</param>
    /// <param name="npcId">The NPC that emitted this intent.</param>
    public void DispatchIntent(WorldIntent intent, string? npcId)
    {
      if (intent == null) return;

      if (debugLogging)
      {
        Debug.Log($"[WorldIntentDispatcher] Dispatching: {intent.IntentType} from {npcId ?? "unknown"} -> {intent.Target ?? "none"}");
      }

      TotalIntentsDispatched++;

      // Record in history
      var record = new WorldIntentRecord(intent, npcId, Time.time);
      intentHistory.Enqueue(record);
      while (intentHistory.Count > maxHistorySize)
      {
        intentHistory.Dequeue();
      }

      // Fire general event
      onAnyIntent?.Invoke(intent.IntentType, intent.Target ?? "", intent.Parameters);

      // Fire type-specific Unity events
      foreach (var handler in intentHandlers)
      {
        if (string.Equals(handler.intentType, intent.IntentType, StringComparison.OrdinalIgnoreCase))
        {
          handler.onIntentReceived?.Invoke(intent, npcId ?? "");
        }
      }

      // Fire code-registered handlers
      if (codeHandlers.TryGetValue(intent.IntentType.ToLowerInvariant(), out var handlers))
      {
        foreach (var handler in handlers)
        {
          try
          {
            handler(intent, npcId);
          }
          catch (Exception ex)
          {
            Debug.LogError($"[WorldIntentDispatcher] Handler error for {intent.IntentType}: {ex.Message}");
          }
        }
      }

      // Also check for wildcard handlers
      if (codeHandlers.TryGetValue("*", out var wildcardHandlers))
      {
        foreach (var handler in wildcardHandlers)
        {
          try
          {
            handler(intent, npcId);
          }
          catch (Exception ex)
          {
            Debug.LogError($"[WorldIntentDispatcher] Wildcard handler error: {ex.Message}");
          }
        }
      }
    }

    /// <summary>
    /// Dispatches multiple intents from a mutation batch result.
    /// </summary>
    /// <param name="batchResult">The mutation batch result containing intents.</param>
    /// <param name="npcId">The NPC that emitted these intents.</param>
    public void DispatchBatch(MutationBatchResult batchResult, string? npcId)
    {
      if (batchResult == null) return;

      foreach (var intent in batchResult.EmittedIntents)
      {
        DispatchIntent(intent, npcId);
      }
    }

    /// <summary>
    /// Registers a code-based handler for a specific intent type.
    /// </summary>
    /// <param name="intentType">The intent type to handle (case-insensitive). Use "*" for all intents.</param>
    /// <param name="handler">The handler action. Parameters: WorldIntent, NpcId.</param>
    public void RegisterHandler(string intentType, Action<WorldIntent, string?> handler)
    {
      var key = intentType.ToLowerInvariant();
      if (!codeHandlers.ContainsKey(key))
      {
        codeHandlers[key] = new List<Action<WorldIntent, string?>>();
      }
      codeHandlers[key].Add(handler);

      if (debugLogging)
      {
        Debug.Log($"[WorldIntentDispatcher] Registered handler for: {intentType}");
      }
    }

    /// <summary>
    /// Unregisters a code-based handler.
    /// </summary>
    /// <param name="intentType">The intent type.</param>
    /// <param name="handler">The handler to remove.</param>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterHandler(string intentType, Action<WorldIntent, string?> handler)
    {
      var key = intentType.ToLowerInvariant();
      if (codeHandlers.TryGetValue(key, out var handlers))
      {
        return handlers.Remove(handler);
      }
      return false;
    }

    /// <summary>
    /// Clears all code-registered handlers.
    /// </summary>
    public void ClearCodeHandlers()
    {
      codeHandlers.Clear();
    }

    /// <summary>
    /// Clears the intent history.
    /// </summary>
    public void ClearHistory()
    {
      intentHistory.Clear();
    }

    /// <summary>
    /// Resets statistics.
    /// </summary>
    public void ResetStatistics()
    {
      TotalIntentsDispatched = 0;
      intentHistory.Clear();
    }

    /// <summary>
    /// Gets intents from a specific NPC.
    /// </summary>
    /// <param name="npcId">The NPC ID to filter by.</param>
    /// <returns>Intents from the specified NPC.</returns>
    public IEnumerable<WorldIntentRecord> GetIntentsFromNpc(string npcId)
    {
      foreach (var record in intentHistory)
      {
        if (string.Equals(record.NpcId, npcId, StringComparison.OrdinalIgnoreCase))
        {
          yield return record;
        }
      }
    }

    /// <summary>
    /// Gets intents of a specific type.
    /// </summary>
    /// <param name="intentType">The intent type to filter by.</param>
    /// <returns>Intents of the specified type.</returns>
    public IEnumerable<WorldIntentRecord> GetIntentsByType(string intentType)
    {
      foreach (var record in intentHistory)
      {
        if (string.Equals(record.Intent.IntentType, intentType, StringComparison.OrdinalIgnoreCase))
        {
          yield return record;
        }
      }
    }

    /// <summary>
    /// Hooks this dispatcher to a mutation controller to automatically dispatch emitted intents.
    /// This method is idempotent: calling it multiple times with the same controller will not
    /// result in double-subscription.
    /// </summary>
    /// <param name="controller">The mutation controller to hook.</param>
    public void HookToController(MemoryMutationController controller)
    {
      if (controller == null) return;
      
      // Idempotent: only subscribe if not already hooked
      if (!hookedControllers.Contains(controller))
      {
        controller.OnWorldIntentEmitted += HandleWorldIntent;
        hookedControllers.Add(controller);
      }
    }

    /// <summary>
    /// Unhooks this dispatcher from a mutation controller.
    /// </summary>
    /// <param name="controller">The mutation controller to unhook.</param>
    public void UnhookFromController(MemoryMutationController controller)
    {
      if (controller == null) return;
      
      // Only unsubscribe if we're actually hooked
      if (hookedControllers.Contains(controller))
      {
        controller.OnWorldIntentEmitted -= HandleWorldIntent;
        hookedControllers.Remove(controller);
      }
    }

    private void HandleWorldIntent(object? sender, WorldIntentEventArgs e)
    {
      DispatchIntent(e.Intent, e.NpcId);
    }
  }

  /// <summary>
  /// Record of a dispatched world intent for history tracking.
  /// </summary>
  [Serializable]
  public class WorldIntentRecord
  {
    /// <summary>
    /// The world intent that was dispatched.
    /// </summary>
    public WorldIntent Intent { get; }

    /// <summary>
    /// The NPC that emitted this intent.
    /// </summary>
    public string? NpcId { get; }

    /// <summary>
    /// The game time when this intent was dispatched.
    /// </summary>
    public float GameTime { get; }

    /// <summary>
    /// Creates a new intent record.
    /// </summary>
    /// <param name="intent">The world intent that was dispatched.</param>
    /// <param name="npcId">The NPC that emitted this intent.</param>
    /// <param name="gameTime">The game time when this intent was dispatched.</param>
    public WorldIntentRecord(WorldIntent intent, string? npcId, float gameTime)
    {
      Intent = intent;
      NpcId = npcId;
      GameTime = gameTime;
    }

    /// <inheritdoc/>
    /// <returns>A string representation of the world intent record.</returns>
    public override string ToString()
    {
      return $"[{GameTime:F1}s] {NpcId ?? "?"} -> {Intent.IntentType}: {Intent.Target ?? "none"}";
    }
  }
}
