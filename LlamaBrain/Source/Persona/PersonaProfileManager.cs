using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using LlamaBrain.Utilities;

namespace LlamaBrain.Persona
{
  /// <summary>
  /// Manager for persona profiles
  /// </summary>
  public sealed class PersonaProfileManager
  {
    /// <summary>
    /// The directory to save profiles
    /// </summary>
    private readonly string _profilesDir;

    /// <summary>
    /// Maximum file size in bytes (1MB)
    /// </summary>
    private const long MaxFileSizeBytes = 1024 * 1024;

    /// <summary>
    /// Maximum JSON string length
    /// </summary>
    private const int MaxJsonLength = 500000; // 500KB

    /// <summary>
    /// Maximum number of profiles to load at once
    /// </summary>
    private const int MaxProfilesToLoad = 1000;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="profilesDir">The directory to save profiles</param>
    public PersonaProfileManager(string profilesDir)
    {
      if (string.IsNullOrWhiteSpace(profilesDir))
        throw new ArgumentException("Profiles directory cannot be null or empty", nameof(profilesDir));

      // Validate and sanitize the path
      _profilesDir = ValidateAndSanitizePath(profilesDir);

      try
      {
        Directory.CreateDirectory(_profilesDir);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Failed to create profiles directory: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Save a persona profile to a file
    /// </summary>
    /// <param name="profile">The profile to save</param>
    public void SaveProfile(PersonaProfile profile)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      // Validate persona ID for file safety
      var safePersonaId = ValidateAndSanitizePersonaId(profile.PersonaId);

      try
      {
        var json = JsonConvert.SerializeObject(profile, Formatting.Indented);

        // Validate JSON size
        if (json.Length > MaxJsonLength)
        {
          throw new InvalidOperationException($"Profile JSON too large: {json.Length} characters (max: {MaxJsonLength})");
        }

        var filePath = Path.Combine(_profilesDir, $"{safePersonaId}.profile.json");

        // Validate final file path
        ValidateFilePath(filePath);

        // Write to temporary file first, then move to final location
        var tempPath = filePath + ".tmp";
        File.WriteAllText(tempPath, json);

        // Verify the written file size
        var fileInfo = new FileInfo(tempPath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
          File.Delete(tempPath);
          throw new InvalidOperationException($"Profile file too large: {fileInfo.Length} bytes (max: {MaxFileSizeBytes})");
        }

        // Move to final location
        if (File.Exists(filePath))
          File.Delete(filePath);
        File.Move(tempPath, filePath);

        Logger.Info($"Successfully saved profile for persona: {safePersonaId}");
      }
      catch (Exception ex)
      {
        Logger.Error($"Failed to save profile for persona {safePersonaId}: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Load a persona profile from a file
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <returns>The loaded profile, or null if not found</returns>
    public PersonaProfile? LoadProfile(string personaId)
    {
      if (string.IsNullOrWhiteSpace(personaId))
        return null;

      var safePersonaId = ValidateAndSanitizePersonaId(personaId);
      var filePath = Path.Combine(_profilesDir, $"{safePersonaId}.profile.json");

      try
      {
        // Validate file path
        ValidateFilePath(filePath);

        if (!File.Exists(filePath))
          return null;

        // Check file size before reading
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
          Logger.Error($"Profile file too large for persona {safePersonaId}: {fileInfo.Length} bytes (max: {MaxFileSizeBytes})");
          return null;
        }

        var json = File.ReadAllText(filePath);

        // Validate JSON length
        if (json.Length > MaxJsonLength)
        {
          Logger.Error($"Profile JSON too large for persona {safePersonaId}: {json.Length} characters (max: {MaxJsonLength})");
          return null;
        }

        var profile = JsonConvert.DeserializeObject<PersonaProfile>(json);

        // Validate deserialized profile
        if (profile != null && string.IsNullOrEmpty(profile.PersonaId))
        {
          Logger.Warn($"Loaded profile for persona {safePersonaId} has empty PersonaId");
          profile.PersonaId = safePersonaId; // Fix the ID
        }

        return profile;
      }
      catch (JsonException ex)
      {
        Logger.Error($"Failed to deserialize profile for persona {safePersonaId}: {ex.Message}");
        return null;
      }
      catch (Exception ex)
      {
        Logger.Error($"Failed to load profile for persona {safePersonaId}: {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// Get all available profile IDs
    /// </summary>
    /// <returns>List of persona IDs that have saved profiles</returns>
    public List<string> GetAvailableProfileIds()
    {
      var profileIds = new List<string>();

      try
      {
        var files = Directory.GetFiles(_profilesDir, "*.profile.json");

        // Limit the number of files processed
        if (files.Length > MaxProfilesToLoad)
        {
          Logger.Warn($"Too many profile files found: {files.Length} (max: {MaxProfilesToLoad}). Processing first {MaxProfilesToLoad} files.");
          files = files.Take(MaxProfilesToLoad).ToArray();
        }

        foreach (var file in files)
        {
          try
          {
            // Validate file path
            ValidateFilePath(file);

            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.EndsWith(".profile"))
            {
              var personaId = fileName.Substring(0, fileName.Length - 8); // Remove ".profile"

              // Validate the extracted persona ID
              if (IsValidPersonaId(personaId))
              {
                profileIds.Add(personaId);
              }
              else
              {
                Logger.Warn($"Invalid persona ID extracted from filename: {fileName}");
              }
            }
          }
          catch (Exception ex)
          {
            Logger.Warn($"Error processing profile file {file}: {ex.Message}");
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Error($"Failed to get available profile IDs: {ex.Message}");
      }

      return profileIds;
    }

    /// <summary>
    /// Load all available profiles
    /// </summary>
    /// <returns>Dictionary of persona ID to profile</returns>
    public Dictionary<string, PersonaProfile> LoadAllProfiles()
    {
      var profiles = new Dictionary<string, PersonaProfile>();
      var profileIds = GetAvailableProfileIds();

      foreach (var personaId in profileIds)
      {
        try
        {
          var profile = LoadProfile(personaId);
          if (profile != null)
          {
            profiles[personaId] = profile;
          }
        }
        catch (Exception ex)
        {
          Logger.Warn($"Failed to load profile for persona {personaId}: {ex.Message}");
        }
      }

      return profiles;
    }

    /// <summary>
    /// Delete a persona profile
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <returns>True if the profile was deleted, false if it didn't exist</returns>
    public bool DeleteProfile(string personaId)
    {
      if (string.IsNullOrWhiteSpace(personaId))
        return false;

      var safePersonaId = ValidateAndSanitizePersonaId(personaId);
      var filePath = Path.Combine(_profilesDir, $"{safePersonaId}.profile.json");

      try
      {
        // Validate file path
        ValidateFilePath(filePath);

        if (File.Exists(filePath))
        {
          File.Delete(filePath);
          Logger.Info($"Successfully deleted profile for persona: {safePersonaId}");
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        Logger.Error($"Failed to delete profile for persona {safePersonaId}: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Validate and sanitize a path
    /// </summary>
    private string ValidateAndSanitizePath(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("Path cannot be null or empty", nameof(path));

      // Get the full path to resolve any relative paths
      var fullPath = Path.GetFullPath(path);

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

      // Ensure the file path is within the profiles directory
      var fullPath = Path.GetFullPath(filePath);
      var profilesDirFullPath = Path.GetFullPath(_profilesDir);

      if (!fullPath.StartsWith(profilesDirFullPath, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("File path is outside the profiles directory");

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

    /// <summary>
    /// Check if a persona ID is valid
    /// </summary>
    private bool IsValidPersonaId(string personaId)
    {
      if (string.IsNullOrWhiteSpace(personaId))
        return false;

      // Check for invalid characters
      var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
      return !invalidChars.Any(c => personaId.Contains(c)) && personaId.Length <= 100;
    }
  }
}