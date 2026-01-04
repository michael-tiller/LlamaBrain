using System;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Result of a save operation.
  /// </summary>
  [Serializable]
  public sealed class SaveResult
  {
    /// <summary>
    /// Whether the save operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the save failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The slot name that was saved to.
    /// </summary>
    public string? SlotName { get; set; }

    /// <summary>
    /// When the save was created (UTC ticks for determinism).
    /// </summary>
    public long SavedAtUtcTicks { get; set; }

    /// <summary>
    /// Creates a successful save result.
    /// </summary>
    /// <param name="slotName">The slot that was saved to</param>
    /// <param name="savedAtUtcTicks">When the save occurred</param>
    /// <returns>A SaveResult indicating success</returns>
    public static SaveResult Succeeded(string slotName, long savedAtUtcTicks) => new SaveResult
    {
      Success = true,
      SlotName = slotName,
      SavedAtUtcTicks = savedAtUtcTicks
    };

    /// <summary>
    /// Creates a failed save result.
    /// </summary>
    /// <param name="errorMessage">The error that occurred</param>
    /// <returns>A SaveResult indicating failure</returns>
    public static SaveResult Failed(string errorMessage) => new SaveResult
    {
      Success = false,
      ErrorMessage = errorMessage
    };
  }
}
