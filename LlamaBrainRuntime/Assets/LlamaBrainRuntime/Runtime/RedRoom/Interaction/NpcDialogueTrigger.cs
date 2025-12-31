#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using LlamaBrain.Runtime.RedRoom.AI;
using System.Threading;
using Cysharp.Threading.Tasks;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Metrics;
using LlamaBrain.Persona;
using LlamaBrain.Runtime.Core.Expectancy;
using Random = UnityEngine.Random;


namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  /// <summary>
  /// Triggers dialogue interactions with NPCs when the player interacts with trigger zones.
  /// Implements the ITrigger interface for collision-based interaction detection.
  /// Also implements ITriggerInfo for metrics collection.
  /// </summary>
  public class NpcDialogueTrigger : MonoBehaviour, ITrigger, ITriggerInfo
  {
    [SerializeField]
    private string _id = string.Empty;

    public System.Guid Id => System.Guid.Parse(_id);

    // ITriggerInfo implementation
    Guid ITriggerInfo.Id => Id;
    string ITriggerInfo.Name => GetTriggerName();
    string ITriggerInfo.PromptText => GetPromptText();
    int ITriggerInfo.PromptCount => PromptCount;


    [SerializeField]
    [TextArea(3, 10)]
    private string promptText = "";
    [SerializeField]
    private List<string> fallbackText = new List<string>();

    [Header("Expectancy Rules")]
    [Tooltip("Trigger-specific rules that apply when this trigger is activated.")]
    [SerializeField] private List<ExpectancyRuleAsset> triggerRules = new List<ExpectancyRuleAsset>();

    private string currentFallbackText = "";
    private CancellationTokenSource? cancellationTokenSource = null;
    private NpcFollowerExample? currentNpc = null;

    [SerializeField]
    private string conversationText = "";
    /// <summary>
    /// Gets the current conversation text displayed to the player.
    /// </summary>
    public string ConversationText => conversationText;

    [SerializeField]
    private int promptCount = 0;

    /// <summary>
    /// Gets the number of times this trigger has been prompted (player pressed E).
    /// </summary>
    public int PromptCount => promptCount;

    /// <summary>
    /// Gets the current NPC associated with this trigger.
    /// </summary>
    public NpcFollowerExample? CurrentNpc => currentNpc;

    private const string DEFAULT_PROMPT_TEXT = "MISSING TEXT";

    [Header("Events")]
    [SerializeField]
    private UnityEvent<string> onConversationTextGenerated = new UnityEvent<string>();


    private void OnDestroy()
    {
      cancellationTokenSource?.Cancel();
      cancellationTokenSource?.Dispose();
      if (currentNpc != null)
      {
        currentNpc.CurrentDialogueTrigger = null;
      }
    }

    private void Reset()
    {
      if (string.IsNullOrEmpty(_id)) _id = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Initializes the trigger ID if not already set.
    /// </summary>
    public void Awake()
    {
      if (string.IsNullOrEmpty(_id)) _id = System.Guid.NewGuid().ToString();
    }
    private void Start()
    {
      currentFallbackText = GetRandomFallbackText();
    }

    /// <summary>
    /// Subscribe to the conversation text generated event.
    /// </summary>
    /// <param name="callback">The callback to invoke when conversation text is generated</param>
    public void SubscribeToConversationTextGenerated(UnityAction<string> callback)
    {
      onConversationTextGenerated.AddListener(callback);
    }

    /// <summary>
    /// Unsubscribe from the conversation text generated event.
    /// </summary>
    /// <param name="callback">The callback to remove from the event</param>
    public void UnsubscribeFromConversationTextGenerated(UnityAction<string> callback)
    {
      onConversationTextGenerated.RemoveListener(callback);
    }


    /// <summary>
    /// Gets a random fallback text from the fallback text list.
    /// </summary>
    private string GetRandomFallbackText()
    {
      if (fallbackText.Count == 0) return DEFAULT_PROMPT_TEXT;
      return fallbackText[Random.Range(0, fallbackText.Count)];
    }

    /// <summary>
    /// Gets the trigger-specific rules for this trigger.
    /// </summary>
    public IReadOnlyList<ExpectancyRuleAsset> TriggerRules => triggerRules;

    /// <summary>
    /// Generates a conversation text asynchronously using the NPC's agent.
    /// </summary>
    private async UniTask<string> GenerateConversationTextAsync(NpcFollowerExample npc)
    {
      if (npc == null || npc.Agent == null)
      {
        Debug.LogWarning($"[NpcDialogueTrigger] No NPC or agent available. Using fallback.");
        return currentFallbackText;
      }

      var brainAgent = npc.Agent.GetBrainAgent();
      if (brainAgent == null)
      {
        Debug.LogWarning($"[NpcDialogueTrigger] No LlamaBrainAgent found. Using fallback.");
        return currentFallbackText;
      }

      Debug.Log($"[NpcDialogueTrigger] Generating dialogue for NPC: {npc.gameObject.name}");

      cancellationTokenSource?.Cancel();
      cancellationTokenSource = new CancellationTokenSource();

      try
      {
        // Create interaction context with trigger information
        var npcId = npc.gameObject.name;
        var triggerId = _id;
        var context = InteractionContext.FromZoneTrigger(npcId, triggerId, promptText, Time.time);
        context.SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Use context-aware method if trigger has rules or if agent supports it
        string response;
        if (triggerRules.Count > 0 || brainAgent.ExpectancyConfig != null)
        {
          // Pass trigger rules and fallbacks to the agent - it will evaluate both NPC rules and trigger rules
          // Trigger fallbacks take priority over agent-level fallbacks
          response = await brainAgent.SendPlayerInputWithContextAsync(promptText, context, triggerRules, fallbackText);
        }
        else
        {
          // Fallback to simple method if no expectancy system
          response = await npc.Agent.SayToNpc(promptText, cancellationTokenSource.Token);
        }

        conversationText = response ?? currentFallbackText;
        Debug.Log($"[NpcDialogueTrigger] Dialogue generated. Length: {response?.Length ?? 0}");

        // Record metrics if collector is available
        if (DialogueMetricsCollector.Instance != null && brainAgent.LastMetrics != null)
        {
          var wasTruncated = brainAgent.LastMetrics.GeneratedTokenCount >= brainAgent.MaxResponseTokens;
          DialogueMetricsCollector.Instance.RecordInteraction(
            brainAgent.LastMetrics,
            this,
            npc.gameObject.name,
            wasTruncated,
            brainAgent);
        }

        var finalResponse = response ?? currentFallbackText;
        onConversationTextGenerated?.Invoke(finalResponse);
        return finalResponse;
      }
      catch (System.OperationCanceledException)
      {
        Debug.Log($"[NpcDialogueTrigger] Conversation generation was cancelled.");
        return currentFallbackText;
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"[NpcDialogueTrigger] Error generating conversation: {ex.Message}");
        conversationText = currentFallbackText;
        onConversationTextGenerated?.Invoke(currentFallbackText);
        return currentFallbackText;
      }
    }

    /// <summary>
    /// Called when an NPC trigger collider enters this trigger zone (implements ITrigger interface).
    /// </summary>
    /// <param name="other">The trigger collider that entered this zone</param>
    public void OnCollideEnter(NpcTriggerCollider other)
    {
      Debug.Log($"[NpcDialogueTrigger] OnCollideEnter: '{other.gameObject.name}' entered '{gameObject.name}'");

      currentNpc = other.gameObject.GetComponentInParent<NpcFollowerExample>();
      if (currentNpc != null)
      {
        Debug.Log($"[NpcDialogueTrigger] Found NPC: {currentNpc.gameObject.name}. Setting trigger and generating dialogue.");
        currentNpc.CurrentDialogueTrigger = this;
        GenerateConversationTextAsync(currentNpc).Forget();
      }
      else
      {
        Debug.LogWarning($"[NpcDialogueTrigger] No NpcFollowerExample found on {other.gameObject.name} or parents.");
      }
    }

    /// <summary>
    /// Called when an NPC trigger collider exits this trigger zone (implements ITrigger interface).
    /// </summary>
    /// <param name="other">The trigger collider that exited this zone</param>
    public void OnCollideExit(NpcTriggerCollider other)
    {
      Debug.Log($"[NpcDialogueTrigger] OnCollideExit: '{other.gameObject.name}' exited '{gameObject.name}'");
      cancellationTokenSource?.Cancel();
      if (currentNpc != null)
      {
        currentNpc.CurrentDialogueTrigger = null;
      }
      currentNpc = null;
    }

    /// <summary>
    /// Triggers conversation generation. Uses the current NPC if none specified.
    /// </summary>
    /// <param name="npc">The NPC to generate conversation for. If null, uses the current NPC.</param>
    public void TriggerConversation(NpcFollowerExample? npc = null)
    {
      var targetNpc = npc ?? currentNpc;
      if (targetNpc != null)
      {
        GenerateConversationTextAsync(targetNpc).Forget();
        IncrementPromptCount();
      }
      else
      {
        Debug.LogWarning($"[NpcDialogueTrigger] No NPC available to generate conversation.");
        conversationText = currentFallbackText;
        onConversationTextGenerated?.Invoke(currentFallbackText);
      }
    }

    /// <summary>
    /// Increments the prompt count when the player interacts with this trigger.
    /// </summary>
    public void IncrementPromptCount()
    {
      promptCount++;
      Debug.Log($"[NpcDialogueTrigger] Prompt count incremented for '{gameObject.name}' (ID: {_id}). New count: {promptCount}");
    }

    /// <summary>
    /// Resets the prompt count to zero.
    /// </summary>
    public void ResetPromptCount()
    {
      Debug.Log($"[NpcDialogueTrigger] Prompt count reset for '{gameObject.name}' (ID: {_id}). Previous count: {promptCount}");
      promptCount = 0;
    }

    /// <summary>
    /// Gets the name of this trigger (implements ITrigger interface).
    /// </summary>
    /// <returns>The name of the GameObject this trigger is attached to</returns>
    public string GetTriggerName()
    {
      return gameObject.name;
    }
    
    /// <summary>
    /// Gets the prompt text for this trigger (for metrics collection)
    /// </summary>
    /// <returns>The prompt text configured for this trigger</returns>
    public string GetPromptText()
    {
      return promptText;
    }

    /// <summary>
    /// Gets fallback usage statistics from the associated NPC's agent (if available).
    /// Returns null if no agent is available or agent doesn't support fallback stats.
    /// </summary>
    /// <returns>Fallback statistics or null</returns>
    public LlamaBrain.Runtime.Core.AuthorControlledFallback.FallbackStats? GetFallbackStats()
    {
      if (currentNpc?.Agent == null)
        return null;

      var brainAgent = currentNpc.Agent.GetBrainAgent();
      return brainAgent?.FallbackStats;
    }

    /// <summary>
    /// Gets mutation statistics from the associated NPC's agent (if available).
    /// Returns null if no agent is available or agent doesn't support mutation stats.
    /// </summary>
    /// <returns>Mutation statistics or null</returns>
    public MutationStatistics? GetMutationStats()
    {
      if (currentNpc?.Agent == null)
        return null;

      var brainAgent = currentNpc.Agent.GetBrainAgent();
      return brainAgent?.MutationStats;
    }

    /// <summary>
    /// Gets the last mutation batch result from the associated NPC's agent (if available).
    /// Returns null if no agent is available or no mutations have been executed.
    /// </summary>
    /// <returns>The last mutation batch result or null</returns>
    public MutationBatchResult? GetLastMutationBatchResult()
    {
      if (currentNpc?.Agent == null)
        return null;

      var brainAgent = currentNpc.Agent.GetBrainAgent();
      return brainAgent?.LastMutationBatchResult;
    }
  }
}
