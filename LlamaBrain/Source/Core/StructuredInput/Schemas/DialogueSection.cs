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

        /// <summary>
        /// Optional timestamp in game time units (e.g., seconds since session start).
        /// Null if timestamp tracking is disabled.
        /// </summary>
        [JsonProperty("timestamp", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
        public float? Timestamp { get; set; }

        /// <summary>
        /// Optional metadata for additional context about this dialogue entry.
        /// Examples: emotion tags, location, trigger source.
        /// </summary>
        [JsonProperty("metadata", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public DialogueMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Optional metadata attached to a dialogue entry.
    /// </summary>
    [Serializable]
    public sealed class DialogueMetadata
    {
        /// <summary>
        /// Emotional tone of the utterance (e.g., "angry", "friendly", "neutral").
        /// </summary>
        [JsonProperty("emotion", NullValueHandling = NullValueHandling.Ignore)]
        public string? Emotion { get; set; }

        /// <summary>
        /// Location where the dialogue occurred.
        /// </summary>
        [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
        public string? Location { get; set; }

        /// <summary>
        /// What triggered this dialogue (e.g., "zone_enter", "player_greeting", "quest_update").
        /// </summary>
        [JsonProperty("trigger", NullValueHandling = NullValueHandling.Ignore)]
        public string? Trigger { get; set; }

        /// <summary>
        /// Turn number in the conversation (0-indexed).
        /// </summary>
        [JsonProperty("turnNumber", NullValueHandling = NullValueHandling.Ignore)]
        public int? TurnNumber { get; set; }
    }
}
