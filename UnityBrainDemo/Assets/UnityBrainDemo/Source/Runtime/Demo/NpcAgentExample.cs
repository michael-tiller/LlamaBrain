using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Cysharp.Threading.Tasks;
using UnityBrainDemo.Runtime.Core;
using UnityBrain.Persona;
using UnityBrain.Core;
using UnityBrainDemo.Runtime.Persona;

namespace UnityBrainDemo.Runtime.Demo
{

    [RequireComponent(typeof(UnityBrainAgent))]
    public class NpcAgentExample : MonoBehaviour
    {
        public ClientManager client;
        public PersonaMemoryProvider MemoryProvider = new();
        public string PlayerName = "Adventurer";

        [SerializeField]
        private UnityBrainAgent agent;
        [SerializeField]
        private UnityBrainSettings settings;

        private void Reset()
        {
            agent = GetComponent<UnityBrainAgent>();
        }

        private void Start()
        {
            if (agent == null)
            {
                agent = GetComponent<UnityBrainAgent>();
            }

            if (client == null)
            {
                client = new ClientManager(settings.ToProcessConfig());
            }

            agent.Initialize(client.CreateClient(), MemoryProvider);
        }

        public async void SayToNpc(string input)
        {
            var response = await agent.SendPlayerInputAsync(PlayerName, input);
            Debug.Log($"NPC Responds: {response}");
        }
    }
}
