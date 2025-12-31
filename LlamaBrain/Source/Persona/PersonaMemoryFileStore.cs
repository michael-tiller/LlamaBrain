using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using LlamaBrain.Utilities;

namespace LlamaBrain.Persona
{
  /// <summary>
  /// File store for the persona memory
  /// </summary>
  public sealed class PersonaMemoryFileStore : PersonaMemoryStore
  {
    /// <summary>
    /// The directory to save the memory
    /// </summary>
    private readonly string _saveDir;

    /// <summary>
    /// The file system abstraction
    /// </summary>
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Maximum file size in bytes (5MB for memory files)
    /// </summary>
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum JSON string length
    /// </summary>
    private const int MaxJsonLength = 2000000; // 2MB

    /// <summary>
    /// Maximum number of memory entries per persona
    /// </summary>
    private const int MaxMemoryEntries = 10000;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="saveDir">The directory to save the memory</param>
    public PersonaMemoryFileStore(string saveDir) : this(saveDir, new FileSystem())
    {
    }

    /// <summary>
    /// Constructor with file system injection for testing
    /// </summary>
    /// <param name="saveDir">The directory to save the memory</param>
    /// <param name="fileSystem">The file system abstraction</param>
    public PersonaMemoryFileStore(string saveDir, IFileSystem fileSystem)
    {
      if (string.IsNullOrWhiteSpace(saveDir))
        throw new ArgumentException("Save directory cannot be null or empty", nameof(saveDir));

      _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

      // Validate and sanitize the path
      _saveDir = ValidateAndSanitizePath(saveDir);

      try
      {
        _fileSystem.CreateDirectory(_saveDir);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Failed to create save directory: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Save the memory to a file
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    public void Save(string personaId)
    {
      if (string.IsNullOrWhiteSpace(personaId))
        throw new ArgumentException("Persona ID cannot be null or empty", nameof(personaId));

      var safePersonaId = ValidateAndSanitizePersonaId(personaId);

      try
      {
        var memory = GetMemory(personaId);

        // Limit the number of memory entries
        if (memory.Count > MaxMemoryEntries)
        {
          Logger.Warn($"Memory for persona {safePersonaId} has {memory.Count} entries (max: {MaxMemoryEntries}). Truncating to latest entries.");
          memory = memory.Skip(memory.Count - MaxMemoryEntries).ToList();
        }

        var json = JsonConvert.SerializeObject(memory);

        // Validate JSON size
        if (json.Length > MaxJsonLength)
        {
          throw new InvalidOperationException($"Memory JSON too large for persona {safePersonaId}: {json.Length} characters (max: {MaxJsonLength})");
        }

        var filePath = _fileSystem.CombinePath(_saveDir, $"{safePersonaId}.json");

        // Validate final file path
        ValidateFilePath(filePath);

        // Write to temporary file first, then move to final location
        var tempPath = filePath + ".tmp";
        _fileSystem.WriteAllText(tempPath, json);

        // Verify the written file size
        var fileInfo = _fileSystem.GetFileInfo(tempPath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
          _fileSystem.DeleteFile(tempPath);
          throw new InvalidOperationException($"Memory file too large for persona {safePersonaId}: {fileInfo.Length} bytes (max: {MaxFileSizeBytes})");
        }

        // Move to final location
        if (_fileSystem.FileExists(filePath))
          _fileSystem.DeleteFile(filePath);
        _fileSystem.MoveFile(tempPath, filePath);

        Logger.Info($"Successfully saved memory for persona: {safePersonaId} ({memory.Count} entries)");
      }
      catch (Exception ex)
      {
        Logger.Error($"Failed to save memory for persona {safePersonaId}: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Save the memory to a file using a PersonaProfile
    /// </summary>
    /// <param name="profile">The persona profile</param>
    public void Save(PersonaProfile profile)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      Save(profile.PersonaId);
    }

    /// <summary>
    /// Load the memory from a file
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    public void Load(string personaId)
    {
      if (string.IsNullOrWhiteSpace(personaId))
        return;

      var safePersonaId = ValidateAndSanitizePersonaId(personaId);
      var file = _fileSystem.CombinePath(_saveDir, $"{safePersonaId}.json");

      try
      {
        // Validate file path
        ValidateFilePath(file);

        if (!_fileSystem.FileExists(file))
          return;

        // Check file size before reading
        var fileInfo = _fileSystem.GetFileInfo(file);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
          Logger.Error($"Memory file too large for persona {safePersonaId}: {fileInfo.Length} bytes (max: {MaxFileSizeBytes})");
          return;
        }

        var json = _fileSystem.ReadAllText(file);

        // Validate JSON length
        if (json.Length > MaxJsonLength)
        {
          Logger.Error($"Memory JSON too large for persona {safePersonaId}: {json.Length} characters (max: {MaxJsonLength})");
          return;
        }

        var memory = JsonConvert.DeserializeObject<List<string>>(json);
        if (memory != null)
        {
          // Validate memory entries
          var validEntries = new List<string>();
          foreach (var entry in memory)
          {
            if (!string.IsNullOrWhiteSpace(entry) && entry.Length <= 10000) // Max 10KB per entry
            {
              validEntries.Add(entry);
            }
            else
            {
              Logger.Warn($"Skipping invalid memory entry for persona {safePersonaId}: {(entry?.Length ?? 0)} characters");
            }
          }

          // Limit the number of entries loaded
          if (validEntries.Count > MaxMemoryEntries)
          {
            Logger.Warn($"Memory for persona {safePersonaId} has {validEntries.Count} entries (max: {MaxMemoryEntries}). Loading only latest entries.");
            validEntries = validEntries.Skip(validEntries.Count - MaxMemoryEntries).ToList();
          }

          foreach (var m in validEntries)
          {
            AddMemory(safePersonaId, m);
          }

          Logger.Info($"Successfully loaded memory for persona: {safePersonaId} ({validEntries.Count} entries)");
        }
        else
        {
          Logger.Warn($"Failed to deserialize memory for persona {safePersonaId} - received null response");
        }
      }
      catch (JsonException ex)
      {
        Logger.Error($"Failed to deserialize memory for persona {safePersonaId}: {ex.Message}");
      }
      catch (Exception ex)
      {
        Logger.Error($"Failed to load memory for persona {safePersonaId}: {ex.Message}");
      }
    }

    /// <summary>
    /// Load the memory from a file using a PersonaProfile
    /// </summary>
    /// <param name="profile">The persona profile</param>
    public void Load(PersonaProfile profile)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      Load(profile.PersonaId);
    }

    /// <summary>
    /// Validate and sanitize a path
    /// </summary>
    private string ValidateAndSanitizePath(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("Path cannot be null or empty", nameof(path));

      // Get the full path to resolve any relative paths
      var fullPath = _fileSystem.GetFullPath(path);

      // Check for path traversal attempts
      if (fullPath.Contains("..") || fullPath.Contains("//"))
        throw new ArgumentException("Path contains invalid characters", nameof(path));

      return fullPath;
    }

    /// <summary>
    /// Validate a file path
    /// </summary>
    private void ValidateFilePath(string filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("File path cannot be null or empty");

      // Ensure the file path is within the save directory
      var fullPath = _fileSystem.GetFullPath(filePath);
      var saveDirFullPath = _fileSystem.GetFullPath(_saveDir);

      if (!fullPath.StartsWith(saveDirFullPath, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("File path is outside the save directory");

      // Check for invalid characters
      if (fullPath.Contains("..") || fullPath.Contains("//"))
        throw new ArgumentException("File path contains invalid characters");
    }

    /// <summary>
    /// Validate and sanitize a persona ID
    /// </summary>
    private string ValidateAndSanitizePersonaId(string personaId)
    {
      if (string.IsNullOrWhiteSpace(personaId))
        throw new ArgumentException("Persona ID cannot be null or empty", nameof(personaId));

      // Remove any path separators and invalid characters
      var sanitized = personaId.Replace('/', '_')
                               .Replace('\\', '_')
                               .Replace(':', '_')
                               .Replace('*', '_')
                               .Replace('?', '_')
                               .Replace('"', '_')
                               .Replace('<', '_')
                               .Replace('>', '_')
                               .Replace('|', '_')
                               .Trim();

      if (string.IsNullOrWhiteSpace(sanitized))
        throw new ArgumentException("Persona ID contains only invalid characters", nameof(personaId));

      // Limit length
      if (sanitized.Length > 100)
        sanitized = sanitized.Substring(0, 100);

      return sanitized;
    }
  }
}