using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Integration
{
    /// <summary>
    /// Integration tests for StructuredDialoguePipeline.
    /// Tests pipeline configuration, input validation, and component integration.
    /// </summary>
    [TestFixture]
    public class StructuredPipelineIntegrationTests
    {
        private PersonaProfile _testProfile = null!;
        private PersonaMemoryStore _memoryStore = null!;
        private AuthoritativeMemorySystem _memorySystem = null!;
        private IApiClient _mockApiClient = null!;
        private BrainAgent _agent = null!;
        private ValidationGate _validationGate = null!;
        private MemoryMutationController _mutationController = null!;

        [SetUp]
        public void SetUp()
        {
            _testProfile = PersonaProfile.Create("shopkeeper", "Friendly Shopkeeper");
            _testProfile.SystemPrompt = "You are a friendly shopkeeper in a fantasy world.";

            _memoryStore = new PersonaMemoryStore();
            _memorySystem = _memoryStore.GetOrCreateSystem(_testProfile.PersonaId);

            // Add some baseline memories
            _memorySystem.AddCanonicalFact("shop_location", "The shop is in the market district", "world_lore");

            _mockApiClient = Substitute.For<IApiClient>();
            _agent = new BrainAgent(_testProfile, _mockApiClient, _memoryStore);
            _validationGate = new ValidationGate();
            _mutationController = new MemoryMutationController();
        }

        [TearDown]
        public void TearDown()
        {
            _agent?.Dispose();
        }

        #region Pipeline Configuration Tests

        [Test]
        public void Pipeline_DefaultConfig_UsesStructuredOutput()
        {
            // Arrange & Act
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);

            // Assert
            Assert.That(pipeline.Config.UseStructuredOutput, Is.True);
            Assert.That(pipeline.Config.FallbackToRegex, Is.True);
            Assert.That(pipeline.Config.MaxRetries, Is.EqualTo(3));
        }

        [Test]
        public void Pipeline_StructuredOnlyConfig_DisablesFallback()
        {
            // Arrange
            var config = StructuredPipelineConfig.StructuredOnly;
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate, null, null, config);

            // Assert
            Assert.That(pipeline.Config.UseStructuredOutput, Is.True);
            Assert.That(pipeline.Config.FallbackToRegex, Is.False);
        }

        [Test]
        public void Pipeline_RegexOnlyConfig_DisablesStructuredOutput()
        {
            // Arrange
            var config = StructuredPipelineConfig.RegexOnly;
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate, null, null, config);

            // Assert
            Assert.That(pipeline.Config.UseStructuredOutput, Is.False);
            Assert.That(pipeline.Config.FallbackToRegex, Is.True);
        }

        #endregion

        #region Input Validation Tests

        [Test]
        public async Task Pipeline_EmptyInput_ReturnsFailedResult()
        {
            // Arrange
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);

            // Act
            var result = await pipeline.ProcessDialogueAsync("");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("cannot be null or empty"));
        }

        [Test]
        public async Task Pipeline_WhitespaceInput_ReturnsFailedResult()
        {
            // Arrange
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);

            // Act
            var result = await pipeline.ProcessDialogueAsync("   ");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("cannot be null or empty"));
        }

        #endregion

        #region Metrics Tests

        [Test]
        public void Pipeline_MetricsInitialState_AllZeros()
        {
            // Arrange
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);

            // Assert
            Assert.That(pipeline.Metrics.TotalRequests, Is.EqualTo(0));
            Assert.That(pipeline.Metrics.StructuredSuccessCount, Is.EqualTo(0));
            Assert.That(pipeline.Metrics.StructuredFailureCount, Is.EqualTo(0));
            Assert.That(pipeline.Metrics.ValidationFailureCount, Is.EqualTo(0));
        }

        [Test]
        public void Pipeline_ResetMetrics_ClearsAll()
        {
            // Arrange
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);
            pipeline.Metrics.RecordStructuredSuccess();
            pipeline.Metrics.RecordRetry();

            // Act
            pipeline.ResetMetrics();

            // Assert
            Assert.That(pipeline.Metrics.TotalRequests, Is.EqualTo(0));
            Assert.That(pipeline.Metrics.TotalRetries, Is.EqualTo(0));
        }

        #endregion

        #region Pipeline With Mutation Controller

        [Test]
        public void Pipeline_WithMutationController_RequiresMemorySystem()
        {
            // Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new StructuredDialoguePipeline(
                    _agent,
                    _validationGate,
                    _mutationController,
                    null)); // No memory system

            Assert.That(ex!.Message, Does.Contain("memorySystem is required"));
        }

        [Test]
        public void Pipeline_WithMutationControllerAndMemorySystem_Succeeds()
        {
            // Act
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem);

            // Assert
            Assert.That(pipeline, Is.Not.Null);
        }

        #endregion

    }
}
