using System;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Metadata about a save slot.
  /// </summary>
  [Serializable]
  public sealed class SaveSlotInfo
  {
    /// <summary>
    /// The name of the save slot.
    /// </summary>
    public string SlotName { get; set; } = "";

    /// <summary>
    /// When this save was created (UTC ticks for determinism).
    /// </summary>
    public long SavedAtUtcTicks { get; set; }

    /// <summary>
    /// The save data schema version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Optional display name for UI purposes.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Size of the save file in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Number of personas stored in this save.
    /// </summary>
    public int PersonaCount { get; set; }
  }
}
