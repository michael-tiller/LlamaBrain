using System;
using System.Collections.Generic;
using LlamaBrain.Persistence.Dtos;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Snapshot of a persona's conversation history.
  /// </summary>
  [Serializable]
  public sealed class ConversationHistorySnapshot
  {
    /// <summary>
    /// The persona ID this conversation history belongs to.
    /// </summary>
    public string PersonaId { get; set; } = "";

    /// <summary>
    /// The dialogue entries in chronological order.
    /// </summary>
    public List<DialogueEntryDto> Entries { get; set; }
        = new List<DialogueEntryDto>();
  }
}
