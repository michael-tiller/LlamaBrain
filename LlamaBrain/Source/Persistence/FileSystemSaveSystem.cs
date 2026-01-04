using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Persona;
using LlamaBrain.Utilities;
using Newtonsoft.Json;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Default save system implementation using IFileSystem for file operations.
  /// Uses JSON serialization with atomic writes (temp-then-move pattern).
  /// </summary>
  public sealed class FileSystemSaveSystem : ISaveSystem
  {
    private readonly IFileSystem _fileSystem;
    private readonly IClock _clock;
    private readonly string _saveDirectory;
    private const string SaveExtension = ".llamasave";

    /// <summary>
    /// Maximum allowed save file size in bytes (5MB).
    /// </summary>
    public const int MaxFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Creates a new file system save system.
    /// </summary>
    /// <param name="saveDirectory">Directory to store save files</param>
    /// <param name="fileSystem">File system abstraction for I/O operations</param>
    /// <param name="clock">Clock for timestamp generation</param>
    public FileSystemSaveSystem(string saveDirectory, IFileSystem fileSystem, IClock clock)
    {
      _saveDirectory = saveDirectory ?? throw new ArgumentNullException(nameof(saveDirectory));
      _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
      _clock = clock ?? throw new ArgumentNullException(nameof(clock));

      // Ensure save directory exists
      _fileSystem.CreateDirectory(_saveDirectory);
    }

    /// <summary>
    /// Creates a new file system save system with default clock.
    /// </summary>
    /// <param name="saveDirectory">Directory to store save files</param>
    /// <param name="fileSystem">File system abstraction for I/O operations</param>
    public FileSystemSaveSystem(string saveDirectory, IFileSystem fileSystem)
      : this(saveDirectory, fileSystem, new SystemClock())
    {
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
        var sanitizedSlot = SanitizeSlotName(slotName);
        var filePath = GetSavePath(sanitizedSlot);
        var tempPath = filePath + ".tmp";

        // Serialize to JSON
        var json = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings
        {
          NullValueHandling = NullValueHandling.Ignore,
          TypeNameHandling = TypeNameHandling.None
        });

        // Check file size
        if (json.Length > MaxFileSizeBytes)
        {
          return SaveResult.Failed($"Save data exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)}MB");
        }

        // Atomic write: write to temp file, then move
        _fileSystem.WriteAllText(tempPath, json);

        // Remove existing file if present
        if (_fileSystem.FileExists(filePath))
        {
          _fileSystem.DeleteFile(filePath);
        }

        // Move temp to final location
        _fileSystem.MoveFile(tempPath, filePath);

        return SaveResult.Succeeded(slotName, data.SavedAtUtcTicks);
      }
      catch (Exception ex)
      {
        return SaveResult.Failed($"Failed to save: {ex.Message}");
      }
    }

    /// <inheritdoc/>
    public SaveData? Load(string slotName)
    {
      if (string.IsNullOrEmpty(slotName))
        return null;

      try
      {
        var sanitizedSlot = SanitizeSlotName(slotName);
        var filePath = GetSavePath(sanitizedSlot);

        if (!_fileSystem.FileExists(filePath))
          return null;

        // Check file size before loading
        var fileInfo = _fileSystem.GetFileInfo(filePath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
          throw new InvalidOperationException($"Save file exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)}MB");
        }

        var json = _fileSystem.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<SaveData>(json);
      }
      catch
      {
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
        var sanitizedSlot = SanitizeSlotName(slotName);
        var filePath = GetSavePath(sanitizedSlot);
        return _fileSystem.FileExists(filePath);
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
        var sanitizedSlot = SanitizeSlotName(slotName);
        var filePath = GetSavePath(sanitizedSlot);

        if (!_fileSystem.FileExists(filePath))
          return false;

        _fileSystem.DeleteFile(filePath);
        return true;
      }
      catch
      {
        return false;
      }
    }

    /// <inheritdoc/>
    public IReadOnlyList<SaveSlotInfo> ListSlots()
    {
      try
      {
        var files = _fileSystem.GetFiles(_saveDirectory, $"*{SaveExtension}");
        var slots = new List<SaveSlotInfo>();

        foreach (var file in files)
        {
          try
          {
            var slotName = _fileSystem.GetFileNameWithoutExtension(file);
            var fileInfo = _fileSystem.GetFileInfo(file);

            // Try to read just the header for version info
            var json = _fileSystem.ReadAllText(file);
            var data = JsonConvert.DeserializeObject<SaveData>(json);

            if (data != null)
            {
              slots.Add(new SaveSlotInfo
              {
                SlotName = slotName,
                SavedAtUtcTicks = data.SavedAtUtcTicks,
                Version = data.Version,
                FileSizeBytes = fileInfo.Length,
                PersonaCount = data.PersonaMemories?.Count ?? 0
              });
            }
          }
          catch
          {
            // Skip invalid files
          }
        }

        // Sort by save time, most recent first
        return slots.OrderByDescending(s => s.SavedAtUtcTicks).ToList();
      }
      catch
      {
        return new List<SaveSlotInfo>();
      }
    }

    #region Private Helpers

    private string GetSavePath(string sanitizedSlotName)
    {
      return _fileSystem.CombinePath(_saveDirectory, sanitizedSlotName + SaveExtension);
    }

    /// <summary>
    /// Sanitizes slot name to prevent path traversal and invalid characters.
    /// </summary>
    private static string SanitizeSlotName(string slotName)
    {
      if (string.IsNullOrEmpty(slotName))
        throw new ArgumentException("Slot name cannot be null or empty", nameof(slotName));

      // Remove path separators and other dangerous characters
      var sanitized = slotName
        .Replace('/', '_')
        .Replace('\\', '_')
        .Replace(':', '_')
        .Replace('*', '_')
        .Replace('?', '_')
        .Replace('"', '_')
        .Replace('<', '_')
        .Replace('>', '_')
        .Replace('|', '_')
        .Replace('\0', '_');

      // Remove leading/trailing dots and spaces
      sanitized = sanitized.Trim().Trim('.');

      if (string.IsNullOrEmpty(sanitized))
        throw new ArgumentException("Slot name cannot be empty after sanitization", nameof(slotName));

      // Limit length
      if (sanitized.Length > 64)
        sanitized = sanitized.Substring(0, 64);

      return sanitized;
    }

    #endregion
  }
}
