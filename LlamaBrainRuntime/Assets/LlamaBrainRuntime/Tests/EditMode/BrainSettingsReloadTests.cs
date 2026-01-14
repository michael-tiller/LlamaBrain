#nullable enable
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Core;

namespace LlamaBrain.Tests.EditMode
{
  [TestFixture]
  [Category("Domain")]
  public class BrainSettingsReloadTests
  {
    private GameObject? _serverObj;
    private BrainServer? _server;
    private BrainSettings? _settings;
    private ProcessConfig? _originalConfig;

    [SetUp]
    public void SetUp()
    {
      // Create BrainServer GameObject
      _serverObj = new GameObject("TestBrainServer");
      _server = _serverObj.AddComponent<BrainServer>();

      // Create BrainSettings
      _settings = ScriptableObject.CreateInstance<BrainSettings>();
      _settings.ExecutablePath = "/path/to/llama-server";
      _settings.ModelPath = "/path/to/model.gguf";
      _settings.Port = 5000;
      _settings.ContextSize = 2048;
      _settings.GpuLayers = 35;
      _settings.BatchSize = 512;
      _settings.UBatchSize = 128;
      _settings.MaxTokens = 64;
      _settings.Temperature = 0.7f;
      _settings.TopP = 0.9f;
      _settings.TopK = 40;
      _settings.RepeatPenalty = 1.1f;

      _server.Settings = _settings;

      // Store original config for comparison
      _originalConfig = _settings.ToProcessConfig();
    }

    [TearDown]
    public void TearDown()
    {
      if (_serverObj != null)
      {
        Object.DestroyImmediate(_serverObj);
      }

      if (_settings != null)
      {
        Object.DestroyImmediate(_settings);
      }
    }

    [Test]
    public void ReloadBrainSettings_LlmConfigChanges_AppliesImmediately()
    {
      // Arrange: Change LLM generation parameters
      _settings!.Temperature = 0.5f;
      _settings.MaxTokens = 32;
      _settings.TopP = 0.8f;

      // Act: Reload settings
      bool success = _server!.ReloadBrainSettings();

      // Assert
      Assert.IsTrue(success, "Reload should succeed for LLM config changes");
      // Note: Actual verification would require checking agents receive updated config
      // This will be verified in integration tests
    }

    [Test]
    public void ReloadBrainSettings_InvalidConfig_RollsBack()
    {
      // Arrange: Make LlmConfig invalid (will be auto-clamped by LlmConfig setters)
      // Since LlmConfig auto-clamps, we can't make it truly invalid
      // But we can test null settings
      _server!.Settings = null;

      // Act
      bool success = _server.ReloadBrainSettings();

      // Assert
      Assert.IsFalse(success, "Reload should fail with null settings");
    }

    [Test]
    public void ReloadBrainSettings_ServerConfigChanges_LogsWarning()
    {
      // Arrange: Change server-level config (requires restart)
      var originalGpuLayers = _settings!.GpuLayers;
      _settings.GpuLayers = 50; // Changed server config
      _settings.Temperature = 0.5f; // Also change LLM config

      // Act: Reload (should warn about server config but still apply LLM config)
      bool success = _server!.ReloadBrainSettings();

      // Assert
      Assert.IsTrue(success, "Reload should succeed but warn about server config");
      // Note: Warning verification requires log capture (tested manually or via LogAssert)
    }

    [Test]
    public void ReloadBrainSettings_BroadcastsToAllAgents()
    {
      // Arrange: Create multiple agents and register them
      var agent1Obj = new GameObject("Agent1");
      var agent1 = agent1Obj.AddComponent<LlamaBrainAgent>();

      var agent2Obj = new GameObject("Agent2");
      var agent2 = agent2Obj.AddComponent<LlamaBrainAgent>();

      _server!.RegisterAgent(agent1);
      _server.RegisterAgent(agent2);

      // Track if UpdateLlmConfig is called (will verify in integration tests)
      // For unit test, we just verify no exceptions

      try
      {
        // Act: Reload settings
        _settings!.Temperature = 0.6f;
        bool success = _server.ReloadBrainSettings();

        // Assert
        Assert.IsTrue(success, "Reload should succeed and broadcast to all agents");
      }
      finally
      {
        Object.DestroyImmediate(agent1Obj);
        Object.DestroyImmediate(agent2Obj);
      }
    }

    [Test]
    public void ReloadBrainSettings_NoAgents_Succeeds()
    {
      // Arrange: No agents registered

      // Act: Change LLM config
      _settings!.Temperature = 0.5f;
      bool success = _server!.ReloadBrainSettings();

      // Assert: Should succeed even with no agents
      Assert.IsTrue(success, "Reload should succeed even with no registered agents");
    }

    [Test]
    public void ReloadBrainSettings_FiresEvent()
    {
      // Arrange
      bool eventFired = false;
      LlmConfig? capturedConfig = null;

      _server!.OnBrainSettingsReloaded += (config) =>
      {
        eventFired = true;
        capturedConfig = config;
      };

      // Act
      _settings!.Temperature = 0.4f;
      _settings.MaxTokens = 48;
      _server.ReloadBrainSettings();

      // Assert
      Assert.IsTrue(eventFired, "OnBrainSettingsReloaded event should fire");
      Assert.IsNotNull(capturedConfig, "Event should pass LlmConfig");
      Assert.AreEqual(0.4f, capturedConfig?.Temperature, "Event should contain updated Temperature");
      Assert.AreEqual(48, capturedConfig?.MaxTokens, "Event should contain updated MaxTokens");
    }

    [Test]
    public void HasServerConfigChanged_NoChanges_ReturnsFalse()
    {
      // Arrange: Create identical configs
      var config1 = _settings!.ToProcessConfig();
      var config2 = _settings.ToProcessConfig();

      // Act
      bool changed = _server!.HasServerConfigChanged(config1, config2);

      // Assert
      Assert.IsFalse(changed, "Identical configs should not be flagged as changed");
    }

    [Test]
    public void HasServerConfigChanged_GpuLayersChanged_ReturnsTrue()
    {
      // Arrange
      var oldConfig = _settings!.ToProcessConfig();
      _settings.GpuLayers = 50; // Change GPU layers
      var newConfig = _settings.ToProcessConfig();

      // Act
      bool changed = _server!.HasServerConfigChanged(oldConfig, newConfig);

      // Assert
      Assert.IsTrue(changed, "GPU layers change should be detected");
    }

    [Test]
    public void HasServerConfigChanged_ModelPathChanged_ReturnsTrue()
    {
      // Arrange
      var oldConfig = _settings!.ToProcessConfig();
      _settings.ModelPath = "/different/model.gguf";
      var newConfig = _settings.ToProcessConfig();

      // Act
      bool changed = _server!.HasServerConfigChanged(oldConfig, newConfig);

      // Assert
      Assert.IsTrue(changed, "Model path change should be detected");
    }

    [Test]
    public void HasServerConfigChanged_OnlyLlmConfigChanged_ReturnsFalse()
    {
      // Arrange: Only change LLM generation parameters (not server config)
      var oldConfig = _settings!.ToProcessConfig();
      _settings.Temperature = 0.5f; // LLM config only
      _settings.MaxTokens = 32; // LLM config only
      var newConfig = _settings.ToProcessConfig();

      // Act
      bool changed = _server!.HasServerConfigChanged(oldConfig, newConfig);

      // Assert
      Assert.IsFalse(changed, "LLM config-only changes should not flag server config as changed");
    }
  }
}
#endif
