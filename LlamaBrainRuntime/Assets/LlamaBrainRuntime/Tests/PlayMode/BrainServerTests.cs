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
      settings.ModelPath = "Backend/model/stablelm-zephyr-3b.Q4_0.gguf";
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
      Assert.AreEqual("Backend/model/stablelm-zephyr-3b.Q4_0.gguf", config.Model);
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
  }
}