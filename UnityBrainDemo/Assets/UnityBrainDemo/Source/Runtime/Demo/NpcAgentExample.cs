using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Cysharp.Threading.Tasks;
using UnityBrainDemo.Runtime.Core;
using UnityBrain.Persona;
using UnityBrain.Core;

/// <summary>
/// This namespace contains the demo components for the UnityBrainDemo project.
/// </summary>
namespace UnityBrainDemo.Runtime.Demo
{
    /// <summary>
    /// A demo component that shows how to use the UnityBrainAgent component.
    /// </summary>
    [RequireComponent(typeof(UnityBrainAgent))]
    public class NpcAgentExample : MonoBehaviour
    {
        /// <summary>
        /// The client for the UnityBrain server.
        /// </summary>
        public ClientManager client;
        /// <summary>
        /// The memory provider for the persona.
        /// </summary>
        public PersonaMemoryStore MemoryProvider = new PersonaMemoryStore();
        /// <summary>
        /// The name of the player.
        /// </summary>
        public string PlayerName = "Adventurer";

        /// <summary>
        /// The agent for the UnityBrain server.
        /// </summary>
        [SerializeField]
        private UnityBrainAgent agent;
        /// <summary>
        /// The settings for the UnityBrain server.
        /// </summary>
        [SerializeField]
        private UnityBrainSettings settings;

        /// <summary>
        /// The event that is triggered when the NPC responds.
        /// </summary>
        [Header("Events")]
        [SerializeField] private UnityEvent<string> onNpcResponse;

        /// <summary>
        /// Resets the agent.
        /// </summary>
        private void Reset()
        {
            agent = GetComponent<UnityBrainAgent>();
        }

        /// <summary>
        /// Starts the agent.
        /// </summary>
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

        /// <summary>
        /// Sends a message to the NPC.
        /// </summary>
        /// <param name="input">The input to say to the NPC.</param>
        public async void SayToNpc(string input)
        {
            var response = await agent.SendPlayerInputAsync(PlayerName, input);
            onNpcResponse?.Invoke(response);
        }
    }
}
