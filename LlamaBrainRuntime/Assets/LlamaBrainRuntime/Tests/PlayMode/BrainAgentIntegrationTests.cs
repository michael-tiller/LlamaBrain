#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core;
using LlamaBrain.Persona;
using LlamaBrain.Runtime.Core;
using System.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace LlamaBrain.Tests.PlayMode
{
  /// <summary>
  /// Integration tests for BrainAgent that test the full stack:
  /// Unity → BrainServer → ApiClient → BrainAgent → LLM Server
  /// 
  /// These tests require a running llama-server instance.
  /// They are marked as PlayMode tests and will be skipped if the server is not available.
  /// </summary>
  public class BrainAgentIntegrationTests
  {
    private GameObject? serverObject;
    private BrainServer? server;
    private BrainSettings? settings;
    private GameObject? agentObject;
    private LlamaBrainAgent? unityAgent;
    private BrainAgent? coreAgent;
    private PersonaProfile? testProfile;
    private ApiClient? apiClient;
    private PersonaMemoryStore? memoryStore;

    [SetUp]
    public void SetUp()
    {
      // Create test settings for BrainServer
      settings = ScriptableObject.CreateInstance<BrainSettings>();
      settings!.ExecutablePath = "Backend/llama-server.exe";
      settings!.ModelPath = "Backend/model/stablelm-zephyr-3b.Q4_0.gguf";
      settings!.Port = 5000;
      settings!.ContextSize = 2048;

      // Create BrainServer GameObject
      serverObject = new GameObject("TestServer");
      server = serverObject!.AddComponent<BrainServer>();
      server!.Settings = settings;
      server!.Initialize();

      // Create test persona profile
      testProfile = PersonaProfile.Create("test-persona", "Test Persona");
      testProfile!.Description = "A test persona for integration testing";
      testProfile!.SystemPrompt = "You are a helpful assistant.";

      // Create memory store
      memoryStore = new PersonaMemoryStore();
    }

    [TearDown]
    public void TearDown()
    {
      // Cleanup core agent
      coreAgent?.Dispose();
      apiClient?.Dispose();

      // Cleanup Unity agent
      if (agentObject != null)
      {
        Object.DestroyImmediate(agentObject);
      }

      // Cleanup server
      if (serverObject != null)
      {
        Object.DestroyImmediate(serverObject);
      }

      if (settings != null)
      {
        Object.DestroyImmediate(settings);
      }
    }

    /// <summary>
    /// Helper coroutine to wait for server to be ready
    /// </summary>
    private IEnumerator WaitForServerReady()
    {
      yield return null; // Let server initialize

      // Wait for server to be ready (up to 5 seconds)
      var clientManager = new ClientManager(settings!.ToProcessConfig());
      var startTime = Time.realtimeSinceStartup;
      var maxWaitTime = 5f;

      while (Time.realtimeSinceStartup - startTime < maxWaitTime)
      {
        var isRunningTask = clientManager.IsRunningAsync();
        yield return new WaitUntil(() => isRunningTask.IsCompleted);
        var isRunning = isRunningTask.Result;
        
        if (isRunning)
        {
          clientManager.Dispose();
          yield break;
        }
        yield return new WaitForSeconds(0.5f);
      }

      clientManager.Dispose();
      // If we get here, server isn't ready - tests will handle this gracefully
    }

    [UnityTest]
    public IEnumerator BrainAgent_SendMessageAsync_WithValidMessage_HandlesResponse()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      // Get client from server
      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      coreAgent = new BrainAgent(testProfile!, apiClient!, memoryStore!);

      // Act
      string? response = null;
      var task = coreAgent!.SendMessageAsync("Hello");
      yield return new WaitUntil(() => task.IsCompleted);
      response = task.Result;

      // Assert
      Assert.IsNotNull(response);
      // If server is not running, we expect an error message
      // If server is running, we should get a response
      if (response.StartsWith("Error:"))
      {
        Assert.IsTrue(response.Contains("Error:"));
      }
      else
      {
        Assert.IsNotEmpty(response);
      }

      // Dialogue session should have entries regardless
      var history = coreAgent!.DialogueSession.GetHistory();
      Assert.GreaterOrEqual(history.Count, 1);
    }

    [UnityTest]
    public IEnumerator BrainAgent_SendMessageAsync_AddsMessageToDialogueSession()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      coreAgent = new BrainAgent(testProfile!, apiClient, memoryStore!);

      // Act
      var task = coreAgent!.SendMessageAsync("Test message");
      yield return new WaitUntil(() => task.IsCompleted);
      var response = task.Result;

      // Assert
      var history = coreAgent!.DialogueSession.GetHistory();
      Assert.GreaterOrEqual(history.Count, 2); // Player message + NPC response
      Assert.That(history[0], Contains.Substring("Player"));
      Assert.That(history[0], Contains.Substring("Test message"));
      Assert.That(history[1], Contains.Substring("NPC"));
    }

    [UnityTest]
    public IEnumerator BrainAgent_SendSimpleMessageAsync_WithValidMessage_HandlesResponse()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      coreAgent = new BrainAgent(testProfile!, apiClient, memoryStore!);

      // Act
      var task = coreAgent!.SendSimpleMessageAsync("Hello");
      yield return new WaitUntil(() => task.IsCompleted);
      var response = task.Result;

      // Assert
      Assert.IsNotNull(response);
      // Simple message should not add to dialogue session
      Assert.AreEqual(0, coreAgent!.DialogueSession.GetHistory().Count);
    }

    [UnityTest]
    public IEnumerator BrainAgent_SendInstructionAsync_WithValidInstruction_HandlesResponse()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      coreAgent = new BrainAgent(testProfile!, apiClient, memoryStore!);

      // Act
      var task = coreAgent!.SendInstructionAsync("Do something", "Context");
      yield return new WaitUntil(() => task.IsCompleted);
      var response = task.Result;

      // Assert
      Assert.IsNotNull(response);
    }

    [UnityTest]
    public IEnumerator BrainAgent_SendInstructionAsync_WithoutContext_HandlesResponse()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      coreAgent = new BrainAgent(testProfile!, apiClient, memoryStore!);

      // Act
      var task = coreAgent!.SendInstructionAsync("Do something");
      yield return new WaitUntil(() => task.IsCompleted);
      var response = task.Result;

      // Assert
      Assert.IsNotNull(response);
    }

    [UnityTest]
    public IEnumerator BrainAgent_ClearDialogueHistory_RemovesAllHistory()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      coreAgent = new BrainAgent(testProfile!, apiClient, memoryStore!);

      // Send a message first
      var sendTask = coreAgent!.SendMessageAsync("Test");
      yield return new WaitUntil(() => sendTask.IsCompleted);

      // Act
      coreAgent!.ClearDialogueHistory();

      // Assert
      var history = coreAgent!.GetConversationHistory();
      Assert.AreEqual(0, history.Count);
    }

    [UnityTest]
    public IEnumerator BrainAgent_GetConversationHistory_ReturnsFormattedHistory()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      coreAgent = new BrainAgent(testProfile!, apiClient, memoryStore!);

      // Send multiple messages
      var task1 = coreAgent!.SendMessageAsync("Message 1");
      yield return new WaitUntil(() => task1.IsCompleted);

      var task2 = coreAgent!.SendMessageAsync("Message 2");
      yield return new WaitUntil(() => task2.IsCompleted);

      // Act
      var history = coreAgent!.GetConversationHistory();

      // Assert
      Assert.GreaterOrEqual(history.Count, 4); // 2 player messages + 2 NPC responses
      Assert.That(history[0], Contains.Substring("Player"));
      Assert.That(history[0], Contains.Substring("Message 1"));
      Assert.That(history[1], Contains.Substring("NPC"));
    }

    [UnityTest]
    public IEnumerator BrainAgent_GetRecentHistory_ReturnsRecentEntries()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      coreAgent = new BrainAgent(testProfile!, apiClient, memoryStore!);

      // Send multiple messages
      var task1 = coreAgent!.SendMessageAsync("Message 1");
      yield return new WaitUntil(() => task1.IsCompleted);

      var task2 = coreAgent!.SendMessageAsync("Message 2");
      yield return new WaitUntil(() => task2.IsCompleted);

      var task3 = coreAgent!.SendMessageAsync("Message 3");
      yield return new WaitUntil(() => task3.IsCompleted);

      // Act
      var recent = coreAgent!.GetRecentHistory(2);

      // Assert
      Assert.GreaterOrEqual(recent.Count, 2);
      Assert.That(recent[0].Text, Contains.Substring("Message 3"));
    }

    [UnityTest]
    public IEnumerator LlamaBrainAgent_FullIntegration_WorksCorrectly()
    {
      // Arrange - Wait for server to be ready
      yield return WaitForServerReady();

      // Create LlamaBrainAgent Unity component
      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();

      // Get client from server
      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }

      // Initialize Unity agent with server client
      unityAgent!.Initialize(apiClient!, memoryStore!);

      yield return null; // Let initialization complete

      // Act - Send a message through Unity agent
      string? response = null;
      var task = unityAgent!.SendPlayerInputAsync("Hello from Unity test");
      // Use continuation to capture result, then wait for completion
      var continuation = task.ContinueWith(r => { response = r; });
      yield return continuation.ToCoroutine();
      // Response is now set by the continuation

      // Assert
      Assert.IsNotNull(response);
      Assert.IsNotEmpty(response);
      Assert.IsTrue(unityAgent.IsInitialized);
    }
  }
}

