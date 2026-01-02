using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.StructuredInput.Schemas;

namespace LlamaBrain.Core.StructuredInput
{
    /// <summary>
    /// Structured context provider for llama.cpp backend.
    /// Supports JsonContext format (no native function calling).
    /// Singleton pattern for consistency with LlamaCppStructuredOutputProvider.
    /// </summary>
    public sealed class LlamaCppStructuredContextProvider : IStructuredContextProvider
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static LlamaCppStructuredContextProvider Instance { get; } = new LlamaCppStructuredContextProvider();

        private LlamaCppStructuredContextProvider()
        {
        }

        /// <inheritdoc/>
        public bool SupportsFormat(StructuredContextFormat format)
        {
            return format switch
            {
                StructuredContextFormat.None => true,
                StructuredContextFormat.JsonContext => true,
                StructuredContextFormat.FunctionCalling => false, // llama.cpp doesn't support native function calling
                _ => false
            };
        }

        /// <inheritdoc/>
        public ContextJsonSchema BuildContext(StateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var schema = new ContextJsonSchema
            {
                SchemaVersion = "1.0",
                Context = BuildContextSection(snapshot),
                Constraints = BuildConstraintSection(snapshot.Constraints),
                Dialogue = BuildDialogueSection(snapshot)
            };

            return schema;
        }

        /// <inheritdoc/>
        public bool ValidateContext(ContextJsonSchema context, out string? error)
        {
            if (context == null)
            {
                error = "Context cannot be null";
                return false;
            }

            if (string.IsNullOrEmpty(context.SchemaVersion))
            {
                error = "Schema version is required";
                return false;
            }

            if (context.Context == null)
            {
                error = "Context section cannot be null";
                return false;
            }

            if (context.Dialogue == null)
            {
                error = "Dialogue section cannot be null";
                return false;
            }

            error = null;
            return true;
        }

        private static ContextSection BuildContextSection(StateSnapshot snapshot)
        {
            return new ContextSection
            {
                CanonicalFacts = snapshot.CanonicalFacts?.ToList() ?? new List<string>(),
                WorldState = ParseWorldState(snapshot.WorldState),
                EpisodicMemories = ParseEpisodicMemories(snapshot.EpisodicMemories),
                Beliefs = ParseBeliefs(snapshot.Beliefs)
            };
        }

        private static List<WorldStateEntry> ParseWorldState(IReadOnlyList<string>? worldState)
        {
            if (worldState == null || worldState.Count == 0)
            {
                return new List<WorldStateEntry>();
            }

            var entries = new List<WorldStateEntry>();
            foreach (var state in worldState)
            {
                // Parse "key=value" format
                var parts = state.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    entries.Add(new WorldStateEntry
                    {
                        Key = parts[0].Trim(),
                        Value = parts[1].Trim()
                    });
                }
                else
                {
                    // Single value without key
                    entries.Add(new WorldStateEntry
                    {
                        Key = state,
                        Value = string.Empty
                    });
                }
            }
            return entries;
        }

        private static List<EpisodicMemoryEntry> ParseEpisodicMemories(IReadOnlyList<string>? memories)
        {
            if (memories == null || memories.Count == 0)
            {
                return new List<EpisodicMemoryEntry>();
            }

            var entries = new List<EpisodicMemoryEntry>();
            int count = memories.Count;
            for (int i = 0; i < count; i++)
            {
                // Calculate recency: newer memories (higher index) have higher recency
                float recency = count > 1 ? (float)i / (count - 1) : 1.0f;

                entries.Add(new EpisodicMemoryEntry
                {
                    Content = memories[i],
                    Recency = recency,
                    Importance = 0.5f // Default importance
                });
            }
            return entries;
        }

        private static List<BeliefEntry> ParseBeliefs(IReadOnlyList<string>? beliefs)
        {
            if (beliefs == null || beliefs.Count == 0)
            {
                return new List<BeliefEntry>();
            }

            var entries = new List<BeliefEntry>();
            for (int i = 0; i < beliefs.Count; i++)
            {
                entries.Add(new BeliefEntry
                {
                    Id = $"belief_{i}",
                    Content = beliefs[i],
                    Confidence = 0.7f, // Default confidence
                    Sentiment = 0.0f   // Neutral sentiment
                });
            }
            return entries;
        }

        private static ConstraintSection BuildConstraintSection(ConstraintSet? constraints)
        {
            var section = new ConstraintSection();

            if (constraints == null)
            {
                return section;
            }

            foreach (var constraint in constraints.All)
            {
                if (string.IsNullOrEmpty(constraint.PromptInjection))
                {
                    continue;
                }

                switch (constraint.Type)
                {
                    case ConstraintType.Prohibition:
                        section.Prohibitions.Add(constraint.PromptInjection);
                        break;
                    case ConstraintType.Requirement:
                        section.Requirements.Add(constraint.PromptInjection);
                        break;
                    case ConstraintType.Permission:
                        section.Permissions.Add(constraint.PromptInjection);
                        break;
                }
            }

            return section;
        }

        private static DialogueSection BuildDialogueSection(StateSnapshot snapshot)
        {
            var section = new DialogueSection
            {
                PlayerInput = snapshot.PlayerInput,
                History = new List<StructuredDialogueEntry>()
            };

            if (snapshot.DialogueHistory == null || snapshot.DialogueHistory.Count == 0)
            {
                return section;
            }

            foreach (var line in snapshot.DialogueHistory)
            {
                // Parse "Speaker: Text" format
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    section.History.Add(new StructuredDialogueEntry
                    {
                        Speaker = line.Substring(0, colonIndex).Trim(),
                        Text = line.Substring(colonIndex + 1).Trim()
                    });
                }
                else
                {
                    // No speaker identified
                    section.History.Add(new StructuredDialogueEntry
                    {
                        Speaker = "Unknown",
                        Text = line
                    });
                }
            }

            return section;
        }
    }
}
