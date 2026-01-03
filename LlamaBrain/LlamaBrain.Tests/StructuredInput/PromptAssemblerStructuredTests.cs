using NUnit.Framework;
using LlamaBrain.Core.StructuredInput;
using LlamaBrain.Core.Inference;

namespace LlamaBrain.Tests.StructuredInput
{
    /// <summary>
    /// Tests for PromptAssembler structured context integration.
    /// </summary>
    public class PromptAssemblerStructuredTests
    {
        #region AssembleStructuredPrompt

        [Test]
        public void AssembleStructuredPrompt_ReturnsValidPrompt()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Text, Is.Not.Empty);
        }

        [Test]
        public void AssembleStructuredPrompt_ContainsContextJsonTags()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result.Text, Does.Contain("<context_json>"));
            Assert.That(result.Text, Does.Contain("</context_json>"));
        }

        [Test]
        public void AssembleStructuredPrompt_ContainsSchemaVersion()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result.Text, Does.Contain("schemaVersion"));
            Assert.That(result.Text, Does.Contain("1.0"));
        }

        [Test]
        public void AssembleStructuredPrompt_ContainsSystemPrompt()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("You are a helpful NPC.")
                .WithPlayerInput("Hello")
                .Build();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result.Text, Does.Contain("You are a helpful NPC."));
            Assert.That(result.Breakdown.SystemPrompt, Is.GreaterThan(0));
        }

        [Test]
        public void AssembleStructuredPrompt_ContainsCanonicalFacts()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("Hello")
                .WithCanonicalFacts(new[] { "The king is Arthur" })
                .Build();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result.Text, Does.Contain("The king is Arthur"));
        }

        [Test]
        public void AssembleStructuredPrompt_ContainsPlayerInput()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = new StateSnapshotBuilder()
                .WithSystemPrompt("Test")
                .WithPlayerInput("What is your name?")
                .Build();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result.Text, Does.Contain("What is your name?"));
        }

        [Test]
        public void AssembleStructuredPrompt_ContainsNpcPrompt()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot, "Shopkeeper");

            // Assert
            Assert.That(result.Text, Does.Contain("Shopkeeper:"));
        }

        [Test]
        public void AssembleStructuredPrompt_TracksContextBreakdown()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result.Breakdown.Context, Is.GreaterThan(0));
            Assert.That(result.Breakdown.Formatting, Is.GreaterThan(0));
        }

        [Test]
        public void AssembleStructuredPrompt_EstimatesTokens()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result.EstimatedTokens, Is.GreaterThan(0));
            Assert.That(result.CharacterCount, Is.GreaterThan(0));
        }

        #endregion

        #region Fallback Behavior

        [Test]
        public void AssembleStructuredPrompt_WithNoneFormat_FallsBackToText()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();
            var config = StructuredContextConfig.TextOnly;

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot, structuredConfig: config);

            // Assert - Should NOT contain JSON context tags
            Assert.That(result.Text, Does.Not.Contain("<context_json>"));
            Assert.That(result.Text, Does.Not.Contain("schemaVersion"));
        }

        [Test]
        public void AssembleStructuredPrompt_WithConfiguredAssembler_UsesConfig()
        {
            // Arrange
            var config = new PromptAssemblerConfig
            {
                StructuredContextConfig = new StructuredContextConfig
                {
                    ContextBlockOpenTag = "<custom>",
                    ContextBlockCloseTag = "</custom>"
                }
            };
            var assembler = new PromptAssembler(config);
            var snapshot = CreateTestSnapshot();

            // Act
            var result = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result.Text, Does.Contain("<custom>"));
            Assert.That(result.Text, Does.Contain("</custom>"));
        }

        [Test]
        public void AssembleStructuredPrompt_WithCompactJson_ReducesSize()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();
            var compactConfig = new StructuredContextConfig { UseCompactJson = true };
            var indentedConfig = new StructuredContextConfig { UseCompactJson = false };

            // Act
            var compactResult = assembler.AssembleStructuredPrompt(snapshot, structuredConfig: compactConfig);
            var indentedResult = assembler.AssembleStructuredPrompt(snapshot, structuredConfig: indentedConfig);

            // Assert
            Assert.That(compactResult.CharacterCount, Is.LessThan(indentedResult.CharacterCount));
        }

        #endregion

        #region Config Properties

        [Test]
        public void PromptAssemblerConfig_UseStructuredContext_ReturnsTrueWhenConfigured()
        {
            // Arrange
            var config = new PromptAssemblerConfig
            {
                StructuredContextConfig = StructuredContextConfig.Default
            };

            // Assert
            Assert.That(config.UseStructuredContext, Is.True);
        }

        [Test]
        public void PromptAssemblerConfig_UseStructuredContext_ReturnsFalseWhenNull()
        {
            // Arrange
            var config = new PromptAssemblerConfig
            {
                StructuredContextConfig = null
            };

            // Assert
            Assert.That(config.UseStructuredContext, Is.False);
        }

        [Test]
        public void PromptAssemblerConfig_UseStructuredContext_ReturnsFalseWhenNone()
        {
            // Arrange
            var config = new PromptAssemblerConfig
            {
                StructuredContextConfig = StructuredContextConfig.TextOnly
            };

            // Assert
            Assert.That(config.UseStructuredContext, Is.False);
        }

        #endregion

        #region Determinism

        [Test]
        public void AssembleStructuredPrompt_SameInput_ProducesSameOutput()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot1 = CreateTestSnapshot();
            var snapshot2 = CreateTestSnapshot();

            // Act
            var result1 = assembler.AssembleStructuredPrompt(snapshot1);
            var result2 = assembler.AssembleStructuredPrompt(snapshot2);

            // Assert
            Assert.That(result1.Text, Is.EqualTo(result2.Text));
        }

        [Test]
        public void AssembleStructuredPrompt_MultipleCallsSameSnapshot_ProducesSameOutput()
        {
            // Arrange
            var assembler = new PromptAssembler();
            var snapshot = CreateTestSnapshot();

            // Act
            var result1 = assembler.AssembleStructuredPrompt(snapshot);
            var result2 = assembler.AssembleStructuredPrompt(snapshot);
            var result3 = assembler.AssembleStructuredPrompt(snapshot);

            // Assert
            Assert.That(result1.Text, Is.EqualTo(result2.Text));
            Assert.That(result2.Text, Is.EqualTo(result3.Text));
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
