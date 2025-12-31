#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// Provides author-controlled fallback responses when inference fails.
  /// Supports context-aware fallbacks based on trigger reason and tracks usage statistics.
  /// </summary>
  public class AuthorControlledFallback
  {
    /// <summary>
    /// Configuration for fallback responses.
    /// </summary>
    public class FallbackConfig
    {
      /// <summary>
      /// Generic safe responses that work in any context.
      /// </summary>
      public List<string> GenericFallbacks { get; set; } = new List<string>
      {
        "I'm not sure how to respond to that.",
        "Let me think about that.",
        "That's interesting.",
        "I see.",
        "Hmm, I'm not certain about that."
      };

      /// <summary>
      /// Fallback responses for player utterances.
      /// </summary>
      public List<string> PlayerUtteranceFallbacks { get; set; } = new List<string>
      {
        "I'm sorry, I didn't quite catch that.",
        "Could you repeat that?",
        "I'm not sure I understand.",
        "What did you say?"
      };

      /// <summary>
      /// Fallback responses for zone triggers.
      /// </summary>
      public List<string> ZoneTriggerFallbacks { get; set; } = new List<string>
      {
        "Hello there.",
        "Greetings, traveler.",
        "How can I help you?",
        "What brings you here?"
      };

      /// <summary>
      /// Fallback responses for time-based triggers.
      /// </summary>
      public List<string> TimeTriggerFallbacks { get; set; } = new List<string>
      {
        "Another day passes...",
        "Time moves on.",
        "The world keeps turning."
      };

      /// <summary>
      /// Fallback responses for quest triggers.
      /// </summary>
      public List<string> QuestTriggerFallbacks { get; set; } = new List<string>
      {
        "I have a task for you.",
        "There's something I need to tell you.",
        "I have important news."
      };

      /// <summary>
      /// Fallback responses for NPC interactions.
      /// </summary>
      public List<string> NpcInteractionFallbacks { get; set; } = new List<string>
      {
        "Hello, friend.",
        "Good to see you.",
        "How are you doing?"
      };

      /// <summary>
      /// Fallback responses for world events.
      /// </summary>
      public List<string> WorldEventFallbacks { get; set; } = new List<string>
      {
        "Something has changed in the world.",
        "The world shifts around us.",
        "Can you feel it too?"
      };

      /// <summary>
      /// Fallback responses for custom triggers.
      /// </summary>
      public List<string> CustomTriggerFallbacks { get; set; } = new List<string>
      {
        "I'm here if you need me.",
        "What can I do for you?",
        "Is there something you need?"
      };

      /// <summary>
      /// Emergency fallbacks used when all other lists are empty.
      /// </summary>
      public List<string> EmergencyFallbacks { get; set; } = new List<string>
      {
        "I'm having trouble responding right now.",
        "Something seems off, but I'm still here.",
        "I apologize, but I can't form a proper response."
      };
    }

    /// <summary>
    /// Statistics about fallback usage.
    /// </summary>
    public class FallbackStats : IFallbackStats
    {
      /// <summary>
      /// Total number of times fallbacks have been used.
      /// </summary>
      public int TotalFallbacks { get; internal set; }

      /// <summary>
      /// Number of fallbacks by trigger reason.
      /// </summary>
      public Dictionary<TriggerReason, int> FallbacksByTriggerReason { get; internal set; } = new Dictionary<TriggerReason, int>();

      /// <summary>
      /// Number of fallbacks by failure reason (first 100 characters of failure reason).
      /// </summary>
      public Dictionary<string, int> FallbacksByFailureReason { get; internal set; } = new Dictionary<string, int>();

      /// <summary>
      /// Number of times emergency fallbacks were used.
      /// </summary>
      public int EmergencyFallbacks { get; internal set; }

      // IFallbackStats interface implementation
      IReadOnlyDictionary<TriggerReason, int> IFallbackStats.FallbacksByTriggerReason => FallbacksByTriggerReason;
      IReadOnlyDictionary<string, int> IFallbackStats.FallbacksByFailureReason => FallbacksByFailureReason;

      /// <summary>
      /// Gets a formatted string representation of the statistics.
      /// </summary>
      /// <returns>A formatted string containing fallback statistics.</returns>
      public override string ToString()
      {
        var lines = new List<string>
        {
          $"Total Fallbacks: {TotalFallbacks}",
          $"Emergency Fallbacks: {EmergencyFallbacks}"
        };

        if (FallbacksByTriggerReason.Count > 0)
        {
          lines.Add("By Trigger Reason:");
          foreach (var kvp in FallbacksByTriggerReason.OrderByDescending(x => x.Value))
          {
            lines.Add($"  {kvp.Key}: {kvp.Value}");
          }
        }

        if (FallbacksByFailureReason.Count > 0)
        {
          lines.Add("By Failure Reason:");
          foreach (var kvp in FallbacksByFailureReason.OrderByDescending(x => x.Value).Take(10))
          {
            lines.Add($"  {kvp.Key.Substring(0, System.Math.Min(50, kvp.Key.Length))}...: {kvp.Value}");
          }
        }

        return string.Join("\n", lines);
      }
    }

    private readonly FallbackConfig config;
    private readonly FallbackStats stats = new FallbackStats();
    private readonly System.Random random = new System.Random();

    /// <summary>
    /// Gets the fallback statistics.
    /// </summary>
    public FallbackStats Stats => stats;

    /// <summary>
    /// Creates a new AuthorControlledFallback with default configuration.
    /// </summary>
    public AuthorControlledFallback() : this(new FallbackConfig())
    {
    }

    /// <summary>
    /// Creates a new AuthorControlledFallback with custom configuration.
    /// </summary>
    /// <param name="config">The fallback configuration to use.</param>
    public AuthorControlledFallback(FallbackConfig config)
    {
      this.config = config ?? new FallbackConfig();
    }

    /// <summary>
    /// Gets a fallback response based on the interaction context and failure reason.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="failureReason">The reason the inference failed.</param>
    /// <param name="finalResult">Optional final inference result for additional context.</param>
    /// <param name="triggerFallbacks">Optional trigger-specific fallback list. If provided, these take priority over default fallbacks.</param>
    /// <returns>A fallback response string.</returns>
    public string GetFallbackResponse(InteractionContext context, string failureReason, InferenceResultWithRetries? finalResult = null, IReadOnlyList<string>? triggerFallbacks = null)
    {
      if (context == null)
      {
        context = new InteractionContext
        {
          TriggerReason = TriggerReason.PlayerUtterance,
          GameTime = Time.time
        };
      }

      // Update statistics
      stats.TotalFallbacks++;
      
      if (!stats.FallbacksByTriggerReason.ContainsKey(context.TriggerReason))
      {
        stats.FallbacksByTriggerReason[context.TriggerReason] = 0;
      }
      stats.FallbacksByTriggerReason[context.TriggerReason]++;

      // Track failure reason (truncate to first 100 chars to avoid memory bloat)
      var failureReasonKey = failureReason?.Length > 100 ? failureReason.Substring(0, 100) : failureReason ?? "Unknown";
      if (!stats.FallbacksByFailureReason.ContainsKey(failureReasonKey))
      {
        stats.FallbacksByFailureReason[failureReasonKey] = 0;
      }
      stats.FallbacksByFailureReason[failureReasonKey]++;

      // Priority 1: Use trigger-specific fallbacks if provided
      List<string>? fallbackList = null;
      if (triggerFallbacks != null && triggerFallbacks.Count > 0)
      {
        fallbackList = new List<string>(triggerFallbacks);
        UnityEngine.Debug.Log($"[AuthorControlledFallback] Using {fallbackList.Count} trigger-specific fallback(s)");
      }

      // Priority 2: Get context-specific fallback list from config
      if (fallbackList == null || fallbackList.Count == 0)
      {
        fallbackList = GetFallbackListForTrigger(context.TriggerReason);
      }

      // Priority 3: If list is empty, try generic fallbacks
      if (fallbackList.Count == 0)
      {
        fallbackList = config.GenericFallbacks;
      }

      // Priority 4: If still empty, use emergency fallbacks
      if (fallbackList.Count == 0)
      {
        fallbackList = config.EmergencyFallbacks;
        stats.EmergencyFallbacks++;
        UnityEngine.Debug.LogWarning("[AuthorControlledFallback] Emergency fallback list is empty! Using hardcoded fallback.");
        
        // Last resort: hardcoded fallback
        if (fallbackList.Count == 0)
        {
          return "I'm having trouble responding right now.";
        }
      }

      // Select random fallback from the list
      var selectedFallback = fallbackList[random.Next(fallbackList.Count)];

      // Log the fallback usage
      var triggerReason = context.TriggerReason.ToString();
      var fullFailureReason = string.IsNullOrEmpty(failureReason) ? "Unknown" : failureReason;
      var source = triggerFallbacks != null && triggerFallbacks.Count > 0 ? "trigger-specific" : "default";
      UnityEngine.Debug.LogWarning($"[AuthorControlledFallback] Using {source} fallback. Reason: {fullFailureReason}, Trigger: {triggerReason}");

      return selectedFallback;
    }

    /// <summary>
    /// Gets the appropriate fallback list for the given trigger reason.
    /// </summary>
    /// <param name="triggerReason">The trigger reason.</param>
    /// <returns>A list of fallback responses.</returns>
    private List<string> GetFallbackListForTrigger(TriggerReason triggerReason)
    {
      return triggerReason switch
      {
        TriggerReason.PlayerUtterance => config.PlayerUtteranceFallbacks,
        TriggerReason.ZoneTrigger => config.ZoneTriggerFallbacks,
        TriggerReason.TimeTrigger => config.TimeTriggerFallbacks,
        TriggerReason.QuestTrigger => config.QuestTriggerFallbacks,
        TriggerReason.NpcInteraction => config.NpcInteractionFallbacks,
        TriggerReason.WorldEvent => config.WorldEventFallbacks,
        TriggerReason.Custom => config.CustomTriggerFallbacks,
        _ => config.GenericFallbacks
      };
    }
  }
}

