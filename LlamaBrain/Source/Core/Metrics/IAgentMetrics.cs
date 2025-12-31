using System.Collections.Generic;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Core.Metrics
{
  /// <summary>
  /// Provides architectural metrics from an agent's inference state.
  /// This interface abstracts agent metrics to allow metrics collection
  /// from any host implementation (Unity, console, web, etc.).
  /// </summary>
  public interface IAgentMetrics
  {
    /// <summary>
    /// The last inference result with retry information.
    /// </summary>
    InferenceResultWithRetries? LastInferenceResult { get; }

    /// <summary>
    /// The last validation gate result.
    /// </summary>
    GateResult? LastGateResult { get; }

    /// <summary>
    /// The last constraint set evaluated by the expectancy engine.
    /// </summary>
    ConstraintSet? LastConstraints { get; }

    /// <summary>
    /// The last state snapshot used for inference.
    /// </summary>
    StateSnapshot? LastSnapshot { get; }

    /// <summary>
    /// Fallback statistics (if available).
    /// </summary>
    IFallbackStats? FallbackStats { get; }
  }
}

