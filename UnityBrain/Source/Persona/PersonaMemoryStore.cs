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
    /// Clears the memory of the persona.
    /// </summary>
    /// <param name="personaId">The ID of the persona.</param>
    public void ClearMemory(string personaId)
    {
      if (_memories.ContainsKey(personaId))
        _memories[personaId].Clear();
    }
  }

}