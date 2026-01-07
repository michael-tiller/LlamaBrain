using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BayatGames.SaveGameFree;
using LlamaBrain.Persistence;
using UnityEngine;

namespace LlamaBrain.Runtime.Persistence
{
  /// <summary>
  /// Unity implementation of ISaveSystem using SaveGameFree plugin.
  /// Handles Unity-specific serialization and path management.
  /// </summary>
  public sealed class SaveGameFreeSaveSystem : ISaveSystem
  {
    private const string SaveDirectoryPrefix = "LlamaBrain/Saves/";
    private const string MetadataFileName = "_metadata";
    private const string ActiveSlotFileName = "_active_slot";

    /// <summary>
    /// Creates a new SaveGameFree-based save system.
    /// </summary>
    public SaveGameFreeSaveSystem()
    {
      // Ensure save directory exists
      var savePath = GetFullSavePath("");
      var directory = Path.GetDirectoryName(savePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }
    }

    /// <inheritdoc/>
    public SaveResult Save(string slotName, SaveData data)
    {
      if (string.IsNullOrEmpty(slotName))
        return SaveResult.Failed("Slot name cannot be null or empty");

      if (data == null)
        return SaveResult.Failed("Save data cannot be null");

      try
      {
        var identifier = GetSaveIdentifier(slotName);

        // Save the data
        SaveGame.Save(identifier, data);

        // Update metadata
        UpdateMetadata(slotName, data);

        return SaveResult.Succeeded(slotName, data.SavedAtUtcTicks);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[LlamaBrain] Failed to save to slot '{slotName}': {ex.Message}");
        return SaveResult.Failed(ex.Message);
      }
    }

    /// <inheritdoc/>
    public SaveData Load(string slotName)
    {
      if (string.IsNullOrEmpty(slotName))
        return null;

      try
      {
        var identifier = GetSaveIdentifier(slotName);

        if (!SaveGame.Exists(identifier))
          return null;

        return SaveGame.Load<SaveData>(identifier);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[LlamaBrain] Failed to load slot '{slotName}': {ex.Message}");
        return null;
      }
    }

    /// <inheritdoc/>
    public bool SlotExists(string slotName)
    {
      if (string.IsNullOrEmpty(slotName))
        return false;

      try
      {
        var identifier = GetSaveIdentifier(slotName);
        return SaveGame.Exists(identifier);
      }
      catch
      {
        return false;
      }
    }

    /// <inheritdoc/>
    public bool DeleteSlot(string slotName)
    {
      if (string.IsNullOrEmpty(slotName))
        return false;

      try
      {
        var identifier = GetSaveIdentifier(slotName);

        if (!SaveGame.Exists(identifier))
          return false;

        SaveGame.Delete(identifier);

        // Remove from metadata
        RemoveFromMetadata(slotName);

        return true;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[LlamaBrain] Failed to delete slot '{slotName}': {ex.Message}");
        return false;
      }
    }

    /// <inheritdoc/>
    public IReadOnlyList<SaveSlotInfo> ListSlots()
    {
      try
      {
        var metadataId = SaveDirectoryPrefix + MetadataFileName;
        if (!SaveGame.Exists(metadataId))
          return new List<SaveSlotInfo>();

        var metadata = SaveGame.Load<SaveSlotMetadataList>(metadataId);
        if (metadata?.Slots == null)
          return new List<SaveSlotInfo>();

        // Sort by save time descending
        return metadata.Slots
          .OrderByDescending(s => s.SavedAtUtcTicks)
          .ToList();
      }
      catch (Exception ex)
      {
        Debug.LogError($"[LlamaBrain] Failed to list slots: {ex.Message}");
        return new List<SaveSlotInfo>();
      }
    }

    #region Private Helpers

    private string GetSaveIdentifier(string slotName)
    {
      var sanitized = SanitizeSlotName(slotName);
      return SaveDirectoryPrefix + sanitized;
    }

    private string GetFullSavePath(string identifier)
    {
      return Path.Combine(Application.persistentDataPath, identifier);
    }

    private static string SanitizeSlotName(string slotName)
    {
      if (string.IsNullOrEmpty(slotName))
        throw new ArgumentException("Slot name cannot be null or empty", nameof(slotName));

      // Remove path separators and dangerous characters
      var sanitized = slotName
        .Replace('/', '_')
        .Replace('\\', '_')
        .Replace(':', '_')
        .Replace('*', '_')
        .Replace('?', '_')
        .Replace('"', '_')
        .Replace('<', '_')
        .Replace('>', '_')
        .Replace('|', '_');

      // Trim and limit length
      sanitized = sanitized.Trim().Trim('.');

      if (string.IsNullOrEmpty(sanitized))
        throw new ArgumentException("Slot name cannot be empty after sanitization", nameof(slotName));

      if (sanitized.Length > 64)
        sanitized = sanitized.Substring(0, 64);

      return sanitized;
    }

    private void UpdateMetadata(string slotName, SaveData data)
    {
      try
      {
        var metadataId = SaveDirectoryPrefix + MetadataFileName;
        var metadata = SaveGame.Exists(metadataId)
          ? SaveGame.Load<SaveSlotMetadataList>(metadataId)
          : new SaveSlotMetadataList();

        metadata.Slots ??= new List<SaveSlotInfo>();

        // Remove existing entry if present
        metadata.Slots.RemoveAll(s => s.SlotName == slotName);

        // Add new entry
        metadata.Slots.Add(new SaveSlotInfo
        {
          SlotName = slotName,
          SavedAtUtcTicks = data.SavedAtUtcTicks,
          Version = data.Version,
          PersonaCount = data.PersonaMemories?.Count ?? 0
        });

        SaveGame.Save(metadataId, metadata);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[LlamaBrain] Failed to update metadata: {ex.Message}");
      }
    }

    private void RemoveFromMetadata(string slotName)
    {
      try
      {
        var metadataId = SaveDirectoryPrefix + MetadataFileName;
        if (!SaveGame.Exists(metadataId))
          return;

        var metadata = SaveGame.Load<SaveSlotMetadataList>(metadataId);
        if (metadata?.Slots == null)
          return;

        metadata.Slots.RemoveAll(s => s.SlotName == slotName);
        SaveGame.Save(metadataId, metadata);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[LlamaBrain] Failed to update metadata: {ex.Message}");
      }
    }

    #endregion

    /// <summary>
    /// Helper class to store metadata about all save slots.
    /// </summary>
    [Serializable]
    private class SaveSlotMetadataList
    {
      public List<SaveSlotInfo> Slots = new List<SaveSlotInfo>();
    }

    /// <summary>
    /// Helper class to store the active slot name.
    /// </summary>
    [Serializable]
    private class ActiveSlotData
    {
      public string SlotName;
    }

    #region Active Slot Management

    /// <summary>
    /// Gets the currently active save slot name.
    /// </summary>
    /// <returns>The active slot name, or null if none is set</returns>
    public string GetActiveSlot()
    {
      try
      {
        var activeSlotId = SaveDirectoryPrefix + ActiveSlotFileName;
        if (!SaveGame.Exists(activeSlotId))
          return null;

        var data = SaveGame.Load<ActiveSlotData>(activeSlotId);
        return data?.SlotName;
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[LlamaBrain] Failed to load active slot: {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// Sets the currently active save slot name.
    /// </summary>
    /// <param name="slotName">The slot name to set as active, or null to clear</param>
    public void SetActiveSlot(string slotName)
    {
      try
      {
        var activeSlotId = SaveDirectoryPrefix + ActiveSlotFileName;

        if (string.IsNullOrEmpty(slotName))
        {
          // Clear active slot
          if (SaveGame.Exists(activeSlotId))
          {
            SaveGame.Delete(activeSlotId);
          }
          return;
        }

        var data = new ActiveSlotData { SlotName = slotName };
        SaveGame.Save(activeSlotId, data);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[LlamaBrain] Failed to save active slot: {ex.Message}");
      }
    }

    #endregion
  }
}
