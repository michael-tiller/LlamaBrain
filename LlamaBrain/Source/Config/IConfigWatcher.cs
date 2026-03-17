// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Michael Tiller and contributors

using System;

namespace LlamaBrain.Config
{
  /// <summary>
  /// Interface for config change detection.
  /// Abstracts file watching to support both Unity Editor (AssetDatabase) and standalone (FileSystemWatcher).
  /// </summary>
  public interface IConfigWatcher
  {
    /// <summary>
    /// Fired when a config file changes.
    /// Parameter is the path to the changed config file (Unity asset path or file system path).
    /// </summary>
    event Action<string>? OnConfigChanged;

    /// <summary>
    /// Starts watching for config file changes.
    /// </summary>
    void StartWatching();

    /// <summary>
    /// Stops watching for config file changes.
    /// </summary>
    void StopWatching();

    /// <summary>
    /// Gets whether the watcher is currently active.
    /// </summary>
    bool IsWatching { get; }
  }
}
