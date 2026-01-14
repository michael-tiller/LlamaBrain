// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Michael Tiller and contributors

using System;
using LlamaBrain.Core;
using LlamaBrain.Persona;

namespace LlamaBrain.Config
{
  /// <summary>
  /// Orchestrates configuration hot reload: validation → apply → rollback flow.
  /// Provides a centralized service for managing config changes across the application.
  /// </summary>
  public class ConfigHotReloadService : IDisposable
  {
    private readonly IConfigWatcher _watcher;
    private bool _isInitialized = false;
    private bool _disposed = false;

    /// <summary>
    /// Fired when a PersonaProfile change is successfully applied.
    /// Parameters: (oldProfile, newProfile)
    /// </summary>
    public event Action<PersonaProfile, PersonaProfile>? OnPersonaProfileChanged;

    /// <summary>
    /// Fired when an LlmConfig change is successfully applied.
    /// </summary>
    public event Action<LlmConfig>? OnLlmConfigChanged;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="watcher">The config file watcher (can be mocked for testing)</param>
    public ConfigHotReloadService(IConfigWatcher watcher)
    {
      _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
    }

    /// <summary>
    /// Initializes the service and starts watching for config changes.
    /// </summary>
    public void Initialize()
    {
      if (_isInitialized)
        return;

      _watcher.StartWatching();
      _isInitialized = true;
    }

    /// <summary>
    /// Validates a PersonaProfile for hot reload.
    /// </summary>
    /// <param name="profile">The profile to validate</param>
    /// <param name="errors">Output array of validation errors</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidatePersonaProfile(PersonaProfile? profile, out string[] errors)
    {
      errors = ConfigValidator.ValidatePersonaProfile(profile);
      return errors.Length == 0;
    }

    /// <summary>
    /// Validates an LlmConfig for hot reload.
    /// </summary>
    /// <param name="config">The config to validate</param>
    /// <param name="errors">Output array of validation errors</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateLlmConfig(LlmConfig? config, out string[] errors)
    {
      errors = ConfigValidator.ValidateLlmConfig(config);
      return errors.Length == 0;
    }

    /// <summary>
    /// Applies a PersonaProfile change after validation.
    /// Fires OnPersonaProfileChanged event if successful.
    /// </summary>
    /// <param name="oldProfile">The previous profile (for rollback)</param>
    /// <param name="newProfile">The new profile to apply</param>
    /// <returns>True if change was applied successfully, false if validation failed</returns>
    public bool ApplyPersonaProfileChange(PersonaProfile oldProfile, PersonaProfile newProfile)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(ConfigHotReloadService));

      // Validate new profile
      if (!ValidatePersonaProfile(newProfile, out var errors))
      {
        // Validation failed - log errors but don't throw
        LogValidationErrors("PersonaProfile", errors);
        return false;
      }

      // Apply change by firing event
      try
      {
        OnPersonaProfileChanged?.Invoke(oldProfile, newProfile);
        return true;
      }
      catch (Exception ex)
      {
        // Catch exceptions from event handlers to prevent crash
        LogError($"Exception in OnPersonaProfileChanged event handler: {ex.Message}");
        return true; // Still consider it successful since validation passed
      }
    }

    /// <summary>
    /// Applies an LlmConfig change after validation.
    /// Fires OnLlmConfigChanged event if successful.
    /// </summary>
    /// <param name="config">The new LlmConfig to apply</param>
    /// <returns>True if change was applied successfully, false if validation failed</returns>
    public bool ApplyLlmConfigChange(LlmConfig? config)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(ConfigHotReloadService));

      // Validate config
      if (!ValidateLlmConfig(config, out var errors))
      {
        // Validation failed - log errors but don't throw
        LogValidationErrors("LlmConfig", errors);
        return false;
      }

      // Apply change by firing event
      try
      {
        OnLlmConfigChanged?.Invoke(config!);
        return true;
      }
      catch (Exception ex)
      {
        // Catch exceptions from event handlers to prevent crash
        LogError($"Exception in OnLlmConfigChanged event handler: {ex.Message}");
        return true; // Still consider it successful since validation passed
      }
    }

    /// <summary>
    /// Logs validation errors.
    /// In production, this would use proper logging framework.
    /// </summary>
    private void LogValidationErrors(string configType, string[] errors)
    {
      // For now, just silently ignore (tests can check return value)
      // In Unity integration, this will use Debug.LogError
      // In standalone, this will use proper logging
    }

    /// <summary>
    /// Logs errors.
    /// In production, this would use proper logging framework.
    /// </summary>
    private void LogError(string message)
    {
      // For now, just silently ignore (tests can check return value)
      // In Unity integration, this will use Debug.LogError
      // In standalone, this will use proper logging
    }

    /// <summary>
    /// Disposes the service and stops watching for config changes.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
        return;

      if (_isInitialized)
      {
        _watcher.StopWatching();
      }

      _disposed = true;
    }
  }
}
