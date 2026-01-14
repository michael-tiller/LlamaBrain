#nullable enable
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using LlamaBrain.Config;
using LlamaBrain.Runtime.Core;

namespace LlamaBrain.Editor.Config
{
  /// <summary>
  /// Watches for PersonaConfig and BrainSettings ScriptableObject changes in Unity Editor.
  /// Uses AssetDatabase callbacks to detect when config assets are modified.
  /// Implements debouncing to avoid firing multiple events for rapid changes.
  /// </summary>
  public class UnityEditorConfigWatcher : AssetPostprocessor, IConfigWatcher
  {
    /// <summary>
    /// Fired when a PersonaConfig or BrainSettings asset changes.
    /// </summary>
    public event Action<string>? OnConfigChanged;

    /// <summary>
    /// Gets whether the watcher is currently active.
    /// </summary>
    public bool IsWatching { get; private set; }

    /// <summary>
    /// Singleton instance (AssetPostprocessor methods are static, so we need a static instance)
    /// </summary>
    private static UnityEditorConfigWatcher? _instance;

    /// <summary>
    /// Debounce timer in milliseconds (100ms window to group rapid changes)
    /// </summary>
    private const float DebounceDelaySeconds = 0.1f;

    /// <summary>
    /// Pending config changes awaiting debounce
    /// </summary>
    private static readonly HashSet<string> _pendingChanges = new HashSet<string>();

    /// <summary>
    /// Last time a change was detected (for debouncing)
    /// </summary>
    private static double _lastChangeTime = 0;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static UnityEditorConfigWatcher Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new UnityEditorConfigWatcher();
        }
        return _instance;
      }
    }

    /// <summary>
    /// Private constructor for singleton pattern.
    /// </summary>
    private UnityEditorConfigWatcher()
    {
    }

    /// <summary>
    /// Starts watching for config file changes.
    /// </summary>
    public void StartWatching()
    {
      IsWatching = true;
      EditorApplication.update += ProcessPendingChanges;
    }

    /// <summary>
    /// Stops watching for config file changes.
    /// </summary>
    public void StopWatching()
    {
      IsWatching = false;
      EditorApplication.update -= ProcessPendingChanges;
      _pendingChanges.Clear();
    }

    /// <summary>
    /// Called by Unity after assets are imported.
    /// This is a static callback from Unity's AssetPostprocessor.
    /// </summary>
    private static void OnPostprocessAllAssets(
      string[] importedAssets,
      string[] deletedAssets,
      string[] movedAssets,
      string[] movedFromAssetPaths)
    {
      if (_instance == null || !_instance.IsWatching)
        return;

      // Check imported assets for PersonaConfig or BrainSettings
      foreach (var assetPath in importedAssets)
      {
        if (IsConfigAsset(assetPath))
        {
          _pendingChanges.Add(assetPath);
          _lastChangeTime = EditorApplication.timeSinceStartup;
        }
      }
    }

    /// <summary>
    /// Checks if the asset path is a PersonaConfig or BrainSettings asset.
    /// </summary>
    private static bool IsConfigAsset(string assetPath)
    {
      // Load asset to check its type
      var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
      if (asset == null)
        return false;

      // Check if it's PersonaConfig or BrainSettings
      return asset is PersonaConfig || asset is BrainSettings;
    }

    /// <summary>
    /// Processes pending changes after debounce delay.
    /// Called every Editor update frame.
    /// </summary>
    private void ProcessPendingChanges()
    {
      if (_pendingChanges.Count == 0)
        return;

      // Check if debounce delay has elapsed
      double timeSinceLastChange = EditorApplication.timeSinceStartup - _lastChangeTime;
      if (timeSinceLastChange < DebounceDelaySeconds)
        return;

      // Fire events for all pending changes
      foreach (var assetPath in _pendingChanges)
      {
        try
        {
          OnConfigChanged?.Invoke(assetPath);
        }
        catch (Exception ex)
        {
          Debug.LogError($"[UnityEditorConfigWatcher] Error processing config change for {assetPath}: {ex.Message}");
        }
      }

      _pendingChanges.Clear();
    }
  }
}
#endif
