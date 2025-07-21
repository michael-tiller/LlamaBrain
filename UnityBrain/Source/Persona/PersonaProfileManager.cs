using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityBrain.Utilities;

namespace UnityBrain.Persona
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
    /// Constructor
    /// </summary>
    /// <param name="profilesDir">The directory to save profiles</param>
    public PersonaProfileManager(string profilesDir)
    {
      _profilesDir = profilesDir;
      Directory.CreateDirectory(_profilesDir);
    }

    /// <summary>
    /// Save a persona profile to a file
    /// </summary>
    /// <param name="profile">The profile to save</param>
    public void SaveProfile(PersonaProfile profile)
    {
      if (profile == null)
        throw new System.ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new System.ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
      var filePath = Path.Combine(_profilesDir, $"{profile.PersonaId}.profile.json");
      File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Load a persona profile from a file
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <returns>The loaded profile, or null if not found</returns>
    public PersonaProfile? LoadProfile(string personaId)
    {
      var filePath = Path.Combine(_profilesDir, $"{personaId}.profile.json");
      if (!File.Exists(filePath))
        return null;

      try
      {
        var json = File.ReadAllText(filePath);
        var profile = JsonConvert.DeserializeObject<PersonaProfile>(json);
        return profile;
      }
      catch (System.Exception ex)
      {
        Logger.Error($"Failed to load profile for persona {personaId}: {ex.Message}");
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
      var files = Directory.GetFiles(_profilesDir, "*.profile.json");

      foreach (var file in files)
      {
        var fileName = Path.GetFileNameWithoutExtension(file);
        if (fileName.EndsWith(".profile"))
        {
          var personaId = fileName.Substring(0, fileName.Length - 8); // Remove ".profile"
          profileIds.Add(personaId);
        }
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
        var profile = LoadProfile(personaId);
        if (profile != null)
        {
          profiles[personaId] = profile;
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
      var filePath = Path.Combine(_profilesDir, $"{personaId}.profile.json");
      if (File.Exists(filePath))
      {
        File.Delete(filePath);
        return true;
      }
      return false;
    }
  }
}