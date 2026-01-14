#nullable enable
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using LlamaBrain.Runtime.Core;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Editor.Config
{
  /// <summary>
  /// Manages hot reload of PersonaConfig and BrainSettings in Unity Editor.
  /// Automatically wires UnityEditorConfigWatcher to LlamaBrainAgents.
  /// Uses [InitializeOnLoad] to start watching when editor loads.
  /// </summary>
  [InitializeOnLoadAttribute]
  public static class ConfigHotReloadManager
  {
    /// <summary>
    /// Whether hot reload is enabled (can be disabled via menu or settings).
    /// </summary>
    public static bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Statistics for debugging/metrics.
    /// </summary>
    public static int TotalReloads { get; private set; } = 0;
    public static int SuccessfulReloads { get; private set; } = 0;
    public static int FailedReloads { get; private set; } = 0;

    /// <summary>
    /// Static constructor - called when Unity Editor loads.
    /// </summary>
    static ConfigHotReloadManager()
    {
      // Subscribe to config watcher events
      UnityEditorConfigWatcher.Instance.OnConfigChanged += HandleConfigChanged;
      UnityEditorConfigWatcher.Instance.StartWatching();

      Debug.Log("[ConfigHotReloadManager] Initialized - watching for PersonaConfig and BrainSettings changes");
    }

    /// <summary>
    /// Enables hot reload.
    /// </summary>
    public static void Enable()
    {
      IsEnabled = true;
      Debug.Log("[ConfigHotReloadManager] Hot reload enabled");
    }

    /// <summary>
    /// Disables hot reload.
    /// </summary>
    public static void Disable()
    {
      IsEnabled = false;
      Debug.Log("[ConfigHotReloadManager] Hot reload disabled");
    }

    /// <summary>
    /// Resets statistics.
    /// </summary>
    public static void ResetStats()
    {
      TotalReloads = 0;
      SuccessfulReloads = 0;
      FailedReloads = 0;
    }

    /// <summary>
    /// Handles config file changes detected by UnityEditorConfigWatcher.
    /// </summary>
    private static void HandleConfigChanged(string assetPath)
    {
      if (!IsEnabled)
      {
        Debug.Log($"[ConfigHotReloadManager] Hot reload disabled, ignoring change to {assetPath}");
        return;
      }

      Debug.Log($"[ConfigHotReloadManager] Config changed: {assetPath}");

      // Load the changed asset
      var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
      if (asset == null)
      {
        Debug.LogWarning($"[ConfigHotReloadManager] Could not load asset at {assetPath}");
        return;
      }

      // Handle PersonaConfig changes
      if (asset is PersonaConfig personaConfig)
      {
        HandlePersonaConfigChanged(personaConfig, assetPath);
      }
      // Handle BrainSettings changes
      else if (asset is BrainSettings brainSettings)
      {
        HandleBrainSettingsChanged(brainSettings, assetPath);
      }
    }

    /// <summary>
    /// Handles PersonaConfig changes by reloading all agents using that config.
    /// </summary>
    private static void HandlePersonaConfigChanged(PersonaConfig config, string assetPath)
    {
      Debug.Log($"[ConfigHotReloadManager] PersonaConfig changed: {config.Name} ({assetPath})");

      // Find all LlamaBrainAgents in the scene using this config
      var agents = FindAgentsUsingConfig(config);

      if (agents.Count == 0)
      {
        Debug.Log($"[ConfigHotReloadManager] No agents currently using {config.Name}, skipping reload");
        return;
      }

      Debug.Log($"[ConfigHotReloadManager] Found {agents.Count} agent(s) using {config.Name}, reloading...");

      // Reload each agent
      int successCount = 0;
      int failCount = 0;

      foreach (var agent in agents)
      {
        TotalReloads++;

        bool success = agent.ReloadPersonaConfig();
        if (success)
        {
          successCount++;
          SuccessfulReloads++;
          Debug.Log($"[ConfigHotReloadManager] ✓ Successfully reloaded {agent.name}");
        }
        else
        {
          failCount++;
          FailedReloads++;
          Debug.LogError($"[ConfigHotReloadManager] ✗ Failed to reload {agent.name}");
        }
      }

      Debug.Log($"[ConfigHotReloadManager] PersonaConfig reload complete: {successCount} succeeded, {failCount} failed");
    }

    /// <summary>
    /// Handles BrainSettings changes by reloading BrainServer.
    /// </summary>
    private static void HandleBrainSettingsChanged(BrainSettings settings, string assetPath)
    {
      Debug.Log($"[ConfigHotReloadManager] BrainSettings changed: {assetPath}");

      // Find BrainServer in scene
      var brainServer = Object.FindObjectOfType<BrainServer>();
      if (brainServer == null)
      {
        Debug.Log($"[ConfigHotReloadManager] No BrainServer in scene, skipping BrainSettings reload");
        return;
      }

      // Check if this BrainServer uses these settings
      if (brainServer.Settings != settings)
      {
        Debug.Log($"[ConfigHotReloadManager] BrainServer is not using these settings, skipping reload");
        return;
      }

      Debug.Log($"[ConfigHotReloadManager] Reloading BrainSettings for BrainServer...");

      TotalReloads++;

      bool success = brainServer.ReloadBrainSettings();
      if (success)
      {
        SuccessfulReloads++;
        Debug.Log($"[ConfigHotReloadManager] ✓ Successfully reloaded BrainSettings");
      }
      else
      {
        FailedReloads++;
        Debug.LogError($"[ConfigHotReloadManager] ✗ Failed to reload BrainSettings");
      }

      Debug.Log($"[ConfigHotReloadManager] BrainSettings reload complete: {(success ? "succeeded" : "failed")}");
    }

    /// <summary>
    /// Finds all LlamaBrainAgents in the scene using the specified PersonaConfig.
    /// </summary>
    private static List<LlamaBrainAgent> FindAgentsUsingConfig(PersonaConfig config)
    {
      var allAgents = Object.FindObjectsOfType<LlamaBrainAgent>();
      var matchingAgents = new List<LlamaBrainAgent>();

      foreach (var agent in allAgents)
      {
        if (agent.PersonaConfig == config)
        {
          matchingAgents.Add(agent);
        }
      }

      return matchingAgents;
    }

    /// <summary>
    /// Menu item to enable hot reload.
    /// </summary>
    [MenuItem("LlamaBrain/Hot Reload/Enable")]
    private static void MenuEnable()
    {
      Enable();
    }

    /// <summary>
    /// Menu item to disable hot reload.
    /// </summary>
    [MenuItem("LlamaBrain/Hot Reload/Disable")]
    private static void MenuDisable()
    {
      Disable();
    }

    /// <summary>
    /// Menu item to show hot reload statistics.
    /// </summary>
    [MenuItem("LlamaBrain/Hot Reload/Show Statistics")]
    private static void MenuShowStats()
    {
      Debug.Log($"[ConfigHotReloadManager] Hot Reload Statistics:");
      Debug.Log($"  - Enabled: {IsEnabled}");
      Debug.Log($"  - Total Reloads: {TotalReloads}");
      Debug.Log($"  - Successful: {SuccessfulReloads}");
      Debug.Log($"  - Failed: {FailedReloads}");
      Debug.Log($"  - Success Rate: {(TotalReloads > 0 ? (SuccessfulReloads * 100.0 / TotalReloads) : 0):F1}%");
    }

    /// <summary>
    /// Menu item to reset statistics.
    /// </summary>
    [MenuItem("LlamaBrain/Hot Reload/Reset Statistics")]
    private static void MenuResetStats()
    {
      ResetStats();
      Debug.Log("[ConfigHotReloadManager] Statistics reset");
    }
  }
}
#endif
