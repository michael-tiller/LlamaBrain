using System;

namespace LlamaBrain.Persistence.Dtos
{
  /// <summary>
  /// DTO for serializing conversation history entries.
  /// </summary>
  [Serializable]
  public sealed class DialogueEntryDto
  {
    /// <summary>
    /// Who spoke (e.g., "Player", "NPC").
    /// </summary>
    public string Speaker { get; set; } = "";

    /// <summary>
    /// What was said.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// When this was said (UTC ticks).
    /// </summary>
    public long TimestampTicks { get; set; }
  }
}
