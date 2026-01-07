using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlamaBrain.Core.StructuredInput.Schemas
{
    /// <summary>
    /// Entry for rich relationship data between entities.
    /// Supports detailed relationship modeling with affinity, trust, familiarity, and history.
    /// </summary>
    [Serializable]
    public sealed class RelationshipEntry
    {
        /// <summary>
        /// The entity that owns/holds this relationship view (e.g., NPC ID).
        /// Required field.
        /// </summary>
        [JsonProperty("sourceEntity", Order = 0)]
        public string SourceEntity { get; set; } = string.Empty;

        /// <summary>
        /// The related entity (e.g., player ID, other NPC ID).
        /// Required field.
        /// </summary>
        [JsonProperty("targetEntity", Order = 1)]
        public string TargetEntity { get; set; } = string.Empty;

        /// <summary>
        /// The type of relationship (e.g., "friend", "rival", "mentor", "family", "acquaintance").
        /// </summary>
        [JsonProperty("relationshipType", Order = 2)]
        public string RelationshipType { get; set; } = string.Empty;

        /// <summary>
        /// Affinity score from -1 (hostile) to 1 (friendly).
        /// Represents how positively the source entity feels about the target.
        /// </summary>
        [JsonProperty("affinity", Order = 3)]
        public float Affinity { get; set; }

        /// <summary>
        /// Trust level from 0 (no trust) to 1 (complete trust).
        /// Represents how much the source entity trusts the target.
        /// </summary>
        [JsonProperty("trust", Order = 4)]
        public float Trust { get; set; }

        /// <summary>
        /// Familiarity level from 0 (stranger) to 1 (intimate).
        /// Represents how well the source entity knows the target.
        /// </summary>
        [JsonProperty("familiarity", Order = 5)]
        public float Familiarity { get; set; }

        /// <summary>
        /// Key interaction summaries that shaped this relationship.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("history", Order = 6, NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? History { get; set; }

        /// <summary>
        /// Searchable tags for this relationship (e.g., "ally", "betrayed", "romantic").
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("tags", Order = 7, NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// UTC timestamp (ticks) of the last interaction.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("lastInteractionTimestamp", Order = 8, NullValueHandling = NullValueHandling.Ignore)]
        public long? LastInteractionTimestamp { get; set; }

        /// <summary>
        /// UTC timestamp (ticks) when this relationship was created.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("createdTimestamp", Order = 9, NullValueHandling = NullValueHandling.Ignore)]
        public long? CreatedTimestamp { get; set; }

        /// <summary>
        /// Creates a new relationship entry with required fields.
        /// </summary>
        /// <param name="sourceEntity">The entity that owns this relationship view.</param>
        /// <param name="targetEntity">The related entity.</param>
        /// <param name="relationshipType">The type of relationship.</param>
        /// <returns>A new RelationshipEntry with the specified values.</returns>
        public static RelationshipEntry Create(
            string sourceEntity,
            string targetEntity,
            string relationshipType = "acquaintance")
        {
            return new RelationshipEntry
            {
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                RelationshipType = relationshipType,
                Affinity = 0f,
                Trust = 0.5f,
                Familiarity = 0f
            };
        }

        /// <summary>
        /// Creates a friendly relationship entry.
        /// </summary>
        /// <param name="sourceEntity">The entity that owns this relationship view.</param>
        /// <param name="targetEntity">The related entity.</param>
        /// <param name="affinity">Initial affinity (0-1 for friendly).</param>
        /// <returns>A new friendly RelationshipEntry.</returns>
        public static RelationshipEntry CreateFriendly(
            string sourceEntity,
            string targetEntity,
            float affinity = 0.5f)
        {
            return new RelationshipEntry
            {
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                RelationshipType = "friend",
                Affinity = affinity,
                Trust = 0.6f,
                Familiarity = 0.4f
            };
        }

        /// <summary>
        /// Creates a hostile relationship entry.
        /// </summary>
        /// <param name="sourceEntity">The entity that owns this relationship view.</param>
        /// <param name="targetEntity">The related entity.</param>
        /// <param name="affinity">Initial affinity (-1 to 0 for hostile).</param>
        /// <returns>A new hostile RelationshipEntry.</returns>
        public static RelationshipEntry CreateHostile(
            string sourceEntity,
            string targetEntity,
            float affinity = -0.5f)
        {
            return new RelationshipEntry
            {
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                RelationshipType = "rival",
                Affinity = affinity,
                Trust = 0.1f,
                Familiarity = 0.2f
            };
        }

        /// <summary>
        /// Adds a history entry to this relationship.
        /// </summary>
        /// <param name="entry">The history entry to add.</param>
        /// <returns>This RelationshipEntry for chaining.</returns>
        public RelationshipEntry WithHistory(string entry)
        {
            History ??= new List<string>();
            History.Add(entry);
            return this;
        }

        /// <summary>
        /// Adds a tag to this relationship.
        /// </summary>
        /// <param name="tag">The tag to add.</param>
        /// <returns>This RelationshipEntry for chaining.</returns>
        public RelationshipEntry WithTag(string tag)
        {
            Tags ??= new List<string>();
            if (!Tags.Contains(tag))
            {
                Tags.Add(tag);
            }
            return this;
        }

        /// <summary>
        /// Returns a string representation of this relationship.
        /// </summary>
        /// <returns>A string describing the relationship.</returns>
        public override string ToString()
        {
            return $"Relationship[{SourceEntity} -> {TargetEntity}] Type={RelationshipType}, Affinity={Affinity:F2}, Trust={Trust:F2}, Familiarity={Familiarity:F2}";
        }
    }
}
