using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;

namespace LlamaBrain.Core.FunctionCalling
{
    /// <summary>
    /// Built-in context functions that can be registered with FunctionCallDispatcher.
    /// These provide access to memories, constraints, dialogue history, etc.
    /// </summary>
    public static class BuiltInContextFunctions
    {
        /// <summary>
        /// Registers all built-in context functions with the dispatcher.
        /// </summary>
        /// <param name="dispatcher">The function call dispatcher to register functions with</param>
        /// <param name="snapshot">The current state snapshot (for context access)</param>
        /// <param name="memorySystem">Optional memory system for memory queries</param>
        public static void RegisterAll(
            FunctionCallDispatcher dispatcher,
            StateSnapshot snapshot,
            AuthoritativeMemorySystem? memorySystem = null)
        {
            // Register get_memories function
            dispatcher.RegisterFunction(
                "get_memories",
                (call) => GetMemories(call, snapshot, memorySystem),
                "Get episodic memories for the NPC. Arguments: limit (optional, default 10), minSignificance (optional, default 0.0)",
                @"{
  ""type"": ""object"",
  ""properties"": {
    ""limit"": { ""type"": ""integer"", ""description"": ""Maximum number of memories to return"" },
    ""minSignificance"": { ""type"": ""number"", ""description"": ""Minimum significance threshold (0-1)"" }
  }
}");

            // Register get_beliefs function
            dispatcher.RegisterFunction(
                "get_beliefs",
                (call) => GetBeliefs(call, snapshot, memorySystem),
                "Get beliefs/opinions for the NPC. Arguments: limit (optional, default 10), minConfidence (optional, default 0.0)",
                @"{
  ""type"": ""object"",
  ""properties"": {
    ""limit"": { ""type"": ""integer"", ""description"": ""Maximum number of beliefs to return"" },
    ""minConfidence"": { ""type"": ""number"", ""description"": ""Minimum confidence threshold (0-1)"" }
  }
}");

            // Register get_constraints function
            dispatcher.RegisterFunction(
                "get_constraints",
                (call) => GetConstraints(call, snapshot),
                "Get current constraints (prohibitions, requirements, permissions) for the NPC",
                @"{
  ""type"": ""object"",
  ""properties"": {}
}");

            // Register get_dialogue_history function
            dispatcher.RegisterFunction(
                "get_dialogue_history",
                (call) => GetDialogueHistory(call, snapshot),
                "Get recent dialogue history. Arguments: limit (optional, default 10)",
                @"{
  ""type"": ""object"",
  ""properties"": {
    ""limit"": { ""type"": ""integer"", ""description"": ""Maximum number of dialogue exchanges to return"" }
  }
}");

            // Register get_world_state function
            dispatcher.RegisterFunction(
                "get_world_state",
                (call) => GetWorldState(call, snapshot),
                "Get current world state entries. Arguments: keys (optional, array of specific keys to retrieve)",
                @"{
  ""type"": ""object"",
  ""properties"": {
    ""keys"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""Specific world state keys to retrieve"" }
  }
}");

            // Register get_canonical_facts function
            dispatcher.RegisterFunction(
                "get_canonical_facts",
                (call) => GetCanonicalFacts(call, snapshot),
                "Get canonical facts (authoritative, immutable facts) for the NPC",
                @"{
  ""type"": ""object"",
  ""properties"": {}
}");
        }

        private static FunctionCallResult GetMemories(FunctionCall call, StateSnapshot snapshot, AuthoritativeMemorySystem? memorySystem)
        {
            var limit = call.GetArgumentInt("limit", 10);
            var minSignificance = (float)call.GetArgumentDouble("minSignificance", 0.0);

            var memories = snapshot.EpisodicMemories ?? new List<string>();
            var result = memories
                .Take(limit)
                .Select((m, i) => new
                {
                    content = m,
                    index = i,
                    recency = memories.Count > 1 ? (float)i / (memories.Count - 1) : 1.0f
                })
                .Where(m => m.recency >= minSignificance)
                .Select(m => new Dictionary<string, object>
                {
                    { "content", m.content },
                    { "recency", m.recency },
                    { "importance", 0.5f } // Default importance
                })
                .ToList();

            return FunctionCallResult.SuccessResult(result, call.CallId);
        }

        private static FunctionCallResult GetBeliefs(FunctionCall call, StateSnapshot snapshot, AuthoritativeMemorySystem? memorySystem)
        {
            var limit = call.GetArgumentInt("limit", 10);
            var minConfidence = (float)call.GetArgumentDouble("minConfidence", 0.0);

            var beliefs = snapshot.Beliefs ?? new List<string>();
            var result = beliefs
                .Take(limit)
                .Select((b, i) => new Dictionary<string, object>
                {
                    { "id", $"belief_{i}" },
                    { "content", b },
                    { "confidence", 0.7f }, // Default confidence
                    { "sentiment", 0.0f }   // Neutral sentiment
                })
                .ToList();

            return FunctionCallResult.SuccessResult(result, call.CallId);
        }

        private static FunctionCallResult GetConstraints(FunctionCall call, StateSnapshot snapshot)
        {
            var constraints = snapshot.Constraints ?? new ConstraintSet();
            var result = new Dictionary<string, object>
            {
                { "prohibitions", constraints.All
                    .Where(c => c.Type == ConstraintType.Prohibition)
                    .Select(c => c.PromptInjection ?? "")
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList() },
                { "requirements", constraints.All
                    .Where(c => c.Type == ConstraintType.Requirement)
                    .Select(c => c.PromptInjection ?? "")
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList() },
                { "permissions", constraints.All
                    .Where(c => c.Type == ConstraintType.Permission)
                    .Select(c => c.PromptInjection ?? "")
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList() }
            };

            return FunctionCallResult.SuccessResult(result, call.CallId);
        }

        private static FunctionCallResult GetDialogueHistory(FunctionCall call, StateSnapshot snapshot)
        {
            var limit = call.GetArgumentInt("limit", 10);
            var history = snapshot.DialogueHistory ?? new List<string>();
            var result = history
                .TakeLast(limit)
                .Select(line =>
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        return new Dictionary<string, object>
                        {
                            { "speaker", line.Substring(0, colonIndex).Trim() },
                            { "text", line.Substring(colonIndex + 1).Trim() }
                        };
                    }
                    return new Dictionary<string, object>
                    {
                        { "speaker", "Unknown" },
                        { "text", line }
                    };
                })
                .ToList();

            return FunctionCallResult.SuccessResult(result, call.CallId);
        }

        private static FunctionCallResult GetWorldState(FunctionCall call, StateSnapshot snapshot)
        {
            var worldState = snapshot.WorldState ?? new List<string>();
            var requestedKeys = new List<string>();

            // Check if specific keys were requested
            if (call.Arguments.TryGetValue("keys", out var keysObj) && keysObj is List<object> keys)
            {
                requestedKeys = keys.Select(k => k?.ToString() ?? "").Where(k => !string.IsNullOrEmpty(k)).ToList();
            }

            var result = worldState
                .Select(state =>
                {
                    var parts = state.Split(new[] { '=' }, 2);
                    return new Dictionary<string, object>
                    {
                        { "key", parts.Length > 0 ? parts[0].Trim() : state },
                        { "value", parts.Length > 1 ? parts[1].Trim() : "" }
                    };
                })
                .Where(entry =>
                {
                    if (requestedKeys.Count == 0)
                        return true;
                    var key = entry["key"]?.ToString() ?? "";
                    return requestedKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
                })
                .ToList();

            return FunctionCallResult.SuccessResult(result, call.CallId);
        }

        private static FunctionCallResult GetCanonicalFacts(FunctionCall call, StateSnapshot snapshot)
        {
            var facts = snapshot.CanonicalFacts ?? new List<string>();
            var result = facts
                .Select(f => new Dictionary<string, object>
                {
                    { "fact", f },
                    { "authority", "canonical" }
                })
                .ToList();

            return FunctionCallResult.SuccessResult(result, call.CallId);
        }
    }

}
