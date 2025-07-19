using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityBrain.Core;
using UnityBrain.Persona;
using UnityBrainDemo.Runtime.Persona;

namespace UnityBrainDemo.Runtime.Core
{

    public class UnityBrainAgent : MonoBehaviour
    {
        public string PersonaId;
        public string PersonaName;
        [TextArea] public string Description;

        private PersonaMemoryProvider _memoryProvider;
        private ApiClient _client;
        private List<string> _dialogueHistory = new();

        public void Initialize(ApiClient client, PersonaMemoryProvider memoryProvider)
        {
            _client = client;
            _memoryProvider = memoryProvider;
        }

        public async UniTask<string> SendPlayerInputAsync(string playerName, string input)
        {
            _memoryProvider.AddMemory(PersonaId, input); // optionally log memory
            var context = PersonaContextBuilder.BuildContext(
                PersonaName, Description,
                _memoryProvider.GetMemory(PersonaId),
                _dialogueHistory,
                playerName, input
            );
            var response = await _client.SendPromptAsync(context);
            _dialogueHistory.Add($"Player: {input}");
            _dialogueHistory.Add($"NPC: {response}");
            return response;
        }
    }
}
