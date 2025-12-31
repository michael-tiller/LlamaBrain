#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// Provides author-controlled fallback responses when inference fails after all retries.
  /// Supports generic, context-aware, and emergency fallbacks with failure reason logging.
  /// </summary>
  public class AuthorControlledFallback
  {
    /// <summary>
    /// Configuration for fallback responses.
    /// </summary>
    public class FallbackConfig
    {
      /// <summary>
      /// Generic safe responses (used when no context-specific fallback matches).
      /// </summary>
      public List<string> GenericFallbacks { get; set; } = new List<string>
      {
        "I'm not sure what to say.",
        "Let me think about that.",
        "That's interesting.",
        "I see."
      };

      /// <summary>
      /// Context-aware fallbacks keyed by TriggerReason.
      /// </summary>
      public Dictionary<TriggerReason, List<string>> ContextFallbacks { get; set; } = new Dictionary<TriggerReason, List<string>>();

      /// <summary>
      /// Emergency fallbacks that always work (used as last resort).
      /// </summary>
      public List<string> EmergencyFallbacks { get; set; } = new List<string>
      {
        "Hello there!",
        "Yes?",
        "Hmm?",
        "What can I do for you?"
      };

      /// <summary>
      /// Whether to log failure reasons when fallbacks are used.
      /// </summary>
      public bool LogFailureReasons { get; set; } = true;
    }

    private readonly FallbackConfig config;
    private readonly System.Random random = new System.Random();

    /// <summary>
    /// Statistics for fallback usage tracking.
    /// </summary>
    public class FallbackStats
    {
      /// <summary>
      /// Total number of times fallback was used.
      /// </summary>
      public int TotalFallbacks { get; set; }

      /// <summary>
      /// Number of times each trigger reason triggered a fallback.
      /// </summary>
      public Dictionary<TriggerReason, int> FallbacksByTrigger { get; set; } = new Dictionary<TriggerReason, int>();

      /// <summary>
      /// Number of times each failure reason occurred.
      /// </summary>
      public Dictionary<string, int> FallbacksByFailureReason { get; set; } = new Dictionary<string, int>();

      /// <summary>
      /// Number of times emergency fallback was used.
      /// </summary>
      public int EmergencyFallbacks { get; set; }
    }

    private readonly FallbackStats stats = new FallbackStats();

    /// <summary>
    /// Gets the current fallback statistics.
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
    /// <param name="config">The fallback configuration</param>
    public AuthorControlledFallback(FallbackConfig config)
    {
      this.config = config ?? throw new ArgumentNullException(nameof(config));
      InitializeDefaultContextFallbacks();
    }

    /// <summary>
    /// Initializes default context-aware fallbacks for each trigger reason.
    /// </summary>
    private void InitializeDefaultContextFallbacks()
    {
      // PlayerUtterance fallbacks
      if (!config.ContextFallbacks.ContainsKey(TriggerReason.PlayerUtterance))
      {
        config.ContextFallbacks[TriggerReason.PlayerUtterance] = new List<string>
        {
          "I'm not sure how to respond to that.",
          "That's an interesting question.",
          "Let me think about that for a moment.",
          "I don't have a good answer for that right now."
        };
      }

      // ZoneTrigger fallbacks
      if (!config.ContextFallbacks.ContainsKey(TriggerReason.ZoneTrigger))
      {
        config.ContextFallbacks[TriggerReason.ZoneTrigger] = new List<string>
        {
          "Something caught my attention.",
          "What's happening here?",
          "I notice something interesting.",
          "This place seems familiar."
        };
      }

      // TimeTrigger fallbacks
      if (!config.ContextFallbacks.ContainsKey(TriggerReason.TimeTrigger))
      {
        config.ContextFallbacks[TriggerReason.TimeTrigger] = new List<string>
        {
          "Time passes...",
          "Another day, another adventure.",
          "The world keeps turning.",
          "I wonder what will happen next."
        };
      }

      // QuestTrigger fallbacks
      if (!config.ContextFallbacks.ContainsKey(TriggerReason.QuestTrigger))
      {
        config.ContextFallbacks[TriggerReason.QuestTrigger] = new List<string>
        {
          "I have something important to tell you.",
          "There's a matter we should discuss.",
          "I need to speak with you about something.",
          "This is important."
        };
      }

      // NpcInteraction fallbacks
      if (!config.ContextFallbacks.ContainsKey(TriggerReason.NpcInteraction))
      {
        config.ContextFallbacks[TriggerReason.NpcInteraction] = new List<string>
        {
          "Hello there!",
          "Good to see you.",
          "How are you doing?",
          "Nice to meet you."
        };
      }

      // WorldEvent fallbacks
      if (!config.ContextFallbacks.ContainsKey(TriggerReason.WorldEvent))
      {
        config.ContextFallbacks[TriggerReason.WorldEvent] = new List<string>
        {
          "Something significant just happened.",
          "The world is changing.",
          "I sense something important.",
          "This is a momentous occasion."
        };
      }

      // Custom fallbacks
      if (!config.ContextFallbacks.ContainsKey(TriggerReason.Custom))
      {
        config.ContextFallbacks[TriggerReason.Custom] = new List<string>
        {
          "I'm not sure what to make of this.",
          "This is unusual.",
          "Something unexpected happened.",
          "I need a moment to process this."
        };
      }
    }

    /// <summary>
    /// Gets a fallback response based on the interaction context and failure information.
    /// </summary>
    /// <param name="context">The interaction context (for trigger reason)</param>
    /// <param name="failureReason">The reason inference failed (for logging)</param>
    /// <param name="inferenceResult">The final inference result (for detailed failure info)</param>
    /// <returns>A fallback response string</returns>
    public string GetFallbackResponse(
      InteractionContext? context,
      string? failureReason = null,
      InferenceResultWithRetries? inferenceResult = null)
    {
      // Update statistics
      stats.TotalFallbacks++;

      var triggerReason = context?.TriggerReason ?? TriggerReason.PlayerUtterance;
      if (!stats.FallbacksByTrigger.ContainsKey(triggerReason))
      {
        stats.FallbacksByTrigger[triggerReason] = 0;
      }
      stats.FallbacksByTrigger[triggerReason]++;

      // Build failure reason string
      var fullFailureReason = BuildFailureReasonString(failureReason, inferenceResult);
      if (!string.IsNullOrEmpty(fullFailureReason))
      {
        if (!stats.FallbacksByFailureReason.ContainsKey(fullFailureReason))
        {
          stats.FallbacksByFailureReason[fullFailureReason] = 0;
        }
        stats.FallbacksByFailureReason[fullFailureReason]++;
      }

      // Log failure reason if enabled
      if (config.LogFailureReasons && !string.IsNullOrEmpty(fullFailureReason))
      {
        UnityEngine.Debug.LogWarning($"[AuthorControlledFallback] Using fallback. Reason: {fullFailureReason}, Trigger: {triggerReason}");
      }

      // Try context-aware fallback first
      if (context != null && config.ContextFallbacks.TryGetValue(triggerReason, out var contextFallbacks) && contextFallbacks.Count > 0)
      {
        return SelectRandomFallback(contextFallbacks);
      }

      // Fall back to generic fallbacks
      if (config.GenericFallbacks.Count > 0)
      {
        return SelectRandomFallback(config.GenericFallbacks);
      }

      // Last resort: emergency fallback
      stats.EmergencyFallbacks++;
      UnityEngine.Debug.LogWarning("[AuthorControlledFallback] All fallback lists empty, using emergency fallback");
      return SelectRandomFallback(config.EmergencyFallbacks);
    }

    /// <summary>
    /// Gets a fallback response for a specific trigger reason (simpler overload).
    /// </summary>
    /// <param name="triggerReason">The trigger reason</param>
    /// <param name="failureReason">Optional failure reason for logging</param>
    /// <returns>A fallback response string</returns>
    public string GetFallbackResponse(TriggerReason triggerReason, string? failureReason = null)
    {
      var context = new InteractionContext { TriggerReason = triggerReason };
      return GetFallbackResponse(context, failureReason, null);
    }

    /// <summary>
    /// Gets an emergency fallback (always works, used as absolute last resort).
    /// </summary>
    /// <returns>An emergency fallback response</returns>
    public string GetEmergencyFallback()
    {
      stats.EmergencyFallbacks++;
      if (config.EmergencyFallbacks.Count > 0)
      {
        return SelectRandomFallback(config.EmergencyFallbacks);
      }

      // Ultimate fallback if even emergency list is empty
      UnityEngine.Debug.LogError("[AuthorControlledFallback] Emergency fallback list is empty! Using hardcoded fallback.");
      return "Hello there!";
    }

    /// <summary>
    /// Builds a comprehensive failure reason string from available information.
    /// </summary>
    private string BuildFailureReasonString(string? failureReason, InferenceResultWithRetries? inferenceResult)
    {
      var reasons = new List<string>();

      if (!string.IsNullOrEmpty(failureReason))
      {
        reasons.Add(failureReason);
      }

      if (inferenceResult != null)
      {
        if (!inferenceResult.Success)
        {
          var finalResult = inferenceResult.FinalResult;
          
          if (!string.IsNullOrEmpty(finalResult.ErrorMessage))
          {
            reasons.Add($"API Error: {finalResult.ErrorMessage}");
          }
          else if (finalResult.Violations.Count > 0)
          {
            var violationTypes = finalResult.Violations.Select(v => v.Constraint.Type.ToString()).Distinct();
            reasons.Add($"Validation Failed: {string.Join(", ", violationTypes)}");
          }
          else
          {
            reasons.Add($"Validation Failed: {finalResult.Outcome}");
          }

          reasons.Add($"Attempts: {inferenceResult.AttemptCount}/{inferenceResult.FinalResult.Snapshot.MaxAttempts}");
        }
      }

      return string.Join(" | ", reasons);
    }

    /// <summary>
    /// Selects a random fallback from a list.
    /// </summary>
    private string SelectRandomFallback(List<string> fallbacks)
    {
      if (fallbacks.Count == 0)
      {
        return "Hello there!";
      }

      return fallbacks[random.Next(fallbacks.Count)];
    }

    /// <summary>
    /// Resets fallback statistics.
    /// </summary>
    public void ResetStats()
    {
      stats.TotalFallbacks = 0;
      stats.FallbacksByTrigger.Clear();
      stats.FallbacksByFailureReason.Clear();
      stats.EmergencyFallbacks = 0;
    }

    /// <summary>
    /// Gets a formatted string of current statistics.
    /// </summary>
    /// <returns>A formatted string containing fallback usage statistics</returns>
    public string GetStatsString()
    {
      var lines = new List<string>
      {
        $"Total Fallbacks: {stats.TotalFallbacks}",
        $"Emergency Fallbacks: {stats.EmergencyFallbacks}"
      };

      if (stats.FallbacksByTrigger.Count > 0)
      {
        lines.Add("\nBy Trigger Reason:");
        foreach (var kvp in stats.FallbacksByTrigger.OrderByDescending(x => x.Value))
        {
          lines.Add($"  {kvp.Key}: {kvp.Value}");
        }
      }

      if (stats.FallbacksByFailureReason.Count > 0)
      {
        lines.Add("\nBy Failure Reason:");
        foreach (var kvp in stats.FallbacksByFailureReason.OrderByDescending(x => x.Value).Take(10))
        {
          lines.Add($"  {kvp.Key}: {kvp.Value}");
        }
      }

      return string.Join("\n", lines);
    }
  }
}

