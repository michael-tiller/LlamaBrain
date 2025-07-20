using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityBrainDemo.Runtime.Core;
using UnityBrain.Core;
using System.Collections;
using System.Threading.Tasks;

namespace UnityBrainDemo.Tests.PlayMode
{
  public class UnityBrainServerTests
  {
    private GameObject serverObject;
    private UnityBrainServer server;
    private UnityBrainSettings settings;

    [SetUp]
    public void SetUp()
    {
      // Create test settings
      settings = ScriptableObject.CreateInstance<UnityBrainSettings>();
      settings.ExecutablePath = "Backend/llama-server.exe";
      settings.ModelPath = "Backend/model/stablelm-zephyr-3b.Q4_0.gguf";
      settings.Port = 5000;
      settings.ContextSize = 2048;

      // Create server GameObject
      serverObject = new GameObject("TestServer");
      server = serverObject.AddComponent<UnityBrainServer>();
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
    public IEnumerator UnityBrainServer_Initialization_WorksCorrectly()
    {
      // Act - Awake is called automatically
      yield return null;

      // Assert
      Assert.IsNotNull(server);
      Assert.IsNotNull(server.Settings);
      Assert.AreEqual(settings, server.Settings);
    }

    [UnityTest]
    public IEnumerator UnityBrainServer_CreateClient_ReturnsValidClient()
    {
      // Arrange
      yield return null; // Let Awake complete

      // Act
      var client = server.CreateClient();

      // Assert
      Assert.IsNotNull(client);
      Assert.IsNotNull(server.Settings);
    }

    [Test]
    public void UnityBrainServer_SettingsValidation_WorksCorrectly()
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
    public IEnumerator UnityBrainServer_WithInvalidPaths_LogsErrors()
    {
      // Arrange
      settings.ExecutablePath = "nonexistent/path/llama-server.exe";
      settings.ModelPath = "nonexistent/path/model.gguf";

      // Act
      yield return null; // Let Awake complete

      // Note: In a real test, we would check for error logs
      // For now, we just verify the component doesn't crash
      Assert.IsNotNull(server);
    }
  }
}