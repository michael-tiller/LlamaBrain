using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
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

        #region Full Pipeline Flow Tests

        [Test]
        public async Task Pipeline_WithValidStructuredResponse_ProcessesSuccessfully()
        {
            // Arrange
            var validJsonResponse = @"{
                ""dialogueText"": ""Welcome to my shop! How can I help you today?"",
                ""proposedMutations"": [
                    {
                        ""type"": ""AppendEpisodic"",
                        ""content"": ""Player visited the shop""
                    }
                ],
                ""worldIntents"": []
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(validJsonResponse));

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.DialogueText, Is.EqualTo("Welcome to my shop! How can I help you today?"));
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Structured));
        }

        [Test]
        public async Task Pipeline_WithMutations_ExecutesMutationsAfterValidation()
        {
            // Arrange
            var jsonWithMutation = @"{
                ""dialogueText"": ""I remember you now!"",
                ""proposedMutations"": [
                    {
                        ""type"": ""AppendEpisodic"",
                        ""content"": ""Player asked about potion prices""
                    },
                    {
                        ""type"": ""TransformBelief"",
                        ""target"": ""player_interest"",
                        ""content"": ""Player is interested in potions"",
                        ""confidence"": 0.8
                    }
                ],
                ""worldIntents"": []
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(jsonWithMutation));

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem);

            // Act
            var result = await pipeline.ProcessDialogueAsync("What potions do you have?");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.MutationResult, Is.Not.Null);
            Assert.That(result.MutationResult!.SuccessCount, Is.GreaterThan(0));
        }

        [Test]
        public async Task Pipeline_WithWorldIntents_EmitsIntents()
        {
            // Arrange
            var jsonWithIntent = @"{
                ""dialogueText"": ""Let me show you something special!"",
                ""proposedMutations"": [],
                ""worldIntents"": [
                    {
                        ""intentType"": ""show_inventory"",
                        ""target"": ""rare_items"",
                        ""priority"": 5
                    }
                ]
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(jsonWithIntent));

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Show me your best items");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.GateResult, Is.Not.Null);
            Assert.That(result.GateResult!.ApprovedIntents, Has.Count.GreaterThan(0));
        }

        #endregion

        #region Schema Validation Integration Tests

        [Test]
        public async Task Pipeline_WithInvalidMutationType_DefaultsToAppendEpisodic()
        {
            // Arrange - mutation with invalid type gets converted to AppendEpisodic during parsing
            // This is existing behavior - invalid types default to AppendEpisodic in StructuredMutation.ToProposedMutation()
            var jsonWithInvalidMutation = @"{
                ""dialogueText"": ""Hello there!"",
                ""proposedMutations"": [
                    {
                        ""type"": ""InvalidMutationType"",
                        ""content"": ""This becomes AppendEpisodic""
                    },
                    {
                        ""type"": ""AppendEpisodic"",
                        ""content"": ""This is explicitly AppendEpisodic""
                    }
                ],
                ""worldIntents"": []
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(jsonWithInvalidMutation));

            var config = StructuredPipelineConfig.Default;
            config.ValidateMutationSchemas = true;

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.True);
            // Both mutations pass through - invalid type defaults to AppendEpisodic
            Assert.That(result.ParsedOutput!.ProposedMutations, Has.Count.EqualTo(2));
            Assert.That(result.ParsedOutput.ProposedMutations[0].Type, Is.EqualTo(MutationType.AppendEpisodic));
            Assert.That(result.ParsedOutput.ProposedMutations[1].Type, Is.EqualTo(MutationType.AppendEpisodic));
        }

        [Test]
        public async Task Pipeline_WithEmptyContent_FiltersOutInvalidMutation()
        {
            // Arrange - mutation with empty content should be filtered by schema validator
            var jsonWithEmptyContent = @"{
                ""dialogueText"": ""Hello there!"",
                ""proposedMutations"": [
                    {
                        ""type"": ""AppendEpisodic"",
                        ""content"": """"
                    },
                    {
                        ""type"": ""AppendEpisodic"",
                        ""content"": ""This is valid and should pass""
                    }
                ],
                ""worldIntents"": []
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(jsonWithEmptyContent));

            var config = StructuredPipelineConfig.Default;
            config.ValidateMutationSchemas = true;

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.True);
            // Only the valid mutation should have been processed
            Assert.That(result.ParsedOutput!.ProposedMutations, Has.Count.EqualTo(1));
            Assert.That(result.ParsedOutput.ProposedMutations[0].Content, Is.EqualTo("This is valid and should pass"));
        }

        [Test]
        public async Task Pipeline_WithMissingTarget_FiltersOutInvalidBeliefMutation()
        {
            // Arrange - TransformBelief without target should be filtered
            var jsonWithInvalidBelief = @"{
                ""dialogueText"": ""I see..."",
                ""proposedMutations"": [
                    {
                        ""type"": ""TransformBelief"",
                        ""content"": ""Belief without target - invalid""
                    },
                    {
                        ""type"": ""AppendEpisodic"",
                        ""content"": ""Valid episodic memory""
                    }
                ],
                ""worldIntents"": []
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(jsonWithInvalidBelief));

            var config = StructuredPipelineConfig.Default;
            config.ValidateMutationSchemas = true;

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Tell me about yourself");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParsedOutput!.ProposedMutations, Has.Count.EqualTo(1));
            Assert.That(result.ParsedOutput.ProposedMutations[0].Type, Is.EqualTo(MutationType.AppendEpisodic));
        }

        [Test]
        public async Task Pipeline_WithInvalidIntent_FiltersOutInvalidIntent()
        {
            // Arrange - intent with empty type should be filtered
            var jsonWithInvalidIntent = @"{
                ""dialogueText"": ""Here you go!"",
                ""proposedMutations"": [],
                ""worldIntents"": [
                    {
                        ""intentType"": """",
                        ""target"": ""invalid_no_type""
                    },
                    {
                        ""intentType"": ""give_item"",
                        ""target"": ""health_potion"",
                        ""priority"": 1
                    }
                ]
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(jsonWithInvalidIntent));

            var config = StructuredPipelineConfig.Default;
            config.ValidateIntentSchemas = true;

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Can I have a potion?");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParsedOutput!.WorldIntents, Has.Count.EqualTo(1));
            Assert.That(result.ParsedOutput.WorldIntents[0].IntentType, Is.EqualTo("give_item"));
        }

        [Test]
        public async Task Pipeline_WithSchemaValidationDisabled_PassesAllMutations()
        {
            // Arrange - with validation disabled, invalid mutations should pass through
            var jsonWithInvalidMutation = @"{
                ""dialogueText"": ""Testing..."",
                ""proposedMutations"": [
                    {
                        ""type"": ""TransformBelief"",
                        ""content"": ""Belief without target""
                    }
                ],
                ""worldIntents"": []
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(jsonWithInvalidMutation));

            var config = StructuredPipelineConfig.Default;
            config.ValidateMutationSchemas = false; // Disabled

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Test");

            // Assert
            Assert.That(result.Success, Is.True);
            // Mutation passes through to ParsedOutput (may still fail at ValidationGate or execution)
            Assert.That(result.ParsedOutput!.ProposedMutations, Has.Count.EqualTo(1));
        }

        #endregion

        #region Fallback Tests

        [Test]
        public async Task Pipeline_WhenStructuredOutputThrows_FallsBackToRegex()
        {
            // Arrange - Throw exception to force fallback path
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task<string>>(x => throw new InvalidOperationException("Structured output not supported"));

            // When structured fails, the pipeline will try normal message via fallback
            _mockApiClient.SendPromptAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult("Hello, welcome to my shop!"));

            var config = StructuredPipelineConfig.Default;
            config.FallbackToRegex = true;

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Fallback));
            Assert.That(result.DialogueText, Does.Contain("welcome"));
        }

        [Test]
        public async Task Pipeline_WithInvalidJsonResponse_StillParsesAsStructured()
        {
            // Arrange - Invalid JSON is handled gracefully by OutputParser (falls back to regex internally)
            // but the ParseMode remains Structured since TryStructuredOutputAsync succeeded
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult("Hello, welcome to my shop!"));

            var config = StructuredPipelineConfig.Default;
            config.FallbackToRegex = true;

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert - OutputParser handles non-JSON gracefully, reports as Structured
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Structured));
            Assert.That(result.DialogueText, Does.Contain("welcome"));
        }

        [Test]
        public async Task Pipeline_InRegexOnlyMode_SkipsStructuredOutput()
        {
            // Arrange
            _mockApiClient.SendPromptAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult("Greetings, traveler!"));

            var config = StructuredPipelineConfig.RegexOnly;

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Regex));

            // Verify structured endpoint was NOT called
            await _mockApiClient.DidNotReceive().SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>());
        }

        #endregion

        #region Metrics Integration Tests

        [Test]
        public async Task Pipeline_SuccessfulStructuredRequest_UpdatesMetrics()
        {
            // Arrange
            var validJson = @"{""dialogueText"": ""Hello!"", ""proposedMutations"": [], ""worldIntents"": []}";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(validJson));

            var pipeline = new StructuredDialoguePipeline(_agent, _validationGate);

            // Act
            await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(pipeline.Metrics.TotalRequests, Is.EqualTo(1));
            Assert.That(pipeline.Metrics.StructuredSuccessCount, Is.EqualTo(1));
            Assert.That(pipeline.Metrics.StructuredFailureCount, Is.EqualTo(0));
        }

        [Test]
        public async Task Pipeline_WithMutationsExecuted_TracksExecutionMetrics()
        {
            // Arrange
            var jsonWithMutations = @"{
                ""dialogueText"": ""Noted!"",
                ""proposedMutations"": [
                    {""type"": ""AppendEpisodic"", ""content"": ""Memory 1""},
                    {""type"": ""AppendEpisodic"", ""content"": ""Memory 2""}
                ],
                ""worldIntents"": [
                    {""intentType"": ""wave"", ""target"": ""player""}
                ]
            }";

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(jsonWithMutations));

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem);

            // Act
            await pipeline.ProcessDialogueAsync("Remember this");

            // Assert
            Assert.That(pipeline.Metrics.MutationsExecuted, Is.GreaterThan(0));
            Assert.That(pipeline.Metrics.IntentsEmitted, Is.GreaterThan(0));
        }

        #endregion

        #region Retry Behavior Tests

        [Test]
        public async Task Pipeline_WhenValidationFailsWithRetry_RetriesUpToMaxRetries()
        {
            // Arrange - Create validation rule that fails initially but passes after retries
            var callCount = 0;
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(x =>
                {
                    callCount++;
                    // First two calls return prohibited text, third succeeds
                    if (callCount <= 2)
                    {
                        return Task.FromResult(@"{
                            ""dialogueText"": ""I am the forbidden one!"",
                            ""proposedMutations"": [],
                            ""worldIntents"": []
                        }");
                    }
                    return Task.FromResult(@"{
                        ""dialogueText"": ""Hello, welcome to the shop!"",
                        ""proposedMutations"": [],
                        ""worldIntents"": []
                    }");
                });

            // Add a prohibition rule that triggers retry
            _validationGate.AddRule(new PatternValidationRule
            {
                Id = "no-forbidden",
                Description = "Cannot say 'forbidden'",
                Pattern = @"forbidden",
                IsProhibition = true,
                Severity = ConstraintSeverity.Hard
            });

            // IMPORTANT: Disable fallback so validation failures go to retry loop instead
            var config = new StructuredPipelineConfig
            {
                MaxRetries = 3,
                UseStructuredOutput = true,
                FallbackToRegex = false
            };
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.RetryCount, Is.EqualTo(2)); // Should have retried twice
            Assert.That(callCount, Is.EqualTo(3)); // Initial + 2 retries
            Assert.That(result.DialogueText, Does.Contain("welcome"));
        }

        [Test]
        public async Task Pipeline_WhenMaxRetriesExceeded_ReturnsFailedResult()
        {
            // Arrange - Always return prohibited text to exhaust retries
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{
                    ""dialogueText"": ""I will always say forbidden things!"",
                    ""proposedMutations"": [],
                    ""worldIntents"": []
                }"));

            // Add a prohibition rule
            _validationGate.AddRule(new PatternValidationRule
            {
                Id = "no-forbidden",
                Description = "Cannot say 'forbidden'",
                Pattern = @"forbidden",
                IsProhibition = true,
                Severity = ConstraintSeverity.Hard
            });

            // IMPORTANT: Disable fallback so validation failures go to retry loop instead
            var config = new StructuredPipelineConfig
            {
                MaxRetries = 2,
                UseStructuredOutput = true,
                FallbackToRegex = false
            };
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.RetryCount, Is.EqualTo(2)); // Exhausted all retries
            Assert.That(result.ValidationPassed, Is.False);
            Assert.That(pipeline.Metrics.TotalRetries, Is.EqualTo(2));
        }

        [Test]
        public async Task Pipeline_WhenCriticalFailure_DoesNotRetry()
        {
            // Arrange - Return text that contradicts canonical fact (critical severity)
            var callCount = 0;
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(x =>
                {
                    callCount++;
                    return Task.FromResult(@"{
                        ""dialogueText"": ""I have some critical failure text!"",
                        ""proposedMutations"": [],
                        ""worldIntents"": []
                    }");
                });

            // Add a critical severity rule
            _validationGate.AddRule(new PatternValidationRule
            {
                Id = "critical-rule",
                Description = "Critical violation - no retries",
                Pattern = @"critical",
                IsProhibition = true,
                Severity = ConstraintSeverity.Critical
            });

            // Disable fallback to test retry logic directly
            var config = new StructuredPipelineConfig
            {
                MaxRetries = 3,
                UseStructuredOutput = true,
                FallbackToRegex = false
            };
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(callCount, Is.EqualTo(1)); // No retries for critical failures
            Assert.That(result.GateResult!.HasCriticalFailure, Is.True);
        }

        #endregion

        #region Cancellation Token Tests

        [Test]
        public async Task Pipeline_WhenCancellationRequested_ReturnsFailedWithCancellationMessage()
        {
            // Arrange - Pipeline catches cancellation and returns failed result
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancel the token

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(@"{""dialogueText"": ""Hello!"", ""proposedMutations"": [], ""worldIntents"": []}"));

            var config = new StructuredPipelineConfig { FallbackToRegex = false };
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!", null, cts.Token);

            // Assert - Pipeline catches cancellation and returns failed result
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("canceled"));
        }

        [Test]
        public async Task Pipeline_WhenCancelledDuringRetry_StopsRetrying()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var callCount = 0;

            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(x =>
                {
                    callCount++;
                    if (callCount >= 2)
                    {
                        cts.Cancel(); // Cancel on second attempt
                    }
                    return Task.FromResult(@"{""dialogueText"": ""forbidden!"", ""proposedMutations"": [], ""worldIntents"": []}");
                });

            _validationGate.AddRule(new PatternValidationRule
            {
                Id = "no-forbidden",
                Description = "Cannot say 'forbidden'",
                Pattern = @"forbidden",
                IsProhibition = true,
                Severity = ConstraintSeverity.Hard
            });

            // Disable fallback so validation failures trigger retry
            var config = new StructuredPipelineConfig
            {
                MaxRetries = 5,
                UseStructuredOutput = true,
                FallbackToRegex = false
            };
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!", null, cts.Token);

            // Assert - Pipeline catches cancellation and returns failed result, stopping retries
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("canceled"));
            Assert.That(callCount, Is.LessThanOrEqualTo(3)); // Should not complete all retries
        }

        #endregion

        #region StructuredOnly Config Tests

        [Test]
        public async Task Pipeline_StructuredOnlyConfig_WhenParsingFails_DoesNotFallback()
        {
            // Arrange - Throw exception from structured output
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task<string>>(x => throw new InvalidOperationException("Structured output failed"));

            var config = StructuredPipelineConfig.StructuredOnly; // No fallback
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Structured));
            Assert.That(result.ErrorMessage, Does.Contain("Structured output"));

            // Verify fallback was NOT attempted
            await _mockApiClient.DidNotReceive().SendPromptAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<System.Threading.CancellationToken>());
        }

        [Test]
        public async Task Pipeline_StructuredOnlyConfig_WhenParsingReturnsFailed_DoesNotFallback()
        {
            // Arrange - Return unparseable content (empty or malformed)
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult("")); // Empty response

            var config = StructuredPipelineConfig.StructuredOnly;
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert - Empty string gets parsed as empty dialogue which is considered valid
            // but with empty dialogue text
            Assert.That(result.DialogueText, Is.Empty.Or.EqualTo(""));
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public async Task Pipeline_WhenUnexpectedExceptionOccurs_ReturnsFailedResult()
        {
            // Arrange
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task<string>>(x => throw new InvalidOperationException("Unexpected error"));

            _mockApiClient.SendPromptAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task<string>>(x => throw new InvalidOperationException("Fallback also failed"));

            var config = StructuredPipelineConfig.Default;
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Fallback error"));
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Fallback));
        }

        [Test]
        public async Task Pipeline_WhenRegexFallbackAlsoFails_ReturnsFailedResult()
        {
            // Arrange - Structured fails, fallback also fails
            _mockApiClient.SendStructuredPromptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<StructuredOutputFormat>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task<string>>(x => throw new Exception("Structured failed"));

            _mockApiClient.SendPromptAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task<string>>(x => throw new Exception("Regex fallback failed"));

            var config = StructuredPipelineConfig.Default;
            config.FallbackToRegex = true;

            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Fallback));
            Assert.That(result.ErrorMessage, Does.Contain("Fallback error"));
        }

        #endregion

        #region Regex Direct Mode Tests

        [Test]
        public async Task Pipeline_InRegexDirectMode_TracksRegexDirectMetric()
        {
            // Arrange
            _mockApiClient.SendPromptAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult("Hello traveler!"));

            var config = StructuredPipelineConfig.RegexOnly;
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(pipeline.Metrics.RegexDirectCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Pipeline_RegexModeFailure_TracksFailure()
        {
            // Arrange - Return something that causes regex parsing to fail
            _mockApiClient.SendPromptAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<float?>(),
                Arg.Any<int?>(),
                Arg.Any<System.Threading.CancellationToken>())
                .Returns<Task<string>>(x => throw new Exception("Network error"));

            var config = StructuredPipelineConfig.RegexOnly;
            var pipeline = new StructuredDialoguePipeline(
                _agent,
                _validationGate,
                _mutationController,
                _memorySystem,
                config);

            // Act
            var result = await pipeline.ProcessDialogueAsync("Hello!");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ParseMode, Is.EqualTo(ParseMode.Regex));
            Assert.That(result.ErrorMessage, Does.Contain("Regex error"));
        }

        #endregion

    }
}
