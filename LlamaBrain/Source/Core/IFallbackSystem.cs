using System.Collections.Generic;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Interface for fallback response systems that provide safe responses when inference fails.
  /// </summary>
  public interface IFallbackSystem
  {
    /// <summary>
    /// Gets a fallback response based on the interaction context and failure reason.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="failureReason">The reason the inference failed.</param>
    /// <param name="finalResult">Optional final inference result for additional context.</param>
    /// <param name="triggerFallbacks">Optional trigger-specific fallback list. If provided, these take priority over default fallbacks.</param>
    /// <returns>A fallback response string.</returns>
    string GetFallbackResponse(
      InteractionContext context,
      string failureReason,
      InferenceResultWithRetries? finalResult = null,
      IReadOnlyList<string>? triggerFallbacks = null);

    /// <summary>
    /// Gets the fallback statistics.
    /// </summary>
    IFallbackStats Stats { get; }
  }

  /// <summary>
  /// Interface for fallback statistics tracking.
  /// </summary>
  public interface IFallbackStats
  {
    /// <summary>
    /// Total number of times fallbacks have been used.
    /// </summary>
    int TotalFallbacks { get; }

    /// <summary>
    /// Number of fallbacks by trigger reason.
    /// </summary>
    IReadOnlyDictionary<TriggerReason, int> FallbacksByTriggerReason { get; }

    /// <summary>
    /// Number of fallbacks by failure reason (first 100 characters of failure reason).
    /// </summary>
    IReadOnlyDictionary<string, int> FallbacksByFailureReason { get; }

    /// <summary>
    /// Number of times emergency fallbacks were used.
    /// </summary>
    int EmergencyFallbacks { get; }
  }
}

