using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.StructuredOutput
{
    /// <summary>
    /// Tests for StructuredDialoguePipeline and related types.
    /// </summary>
    [TestFixture]
    public class StructuredPipelineTests
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
            _testProfile = PersonaProfile.Create("test-npc", "Test NPC");
            _testProfile.SystemPrompt = "You are a test NPC.";

            _memoryStore = new PersonaMemoryStore();
            _memorySystem = _memoryStore.GetOrCreateSystem(_testProfile.PersonaId);

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

        #region StructuredPipelineConfig Tests

        [Test]
        public void Config_Default_HasExpectedValues()
        {
            // Act
            var config = StructuredPipelineConfig.Default;

            // Assert
            Assert.That(config.UseStructuredOutput, Is.True);
            Assert.That(config.FallbackToRegex, Is.True);
            Assert.That(config.MaxRetries, Is.EqualTo(3));
            Assert.That(config.TrackMetrics, Is.True);
            Assert.That(config.ValidateMutationSchemas, Is.True);
            Assert.That(config.ValidateIntentSchemas, Is.True);
        }

        [Test]
        public void Config_StructuredOnly_HasNoFallback()
        {
            // Act
            var config = StructuredPipelineConfig.StructuredOnly;

            // Assert
            Assert.That(config.UseStructuredOutput, Is.True);
            Assert.That(config.FallbackToRegex, Is.False);
        }

        [Test]
        public void Config_RegexOnly_DisablesStructuredOutput()
        {
            // Act
            var config = StructuredPipelineConfig.RegexOnly;

            // Assert
            Assert.That(config.UseStructuredOutput, Is.False);
            Assert.That(config.FallbackToRegex, Is.True);
        }

        #endregion

        #region StructuredPipelineResult Tests

        [Test]
        public void Result_Succeeded_HasCorrectProperties()
        {
            // Arrange
            var parsedOutput = new ParsedOutput
            {
                Success = true,
                DialogueText = "Hello player!",
                RawOutput = "{\"dialogue\": \"Hello player!\"}"
            };
            var gateResult = GateResult.Pass(parsedOutput);

            // Act
            var result = StructuredPipelineResult.Succeeded(
                "Hello player!",
                parsedOutput,
                gateResult,
                null,
                ParseMode.Structured);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.DialogueText, Is.EqualTo("Hello player!"));
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Structured));
            Assert.That(result.ValidationPassed, Is.True);
            Assert.That(result.UsedFallback, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void Result_Failed_HasCorrectProperties()
        {
            // Act
            var result = StructuredPipelineResult.Failed(
                "Parse error",
                "{invalid}",
                ParseMode.Structured);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Parse error"));
            Assert.That(result.RawResponse, Is.EqualTo("{invalid}"));
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Structured));
        }

        [Test]
        public void Result_ValidationFailed_HasGateResult()
        {
            // Arrange
            var parsedOutput = new ParsedOutput
            {
                Success = true,
                DialogueText = "I am the king!",
                RawOutput = "{\"dialogue\": \"I am the king!\"}"
            };
            var failure = ValidationFailure.ProhibitionViolated("Cannot claim to be king", "I am the king!");
            var gateResult = GateResult.Fail(failure);

            // Act
            var result = StructuredPipelineResult.ValidationFailed(
                gateResult,
                parsedOutput,
                ParseMode.Structured);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationPassed, Is.False);
            Assert.That(result.GateResult, Is.Not.Null);
            Assert.That(result.DialogueText, Is.EqualTo("I am the king!"));
        }

        [Test]
        public void Result_UsedFallback_ReturnsTrue_WhenParseModeIsFallback()
        {
            // Act
            var result = StructuredPipelineResult.Failed(
                "Structured failed, used fallback",
                "",
                ParseMode.Fallback);

            // Assert
            Assert.That(result.UsedFallback, Is.True);
        }

        [Test]
        public void Result_ToString_ForSuccess_ContainsOK()
        {
            // Arrange
            var parsedOutput = new ParsedOutput { Success = true, DialogueText = "Hello" };
            var result = StructuredPipelineResult.Succeeded("Hello", parsedOutput, GateResult.Pass(parsedOutput));

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("OK"));
            Assert.That(str, Does.Contain("Mode=Structured"));
        }

        [Test]
        public void Result_ToString_ForFailure_ContainsFAIL()
        {
            // Arrange
            var result = StructuredPipelineResult.Failed("Test error");

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("FAIL"));
            Assert.That(str, Does.Contain("Test error"));
        }

        #endregion

        #region StructuredPipelineMetrics Tests

        [Test]
        public void Metrics_InitialState_AllZeros()
        {
            // Act
            var metrics = new StructuredPipelineMetrics();

            // Assert
            Assert.That(metrics.TotalRequests, Is.EqualTo(0));
            Assert.That(metrics.StructuredSuccessCount, Is.EqualTo(0));
            Assert.That(metrics.StructuredFailureCount, Is.EqualTo(0));
            Assert.That(metrics.RegexFallbackCount, Is.EqualTo(0));
            Assert.That(metrics.ValidationFailureCount, Is.EqualTo(0));
        }

        [Test]
        public void Metrics_RecordStructuredSuccess_IncrementsCounters()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();

            // Act
            metrics.RecordStructuredSuccess();
            metrics.RecordStructuredSuccess();

            // Assert
            Assert.That(metrics.TotalRequests, Is.EqualTo(2));
            Assert.That(metrics.StructuredSuccessCount, Is.EqualTo(2));
        }

        [Test]
        public void Metrics_RecordStructuredFailure_IncrementsFailureCount()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();

            // Act
            metrics.RecordStructuredFailure();

            // Assert
            Assert.That(metrics.StructuredFailureCount, Is.EqualTo(1));
            // Note: TotalRequests not incremented here as failure is followed by fallback
        }

        [Test]
        public void Metrics_RecordFallbackToRegex_IncrementsCounters()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();

            // Act
            metrics.RecordFallbackToRegex();

            // Assert
            Assert.That(metrics.TotalRequests, Is.EqualTo(1));
            Assert.That(metrics.RegexFallbackCount, Is.EqualTo(1));
        }

        [Test]
        public void Metrics_StructuredSuccessRate_CalculatesCorrectly()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();
            metrics.RecordStructuredSuccess();
            metrics.RecordStructuredSuccess();
            metrics.RecordStructuredFailure();

            // Act
            var rate = metrics.StructuredSuccessRate;

            // Assert
            // 2 success / 3 attempts = 66.67%
            Assert.That(rate, Is.EqualTo(66.67f).Within(0.1f));
        }

        [Test]
        public void Metrics_FallbackRate_CalculatesCorrectly()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();
            metrics.RecordStructuredSuccess();
            metrics.RecordStructuredSuccess();
            metrics.RecordStructuredFailure();
            metrics.RecordFallbackToRegex();

            // Act
            var rate = metrics.FallbackRate;

            // Assert
            // 1 fallback / 3 structured attempts = 33.33%
            Assert.That(rate, Is.EqualTo(33.33f).Within(0.1f));
        }

        [Test]
        public void Metrics_Reset_ClearsAllCounters()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();
            metrics.RecordStructuredSuccess();
            metrics.RecordStructuredFailure();
            metrics.RecordFallbackToRegex();
            metrics.RecordValidationFailure();
            metrics.RecordMutationsExecuted(5);
            metrics.RecordIntentsEmitted(3);

            // Act
            metrics.Reset();

            // Assert
            Assert.That(metrics.TotalRequests, Is.EqualTo(0));
            Assert.That(metrics.StructuredSuccessCount, Is.EqualTo(0));
            Assert.That(metrics.StructuredFailureCount, Is.EqualTo(0));
            Assert.That(metrics.RegexFallbackCount, Is.EqualTo(0));
            Assert.That(metrics.ValidationFailureCount, Is.EqualTo(0));
            Assert.That(metrics.MutationsExecuted, Is.EqualTo(0));
            Assert.That(metrics.IntentsEmitted, Is.EqualTo(0));
        }

        [Test]
        public void Metrics_RecordMutationsExecuted_AccumulatesCount()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();

            // Act
            metrics.RecordMutationsExecuted(3);
            metrics.RecordMutationsExecuted(2);

            // Assert
            Assert.That(metrics.MutationsExecuted, Is.EqualTo(5));
        }

        [Test]
        public void Metrics_RecordIntentsEmitted_AccumulatesCount()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();

            // Act
            metrics.RecordIntentsEmitted(2);
            metrics.RecordIntentsEmitted(1);

            // Assert
            Assert.That(metrics.IntentsEmitted, Is.EqualTo(3));
        }

        [Test]
        public void Metrics_ToString_ContainsKeyInfo()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();
            metrics.RecordStructuredSuccess();

            // Act
            var str = metrics.ToString();

            // Assert
            Assert.That(str, Does.Contain("1 requests"));
            Assert.That(str, Does.Contain("Structured:"));
        }

        #endregion

        #region StructuredDialoguePipeline Construction Tests

        [Test]
        public void Pipeline_Constructor_WithValidParams_CreatesSuccessfully()
        {
            // Act
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);

            // Assert
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(pipeline.Config, Is.Not.Null);
            Assert.That(pipeline.Metrics, Is.Not.Null);
        }

        [Test]
        public void Pipeline_Constructor_WithNullAgent_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new StructuredDialoguePipeline(null!, _validationGate));
            Assert.That(ex!.ParamName, Is.EqualTo("agent"));
        }

        [Test]
        public void Pipeline_Constructor_WithNullValidationGate_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new StructuredDialoguePipeline(_agent, null!));
            Assert.That(ex!.ParamName, Is.EqualTo("validationGate"));
        }

        [Test]
        public void Pipeline_Constructor_WithMutationControllerButNoMemorySystem_Throws()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new StructuredDialoguePipeline(
                    _agent,
                    _validationGate,
                    _mutationController,
                    null));
            Assert.That(ex!.Message, Does.Contain("memorySystem is required"));
        }

        [Test]
        public void Pipeline_Constructor_WithMutationControllerAndMemorySystem_Succeeds()
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

        [Test]
        public void Pipeline_Constructor_WithCustomConfig_UsesConfig()
        {
            // Arrange
            var config = StructuredPipelineConfig.StructuredOnly;

            // Act
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                null,
                null,
                config);

            // Assert
            Assert.That(pipeline.Config.UseStructuredOutput, Is.True);
            Assert.That(pipeline.Config.FallbackToRegex, Is.False);
        }

        #endregion

        #region StructuredDialoguePipeline Processing Tests

        [Test]
        public async Task Pipeline_ProcessDialogue_WithEmptyInput_ReturnsFailed()
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
        public async Task Pipeline_ProcessDialogue_WithNullInput_ReturnsFailed()
        {
            // Arrange
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);

            // Act
            var result = await pipeline.ProcessDialogueAsync(null!);

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task Pipeline_ProcessDialogue_WithWhitespaceInput_ReturnsFailed()
        {
            // Arrange
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);

            // Act
            var result = await pipeline.ProcessDialogueAsync("   ");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("cannot be null or empty"));
        }

        [Test]
        public void Pipeline_ResetMetrics_ClearsMetrics()
        {
            // Arrange
            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);
            pipeline.Metrics.RecordStructuredSuccess();

            // Act
            pipeline.ResetMetrics();

            // Assert
            Assert.That(pipeline.Metrics.TotalRequests, Is.EqualTo(0));
        }

        #endregion

        #region ParseMode Tests

        [Test]
        public void ParseMode_HasExpectedValues()
        {
            // Assert
            Assert.That((int)ParseMode.Structured, Is.EqualTo(0));
            Assert.That((int)ParseMode.Regex, Is.EqualTo(1));
            Assert.That((int)ParseMode.Fallback, Is.EqualTo(2));
        }

        #endregion

        #region Retry Configuration Tests

        [Test]
        public void Config_Default_HasMaxRetries()
        {
            // Act
            var config = StructuredPipelineConfig.Default;

            // Assert
            Assert.That(config.MaxRetries, Is.EqualTo(3));
        }

        [Test]
        public void Config_MaxRetries_CanBeCustomized()
        {
            // Arrange
            var config = new StructuredPipelineConfig { MaxRetries = 5 };

            // Assert
            Assert.That(config.MaxRetries, Is.EqualTo(5));
        }

        [Test]
        public void Result_RetryCount_DefaultsToZero()
        {
            // Act
            var result = StructuredPipelineResult.Failed("test error");

            // Assert
            Assert.That(result.RetryCount, Is.EqualTo(0));
        }

        [Test]
        public void Result_RetryCount_CanBeSet()
        {
            // Arrange
            var parsedOutput = new ParsedOutput { Success = true, DialogueText = "Hello" };
            var result = StructuredPipelineResult.Succeeded("Hello", parsedOutput, GateResult.Pass(parsedOutput));

            // Act
            result.RetryCount = 2;

            // Assert
            Assert.That(result.RetryCount, Is.EqualTo(2));
        }

        [Test]
        public void Result_ToString_IncludesRetryCount()
        {
            // Arrange
            var parsedOutput = new ParsedOutput { Success = true, DialogueText = "Hello" };
            var result = StructuredPipelineResult.Succeeded("Hello", parsedOutput, GateResult.Pass(parsedOutput));
            result.RetryCount = 1;

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("Retries=1"));
        }

        #endregion

        #region Metrics Retry Tracking Tests

        [Test]
        public void Metrics_RecordRetry_IncrementsTotalRetries()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();

            // Act
            metrics.RecordRetry();
            metrics.RecordRetry();

            // Assert
            Assert.That(metrics.TotalRetries, Is.EqualTo(2));
        }

        [Test]
        public void Metrics_Reset_ClearsTotalRetries()
        {
            // Arrange
            var metrics = new StructuredPipelineMetrics();
            metrics.RecordRetry();
            metrics.RecordRetry();

            // Act
            metrics.Reset();

            // Assert
            Assert.That(metrics.TotalRetries, Is.EqualTo(0));
        }

        #endregion
    }
}
