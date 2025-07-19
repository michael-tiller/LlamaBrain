using System.Collections.Generic;

namespace UnityBrainDemo.Runtime.Persona
{
    /// <summary>
    /// Unity-facing wrapper for persona memory management.
    /// Can be replaced or injected for persistence (e.g., disk, cloud, player profile).
    /// Default implementation is in-memory only.
    /// </summary>
    public class PersonaMemoryProvider
    {
        private readonly Dictionary<string, List<string>> _memories = new();

        public void AddMemory(string personaId, string entry)
        {
            if (!_memories.TryGetValue(personaId, out var list))
                _memories[personaId] = list = new List<string>();
            list.Add(entry);
            if (list.Count > 32) list.RemoveAt(0); // FIFO cap
        }

        public IReadOnlyList<string> GetMemory(string personaId)
        {
            if (_memories.TryGetValue(personaId, out var list)) return list;
            return System.Array.Empty<string>();
        }

        public void ClearMemory(string personaId)
        {
            if (_memories.ContainsKey(personaId))
                _memories[personaId].Clear();
        }
    }
}
