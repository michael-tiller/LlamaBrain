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
  /// </summary>
  [AddComponentMenu("LlamaBrain/World Intent Dispatcher")]
  public class WorldIntentDispatcher : MonoBehaviour
  {
    /// <summary>
    /// Singleton instance for global access.
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

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Debug.LogWarning("[WorldIntentDispatcher] Multiple instances detected, destroying duplicate");
        Destroy(gameObject);
        return;
      }
      Instance = this;
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }

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
    /// </summary>
    /// <param name="controller">The mutation controller to hook.</param>
    public void HookToController(MemoryMutationController controller)
    {
      if (controller == null) return;
      controller.OnWorldIntentEmitted += HandleWorldIntent;
    }

    /// <summary>
    /// Unhooks this dispatcher from a mutation controller.
    /// </summary>
    /// <param name="controller">The mutation controller to unhook.</param>
    public void UnhookFromController(MemoryMutationController controller)
    {
      if (controller == null) return;
      controller.OnWorldIntentEmitted -= HandleWorldIntent;
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
