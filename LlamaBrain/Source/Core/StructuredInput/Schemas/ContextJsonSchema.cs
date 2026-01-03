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
        /// </summary>
        [JsonProperty("context", Order = 1)]
        public ContextSection Context { get; set; } = new ContextSection();

        /// <summary>
        /// Constraints section containing prohibitions, requirements, and permissions.
        /// </summary>
        [JsonProperty("constraints", Order = 2)]
        public ConstraintSection Constraints { get; set; } = new ConstraintSection();

        /// <summary>
        /// Dialogue section containing history and current player input.
        /// </summary>
        [JsonProperty("dialogue", Order = 3)]
        public DialogueSection Dialogue { get; set; } = new DialogueSection();
    }
}
