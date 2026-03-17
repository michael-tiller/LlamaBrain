using NUnit.Framework;
using NSubstitute;
using LlamaBrain.Core;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Persona;
using System.Threading;
using System.Threading.Tasks;

namespace LlamaBrain.Tests.Core
{
  /// <summary>
  /// Tests for Feature 14.2: BrainAgent seed parameter integration.
  /// Verifies that seed values are correctly passed from BrainAgent methods
  /// through to the underlying IApiClient calls.
  /// </summary>
  [TestFixture]
  public class BrainAgentSeedIntegrationTests
  {
    private IApiClient _apiClient = null!;
    private PersonaProfile _testProfile = null!;
    private BrainAgent _agent = null!;

    [SetUp]
    public void SetUp()
    {
      _apiClient = Substitute.For<IApiClient>();

      // Setup default returns for SendPromptAsync
      _apiClient.SendPromptAsync(
          Arg.Any<string>(),
          Arg.Any<int?>(),
          Arg.Any<float?>(),
          Arg.Any<int?>(),
          Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Test response"));

      // Setup default returns for SendStructuredPromptAsync
      _apiClient.SendStructuredPromptAsync(
          Arg.Any<string>(),
          Arg.Any<string>(),
          Arg.Any<StructuredOutputFormat>(),
          Arg.Any<int?>(),
          Arg.Any<float?>(),
          Arg.Any<int?>(),
          Arg.Any<bool>(),
          Arg.Any<int?>(),
          Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"dialogue\":\"Hello\",\"mutations\":[],\"intents\":[]}"));

      _testProfile = PersonaProfile.Create("test-npc", "Test NPC");
      _testProfile.Description = "A test NPC for unit tests";
      _testProfile.SystemPrompt = "You are a test NPC.";

      _agent = new BrainAgent(_testProfile, _apiClient);
    }

    [TearDown]
    public void TearDown()
    {
      _agent?.Dispose();
    }

    #region SendMessageAsync Seed Tests

    [Test]
    public async Task SendMessageAsync_WithSeed_PassesSeedToApiClient()
    {
      // Act
      await _agent.SendMessageAsync("Hello", seed: 42);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        42, // seed should be 42
        Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendMessageAsync_WithNullSeed_PassesNullSeedToApiClient()
    {
      // Act
      await _agent.SendMessageAsync("Hello", seed: null);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        null, // seed should be null
        Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendMessageAsync_WithoutSeed_DefaultsToNullSeed()
    {
      // Act
      await _agent.SendMessageAsync("Hello");

      // Assert - should use null seed by default (backward compatible)
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        null, // default seed should be null
        Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendMessageAsync_WithZeroSeed_PassesZeroToApiClient()
    {
      // Act - InteractionCount = 0 is a valid seed for first interaction
      await _agent.SendMessageAsync("Hello", seed: 0);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        0, // seed should be 0
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendSimpleMessageAsync Seed Tests

    [Test]
    public async Task SendSimpleMessageAsync_WithSeed_PassesSeedToApiClient()
    {
      // Act
      await _agent.SendSimpleMessageAsync("Hello", seed: 123);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        123,
        Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendSimpleMessageAsync_WithoutSeed_DefaultsToNullSeed()
    {
      // Act
      await _agent.SendSimpleMessageAsync("Hello");

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        null,
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendInstructionAsync Seed Tests

    [Test]
    public async Task SendInstructionAsync_WithSeed_PassesSeedToApiClient()
    {
      // Act
      await _agent.SendInstructionAsync("Do something", seed: 456);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        456,
        Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendInstructionAsync_WithContextAndSeed_PassesSeedToApiClient()
    {
      // Act
      await _agent.SendInstructionAsync("Do something", context: "Some context", seed: 789);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        789,
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendStructuredMessageAsync Seed Tests

    [Test]
    public async Task SendStructuredMessageAsync_WithSeed_PassesSeedToApiClient()
    {
      // Arrange
      var schema = @"{""type"":""object"",""properties"":{""response"":{""type"":""string""}}}";
      _apiClient.SendPromptAsync(
          Arg.Any<string>(),
          Arg.Any<int?>(),
          Arg.Any<float?>(),
          Arg.Any<int?>(),
          Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"response\":\"Hello\"}"));

      // Act
      await _agent.SendStructuredMessageAsync("Hello", schema, seed: 111);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        111,
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendStructuredInstructionAsync Seed Tests

    [Test]
    public async Task SendStructuredInstructionAsync_WithSeed_PassesSeedToApiClient()
    {
      // Arrange
      var schema = @"{""type"":""object"",""properties"":{""result"":{""type"":""string""}}}";
      _apiClient.SendPromptAsync(
          Arg.Any<string>(),
          Arg.Any<int?>(),
          Arg.Any<float?>(),
          Arg.Any<int?>(),
          Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"result\":\"Done\"}"));

      // Act
      await _agent.SendStructuredInstructionAsync("Do something", schema, seed: 222);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        222,
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendNativeStructuredMessageAsync Seed Tests

    [Test]
    public async Task SendNativeStructuredMessageAsync_WithSeed_PassesSeedToApiClient()
    {
      // Arrange
      var schema = @"{""type"":""object"",""properties"":{""dialogue"":{""type"":""string""}}}";

      // Act
      await _agent.SendNativeStructuredMessageAsync("Hello", schema, seed: 333);

      // Assert
      _ = _apiClient.Received(1).SendStructuredPromptAsync(
        Arg.Any<string>(),
        schema,
        Arg.Any<StructuredOutputFormat>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        333, // seed should be 333
        Arg.Any<bool>(),
        Arg.Any<int?>(),
        Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendNativeStructuredMessageAsync_WithoutSeed_DefaultsToNullSeed()
    {
      // Arrange
      var schema = @"{""type"":""object"",""properties"":{""dialogue"":{""type"":""string""}}}";

      // Act
      await _agent.SendNativeStructuredMessageAsync("Hello", schema);

      // Assert
      _ = _apiClient.Received(1).SendStructuredPromptAsync(
        Arg.Any<string>(),
        schema,
        Arg.Any<StructuredOutputFormat>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        null, // default seed should be null
        Arg.Any<bool>(),
        Arg.Any<int?>(),
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendNativeStructuredInstructionAsync Seed Tests

    [Test]
    public async Task SendNativeStructuredInstructionAsync_WithSeed_PassesSeedToApiClient()
    {
      // Arrange
      var schema = @"{""type"":""object"",""properties"":{""result"":{""type"":""string""}}}";

      // Act
      await _agent.SendNativeStructuredInstructionAsync("Do it", schema, seed: 444);

      // Assert
      _ = _apiClient.Received(1).SendStructuredPromptAsync(
        Arg.Any<string>(),
        schema,
        Arg.Any<StructuredOutputFormat>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        444, // seed should be 444
        Arg.Any<bool>(),
        Arg.Any<int?>(),
        Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendNativeStructuredInstructionAsync_WithContextAndSeed_PassesSeedToApiClient()
    {
      // Arrange
      var schema = @"{""type"":""object"",""properties"":{""result"":{""type"":""string""}}}";

      // Act
      await _agent.SendNativeStructuredInstructionAsync("Do it", schema, context: "ctx", seed: 555);

      // Assert
      _ = _apiClient.Received(1).SendStructuredPromptAsync(
        Arg.Any<string>(),
        schema,
        Arg.Any<StructuredOutputFormat>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        555, // seed should be 555
        Arg.Any<bool>(),
        Arg.Any<int?>(),
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendNativeDialogueAsync Seed Tests

    [Test]
    public async Task SendNativeDialogueAsync_WithSeed_PassesSeedToApiClient()
    {
      // Act
      await _agent.SendNativeDialogueAsync("Hello", seed: 666);

      // Assert - SendNativeDialogueAsync calls SendNativeStructuredMessageAsync internally
      _ = _apiClient.Received(1).SendStructuredPromptAsync(
        Arg.Any<string>(),
        Arg.Any<string>(),
        Arg.Any<StructuredOutputFormat>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        666, // seed should be 666
        Arg.Any<bool>(),
        Arg.Any<int?>(),
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region InteractionCount as Seed Pattern Tests

    [Test]
    public async Task SendMessageAsync_WithInteractionCountAsSeed_WorksCorrectly()
    {
      // Arrange - simulate using InteractionCount as seed
      var interactionCount = 5;

      // Act
      await _agent.SendMessageAsync("Hello", seed: interactionCount);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        5,
        Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MultipleInteractions_WithIncrementingSeeds_PassCorrectSeeds()
    {
      // Act - simulate multiple interactions with incrementing InteractionCount
      await _agent.SendMessageAsync("Hello 1", seed: 0);
      await _agent.SendMessageAsync("Hello 2", seed: 1);
      await _agent.SendMessageAsync("Hello 3", seed: 2);

      // Assert - verify each call had the correct seed
      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        0,
        Arg.Any<CancellationToken>());

      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        1,
        Arg.Any<CancellationToken>());

      _ = _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        2,
        Arg.Any<CancellationToken>());
    }

    #endregion
  }
}
