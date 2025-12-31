using System;

namespace LlamaBrain.Core.Metrics
{
  /// <summary>
  /// Provides information about a trigger that initiated a dialogue interaction.
  /// This interface abstracts trigger information to allow metrics collection
  /// from any host implementation (Unity, console, web, etc.).
  /// </summary>
  public interface ITriggerInfo
  {
    /// <summary>
    /// The unique identifier of the trigger.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The name of the trigger.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The prompt text used for this interaction.
    /// </summary>
    string PromptText { get; }

    /// <summary>
    /// The number of times the trigger was prompted before this interaction.
    /// </summary>
    int PromptCount { get; }
  }
}

