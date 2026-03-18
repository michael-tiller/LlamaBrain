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
  /// Includes Play Mode polling for dirty config detection.
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
    /// Cached snapshots of config values for dirty detection.
    /// </summary>
    private static Dictionary<PersonaConfig, PersonaConfigSnapshot> _personaSnapshots = new();
    private static Dictionary<BrainSettings, BrainSettingsSnapshot> _brainSnapshots = new();

    /// <summary>
    /// Polling interval in seconds.
    /// </summary>
    private const float PollIntervalSeconds = 0.25f;
    private static double _lastPollTime = 0;

    /// <summary>
    /// Snapshot of PersonaConfig values for change detection.
    /// </summary>
    private struct PersonaConfigSnapshot
    {
      public string PersonaId;
      public string Name;
      public string Description;
      public string SystemPrompt;
      public string Background;
      public bool UseMemory;

      public static PersonaConfigSnapshot From(PersonaConfig config) => new()
      {
        PersonaId = config.PersonaId,
        Name = config.Name,
        Description = config.Description,
        SystemPrompt = config.SystemPrompt,
        Background = config.Background,
        UseMemory = config.UseMemory
      };

      public bool Equals(PersonaConfigSnapshot other) =>
        PersonaId == other.PersonaId &&
        Name == other.Name &&
        Description == other.Description &&
        SystemPrompt == other.SystemPrompt &&
        Background == other.Background &&
        UseMemory == other.UseMemory;
    }

    /// <summary>
    /// Snapshot of BrainSettings values for change detection.
    /// </summary>
    private struct BrainSettingsSnapshot
    {
      public int Port;
      public int ContextSize;
      public int GpuLayers;
      public int MaxTokens;
      public float Temperature;
      public float TopP;
      public int TopK;
      public float RepeatPenalty;
      public bool EnableEmbeddingServer;
      public string EmbeddingModelPath;
      public int EmbeddingServerPort;
      public int EmbeddingDimension;

      public static BrainSettingsSnapshot From(BrainSettings settings) => new()
      {
        Port = settings.Port,
        ContextSize = settings.ContextSize,
        GpuLayers = settings.GpuLayers,
        MaxTokens = settings.MaxTokens,
        Temperature = settings.Temperature,
        TopP = settings.TopP,
        TopK = settings.TopK,
        RepeatPenalty = settings.RepeatPenalty,
        EnableEmbeddingServer = settings.EnableEmbeddingServer,
        EmbeddingModelPath = settings.EmbeddingModelPath ?? "",
        EmbeddingServerPort = settings.EmbeddingServerPort,
        EmbeddingDimension = settings.EmbeddingDimension
      };

      public bool Equals(BrainSettingsSnapshot other) =>
        Port == other.Port &&
        ContextSize == other.ContextSize &&
        GpuLayers == other.GpuLayers &&
        MaxTokens == other.MaxTokens &&
        Mathf.Approximately(Temperature, other.Temperature) &&
        Mathf.Approximately(TopP, other.TopP) &&
        TopK == other.TopK &&
        Mathf.Approximately(RepeatPenalty, other.RepeatPenalty) &&
        EnableEmbeddingServer == other.EnableEmbeddingServer &&
        EmbeddingModelPath == other.EmbeddingModelPath &&
        EmbeddingServerPort == other.EmbeddingServerPort &&
        EmbeddingDimension == other.EmbeddingDimension;
    }

    /// <summary>
    /// Static constructor - called when Unity Editor loads.
    /// </summary>
    static ConfigHotReloadManager()
    {
      // Subscribe to config watcher events (for file-based changes)
      UnityEditorConfigWatcher.Instance.OnConfigChanged += HandleConfigChanged;
      UnityEditorConfigWatcher.Instance.StartWatching();

      // Subscribe to Play Mode polling for in-memory changes
      EditorApplication.update += PollForDirtyConfigs;
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

      Debug.Log("[ConfigHotReloadManager] Initialized - watching for PersonaConfig and BrainSettings changes");
    }

    /// <summary>
    /// Clears snapshots when entering/exiting Play Mode.
    /// </summary>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
      if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingPlayMode)
      {
        _personaSnapshots.Clear();
        _brainSnapshots.Clear();
        _lastPollTime = 0;
      }
    }

    /// <summary>
    /// Polls for dirty configs during Play Mode.
    /// </summary>
    private static void PollForDirtyConfigs()
    {
      if (!IsEnabled || !Application.isPlaying)
        return;

      // Throttle polling
      if (EditorApplication.timeSinceStartup - _lastPollTime < PollIntervalSeconds)
        return;

      _lastPollTime = EditorApplication.timeSinceStartup;

      // Check PersonaConfigs in use by agents (same pattern as LairdGame)
      var agents = Object.FindObjectsByType<LlamaBrainAgent>(FindObjectsSortMode.None);
      foreach (var agent in agents)
      {
        if (agent.PersonaConfig == null)
          continue;

        var config = agent.PersonaConfig;
        var currentSnapshot = PersonaConfigSnapshot.From(config);

        if (_personaSnapshots.TryGetValue(config, out var previousSnapshot))
        {
          if (!currentSnapshot.Equals(previousSnapshot))
          {
            Debug.Log($"[ConfigHotReloadManager] Detected in-memory change to PersonaConfig: {config.Name}");
            HandlePersonaConfigChanged(config, AssetDatabase.GetAssetPath(config));
            // Snapshot is now updated inside HandlePersonaConfigChanged
          }
        }
        else
        {
          // First time seeing this config, store snapshot
          _personaSnapshots[config] = currentSnapshot;
        }
      }

      // Check BrainSettings via singleton (same pattern as LairdGame)
      var brainServer = BrainServer.Instance;
      if (brainServer != null && brainServer.Settings != null)
      {
        var settings = brainServer.Settings;
        var currentSnapshot = BrainSettingsSnapshot.From(settings);

        if (_brainSnapshots.TryGetValue(settings, out var previousSnapshot))
        {
          if (!currentSnapshot.Equals(previousSnapshot))
          {
            Debug.Log($"[ConfigHotReloadManager] Detected in-memory change to BrainSettings: {settings.name}");
            HandleBrainSettingsChanged(settings, AssetDatabase.GetAssetPath(settings));
            // Snapshot is now updated inside HandleBrainSettingsChanged
          }
        }
        else
        {
          _brainSnapshots[settings] = currentSnapshot;
        }
      }
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
    /// Captures initial snapshots for all configs currently in use by agents.
    /// Call this after Enable() to ensure baseline snapshots exist before tests modify configs.
    /// </summary>
    public static void CaptureInitialSnapshots()
    {
      // Capture PersonaConfig snapshots
      var agents = Object.FindObjectsByType<LlamaBrainAgent>(FindObjectsSortMode.None);
      foreach (var agent in agents)
      {
        if (agent.PersonaConfig != null && !_personaSnapshots.ContainsKey(agent.PersonaConfig))
        {
          _personaSnapshots[agent.PersonaConfig] = PersonaConfigSnapshot.From(agent.PersonaConfig);
        }
      }

      // Capture BrainSettings snapshot
      var brainServer = BrainServer.Instance;
      if (brainServer != null && brainServer.Settings != null && !_brainSnapshots.ContainsKey(brainServer.Settings))
      {
        _brainSnapshots[brainServer.Settings] = BrainSettingsSnapshot.From(brainServer.Settings);
      }
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
      // Update snapshot immediately to prevent double-triggering from polling
      var currentSnapshot = PersonaConfigSnapshot.From(config);
      _personaSnapshots[config] = currentSnapshot;

      Debug.Log($"[ConfigHotReloadManager] PersonaConfig changed: {config.Name} ({assetPath})");

      // Log current values for verification
      Debug.Log($"[ConfigHotReloadManager] PersonaConfig values detected:");
      Debug.Log($"  PersonaId: '{config.PersonaId}' | Name: '{config.Name}'");
      Debug.Log($"  Description: '{(config.Description?.Length > 50 ? config.Description.Substring(0, 50) + "..." : config.Description)}'");
      Debug.Log($"  SystemPrompt: '{(config.SystemPrompt?.Length > 60 ? config.SystemPrompt.Substring(0, 60) + "..." : config.SystemPrompt)}'");
      Debug.Log($"  UseMemory: {config.UseMemory}");

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
      // Update snapshot immediately to prevent double-triggering from polling
      var currentSnapshot = BrainSettingsSnapshot.From(settings);
      _brainSnapshots[settings] = currentSnapshot;

      Debug.Log($"[ConfigHotReloadManager] BrainSettings changed: {assetPath}");

      // Log current values for verification
      Debug.Log($"[ConfigHotReloadManager] BrainSettings values detected:");
      Debug.Log($"  Port: {settings.Port} | ContextSize: {settings.ContextSize} | GpuLayers: {settings.GpuLayers}");
      Debug.Log($"  MaxTokens: {settings.MaxTokens} | Temperature: {settings.Temperature:F2} | TopP: {settings.TopP:F2} | TopK: {settings.TopK}");
      Debug.Log($"  BatchSize: {settings.BatchSize} | UBatchSize: {settings.UBatchSize} | RepeatPenalty: {settings.RepeatPenalty:F2}");

      // Find BrainServer in scene
      var brainServer = Object.FindFirstObjectByType<BrainServer>();
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
      var allAgents = Object.FindObjectsByType<LlamaBrainAgent>(FindObjectsSortMode.None);
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

    /// <summary>
    /// Menu item to force reload all configs (useful during Play Mode when Ctrl+S is locked).
    /// </summary>
    [MenuItem("LlamaBrain/Hot Reload/Force Reload All %&r")]
    private static void MenuForceReloadAll()
    {
      if (!Application.isPlaying)
      {
        Debug.Log("[ConfigHotReloadManager] Force reload only works during Play Mode");
        return;
      }

      Debug.Log("[ConfigHotReloadManager] Force reloading all LlamaBrain configs...");

      // Find and reload all PersonaConfigs in use
      var agents = Object.FindObjectsByType<LlamaBrainAgent>(FindObjectsSortMode.None);
      var processedConfigs = new HashSet<PersonaConfig>();
      int agentReloads = 0;

      foreach (var agent in agents)
      {
        if (agent.PersonaConfig != null && !processedConfigs.Contains(agent.PersonaConfig))
        {
          processedConfigs.Add(agent.PersonaConfig);
          HandlePersonaConfigChanged(agent.PersonaConfig, AssetDatabase.GetAssetPath(agent.PersonaConfig));
          agentReloads++;
        }
      }

      // Find and reload BrainSettings
      var brainServer = Object.FindFirstObjectByType<BrainServer>();
      if (brainServer != null && brainServer.Settings != null)
      {
        HandleBrainSettingsChanged(brainServer.Settings, AssetDatabase.GetAssetPath(brainServer.Settings));
      }

      Debug.Log($"[ConfigHotReloadManager] Force reload complete: {processedConfigs.Count} PersonaConfig(s), {(brainServer != null ? 1 : 0)} BrainSettings");
    }
  }
}
#endif
