using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Utility class for converting fallback strings to few-shot examples.
  /// </summary>
  public static class FallbackToFewShotConverter
  {
    /// <summary>
    /// Default player inputs for different trigger reasons.
    /// Used when converting fallback responses to few-shot examples.
    /// </summary>
    private static readonly Dictionary<TriggerReason, string[]> DefaultPlayerInputs = new Dictionary<TriggerReason, string[]>
    {
      { TriggerReason.PlayerUtterance, new[] { "Hello", "Hi there", "Greetings", "Hey" } },
      { TriggerReason.ZoneTrigger, new[] { "Hello", "Who are you?", "What is this place?" } },
      { TriggerReason.TimeTrigger, new[] { "What's happening?", "How are things?" } },
      { TriggerReason.QuestTrigger, new[] { "Do you have any work for me?", "I'm here to help" } },
      { TriggerReason.NpcInteraction, new[] { "Hello friend", "Greetings", "Good day" } },
      { TriggerReason.WorldEvent, new[] { "What was that?", "Did you see that?" } },
      { TriggerReason.Custom, new[] { "Hello", "What can you tell me?" } }
    };

    /// <summary>
    /// Converts a list of fallback responses to few-shot examples.
    /// </summary>
    /// <param name="fallbacks">The fallback responses to convert</param>
    /// <param name="triggerReason">Optional trigger reason to select appropriate player inputs</param>
    /// <param name="maxExamples">Maximum number of examples to generate (default: 3)</param>
    /// <param name="random">Optional random number generator for input selection</param>
    /// <returns>A list of FewShotExample instances</returns>
    public static List<FewShotExample> ConvertFallbacksToFewShot(
      IEnumerable<string> fallbacks,
      TriggerReason triggerReason = TriggerReason.PlayerUtterance,
      int maxExamples = 3,
      Random? random = null)
    {
      var rng = random ?? new Random();
      var fallbackList = fallbacks?.ToList() ?? new List<string>();
      var playerInputs = GetPlayerInputsForTrigger(triggerReason);

      var examples = new List<FewShotExample>();
      var count = Math.Min(fallbackList.Count, maxExamples);

      for (int i = 0; i < count; i++)
      {
        var playerInput = playerInputs[i % playerInputs.Length];
        examples.Add(new FewShotExample(playerInput, fallbackList[i]));
      }

      return examples;
    }

    /// <summary>
    /// Converts a FallbackConfig to a list of few-shot examples.
    /// </summary>
    /// <param name="config">The fallback configuration</param>
    /// <param name="triggerReason">The trigger reason to select fallbacks for</param>
    /// <param name="maxExamples">Maximum number of examples to generate</param>
    /// <param name="random">Optional random number generator</param>
    /// <returns>A list of FewShotExample instances</returns>
    public static List<FewShotExample> ConvertConfigToFewShot(
      FallbackSystem.FallbackConfig config,
      TriggerReason triggerReason = TriggerReason.PlayerUtterance,
      int maxExamples = 3,
      Random? random = null)
    {
      if (config == null) return new List<FewShotExample>();

      var fallbacks = triggerReason switch
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

      // If specific fallbacks are empty, fall back to generic
      if (fallbacks == null || fallbacks.Count == 0)
      {
        fallbacks = config.GenericFallbacks;
      }

      return ConvertFallbacksToFewShot(fallbacks, triggerReason, maxExamples, random);
    }

    /// <summary>
    /// Gets the default player inputs for a trigger reason.
    /// </summary>
    /// <param name="triggerReason">The trigger reason</param>
    /// <returns>Array of player input strings</returns>
    private static string[] GetPlayerInputsForTrigger(TriggerReason triggerReason)
    {
      if (DefaultPlayerInputs.TryGetValue(triggerReason, out var inputs))
      {
        return inputs;
      }
      return DefaultPlayerInputs[TriggerReason.PlayerUtterance];
    }
  }

  /// <summary>
  /// Provides author-controlled fallback responses when inference fails.
  /// Supports context-aware fallbacks based on trigger reason and tracks usage statistics.
  /// Engine-agnostic implementation (no Unity dependencies).
  /// </summary>
  public class FallbackSystem : IFallbackSystem
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
      public IReadOnlyDictionary<TriggerReason, int> FallbacksByTriggerReason { get; internal set; }

      /// <summary>
      /// Number of fallbacks by failure reason (first 100 characters of failure reason).
      /// </summary>
      public IReadOnlyDictionary<string, int> FallbacksByFailureReason { get; internal set; }

      /// <summary>
      /// Number of times emergency fallbacks were used.
      /// </summary>
      public int EmergencyFallbacks { get; internal set; }

      internal Dictionary<TriggerReason, int> FallbacksByTriggerReasonInternal { get; }
      internal Dictionary<string, int> FallbacksByFailureReasonInternal { get; }

      internal FallbackStats()
      {
        var triggerDict = new Dictionary<TriggerReason, int>();
        var failureDict = new Dictionary<string, int>();
        FallbacksByTriggerReason = triggerDict;
        FallbacksByFailureReason = failureDict;
        FallbacksByTriggerReasonInternal = triggerDict;
        FallbacksByFailureReasonInternal = failureDict;
      }

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
            lines.Add($"  {kvp.Key.Substring(0, Math.Min(50, kvp.Key.Length))}...: {kvp.Value}");
          }
        }

        return string.Join("\n", lines);
      }
    }

    private readonly FallbackConfig config;
    private readonly FallbackStats stats;
    private readonly Random random;

    /// <summary>
    /// Gets the fallback statistics.
    /// </summary>
    public IFallbackStats Stats => stats;

    /// <summary>
    /// Creates a new FallbackSystem with default configuration.
    /// </summary>
    public FallbackSystem() : this(new FallbackConfig())
    {
    }

    /// <summary>
    /// Creates a new FallbackSystem with custom configuration.
    /// </summary>
    /// <param name="config">The fallback configuration to use.</param>
    /// <param name="random">Optional random number generator for testing. If null, uses a new Random instance.</param>
    public FallbackSystem(FallbackConfig config, Random? random = null)
    {
      this.config = config ?? new FallbackConfig();
      this.stats = new FallbackStats();
      this.random = random ?? new Random();
    }

    /// <summary>
    /// Gets a fallback response based on the interaction context and failure reason.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="failureReason">The reason the inference failed.</param>
    /// <param name="finalResult">Optional final inference result for additional context.</param>
    /// <param name="triggerFallbacks">Optional trigger-specific fallback list. If provided, these take priority over default fallbacks.</param>
    /// <returns>A fallback response string.</returns>
    public string GetFallbackResponse(
      InteractionContext? context,
      string? failureReason,
      InferenceResultWithRetries? finalResult = null,
      IReadOnlyList<string>? triggerFallbacks = null)
    {
      if (context == null)
      {
        context = new InteractionContext
        {
          TriggerReason = TriggerReason.PlayerUtterance,
          GameTime = 0f
        };
      }

      // Update statistics
      stats.TotalFallbacks++;

      if (!stats.FallbacksByTriggerReasonInternal.ContainsKey(context.TriggerReason))
      {
        stats.FallbacksByTriggerReasonInternal[context.TriggerReason] = 0;
      }
      stats.FallbacksByTriggerReasonInternal[context.TriggerReason]++;

      // Track failure reason (truncate to first 100 chars to avoid memory bloat)
      var failureReasonKey = failureReason?.Length > 100 ? failureReason.Substring(0, 100) : failureReason ?? "Unknown";
      if (!stats.FallbacksByFailureReasonInternal.ContainsKey(failureReasonKey))
      {
        stats.FallbacksByFailureReasonInternal[failureReasonKey] = 0;
      }
      stats.FallbacksByFailureReasonInternal[failureReasonKey]++;

      // Priority 1: Use trigger-specific fallbacks if provided
      List<string>? fallbackList = null;
      if (triggerFallbacks != null && triggerFallbacks.Count > 0)
      {
        fallbackList = new List<string>(triggerFallbacks);
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

        // Last resort: hardcoded fallback
        if (fallbackList.Count == 0)
        {
          return "I'm having trouble responding right now.";
        }
      }

      // Select random fallback from the list
      var selectedFallback = fallbackList[random.Next(fallbackList.Count)];

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

