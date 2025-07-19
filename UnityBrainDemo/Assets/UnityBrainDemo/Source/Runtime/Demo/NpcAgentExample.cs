using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Cysharp.Threading.Tasks;
using UnityBrainDemo.Runtime.Core;
using UnityBrain.Persona;
using UnityBrainDemo.Runtime.Persona;

namespace UnityBrainDemo.Runtime.Demo
{

    public class NpcAgentExample : MonoBehaviour
    {
        public LlamaCppServerController ServerController;
        public PersonaMemoryProvider MemoryProvider;
        public string PlayerName = "Adventurer";

        private LlamaAgentController _agent;

        private void Start()
        {
            _agent = GetComponent<LlamaAgentController>();
            _agent.Initialize(ServerController.CreateClient(), MemoryProvider);
        }

        public async void SayToNpc(string input)
        {
            var response = await _agent.SendPlayerInputAsync(PlayerName, input);
            Debug.Log($"NPC Responds: {response}");
        }
    }
}
