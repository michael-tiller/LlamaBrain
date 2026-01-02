using NUnit.Framework;
using LlamaBrain.Core.StructuredInput;
using LlamaBrain.Core.StructuredInput.Schemas;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.StructuredInput
{
    /// <summary>
    /// Tests for StructuredContextProvider - context building and format support.
    /// </summary>
    public class StructuredContextProviderTests
    {
        #region Format Support

        [Test]
        public void SupportsFormat_JsonContext_ReturnsTrue()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;

            // Act & Assert
            Assert.That(provider.SupportsFormat(StructuredContextFormat.JsonContext), Is.True);
        }

        [Test]
        public void SupportsFormat_None_ReturnsTrue()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;

            // Act & Assert
            Assert.That(provider.SupportsFormat(StructuredContextFormat.None), Is.True);
        }

        [Test]
        public void SupportsFormat_FunctionCalling_ReturnsFalse()
        {
            // Arrange - llama.cpp doesn't support native function calling
            var provider = LlamaCppStructuredContextProvider.Instance;

            // Act & Assert
            Assert.That(provider.SupportsFormat(StructuredContextFormat.FunctionCalling), Is.False);
        }

        #endregion

        #region Singleton

        [Test]
        public void Instance_ReturnsSameInstance()
        {
            // Act
            var instance1 = LlamaCppStructuredContextProvider.Instance;
            var instance2 = LlamaCppStructuredContextProvider.Instance;

            // Assert
            Assert.That(instance1, Is.SameAs(instance2));
        }

        #endregion

        #region Context Building

        [Test]
        public void BuildContext_FromSnapshot_ReturnsValidContext()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var snapshot = CreateTestSnapshot();

            // Act
            var context = provider.BuildContext(snapshot);

            // Assert
            Assert.That(context, Is.Not.Null);
            Assert.That(context.SchemaVersion, Is.EqualTo("1.0"));
        }

        [Test]
        public void BuildContext_IncludesCanonicalFacts()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("You are helpful")
                .WithPlayerInput("Hello")
                .WithCanonicalFacts(new[] { "The king is Arthur" })
                .Build();

            // Act
            var context = provider.BuildContext(snapshot);

            // Assert
            Assert.That(context.Context.CanonicalFacts, Does.Contain("The king is Arthur"));
        }

        [Test]
        public void BuildContext_IncludesWorldState()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("You are helpful")
                .WithPlayerInput("Hello")
                .WithWorldState(new[] { "door_status=open" })
                .Build();

            // Act
            var context = provider.BuildContext(snapshot);

            // Assert
            Assert.That(context.Context.WorldState, Has.Count.EqualTo(1));
            Assert.That(context.Context.WorldState[0].Key, Is.EqualTo("door_status"));
            Assert.That(context.Context.WorldState[0].Value, Is.EqualTo("open"));
        }

        [Test]
        public void BuildContext_IncludesEpisodicMemories()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("You are helpful")
                .WithPlayerInput("Hello")
                .WithEpisodicMemories(new[] { "Player said hello earlier" })
                .Build();

            // Act
            var context = provider.BuildContext(snapshot);

            // Assert
            Assert.That(context.Context.EpisodicMemories, Has.Count.EqualTo(1));
            Assert.That(context.Context.EpisodicMemories[0].Content, Is.EqualTo("Player said hello earlier"));
        }

        [Test]
        public void BuildContext_IncludesDialogueHistory()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("You are helpful")
                .WithPlayerInput("How are you?")
                .WithDialogueHistory(new[] { "Player: Hello", "NPC: Greetings!" })
                .Build();

            // Act
            var context = provider.BuildContext(snapshot);

            // Assert
            Assert.That(context.Dialogue.History, Has.Count.EqualTo(2));
            Assert.That(context.Dialogue.PlayerInput, Is.EqualTo("How are you?"));
        }

        [Test]
        public void BuildContext_IncludesConstraints()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var constraints = new ConstraintSet();
            constraints.Add(new Constraint
            {
                Id = "no-secrets",
                Type = ConstraintType.Prohibition,
                PromptInjection = "Do not reveal secrets"
            });
            constraints.Add(new Constraint
            {
                Id = "be-helpful",
                Type = ConstraintType.Requirement,
                PromptInjection = "Be helpful"
            });

            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("You are helpful")
                .WithPlayerInput("Hello")
                .WithConstraints(constraints)
                .Build();

            // Act
            var context = provider.BuildContext(snapshot);

            // Assert
            Assert.That(context.Constraints.Prohibitions, Does.Contain("Do not reveal secrets"));
            Assert.That(context.Constraints.Requirements, Does.Contain("Be helpful"));
        }

        [Test]
        public void BuildContext_ParsesDialogueSpeakers()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("You are helpful")
                .WithPlayerInput("Question")
                .WithDialogueHistory(new[] { "Player: Hello there", "Shopkeeper: Welcome!" })
                .Build();

            // Act
            var context = provider.BuildContext(snapshot);

            // Assert
            Assert.That(context.Dialogue.History[0].Speaker, Is.EqualTo("Player"));
            Assert.That(context.Dialogue.History[0].Text, Is.EqualTo("Hello there"));
            Assert.That(context.Dialogue.History[1].Speaker, Is.EqualTo("Shopkeeper"));
            Assert.That(context.Dialogue.History[1].Text, Is.EqualTo("Welcome!"));
        }

        [Test]
        public void BuildContext_HandlesDialogueWithoutSpeaker()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("You are helpful")
                .WithPlayerInput("Question")
                .WithDialogueHistory(new[] { "Some text without speaker prefix" })
                .Build();

            // Act
            var context = provider.BuildContext(snapshot);

            // Assert
            Assert.That(context.Dialogue.History[0].Speaker, Is.EqualTo("Unknown"));
            Assert.That(context.Dialogue.History[0].Text, Is.EqualTo("Some text without speaker prefix"));
        }

        #endregion

        #region Validation

        [Test]
        public void ValidateContext_ValidContext_ReturnsTrue()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;
            var context = new ContextJsonSchema();

            // Act
            var result = provider.ValidateContext(context, out var error);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void ValidateContext_NullContext_ReturnsFalse()
        {
            // Arrange
            var provider = LlamaCppStructuredContextProvider.Instance;

            // Act
            var result = provider.ValidateContext(null!, out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        #endregion

        #region Config Tests

        [Test]
        public void StructuredContextConfig_Default_UsesJsonContext()
        {
            // Act
            var config = StructuredContextConfig.Default;

            // Assert
            Assert.That(config.PreferredFormat, Is.EqualTo(StructuredContextFormat.JsonContext));
            Assert.That(config.FallbackToTextAssembly, Is.True);
        }

        [Test]
        public void StructuredContextConfig_TextOnly_UsesNone()
        {
            // Act
            var config = StructuredContextConfig.TextOnly;

            // Assert
            Assert.That(config.PreferredFormat, Is.EqualTo(StructuredContextFormat.None));
            Assert.That(config.FallbackToTextAssembly, Is.True);
        }

        [Test]
        public void StructuredContextConfig_Strict_NoFallback()
        {
            // Act
            var config = StructuredContextConfig.Strict;

            // Assert
            Assert.That(config.PreferredFormat, Is.EqualTo(StructuredContextFormat.JsonContext));
            Assert.That(config.FallbackToTextAssembly, Is.False);
            Assert.That(config.ValidateSchema, Is.True);
        }

        #endregion

        #region Format Enum

        [Test]
        public void StructuredContextFormat_HasExplicitValues()
        {
            // Assert - explicit values per CLAUDE.md determinism requirements
            Assert.That((int)StructuredContextFormat.None, Is.EqualTo(0));
            Assert.That((int)StructuredContextFormat.JsonContext, Is.EqualTo(1));
            Assert.That((int)StructuredContextFormat.FunctionCalling, Is.EqualTo(2));
        }

        #endregion

        #region Helper Methods

        private static StateSnapshot CreateTestSnapshot()
        {
            return new StateSnapshotBuilder()
                .WithSystemPrompt("You are a helpful NPC.")
                .WithPlayerInput("Hello there!")
                .WithCanonicalFacts(new[] { "The king is Arthur" })
                .WithWorldState(new[] { "door_status=open" })
                .WithEpisodicMemories(new[] { "Player visited yesterday" })
                .WithBeliefs(new[] { "I trust the player" })
                .WithDialogueHistory(new[] { "Player: Hi", "NPC: Hello!" })
                .Build();
        }

        #endregion
    }
}
