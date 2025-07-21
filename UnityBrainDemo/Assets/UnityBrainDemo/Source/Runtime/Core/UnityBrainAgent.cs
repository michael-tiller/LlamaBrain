using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityBrain.Core;
using UnityBrain.Persona;

namespace UnityBrainDemo.Runtime.Core
{
    /// <summary>
    /// A UnityBrain agent that can be used to interact with a UnityBrain server.
    /// </summary>
    public class UnityBrainAgent : MonoBehaviour
    {
        /// <summary>
        /// The ID of the persona.
        /// </summary>
        public string PersonaId;
        /// <summary>
        /// The name of the persona.
        /// </summary>
        public string PersonaName;
        [TextArea] public string SystemPrompt;
        /// <summary>
        /// The description of the persona.
        /// </summary>
        [TextArea] public string Description;

        /// <summary>
        /// The memory provider for the persona.
        /// </summary>
        private PersonaMemoryStore _memoryProvider;
        /// <summary>
        /// The client for the UnityBrain server.
        /// </summary>
        private ApiClient _client;
        /// <summary>
        /// The dialogue history.
        /// </summary>
        private List<string> _dialogueHistory = new();


        /// <summary>
        /// The memories of the persona.
        /// </summary>
        public string Memories => Application.isPlaying ? string.Join("\n", _memoryProvider.GetMemory(PersonaId)) : string.Empty;

        /// <summary>
        /// Initializes the UnityBrain agent.
        /// </summary>
        public void Initialize(ApiClient client, PersonaMemoryStore memoryProvider)
        {
            _client = client;
            _memoryProvider = memoryProvider;
        }

        /// <summary>
        /// Sends a player input to the UnityBrain server.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="input">The input from the player.</param>
        /// <returns>The response from the UnityBrain server.</returns>
        public async UniTask<string> SendPlayerInputAsync(string playerName, string input)
        {
            var context = PersonaContextBuilder.BuildContext(
                SystemPrompt,
                PersonaName,
                Description,
                _memoryProvider.GetMemory(PersonaId),
                _dialogueHistory,
                playerName,
                input
            );
            var response = await _client.SendPromptAsync(context);
            Debug.Log($"Memories: {Memories}");
            _dialogueHistory.Add($"Player: {input}");
            _dialogueHistory.Add($"NPC: {response}");
            AddMemory(response);
            return response;
        }

        /// <summary>
        /// Add important information to the NPC's memory
        /// </summary>
        /// <param name="memoryEntry">The memory entry to add</param>
        public void AddMemory(string memoryEntry)
        {
            Debug.Log($"Adding memory: {memoryEntry}");
            _memoryProvider.AddMemory(PersonaId, memoryEntry);
        }
    }
}
