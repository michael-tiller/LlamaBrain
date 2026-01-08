using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Persona;

namespace LlamaBrain.Tests
{
  /// <summary>
  /// Tests for the BrainAgent high-level agent orchestrator
  /// </summary>
  [TestFixture]
  public class BrainAgentTests
  {
    private PersonaProfile _testProfile = null!;
    private IApiClient _apiClient = null!;
    private PersonaMemoryStore _memoryStore = null!;

    [SetUp]
    public void SetUp()
    {
      _testProfile = PersonaProfile.Create("test-persona", "Test Persona");
      _testProfile.Description = "A test persona";
      _testProfile.SystemPrompt = "You are a helpful assistant.";
      _apiClient = Substitute.For<IApiClient>();
      _memoryStore = new PersonaMemoryStore();
    }

    #region Constructor Tests

    [Test]
    public void BrainAgent_Constructor_WithValidParameters_CreatesCorrectly()
    {
      // Arrange & Act
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Assert
      Assert.IsNotNull(agent);
      Assert.AreEqual(_testProfile, agent.Profile);
      Assert.IsNotNull(agent.DialogueSession);
      Assert.IsNotNull(agent.MemoryStore);
      Assert.AreEqual(_testProfile.PersonaId, agent.DialogueSession.PersonaId);
    }

    [Test]
    public void BrainAgent_Constructor_WithNullProfile_ThrowsArgumentNullException()
    {
      // Arrange & Act & Assert
      var exception = Assert.Throws<ArgumentNullException>(() => new BrainAgent(null!, _apiClient, _memoryStore));
      Assert.IsNotNull(exception);
      Assert.AreEqual("profile", exception!.ParamName);
    }

    [Test]
    public void BrainAgent_Constructor_WithNullApiClient_ThrowsArgumentNullException()
    {
      // Arrange & Act & Assert
      var exception = Assert.Throws<ArgumentNullException>(() => new BrainAgent(_testProfile, null!, _memoryStore));
      Assert.IsNotNull(exception);
      Assert.AreEqual("apiClient", exception!.ParamName);
    }

    [Test]
    public void BrainAgent_Constructor_WithNullMemoryStore_CreatesDefaultMemoryStore()
    {
      // Arrange & Act
      var agent = new BrainAgent(_testProfile, _apiClient, null);

      // Assert
      Assert.IsNotNull(agent);
      Assert.IsNotNull(agent.MemoryStore);
    }

    #endregion

    #region SendMessageAsync Tests




    [Test]
    public void SendMessageAsync_WithNullMessage_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendMessageAsync(null!));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Message cannot be null or empty"));
    }

    [Test]
    public void SendMessageAsync_WithEmptyMessage_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendMessageAsync(""));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Message cannot be null or empty"));
    }

    [Test]
    public void SendMessageAsync_WithWhitespaceMessage_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendMessageAsync("   "));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Message cannot be null or empty"));
    }

    [Test]
    public void SendMessageAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await agent.SendMessageAsync("Test"));
      Assert.IsNotNull(exception);
      Assert.AreEqual(nameof(BrainAgent), exception!.ObjectName);
    }

    #endregion

    #region SendSimpleMessageAsync Tests

    [Test]
    public async Task SendSimpleMessageAsync_WithValidMessage_ReturnsResponse()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var expectedResponse = "Simple response";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(expectedResponse));

      // Act
      var result = await agent.SendSimpleMessageAsync("Hello");

      // Assert
      Assert.AreEqual(expectedResponse, result);
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
      // Simple message should not add to dialogue history
      var history = agent.GetConversationHistory();
      Assert.AreEqual(0, history.Count);
    }

    [Test]
    public async Task SendSimpleMessageAsync_WithCancellationToken_PropagatesToApiClient()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var cts = new CancellationTokenSource();
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Response"));

      // Act
      await agent.SendSimpleMessageAsync("Hello", cancellationToken: cts.Token);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), cts.Token);
    }

    [Test]
    public async Task SendSimpleMessageAsync_WhenApiClientThrows_PropagatesException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<string>(new Exception("API Error")));

      // Act & Assert
      var ex = Assert.ThrowsAsync<Exception>(async () => await agent.SendSimpleMessageAsync("Hello"));
      Assert.That(ex!.Message, Does.Contain("API Error"));
    }

    [Test]
    public void SendSimpleMessageAsync_WithNullMessage_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendSimpleMessageAsync(null!));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Message cannot be null or empty"));
    }

    [Test]
    public void SendSimpleMessageAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await agent.SendSimpleMessageAsync("Test"));
      Assert.IsNotNull(exception);
    }

    #endregion

    #region SendInstructionAsync Tests

    [Test]
    public async Task SendInstructionAsync_WithValidInstruction_ReturnsResponse()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var expectedResponse = "Instruction response";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(expectedResponse));

      // Act
      var result = await agent.SendInstructionAsync("Do something");

      // Assert
      Assert.AreEqual(expectedResponse, result);
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendInstructionAsync_WithContext_IncludesContextInPrompt()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var expectedResponse = "Response with context";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(expectedResponse));

      // Act
      await agent.SendInstructionAsync("Do something", "Additional context");

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Is<string>(p => p.Contains("Additional context")), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendInstructionAsync_WithCancellationToken_PropagatesToApiClient()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var cts = new CancellationTokenSource();
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Response"));

      // Act
      await agent.SendInstructionAsync("Do something", context: null, cancellationToken: cts.Token);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), cts.Token);
    }

    [Test]
    public async Task SendInstructionAsync_WhenApiClientThrows_PropagatesException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<string>(new Exception("API Error")));

      // Act & Assert
      var ex = Assert.ThrowsAsync<Exception>(async () => await agent.SendInstructionAsync("Do something"));
      Assert.That(ex!.Message, Does.Contain("API Error"));
    }

    [Test]
    public void SendInstructionAsync_WithNullInstruction_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendInstructionAsync(null!));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Instruction cannot be null or empty"));
    }

    [Test]
    public void SendInstructionAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await agent.SendInstructionAsync("Test"));
      Assert.IsNotNull(exception);
    }

    #endregion

    #region Memory Management Tests

    [Test]
    public void AddMemory_WithValidMemory_AddsToMemoryStore()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act
      agent.AddMemory("Test memory");

      // Assert
      var memories = agent.GetMemories();
      Assert.AreEqual(1, memories.Count);
      Assert.AreEqual("Test memory", memories[0]);
    }

    [Test]
    public void AddMemory_WithNullMemory_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.Throws<ArgumentException>(() => agent.AddMemory(null!));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Memory cannot be null or empty"));
    }

    [Test]
    public void AddMemory_WithEmptyMemory_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.Throws<ArgumentException>(() => agent.AddMemory(""));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Memory cannot be null or empty"));
    }

    [Test]
    public void AddMemory_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.Throws<ObjectDisposedException>(() => agent.AddMemory("Test"));
      Assert.IsNotNull(exception);
    }

    [Test]
    public void GetMemories_ReturnsAllMemories()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.AddMemory("Memory 1");
      agent.AddMemory("Memory 2");
      agent.AddMemory("Memory 3");

      // Act
      var memories = agent.GetMemories();

      // Assert
      Assert.AreEqual(3, memories.Count);
      Assert.Contains("Memory 1", memories.ToList());
      Assert.Contains("Memory 2", memories.ToList());
      Assert.Contains("Memory 3", memories.ToList());
    }

    [Test]
    public void GetMemories_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.Throws<ObjectDisposedException>(() => agent.GetMemories());
      Assert.IsNotNull(exception);
    }

    [Test]
    public void ClearMemories_RemovesAllMemories()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.AddMemory("Memory 1");
      agent.AddMemory("Memory 2");

      // Act
      agent.ClearMemories();

      // Assert
      var memories = agent.GetMemories();
      Assert.AreEqual(0, memories.Count);
    }

    [Test]
    public void ClearMemories_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.Throws<ObjectDisposedException>(() => agent.ClearMemories());
      Assert.IsNotNull(exception);
    }

    #endregion

    #region Dialogue History Tests

    [Test]
    public void ClearDialogueHistory_RemovesAllHistory()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      // Manually add entries to dialogue session without making HTTP calls
      agent.DialogueSession.AppendPlayer("Test message");
      agent.DialogueSession.AppendNpc("Test response");

      // Act
      agent.ClearDialogueHistory();

      // Assert
      var history = agent.GetConversationHistory();
      Assert.AreEqual(0, history.Count);
    }

    [Test]
    public void ClearDialogueHistory_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.Throws<ObjectDisposedException>(() => agent.ClearDialogueHistory());
      Assert.IsNotNull(exception);
    }

    [Test]
    public void GetConversationHistory_ReturnsFormattedHistory()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      // Manually add entries to dialogue session without making HTTP calls
      agent.DialogueSession.AppendPlayer("Message 1");
      agent.DialogueSession.AppendNpc("Response 1");
      agent.DialogueSession.AppendPlayer("Message 2");
      agent.DialogueSession.AppendNpc("Response 2");

      // Act
      var history = agent.GetConversationHistory();

      // Assert
      Assert.GreaterOrEqual(history.Count, 4); // 2 player messages + 2 NPC responses
      Assert.That(history[0], Contains.Substring("Player"));
      Assert.That(history[0], Contains.Substring("Message 1"));
      Assert.That(history[1], Contains.Substring("NPC"));
    }

    [Test]
    public void GetConversationHistory_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.Throws<ObjectDisposedException>(() => agent.GetConversationHistory());
      Assert.IsNotNull(exception);
    }

    [Test]
    public void GetRecentHistory_ReturnsRecentEntries()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      // Manually add entries to dialogue session without making HTTP calls
      agent.DialogueSession.AppendPlayer("Message 1");
      agent.DialogueSession.AppendNpc("Response 1");
      agent.DialogueSession.AppendPlayer("Message 2");
      agent.DialogueSession.AppendNpc("Response 2");
      agent.DialogueSession.AppendPlayer("Message 3");
      agent.DialogueSession.AppendNpc("Response 3");

      // Act
      var recent = agent.GetRecentHistory(2);

      // Assert
      Assert.GreaterOrEqual(recent.Count, 2);
      Assert.That(recent[0].Text, Contains.Substring("Message 3"));
    }

    [Test]
    public void GetRecentHistory_WithZeroCount_ReturnsEmptyList()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.DialogueSession.AppendPlayer("Message 1");
      agent.DialogueSession.AppendNpc("Response 1");

      // Act
      var recent = agent.GetRecentHistory(0);

      // Assert
      Assert.AreEqual(0, recent.Count);
    }

    [Test]
    public void GetRecentHistory_WithNegativeCount_ReturnsEmptyList()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.DialogueSession.AppendPlayer("Message 1");

      // Act
      var recent = agent.GetRecentHistory(-1);

      // Assert
      Assert.AreEqual(0, recent.Count);
    }

    [Test]
    public void GetRecentHistory_WithCountGreaterThanHistory_ReturnsAllHistory()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.DialogueSession.AppendPlayer("Message 1");
      agent.DialogueSession.AppendNpc("Response 1");

      // Act
      var recent = agent.GetRecentHistory(100);

      // Assert
      Assert.GreaterOrEqual(recent.Count, 2);
    }

    [Test]
    public void GetRecentHistory_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();

      // Act & Assert
      var exception = Assert.Throws<ObjectDisposedException>(() => agent.GetRecentHistory(5));
      Assert.IsNotNull(exception);
    }

    #endregion

    #region Profile Update Tests

    [Test]
    public void UpdateProfile_WithValidProfile_UpdatesProperties()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var newProfile = PersonaProfile.Create(_testProfile.PersonaId, "Updated Name");
      newProfile.Description = "Updated Description";
      newProfile.SystemPrompt = "Updated Prompt";
      newProfile.SetTrait("Personality", "New Trait");

      // Act
      agent.UpdateProfile(newProfile);

      // Assert
      Assert.AreEqual("Updated Name", agent.Profile.Name);
      Assert.AreEqual("Updated Description", agent.Profile.Description);
      Assert.AreEqual("Updated Prompt", agent.Profile.SystemPrompt);
      Assert.AreEqual("New Trait", agent.Profile.GetTrait("Personality"));
    }

    [Test]
    public void UpdateProfile_WithNullProfile_ThrowsArgumentNullException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.Throws<ArgumentNullException>(() => agent.UpdateProfile(null!));
      Assert.IsNotNull(exception);
      Assert.AreEqual("newProfile", exception!.ParamName);
    }

    [Test]
    public void UpdateProfile_WithDifferentPersonaId_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var newProfile = PersonaProfile.Create("different-id", "Name");

      // Act & Assert
      var exception = Assert.Throws<ArgumentException>(() => agent.UpdateProfile(newProfile));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("New profile must have the same PersonaId"));
    }

    [Test]
    public void UpdateProfile_WithEmptyName_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var newProfile = PersonaProfile.Create(_testProfile.PersonaId, "");
      newProfile.Name = "";

      // Act & Assert
      var exception = Assert.Throws<ArgumentException>(() => agent.UpdateProfile(newProfile));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Profile name cannot be null or empty"));
    }

    [Test]
    public void UpdateProfile_WithEmptyPersonaId_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var newProfile = PersonaProfile.Create("", "Name");
      newProfile.PersonaId = "";

      // Act & Assert
      var exception = Assert.Throws<ArgumentException>(() => agent.UpdateProfile(newProfile));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Does.Contain("Profile PersonaId cannot be null or empty"));
    }

    [Test]
    public void UpdateProfile_WithNullPersonaId_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var newProfile = PersonaProfile.Create("test-persona", "Name");
      newProfile.PersonaId = null!;

      // Act & Assert
      var exception = Assert.Throws<ArgumentException>(() => agent.UpdateProfile(newProfile));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Does.Contain("Profile PersonaId cannot be null or empty"));
    }

    [Test]
    public void UpdateProfile_UpdatesAllProperties()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var newProfile = PersonaProfile.Create(_testProfile.PersonaId, "New Name");
      newProfile.Description = "New Description";
      newProfile.Background = "New Background";
      newProfile.SystemPrompt = "New Prompt";
      newProfile.UseMemory = false;
      newProfile.SetTrait("Trait1", "Value1");
      newProfile.SetTrait("Trait2", "Value2");
      newProfile.SetMetadata("Meta1", "MetaValue1");

      // Act
      agent.UpdateProfile(newProfile);

      // Assert
      Assert.AreEqual("New Name", agent.Profile.Name);
      Assert.AreEqual("New Description", agent.Profile.Description);
      Assert.AreEqual("New Background", agent.Profile.Background);
      Assert.AreEqual("New Prompt", agent.Profile.SystemPrompt);
      Assert.AreEqual(false, agent.Profile.UseMemory);
      Assert.AreEqual("Value1", agent.Profile.GetTrait("Trait1"));
      Assert.AreEqual("Value2", agent.Profile.GetTrait("Trait2"));
      Assert.AreEqual("MetaValue1", agent.Profile.GetMetadata("Meta1"));
    }

    [Test]
    public void UpdateProfile_ClearsOldTraits()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      _testProfile.SetTrait("OldTrait", "OldValue");
      var newProfile = PersonaProfile.Create(_testProfile.PersonaId, "New Name");
      newProfile.SetTrait("NewTrait", "NewValue");

      // Act
      agent.UpdateProfile(newProfile);

      // Assert
      Assert.IsNull(agent.Profile.GetTrait("OldTrait"));
      Assert.AreEqual("NewValue", agent.Profile.GetTrait("NewTrait"));
    }

    [Test]
    public void UpdateProfile_ClearsOldMetadata()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      _testProfile.SetMetadata("OldMeta", "OldValue");
      var newProfile = PersonaProfile.Create(_testProfile.PersonaId, "New Name");
      newProfile.SetMetadata("NewMeta", "NewValue");

      // Act
      agent.UpdateProfile(newProfile);

      // Assert
      Assert.IsNull(agent.Profile.GetMetadata("OldMeta"));
      Assert.AreEqual("NewValue", agent.Profile.GetMetadata("NewMeta"));
    }

    [Test]
    public void UpdateProfile_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();
      var newProfile = PersonaProfile.Create(_testProfile.PersonaId, "Name");

      // Act & Assert
      var exception = Assert.Throws<ObjectDisposedException>(() => agent.UpdateProfile(newProfile));
      Assert.IsNotNull(exception);
    }

    #endregion

    #region Structured Message Tests

    [Test]
    public void SendStructuredMessageAsync_WithNullMessage_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendStructuredMessageAsync(null!, schema));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Message cannot be null or empty"));
    }

    [Test]
    public void SendStructuredMessageAsync_WithEmptyMessage_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendStructuredMessageAsync("", schema));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Message cannot be null or empty"));
    }

    [Test]
    public void SendStructuredMessageAsync_WithWhitespaceMessage_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendStructuredMessageAsync("   ", schema));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Message cannot be null or empty"));
    }

    [Test]
    public void SendStructuredMessageAsync_WithNullSchema_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendStructuredMessageAsync("Test", null!));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("JSON schema cannot be null or empty"));
    }

    [Test]
    public void SendStructuredMessageAsync_WithEmptySchema_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendStructuredMessageAsync("Test", ""));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("JSON schema cannot be null or empty"));
    }

    [Test]
    public async Task SendStructuredMessageAsync_WithValidInputs_ReturnsCleanedJson()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}";
      var rawResponse = "Here's the JSON: {\"name\":\"test\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(rawResponse));

      // Act
      var result = await agent.SendStructuredMessageAsync("Get name", schema);

      // Assert
      Assert.AreEqual("{\"name\":\"test\"}", result);
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendStructuredMessageAsync_WithPureJsonResponse_ReturnsJson()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      var jsonResponse = "{\"key\":\"value\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(jsonResponse));

      // Act
      var result = await agent.SendStructuredMessageAsync("Test", schema);

      // Assert
      Assert.AreEqual(jsonResponse, result);
    }

    [Test]
    public async Task SendStructuredMessageAsync_WithCancellationToken_PropagatesToApiClient()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      var cts = new CancellationTokenSource();
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"result\":\"ok\"}"));

      // Act
      await agent.SendStructuredMessageAsync("Test", schema, cancellationToken: cts.Token);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), cts.Token);
    }

    [Test]
    public async Task SendStructuredMessageAsync_WhenResponseStartsWithError_ThrowsInvalidOperationException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Error: Something went wrong"));

      // Act & Assert
      var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await agent.SendStructuredMessageAsync("Test", schema));
      Assert.That(ex!.Message, Does.Contain("LLM returned an error"));
    }

    [Test]
    public async Task SendStructuredMessageAsync_WhenResponseHasNoJson_ThrowsInvalidOperationException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("This is just text with no JSON"));

      // Act & Assert
      var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await agent.SendStructuredMessageAsync("Test", schema));
      Assert.That(ex!.Message, Does.Contain("No JSON object found"));
    }

    [Test]
    public async Task SendStructuredMessageAsync_WhenResponseHasInvalidJson_ThrowsInvalidOperationException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"key\":\"value\"")); // Missing closing brace

      // Act & Assert
      var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await agent.SendStructuredMessageAsync("Test", schema));
      Assert.That(ex!.Message, Does.Contain("Incomplete JSON object"));
    }

    [Test]
    public async Task SendStructuredMessageAsync_WhenResponseIsEmpty_ThrowsInvalidOperationException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(""));

      // Act & Assert
      var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await agent.SendStructuredMessageAsync("Test", schema));
      Assert.That(ex!.Message, Does.Contain("Empty response"));
    }

    [Test]
    public async Task SendStructuredMessageAsync_WhenResponseHasNestedJson_ExtractsCorrectly()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      var rawResponse = "Prefix text {\"outer\":{\"inner\":\"value\"}} suffix text";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(rawResponse));

      // Act
      var result = await agent.SendStructuredMessageAsync("Test", schema);

      // Assert
      Assert.AreEqual("{\"outer\":{\"inner\":\"value\"}}", result);
    }

    [Test]
    public async Task SendStructuredMessageAsync_WhenApiClientThrows_PropagatesException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<string>(new Exception("API Error")));

      // Act & Assert
      var ex = Assert.ThrowsAsync<Exception>(async () => await agent.SendStructuredMessageAsync("Test", schema));
      Assert.That(ex!.Message, Does.Contain("API Error"));
    }

    [Test]
    public void SendStructuredMessageAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();
      var schema = "{\"type\":\"object\"}";

      // Act & Assert
      var exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await agent.SendStructuredMessageAsync("Test", schema));
      Assert.IsNotNull(exception);
    }

    [Test]
    public async Task SendStructuredInstructionAsync_WhenResponseStartsWithError_ThrowsInvalidOperationException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Error: Something went wrong"));

      // Act & Assert
      var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await agent.SendStructuredInstructionAsync("Test", schema));
      Assert.That(ex!.Message, Does.Contain("LLM returned an error"));
    }

    [Test]
    public async Task SendStructuredInstructionAsync_WhenApiClientThrows_PropagatesException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<string>(new Exception("API Error")));

      // Act & Assert
      var ex = Assert.ThrowsAsync<Exception>(async () => await agent.SendStructuredInstructionAsync("Test", schema));
      Assert.That(ex!.Message, Does.Contain("API Error"));
    }

    [Test]
    public void SendStructuredInstructionAsync_WithNullInstruction_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendStructuredInstructionAsync(null!, schema));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Does.Contain("Instruction cannot be null or empty"));
    }

    [Test]
    public void SendStructuredInstructionAsync_WithEmptyInstruction_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendStructuredInstructionAsync("", schema));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Does.Contain("Instruction cannot be null or empty"));
    }

    [Test]
    public void SendStructuredInstructionAsync_WithNullSchema_ThrowsArgumentException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act & Assert
      var exception = Assert.ThrowsAsync<ArgumentException>(async () => await agent.SendStructuredInstructionAsync("Test", null!));
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Does.Contain("JSON schema cannot be null or empty"));
    }

    [Test]
    public async Task SendStructuredInstructionAsync_WithValidInputs_ReturnsCleanedJson()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"}}}";
      var rawResponse = "Response: {\"result\":\"success\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(rawResponse));

      // Act
      var result = await agent.SendStructuredInstructionAsync("Do something", schema);

      // Assert
      Assert.AreEqual("{\"result\":\"success\"}", result);
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendStructuredInstructionAsync_WithContext_IncludesContext()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"result\":\"ok\"}"));

      // Act
      await agent.SendStructuredInstructionAsync("Do something", schema, "Additional context");

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Is<string>(p => p.Contains("Additional context")), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendStructuredInstructionAsync_WithCancellationToken_PropagatesToApiClient()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\"}";
      var cts = new CancellationTokenSource();
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"result\":\"ok\"}"));

      // Act
      await agent.SendStructuredInstructionAsync("Test", schema, context: null, cancellationToken: cts.Token);

      // Assert
      _ = _apiClient.Received(1).SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), cts.Token);
    }

    [Test]
    public void SendStructuredInstructionAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.Dispose();
      var schema = "{\"type\":\"object\"}";

      // Act & Assert
      var exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await agent.SendStructuredInstructionAsync("Test", schema));
      Assert.IsNotNull(exception);
    }

    [Test]
    public async Task SendStructuredMessageAsync_Generic_DeserializesToType()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"name\":\"test\"}"));

      // Act
      var result = await agent.SendStructuredMessageAsync<TestResponse>("Get name", schema);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual("test", result!.Name);
    }

    [Test]
    public async Task SendStructuredInstructionAsync_Generic_DeserializesToType()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\",\"properties\":{\"value\":{\"type\":\"string\"}}}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"value\":\"result\"}"));

      // Act
      var result = await agent.SendStructuredInstructionAsync<TestResponse2>("Do something", schema);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual("result", result!.Value);
    }

    [Test]
    public async Task SendStructuredInstructionAsync_Generic_WithContext_DeserializesToType()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var schema = "{\"type\":\"object\",\"properties\":{\"value\":{\"type\":\"string\"}}}";
      _apiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("{\"value\":\"result\"}"));

      // Act
      var result = await agent.SendStructuredInstructionAsync<TestResponse2>("Do something", schema, "Context");

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual("result", result!.Value);
    }

    #endregion

    #region Helper Classes for Testing

    private class TestResponse
    {
      public string Name { get; set; } = string.Empty;
    }

    private class TestResponse2
    {
      public string Value { get; set; } = string.Empty;
    }

    #endregion

    #region KV Cache Tests

    [Test]
    public void EnableKvCaching_DefaultsToFalse()
    {
      // Arrange & Act
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Assert
      Assert.IsFalse(agent.EnableKvCaching);
    }

    [Test]
    public void EnableKvCaching_WhenSetToTrue_EnablesKvCacheConfig()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act
      agent.EnableKvCaching = true;

      // Assert
      Assert.IsTrue(agent.EnableKvCaching);
      Assert.IsTrue(agent.KvCacheConfig.EnableCaching);
    }

    [Test]
    public void KvCacheConfig_CanBeSetDirectly()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      var config = new LlamaBrain.Core.Inference.KvCacheConfig
      {
        EnableCaching = true,
        NKeepTokens = 500
      };

      // Act
      agent.KvCacheConfig = config;

      // Assert
      Assert.IsTrue(agent.EnableKvCaching);
      Assert.AreEqual(500, agent.KvCacheConfig.NKeepTokens);
    }

    [Test]
    public async Task SendMessageAsync_WithKvCachingEnabled_PassesNKeepToApiClient()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.EnableKvCaching = true;

      var expectedMetrics = new CompletionMetrics { Content = "Test response" };
      int? capturedNKeep = null;

      _apiClient.SendPromptWithMetricsAsync(
          Arg.Any<string>(),
          Arg.Any<int?>(),
          Arg.Any<float?>(),
          Arg.Any<int?>(),
          Arg.Any<bool>(),
          Arg.Do<int?>(n => capturedNKeep = n),
          Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(expectedMetrics));

      // Act
      await agent.SendMessageAsync("Hello");

      // Assert
      Assert.IsNotNull(capturedNKeep, "nKeep should be passed to API client");
      Assert.Greater(capturedNKeep!.Value, 0, "nKeep should be calculated from static prefix");
    }

    [Test]
    public async Task SendMessageAsync_WithKvCachingEnabled_UsesConfiguredNKeepTokens()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.KvCacheConfig = new LlamaBrain.Core.Inference.KvCacheConfig
      {
        EnableCaching = true,
        NKeepTokens = 200  // Explicit override
      };

      var expectedMetrics = new CompletionMetrics { Content = "Test response" };
      int? capturedNKeep = null;

      _apiClient.SendPromptWithMetricsAsync(
          Arg.Any<string>(),
          Arg.Any<int?>(),
          Arg.Any<float?>(),
          Arg.Any<int?>(),
          Arg.Any<bool>(),
          Arg.Do<int?>(n => capturedNKeep = n),
          Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(expectedMetrics));

      // Act
      await agent.SendMessageAsync("Hello");

      // Assert
      Assert.AreEqual(200, capturedNKeep, "nKeep should use configured NKeepTokens value");
    }

    [Test]
    public async Task SendMessageAsync_WithKvCachingDisabled_DoesNotCallMetricsApi()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);
      agent.EnableKvCaching = false;

      _apiClient.SendPromptAsync(
          Arg.Any<string>(),
          Arg.Any<int?>(),
          Arg.Any<float?>(),
          Arg.Any<int?>(),
          Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Test response"));

      // Act
      await agent.SendMessageAsync("Hello");

      // Assert
      await _apiClient.DidNotReceive().SendPromptWithMetricsAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        Arg.Any<int?>(),
        Arg.Any<bool>(),
        Arg.Any<int?>(),
        Arg.Any<CancellationToken>());

      await _apiClient.Received(1).SendPromptAsync(
        Arg.Any<string>(),
        Arg.Any<int?>(),
        Arg.Any<float?>(),
        Arg.Any<int?>(),
        Arg.Any<CancellationToken>());
    }

    #endregion

    #region Disposal Tests

    [Test]
    public void Dispose_DisposesApiClient()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act
      agent.Dispose();

      // Assert
      // Verify that operations fail after disposal
      var exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await agent.SendMessageAsync("Test"));
      Assert.IsNotNull(exception);
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
      // Arrange
      var agent = new BrainAgent(_testProfile, _apiClient, _memoryStore);

      // Act
      agent.Dispose();
      agent.Dispose(); // Should not throw

      // Assert
      // Verify that operations fail after disposal
      var exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await agent.SendMessageAsync("Test"));
      Assert.IsNotNull(exception);
    }

    #endregion
  }
}

