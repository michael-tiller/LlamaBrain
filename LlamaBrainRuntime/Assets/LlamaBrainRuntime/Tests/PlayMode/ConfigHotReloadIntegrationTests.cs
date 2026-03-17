#nullable enable
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Editor.Config;
using LlamaBrain.Persona;
using System.Collections;
using System.Text.RegularExpressions;

namespace LlamaBrain.Tests.PlayMode
{
  /// <summary>
  /// Integration tests for ConfigHotReloadManager.
  /// Tests the actual polling path used during Play Mode in the Editor.
  /// </summary>
  [TestFixture]
  [Category("Integration")]
  public class ConfigHotReloadIntegrationTests
  {
    private GameObject? _agentObj;
    private LlamaBrainAgent? _agent;
    private PersonaConfig? _config;
    private string _testAssetFolder = "Assets/TestConfigsIntegration";

    [SetUp]
    public void SetUp()
    {
      // Disable hot reload during setup to avoid triggering reloads from asset creation
      ConfigHotReloadManager.Disable();

      // Create test asset folder
      if (!AssetDatabase.IsValidFolder(_testAssetFolder))
      {
        AssetDatabase.CreateFolder("Assets", "TestConfigsIntegration");
      }

      // Create agent GameObject
      _agentObj = new GameObject("TestAgent");
      _agent = _agentObj.AddComponent<LlamaBrainAgent>();

      // Create and save PersonaConfig asset
      _config = ScriptableObject.CreateInstance<PersonaConfig>();
      _config.PersonaId = "integration_test_001";
      _config.Name = "Integration Test Wizard";
      _config.Description = "Test wizard for integration tests";
      _config.SystemPrompt = "You are a test wizard.";
      _config.Background = "Test background";
      _config.UseMemory = true;

      string configPath = $"{_testAssetFolder}/IntegrationTestConfig.asset";
      AssetDatabase.CreateAsset(_config, configPath);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      // Assign config to agent and initialize
      _agent.PersonaConfig = _config;
      _agent.ConvertConfigToProfile();

      // Force process any pending watcher events while disabled (they'll be ignored)
      // This clears the debounce queue before we enable, preventing race conditions
      UnityEditorConfigWatcher.Instance.ForceProcessPendingChanges();

      // Enable hot reload and reset stats AFTER setup is complete
      // This prevents asset creation from triggering reloads that pollute the stats
      ConfigHotReloadManager.Enable();
      ConfigHotReloadManager.ResetStats();

      // Capture initial snapshots so polling can detect changes from this baseline
      ConfigHotReloadManager.CaptureInitialSnapshots();
    }

    [TearDown]
    public void TearDown()
    {
      ConfigHotReloadManager.Enable(); // Re-enable for other tests

      if (_agentObj != null)
      {
        Object.DestroyImmediate(_agentObj);
      }

      if (_config != null)
      {
        string configPath = AssetDatabase.GetAssetPath(_config);
        if (!string.IsNullOrEmpty(configPath))
        {
          AssetDatabase.DeleteAsset(configPath);
        }
        Object.DestroyImmediate(_config);
      }

      // Clean up test assets
      if (AssetDatabase.IsValidFolder(_testAssetFolder))
      {
        AssetDatabase.DeleteAsset(_testAssetFolder);
      }

      AssetDatabase.Refresh();
    }

    /// <summary>
    /// Waits for polling to detect and process config changes.
    /// Polling interval is 0.25s, so we wait a bit longer.
    /// </summary>
    private IEnumerator WaitForPollingCycle()
    {
      // Wait for polling interval (0.25s) + buffer
      yield return new WaitForSeconds(0.4f);
    }

    [UnityTest]
    public IEnumerator AssetModified_AutoReloads_UpdatesProfile()
    {
      // Arrange: Verify initial state
      Assert.AreEqual("Integration Test Wizard", _agent!.RuntimeProfile?.Name);
      Assert.AreEqual(0, ConfigHotReloadManager.TotalReloads, "Should start with 0 reloads");

      // Act: Modify config (in-memory change detected by polling)
      _config!.Name = "Auto-Reloaded Wizard";
      _config.SystemPrompt = "You are an automatically reloaded wizard.";

      // Wait for polling to detect and process the change
      yield return WaitForPollingCycle();

      // Assert: Profile should be updated via hot reload
      Assert.AreEqual("Auto-Reloaded Wizard", _agent.RuntimeProfile?.Name,
        "Profile name should be updated via hot reload");
      Assert.AreEqual("You are an automatically reloaded wizard.", _agent.RuntimeProfile?.SystemPrompt,
        "System prompt should be updated via hot reload");
      Assert.That(ConfigHotReloadManager.TotalReloads, Is.GreaterThan(0),
        "Hot reload should have been triggered");
      Assert.AreEqual(ConfigHotReloadManager.TotalReloads, ConfigHotReloadManager.SuccessfulReloads,
        "All reloads should have succeeded");
    }

    [UnityTest]
    public IEnumerator MultipleAgents_SameConfig_AllReload()
    {
      // Arrange: Create second agent using same config
      var agent2Obj = new GameObject("TestAgent2");
      var agent2 = agent2Obj.AddComponent<LlamaBrainAgent>();
      agent2.PersonaConfig = _config;
      agent2.ConvertConfigToProfile();

      Assert.AreEqual("Integration Test Wizard", _agent!.RuntimeProfile?.Name);
      Assert.AreEqual("Integration Test Wizard", agent2.RuntimeProfile?.Name);

      ConfigHotReloadManager.ResetStats();

      try
      {
        // Act: Modify config once
        _config!.Name = "Multi-Agent Wizard";

        // Wait for polling
        yield return WaitForPollingCycle();

        // Assert: Both agents should be reloaded
        Assert.AreEqual("Multi-Agent Wizard", _agent.RuntimeProfile?.Name,
          "First agent should be updated");
        Assert.AreEqual("Multi-Agent Wizard", agent2.RuntimeProfile?.Name,
          "Second agent should be updated");

        // Should have reloaded 2 agents (1 config change × 2 agents)
        Assert.AreEqual(2, ConfigHotReloadManager.SuccessfulReloads,
          "Should have reloaded both agents");
      }
      finally
      {
        Object.DestroyImmediate(agent2Obj);
      }
    }

    [UnityTest]
    public IEnumerator HotReloadDisabled_NoAutoReload()
    {
      // Arrange
      ConfigHotReloadManager.Disable();
      var originalName = _agent!.RuntimeProfile?.Name;
      ConfigHotReloadManager.ResetStats();

      // Act: Modify config while disabled
      _config!.Name = "Should Not Reload";

      // Wait for polling cycle
      yield return WaitForPollingCycle();

      // Assert: Profile should NOT be updated
      Assert.AreEqual(originalName, _agent.RuntimeProfile?.Name,
        "Profile should not change when hot reload is disabled");
      Assert.AreEqual(0, ConfigHotReloadManager.TotalReloads,
        "No reloads should have been attempted");
    }

    [UnityTest]
    public IEnumerator InvalidConfig_FailsGracefully()
    {
      // Arrange
      ConfigHotReloadManager.ResetStats();
      var originalName = _agent!.RuntimeProfile?.Name;

      // Expect the validation error log and the reload failure log
      LogAssert.Expect(LogType.Error, new Regex(@"\[HotReload\] PersonaConfig validation failed.*"));
      LogAssert.Expect(LogType.Error, new Regex(@"\[ConfigHotReloadManager\] ✗ Failed to reload.*"));

      // Act: Make config invalid (empty name)
      _config!.Name = ""; // Invalid

      // Wait for polling
      yield return WaitForPollingCycle();

      // Assert: Reload should fail, but not crash
      Assert.AreEqual(originalName, _agent.RuntimeProfile?.Name,
        "Profile should not change with invalid config");
      Assert.That(ConfigHotReloadManager.TotalReloads, Is.GreaterThan(0),
        "Reload should have been attempted");
      Assert.AreEqual(ConfigHotReloadManager.TotalReloads, ConfigHotReloadManager.FailedReloads,
        "All reloads should have failed");
    }

    [UnityTest]
    public IEnumerator EventFired_OnSuccessfulReload()
    {
      // Arrange
      bool eventFired = false;
      PersonaProfile? capturedOldProfile = null;
      PersonaProfile? capturedNewProfile = null;

      _agent!.OnPersonaConfigReloaded += (agent, oldProfile, newProfile) =>
      {
        eventFired = true;
        capturedOldProfile = oldProfile;
        capturedNewProfile = newProfile;
      };

      // Act: Trigger hot reload
      _config!.Name = "Event Test Wizard";

      // Wait for polling
      yield return WaitForPollingCycle();

      // Assert
      Assert.IsTrue(eventFired, "OnPersonaConfigReloaded event should fire");
      Assert.IsNotNull(capturedOldProfile, "Old profile should be captured");
      Assert.IsNotNull(capturedNewProfile, "New profile should be captured");
      Assert.AreEqual("Integration Test Wizard", capturedOldProfile?.Name);
      Assert.AreEqual("Event Test Wizard", capturedNewProfile?.Name);
    }

    [UnityTest]
    public IEnumerator Statistics_TrackReloads()
    {
      // Arrange
      ConfigHotReloadManager.ResetStats();

      // Act: Perform successful reload
      _config!.Name = "Stats Test 1";

      yield return WaitForPollingCycle();

      // Expect the validation error log and reload failure log
      LogAssert.Expect(LogType.Error, new Regex(@"\[HotReload\] PersonaConfig validation failed.*"));
      LogAssert.Expect(LogType.Error, new Regex(@"\[ConfigHotReloadManager\] ✗ Failed to reload.*"));

      // Act: Perform failed reload
      _config.Name = ""; // Invalid

      yield return WaitForPollingCycle();

      // Assert
      Assert.AreEqual(2, ConfigHotReloadManager.TotalReloads, "Should have 2 total reloads");
      Assert.AreEqual(1, ConfigHotReloadManager.SuccessfulReloads, "Should have 1 successful reload");
      Assert.AreEqual(1, ConfigHotReloadManager.FailedReloads, "Should have 1 failed reload");
    }

    [UnityTest]
    public IEnumerator NoAgentsUsingConfig_SkipsReload()
    {
      // Arrange: Remove config from agent
      _agent!.PersonaConfig = null;
      ConfigHotReloadManager.ResetStats();

      // Act: Modify config
      _config!.Name = "Unused Config";

      // Wait for polling
      yield return WaitForPollingCycle();

      // Assert: No reloads should be attempted (no agents using this config)
      Assert.AreEqual(0, ConfigHotReloadManager.TotalReloads,
        "Should not attempt reload when no agents use the config");
    }
  }
}
#endif
