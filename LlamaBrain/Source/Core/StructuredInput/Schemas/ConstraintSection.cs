using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlamaBrain.Core.StructuredInput.Schemas
{
    /// <summary>
    /// Constraints section containing prohibitions, requirements, permissions,
    /// validation requirements, and authority information.
    /// </summary>
    [Serializable]
    public sealed class ConstraintSection
    {
        /// <summary>
        /// Actions the NPC must NOT do.
        /// </summary>
        [JsonProperty("prohibitions", Order = 0)]
        public List<string> Prohibitions { get; set; } = new List<string>();

        /// <summary>
        /// Actions the NPC MUST do.
        /// </summary>
        [JsonProperty("requirements", Order = 1)]
        public List<string> Requirements { get; set; } = new List<string>();

        /// <summary>
        /// Actions the NPC MAY do.
        /// </summary>
        [JsonProperty("permissions", Order = 2)]
        public List<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// Validation requirements for response format and content.
        /// </summary>
        [JsonProperty("validation", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public ValidationRequirements? Validation { get; set; }

        /// <summary>
        /// Authority information about who set these constraints.
        /// </summary>
        [JsonProperty("authority", Order = 4, NullValueHandling = NullValueHandling.Ignore)]
        public ConstraintAuthority? Authority { get; set; }
    }

    /// <summary>
    /// Validation requirements for response content.
    /// Used to enforce format and content rules beyond prohibitions/requirements.
    /// </summary>
    [Serializable]
    public sealed class ValidationRequirements
    {
        /// <summary>
        /// Minimum response length in characters. Null means no minimum.
        /// </summary>
        [JsonProperty("minLength", NullValueHandling = NullValueHandling.Ignore)]
        public int? MinLength { get; set; }

        /// <summary>
        /// Maximum response length in characters. Null means no maximum.
        /// </summary>
        [JsonProperty("maxLength", NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxLength { get; set; }

        /// <summary>
        /// Keywords that MUST appear in the response (case-insensitive).
        /// </summary>
        [JsonProperty("requiredKeywords", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? RequiredKeywords { get; set; }

        /// <summary>
        /// Keywords that must NOT appear in the response (case-insensitive).
        /// </summary>
        [JsonProperty("forbiddenKeywords", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? ForbiddenKeywords { get; set; }

        /// <summary>
        /// Required response format (e.g., "single_sentence", "paragraph", "list").
        /// </summary>
        [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)]
        public string? Format { get; set; }

        /// <summary>
        /// Whether the response must be a question.
        /// </summary>
        [JsonProperty("mustBeQuestion", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MustBeQuestion { get; set; }

        /// <summary>
        /// Whether the response must NOT be a question.
        /// </summary>
        [JsonProperty("mustNotBeQuestion", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MustNotBeQuestion { get; set; }
    }

    /// <summary>
    /// Authority source for constraints (who set them and when).
    /// </summary>
    [Serializable]
    public sealed class ConstraintAuthority
    {
        /// <summary>
        /// Source of the constraints.
        /// </summary>
        [JsonProperty("source")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ConstraintSource Source { get; set; } = ConstraintSource.System;

        /// <summary>
        /// Identifier for who set the constraints (e.g., NPC ID, quest ID).
        /// </summary>
        [JsonProperty("sourceId", NullValueHandling = NullValueHandling.Ignore)]
        public string? SourceId { get; set; }

        /// <summary>
        /// Priority level (higher = more authoritative). Default is 0.
        /// </summary>
        [JsonProperty("priority")]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Whether these constraints can be overridden by lower-priority sources.
        /// </summary>
        [JsonProperty("isOverridable")]
        public bool IsOverridable { get; set; } = true;

        /// <summary>
        /// Game time when these constraints were set.
        /// </summary>
        [JsonProperty("setAt", NullValueHandling = NullValueHandling.Ignore)]
        public float? SetAt { get; set; }

        /// <summary>
        /// Optional expiration time (game time). Null means no expiration.
        /// </summary>
        [JsonProperty("expiresAt", NullValueHandling = NullValueHandling.Ignore)]
        public float? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Source of constraint authority.
    /// </summary>
    public enum ConstraintSource
    {
        /// <summary>
        /// System-level constraints (highest authority, e.g., content safety).
        /// </summary>
        System = 0,

        /// <summary>
        /// Designer-authored constraints (e.g., persona definition).
        /// </summary>
        Designer = 1,

        /// <summary>
        /// NPC-originated constraints (e.g., from NPC dialogue or actions).
        /// </summary>
        Npc = 2,

        /// <summary>
        /// Player-originated constraints (e.g., from player requests).
        /// </summary>
        Player = 3,

        /// <summary>
        /// Quest or game-event constraints (e.g., during a specific mission).
        /// </summary>
        Quest = 4,

        /// <summary>
        /// Environmental constraints (e.g., location-based rules).
        /// </summary>
        Environment = 5
    }
}
