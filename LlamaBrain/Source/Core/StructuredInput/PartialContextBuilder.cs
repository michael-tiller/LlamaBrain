using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.StructuredInput.Schemas;

namespace LlamaBrain.Core.StructuredInput
{
    /// <summary>
    /// Builder for creating partial context schemas with only the desired sections populated.
    /// Supports fluent API for constructing context incrementally.
    /// </summary>
    public sealed class PartialContextBuilder
    {
        private ContextSection? _context;
        private ConstraintSection? _constraints;
        private DialogueSection? _dialogue;
        private string _schemaVersion = "1.0";

        /// <summary>
        /// Sets the schema version.
        /// </summary>
        /// <param name="version">The schema version string.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithSchemaVersion(string version)
        {
            _schemaVersion = version;
            return this;
        }

        /// <summary>
        /// Adds canonical facts to the context.
        /// </summary>
        /// <param name="facts">The canonical facts to add.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithCanonicalFacts(IEnumerable<string> facts)
        {
            EnsureContextExists();
            _context!.CanonicalFacts = facts.ToList();
            return this;
        }

        /// <summary>
        /// Adds world state entries to the context.
        /// </summary>
        /// <param name="worldState">The world state entries to add.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithWorldState(IEnumerable<WorldStateEntry> worldState)
        {
            EnsureContextExists();
            _context!.WorldState = worldState.ToList();
            return this;
        }

        /// <summary>
        /// Adds episodic memories to the context.
        /// </summary>
        /// <param name="memories">The episodic memories to add.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithEpisodicMemories(IEnumerable<EpisodicMemoryEntry> memories)
        {
            EnsureContextExists();
            _context!.EpisodicMemories = memories.ToList();
            return this;
        }

        /// <summary>
        /// Adds beliefs to the context.
        /// </summary>
        /// <param name="beliefs">The beliefs to add.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithBeliefs(IEnumerable<BeliefEntry> beliefs)
        {
            EnsureContextExists();
            _context!.Beliefs = beliefs.ToList();
            return this;
        }

        /// <summary>
        /// Adds relationships to the context.
        /// </summary>
        /// <param name="relationships">The relationships to add.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithRelationships(IEnumerable<RelationshipEntry> relationships)
        {
            EnsureContextExists();
            _context!.Relationships = relationships.ToList();
            return this;
        }

        /// <summary>
        /// Sets the constraints section.
        /// </summary>
        /// <param name="constraints">The constraints section.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithConstraints(ConstraintSection constraints)
        {
            _constraints = constraints;
            return this;
        }

        /// <summary>
        /// Sets the dialogue section.
        /// </summary>
        /// <param name="dialogue">The dialogue section.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithDialogue(DialogueSection dialogue)
        {
            _dialogue = dialogue;
            return this;
        }

        /// <summary>
        /// Sets the current player input in the dialogue section.
        /// Creates the dialogue section if it doesn't exist.
        /// </summary>
        /// <param name="input">The current player input.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithCurrentInput(string input)
        {
            EnsureDialogueExists();
            _dialogue!.PlayerInput = input;
            return this;
        }

        /// <summary>
        /// Adds dialogue history entries.
        /// Creates the dialogue section if it doesn't exist.
        /// </summary>
        /// <param name="history">The dialogue history entries.</param>
        /// <returns>This builder for chaining.</returns>
        public PartialContextBuilder WithDialogueHistory(IEnumerable<StructuredDialogueEntry> history)
        {
            EnsureDialogueExists();
            _dialogue!.History = history.ToList();
            return this;
        }

        /// <summary>
        /// Builds the context JSON schema with only the populated sections.
        /// </summary>
        /// <returns>A ContextJsonSchema with only the sections that have been set.</returns>
        public ContextJsonSchema Build()
        {
            return new ContextJsonSchema
            {
                SchemaVersion = _schemaVersion,
                Context = _context,
                Constraints = _constraints,
                Dialogue = _dialogue
            };
        }

        /// <summary>
        /// Ensures the context section exists.
        /// </summary>
        private void EnsureContextExists()
        {
            _context ??= new ContextSection();
        }

        /// <summary>
        /// Ensures the dialogue section exists.
        /// </summary>
        private void EnsureDialogueExists()
        {
            _dialogue ??= new DialogueSection();
        }
    }
}
