using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlamaBrain.Core.StructuredInput.Schemas
{
    /// <summary>
    /// Constraints section containing prohibitions, requirements, and permissions.
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
    }
}
