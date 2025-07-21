using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityBrain.Utilities;

namespace UnityBrain.Persona
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
    /// Constructor
    /// </summary>
    /// <param name="saveDir">The directory to save the memory</param>
    public PersonaMemoryFileStore(string saveDir)
    {
      _saveDir = saveDir;
      Directory.CreateDirectory(_saveDir);
    }

    /// <summary>
    /// Save the memory to a file
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    public void Save(string personaId)
    {
      var memory = GetMemory(personaId);
      var json = JsonConvert.SerializeObject(memory);
      File.WriteAllText(Path.Combine(_saveDir, $"{personaId}.json"), json);
    }

    /// <summary>
    /// Save the memory to a file using a PersonaProfile
    /// </summary>
    /// <param name="profile">The persona profile</param>
    public void Save(PersonaProfile profile)
    {
      if (profile == null)
        throw new System.ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new System.ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      Save(profile.PersonaId);
    }

    /// <summary>
    /// Load the memory from a file
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    public void Load(string personaId)
    {
      var file = Path.Combine(_saveDir, $"{personaId}.json");
      if (File.Exists(file))
      {
        var json = File.ReadAllText(file);
        var memory = JsonConvert.DeserializeObject<List<string>>(json);
        if (memory != null)
        {
          foreach (var m in memory) AddMemory(personaId, m);
        }
        else
        {
          Logger.Warn($"Failed to deserialize memory for persona {personaId} - received null response");
        }
      }
    }

    /// <summary>
    /// Load the memory from a file using a PersonaProfile
    /// </summary>
    /// <param name="profile">The persona profile</param>
    public void Load(PersonaProfile profile)
    {
      if (profile == null)
        throw new System.ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new System.ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      Load(profile.PersonaId);
    }
  }
}