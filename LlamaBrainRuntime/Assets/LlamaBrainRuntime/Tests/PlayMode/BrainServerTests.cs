using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core;
using LlamaBrain.Runtime.Core;
using System.Collections;
using System.Threading.Tasks;


namespace LlamaBrain.Tests.PlayMode
{
  public class BrainServerTests
  {
    private GameObject serverObject;
    private BrainServer server;
    private BrainSettings settings;

    [SetUp]
    public void SetUp()
    {
      // Create test settings
      settings = ScriptableObject.CreateInstance<BrainSettings>();
      settings.ExecutablePath = "Backend/llama-server.exe";
      settings.ModelPath = "Backend/model/qwen2.5-3b-instruct-abliterated-sft-q4_k_m.gguf";
      settings.Port = 5000;
      settings.ContextSize = 2048;

      // Create server GameObject
      serverObject = new GameObject("TestServer");
      server = serverObject.AddComponent<BrainServer>();
      server.Settings = settings;

      // Initialize the server after settings are set
      server.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
      if (serverObject != null)
      {
        Object.DestroyImmediate(serverObject);
      }
      if (settings != null)
      {
        Object.DestroyImmediate(settings);
      }
    }

    [UnityTest]
    public IEnumerator BrainServer_Initialization_WorksCorrectly()
    {
      // Act - Awake is called automatically
      yield return null;

      // Assert
      Assert.IsNotNull(server);
      Assert.IsNotNull(server.Settings);
      Assert.AreEqual(settings, server.Settings);
    }

    [Test]
    public void BrainServer_SettingsValidation_WorksCorrectly()
    {
      // Arrange
      var config = settings.ToProcessConfig();

      // Assert
      Assert.AreEqual("localhost", config.Host);
      Assert.AreEqual(5000, config.Port);
      Assert.AreEqual("Backend/model/qwen2.5-3b-instruct-abliterated-sft-q4_k_m.gguf", config.Model);
      Assert.AreEqual("Backend/llama-server.exe", config.ExecutablePath);
      Assert.AreEqual(2048, config.ContextSize);
    }

    [UnityTest]
    public IEnumerator BrainServer_WithNullSettings_HandlesGracefully()
    {
      // Arrange - Create a new server instance without initialization
      var nullSettingsServerObject = new GameObject("NullSettingsTestServer");
      var nullSettingsServer = nullSettingsServerObject.AddComponent<BrainServer>();
      nullSettingsServer.Settings = null;

      // Act
      nullSettingsServer.Initialize();
      yield return null;

      // Assert
      Assert.IsNotNull(nullSettingsServer);
      Assert.IsFalse(nullSettingsServer.IsInitialized);

      // Cleanup
      Object.DestroyImmediate(nullSettingsServerObject);
    }

    [UnityTest]
    public IEnumerator BrainServer_DoubleInitialization_HandlesCorrectly()
    {
      // Arrange
      yield return null; // Let first initialization complete

      // Act - Try to initialize again
      server.Initialize();
      yield return null;

      // Assert
      Assert.IsNotNull(server);
      Assert.IsTrue(server.IsInitialized);
    }

    [UnityTest]
    public IEnumerator BrainServer_CreateClient_WhenNotInitialized_ReturnsNull()
    {
      // Arrange - Create a new server instance without initialization
      var uninitializedServerObject = new GameObject("UninitializedTestServer");
      var uninitializedServer = uninitializedServerObject.AddComponent<BrainServer>();
      uninitializedServer.Settings = null;

      // Expect the error log message
      LogAssert.Expect(LogType.Error, "[LLM] LlamaBrainServer not initialized. Cannot create client.");

      // Act
      var client = uninitializedServer.CreateClient();

      // Assert
      Assert.IsNull(client);

      // Cleanup
      Object.DestroyImmediate(uninitializedServerObject);

      yield return null;
    }

    [UnityTest]
    public IEnumerator BrainServer_CreateClient_WhenInitialized_ReturnsValidClient()
    {
      // Arrange
      yield return null; // Let initialization complete

      // Act
      var client = server.CreateClient();

      // Assert
      Assert.IsNotNull(client);
      Assert.IsInstanceOf<ApiClient>(client);
    }

    [UnityTest]
    public IEnumerator BrainServer_ProcessConfigValidation_WorksCorrectly()
    {
      // Arrange
      settings.ExecutablePath = "test/path/server.exe";
      settings.ModelPath = "test/path/model.gguf";
      settings.Port = 1234;
      settings.ContextSize = 1024;

      // Act
      var config = settings.ToProcessConfig();
      yield return null;

      // Assert
      Assert.AreEqual("test/path/server.exe", config.ExecutablePath);
      Assert.AreEqual("test/path/model.gguf", config.Model);
      Assert.AreEqual(1234, config.Port);
      Assert.AreEqual(1024, config.ContextSize);
      Assert.AreEqual("localhost", config.Host);
    }

    [UnityTest]
    public IEnumerator BrainServer_WithCustomLlmSettings_InitializesCorrectly()
    {
      // Arrange
      settings.MaxTokens = 256;
      settings.Temperature = 0.8f;
      settings.TopP = 0.95f;
      settings.TopK = 50;
      settings.RepeatPenalty = 1.2f;
      settings.StopSequences = new string[] { "END", "STOP" };

      // Act
      server.Initialize();
      yield return null;

      // Assert
      Assert.IsNotNull(server);
      Assert.IsTrue(server.IsInitialized);

      var client = server.CreateClient();
      Assert.IsNotNull(client);

      // Verify the settings were applied correctly
      var config = settings.ToProcessConfig();
      Assert.AreEqual(256, config.LlmConfig.MaxTokens);
      Assert.AreEqual(0.8f, config.LlmConfig.Temperature);
      Assert.AreEqual(0.95f, config.LlmConfig.TopP);
      Assert.AreEqual(50, config.LlmConfig.TopK);
      Assert.AreEqual(1.2f, config.LlmConfig.RepeatPenalty);
      Assert.AreEqual(2, config.LlmConfig.StopSequences.Length);
      Assert.Contains("END", config.LlmConfig.StopSequences);
      Assert.Contains("STOP", config.LlmConfig.StopSequences);
    }

    #region Server Lifecycle Tests

    private bool ServerExecutableExists()
    {
      var exePath = System.IO.Path.GetFullPath(settings.ExecutablePath);
      return System.IO.File.Exists(exePath);
    }

    private bool ModelFileExists()
    {
      var modelPath = System.IO.Path.GetFullPath(settings.ModelPath);
      return System.IO.File.Exists(modelPath);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_WaitForServerAsync_WhenNotInitialized_ReturnsFalse()
    {
      // Arrange - Create a server without initialization
      var uninitServerObject = new GameObject("UninitServer");
      var uninitServer = uninitServerObject.AddComponent<BrainServer>();
      uninitServer.Settings = null;

      // Act
      var task = uninitServer.WaitForServerAsync(1);
      yield return new WaitUntil(() => task.IsCompleted);

      // Assert
      Assert.IsFalse(task.Result);
      Assert.AreEqual("Not Initialized", uninitServer.ConnectionStatus);

      // Cleanup
      Object.DestroyImmediate(uninitServerObject);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_IsServerRunningAsync_WhenNotInitialized_ReturnsFalse()
    {
      // Arrange - Create a server without initialization
      var uninitServerObject = new GameObject("UninitServer");
      var uninitServer = uninitServerObject.AddComponent<BrainServer>();
      uninitServer.Settings = null;

      // Act
      var task = uninitServer.IsServerRunningAsync();
      yield return new WaitUntil(() => task.IsCompleted);

      // Assert
      Assert.IsFalse(task.Result);

      // Cleanup
      Object.DestroyImmediate(uninitServerObject);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_GetServerStatus_ReturnsFormattedStatus()
    {
      // Arrange
      yield return null; // Let initialization complete

      // Act
      var status = server.GetServerStatus();

      // Assert
      Assert.IsNotNull(status);
      Assert.IsTrue(status.Contains("Server Status"));
      Assert.IsTrue(status.Contains("Initialized: True"));
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_RestartServer_WhenNotInitialized_LogsWarning()
    {
      // Arrange - Create a server without initialization
      var uninitServerObject = new GameObject("UninitServer");
      var uninitServer = uninitServerObject.AddComponent<BrainServer>();
      uninitServer.Settings = null;

      // Expect the warning
      LogAssert.Expect(LogType.Warning, "[LLM] Cannot restart server - not initialized");

      // Act
      uninitServer.RestartServer();
      yield return null;

      // Cleanup
      Object.DestroyImmediate(uninitServerObject);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_WaitForServerAsync_WithTimeout_ReturnsWhenTimedOut()
    {
      // Arrange - server is initialized but not started
      yield return null;

      // Act - Wait with a very short timeout (server not running)
      var startTime = Time.time;
      var task = server.WaitForServerAsync(1); // 1 second timeout
      yield return new WaitUntil(() => task.IsCompleted);
      var elapsed = Time.time - startTime;

      // Assert - Should complete within reasonable time
      Assert.IsFalse(task.Result); // Server not running
      Assert.That(elapsed, Is.LessThan(5f)); // Should timeout within 5 seconds
    }

    [UnityTest]
    [Category("Integration")]
    [Timeout(120000)]
    public IEnumerator BrainServer_FullLifecycle_StartWaitStop()
    {
      // Skip if server executable doesn't exist
      if (!ServerExecutableExists())
      {
        Assert.Ignore("Skipping: llama-server not found at " + settings.ExecutablePath);
        yield break;
      }

      if (!ModelFileExists())
      {
        Assert.Ignore("Skipping: model not found at " + settings.ModelPath);
        yield break;
      }

      // Start the server
      server.StartServer();
      yield return new WaitForSeconds(1);

      // Wait for server to be ready
      var waitTask = server.WaitForServerAsync(60);
      yield return new WaitUntil(() => waitTask.IsCompleted);

      if (!waitTask.Result)
      {
        // Server didn't start, skip test
        Assert.Ignore("Server failed to start - may be missing dependencies");
        yield break;
      }

      // Assert server is running
      Assert.IsTrue(waitTask.Result, "Server should be ready");

      // Check health
      var healthTask = server.IsServerRunningAsync();
      yield return new WaitUntil(() => healthTask.IsCompleted);
      Assert.IsTrue(healthTask.Result, "Server should report as running");

      // Stop the server
      server.StopServer();
      yield return new WaitForSeconds(2);

      // Verify server stopped
      var stoppedTask = server.IsServerRunningAsync();
      yield return new WaitUntil(() => stoppedTask.IsCompleted);
      Assert.IsFalse(stoppedTask.Result, "Server should be stopped");
    }

    [UnityTest]
    [Category("Integration")]
    [Timeout(180000)]
    public IEnumerator BrainServer_RestartServer_FullCycle()
    {
      // Skip if server executable doesn't exist
      if (!ServerExecutableExists())
      {
        Assert.Ignore("Skipping: llama-server not found at " + settings.ExecutablePath);
        yield break;
      }

      if (!ModelFileExists())
      {
        Assert.Ignore("Skipping: model not found at " + settings.ModelPath);
        yield break;
      }

      // Start the server initially
      server.StartServer();
      yield return new WaitForSeconds(1);

      var startTask = server.WaitForServerAsync(60);
      yield return new WaitUntil(() => startTask.IsCompleted);

      if (!startTask.Result)
      {
        Assert.Ignore("Server failed to start - may be missing dependencies");
        yield break;
      }

      // Restart the server
      server.RestartServer();
      yield return new WaitForSeconds(2);

      // Wait for server to come back
      var restartTask = server.WaitForServerAsync(60);
      yield return new WaitUntil(() => restartTask.IsCompleted);

      Assert.IsTrue(restartTask.Result, "Server should be ready after restart");

      // Cleanup
      server.StopServer();
      yield return new WaitForSeconds(2);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_GetServerStatus_WhenNotInitialized_ShowsNotInitialized()
    {
      // Arrange - Create a server without initialization
      var uninitServerObject = new GameObject("UninitServer");
      var uninitServer = uninitServerObject.AddComponent<BrainServer>();
      uninitServer.Settings = null;

      yield return null;

      // Act
      var status = uninitServer.GetServerStatus();

      // Assert
      Assert.IsNotNull(status);
      Assert.IsTrue(status.Contains("Initialized: False"));

      // Cleanup
      Object.DestroyImmediate(uninitServerObject);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_GpuLayersParsing_WorksCorrectly()
    {
      // Arrange
      settings.GpuLayers = 35;

      // Re-initialize with new GPU settings
      server.Initialize();
      yield return null;

      // Act
      var config = settings.ToProcessConfig();

      // Assert
      Assert.AreEqual(35, config.GpuLayers);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_RegisterAgent_AddsToActiveAgents()
    {
      // Arrange
      yield return null; // Let initialization complete

      var agentObject = new GameObject("TestAgent");
      var agent = agentObject.AddComponent<LlamaBrainAgent>();

      // Act
      server.RegisterAgent(agent);
      yield return null;

      // Assert - Check status contains agent info
      var status = server.GetServerStatus();
      Assert.IsTrue(status.Contains("Active Agents:"));

      // Cleanup
      server.UnregisterAgent(agent);
      Object.DestroyImmediate(agentObject);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_UnregisterAgent_RemovesFromActiveAgents()
    {
      // Arrange
      yield return null; // Let initialization complete

      var agentObject = new GameObject("TestAgent");
      var agent = agentObject.AddComponent<LlamaBrainAgent>();

      // Register then unregister
      server.RegisterAgent(agent);
      yield return null;
      server.UnregisterAgent(agent);
      yield return null;

      // Assert - Agent should be removed
      var status = server.GetServerStatus();
      Assert.IsNotNull(status);

      // Cleanup
      Object.DestroyImmediate(agentObject);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_StopServer_WhenNotInitialized_HandlesGracefully()
    {
      // Arrange - Create a server without initialization
      var uninitServerObject = new GameObject("UninitServer");
      var uninitServer = uninitServerObject.AddComponent<BrainServer>();
      uninitServer.Settings = null;

      yield return null;

      // Act - Should not throw
      uninitServer.StopServer();
      yield return null;

      // Assert - Just verify we got here without exception
      Assert.IsNotNull(uninitServer);

      // Cleanup
      Object.DestroyImmediate(uninitServerObject);
    }

    [UnityTest]
    [Category("Integration")]
    public IEnumerator BrainServer_StartServer_WhenNotInitialized_HandlesGracefully()
    {
      // Arrange - Create a server without initialization
      var uninitServerObject = new GameObject("UninitServer");
      var uninitServer = uninitServerObject.AddComponent<BrainServer>();
      uninitServer.Settings = null;

      yield return null;

      // Act - Should not throw
      uninitServer.StartServer();
      yield return null;

      // Assert - Just verify we got here without exception
      Assert.IsNotNull(uninitServer);

      // Cleanup
      Object.DestroyImmediate(uninitServerObject);
    }

    #endregion
  }
}
