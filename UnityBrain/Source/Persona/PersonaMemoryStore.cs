using System;
using System.Collections.Generic;

namespace UnityBrain.Persona
{

  /// <summary>
  /// Store for the persona memory
  /// </summary>
  public class PersonaMemoryStore
  {
    /// <summary>
    /// The memories for the persona
    /// </summary>
    private readonly Dictionary<string, List<string>> _memories = new Dictionary<string, List<string>>();

    /// <summary>
    /// Add a memory to the store
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <param name="message">The message to add</param>
    public void AddMemory(string personaId, string message)
    {
      if (!_memories.TryGetValue(personaId, out var list))
        _memories[personaId] = list = new List<string>();
      list.Add(message);
      if (list.Count > 32) list.RemoveAt(0);
    }

    /// <summary>
    /// Add a memory to the store using a PersonaProfile
    /// </summary>
    /// <param name="profile">The persona profile</param>
    /// <param name="message">The message to add</param>
    public void AddMemory(PersonaProfile profile, string message)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      AddMemory(profile.PersonaId, message);
    }

    /// <summary>
    /// Get the memory for a persona
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <returns>The memory for the persona</returns>
    public IReadOnlyList<string> GetMemory(string personaId)
    {
      if (_memories.TryGetValue(personaId, out var list)) return list;
      return Array.Empty<string>();
    }

    /// <summary>
    /// Get the memory for a persona using a PersonaProfile
    /// </summary>
    /// <param name="profile">The persona profile</param>
    /// <returns>The memory for the persona</returns>
    public IReadOnlyList<string> GetMemory(PersonaProfile profile)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      return GetMemory(profile.PersonaId);
    }

    /// <summary>
    /// Clears the memory of the persona.
    /// </summary>
    /// <param name="personaId">The ID of the persona.</param>
    public void ClearMemory(string personaId)
    {
      if (_memories.ContainsKey(personaId))
        _memories[personaId].Clear();
    }

    /// <summary>
    /// Clears the memory of the persona using a PersonaProfile.
    /// </summary>
    /// <param name="profile">The persona profile.</param>
    public void ClearMemory(PersonaProfile profile)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      ClearMemory(profile.PersonaId);
    }
  }

}