using System;
using Newtonsoft.Json;

namespace LlamaBrain.Core.StructuredInput.Schemas
{
    /// <summary>
    /// Root DTO for structured context JSON.
    /// Contains all context information provided to the LLM in structured format.
    /// </summary>
    [Serializable]
    public sealed class ContextJsonSchema
    {
        /// <summary>
        /// Schema version for forward compatibility.
        /// </summary>
        [JsonProperty("schemaVersion", Order = 0)]
        public string SchemaVersion { get; set; } = "1.0";

        /// <summary>
        /// Context section containing memories (facts, state, episodic, beliefs).
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("context", Order = 1, NullValueHandling = NullValueHandling.Ignore)]
        public ContextSection? Context { get; set; }

        /// <summary>
        /// Constraints section containing prohibitions, requirements, and permissions.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("constraints", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
        public ConstraintSection? Constraints { get; set; }

        /// <summary>
        /// Dialogue section containing history and current player input.
        /// Optional for partial context support.
        /// </summary>
        [JsonProperty("dialogue", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public DialogueSection? Dialogue { get; set; }
    }
}
