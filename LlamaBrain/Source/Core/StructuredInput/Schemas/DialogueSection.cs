using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlamaBrain.Core.StructuredInput.Schemas
{
    /// <summary>
    /// Dialogue section containing conversation history and current input.
    /// </summary>
    [Serializable]
    public sealed class DialogueSection
    {
        /// <summary>
        /// Previous dialogue exchanges.
        /// </summary>
        [JsonProperty("history", Order = 0)]
        public List<StructuredDialogueEntry> History { get; set; } = new List<StructuredDialogueEntry>();

        /// <summary>
        /// Current player input to respond to.
        /// </summary>
        [JsonProperty("playerInput", Order = 1, NullValueHandling = NullValueHandling.Ignore)]
        public string? PlayerInput { get; set; }
    }

    /// <summary>
    /// Single dialogue exchange entry for structured context.
    /// Named differently to avoid conflict with LlamaBrain.Core.DialogueEntry.
    /// </summary>
    [Serializable]
    public sealed class StructuredDialogueEntry
    {
        /// <summary>
        /// Speaker name (e.g., "Player" or NPC name).
        /// </summary>
        [JsonProperty("speaker", Order = 0)]
        public string Speaker { get; set; } = string.Empty;

        /// <summary>
        /// Dialogue text content.
        /// </summary>
        [JsonProperty("text", Order = 1)]
        public string Text { get; set; } = string.Empty;
    }
}
