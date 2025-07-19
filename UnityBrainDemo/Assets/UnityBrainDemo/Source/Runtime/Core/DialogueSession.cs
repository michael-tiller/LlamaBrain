using System.Collections.Generic;

namespace UnityBrainDemo.Runtime.Core
{
    /// <summary>
    /// Tracks the conversation history between the player and an NPC.
    /// </summary>
    public class DialogueSession
    {
        public string PersonaId { get; }
        private readonly List<string> _history = new();

        public DialogueSession(string personaId)
        {
            PersonaId = personaId;
        }

        public void AppendPlayer(string input)
        {
            _history.Add($"Player: {input}");
        }

        public void AppendNpc(string response)
        {
            _history.Add($"NPC: {response}");
        }

        public IReadOnlyList<string> GetHistory() => _history;

        public void Clear()
        {
            _history.Clear();
        }
    }
}
