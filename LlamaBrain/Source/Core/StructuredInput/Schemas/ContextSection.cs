using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlamaBrain.Core.StructuredInput.Schemas
{
    /// <summary>
    /// Context section containing all memory types.
    /// All lists are optional for partial context support.
    /// </summary>
    [Serializable]
    public sealed class ContextSection
    {
        /// <summary>
        /// Canonical facts - immutable world truths.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("canonicalFacts", Order = 0, NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? CanonicalFacts { get; set; }

        /// <summary>
        /// World state entries - mutable game state.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("worldState", Order = 1, NullValueHandling = NullValueHandling.Ignore)]
        public List<WorldStateEntry>? WorldState { get; set; }

        /// <summary>
        /// Episodic memories - conversation history with decay.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("episodicMemories", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
        public List<EpisodicMemoryEntry>? EpisodicMemories { get; set; }

        /// <summary>
        /// Beliefs - NPC opinions and relationships.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("beliefs", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public List<BeliefEntry>? Beliefs { get; set; }

        /// <summary>
        /// Relationships - rich relationship data between entities.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("relationships", Order = 4, NullValueHandling = NullValueHandling.Ignore)]
        public List<RelationshipEntry>? Relationships { get; set; }
    }

    /// <summary>
    /// Entry for world state key-value pairs.
    /// </summary>
    [Serializable]
    public sealed class WorldStateEntry
    {
        /// <summary>
        /// State key (e.g., "door_status").
        /// </summary>
        [JsonProperty("key", Order = 0)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// State value (e.g., "open").
        /// </summary>
        [JsonProperty("value", Order = 1)]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Optional timestamp (UTC ticks) for deterministic ordering.
        /// </summary>
        [JsonProperty("timestamp", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
        public long? Timestamp { get; set; }
    }

    /// <summary>
    /// Entry for episodic memories.
    /// </summary>
    [Serializable]
    public sealed class EpisodicMemoryEntry
    {
        /// <summary>
        /// Memory content text.
        /// </summary>
        [JsonProperty("content", Order = 0)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Recency score (0-1, higher = more recent).
        /// </summary>
        [JsonProperty("recency", Order = 1)]
        public float Recency { get; set; }

        /// <summary>
        /// Importance score (0-1, higher = more important).
        /// </summary>
        [JsonProperty("importance", Order = 2)]
        public float Importance { get; set; }
    }

    /// <summary>
    /// Entry for belief memories.
    /// </summary>
    [Serializable]
    public sealed class BeliefEntry
    {
        /// <summary>
        /// Belief identifier.
        /// </summary>
        [JsonProperty("id", Order = 0)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Belief content text.
        /// </summary>
        [JsonProperty("content", Order = 1)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Confidence level (0-1).
        /// </summary>
        [JsonProperty("confidence", Order = 2)]
        public float Confidence { get; set; }

        /// <summary>
        /// Sentiment value (-1 to 1, negative to positive).
        /// </summary>
        [JsonProperty("sentiment", Order = 3)]
        public float Sentiment { get; set; }
    }
}
