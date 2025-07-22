using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Cysharp.Threading.Tasks;
using LlamaBrain.Unity.Runtime.Core;
using LlamaBrain.Persona;
using LlamaBrain.Core;

/// <summary>
/// This namespace contains the demo components for the LlamaBrain for Unity project.
/// </summary>
namespace LlamaBrain.Unity.Runtime.Demo
{
    /// <summary>
    /// A demo component that shows how to use the LlamaBrainAgent component.
    /// </summary>
    [RequireComponent(typeof(BrainAgent))]
    public class NpcAgentExample : MonoBehaviour
    {
        /// <summary>
        /// The client for the LlamaBrain server.
        /// </summary>
        public ClientManager client;
        /// <summary>
        /// The memory provider for the persona.
        /// </summary>
        public PersonaMemoryStore MemoryProvider = new PersonaMemoryStore();

        /// <summary>
        /// The agent for the LlamaBrain server.
        /// </summary>
        [SerializeField]
        private BrainAgent agent;
        /// <summary>
        /// The settings for the LlamaBrain server.
        /// </summary>
        [SerializeField]
        private BrainSettings settings;

        /// <summary>
        /// The profile manager for managing persona profiles.
        /// </summary>
        private PersonaProfileManager profileManager;

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
            agent = GetComponent<BrainAgent>();
        }

        /// <summary>
        /// Starts the agent.
        /// </summary>
        private void Start()
        {
            if (agent == null)
            {
                agent = GetComponent<BrainAgent>();
            }

            if (client == null)
            {
                client = new ClientManager(settings.ToProcessConfig());
            }

            // Initialize profile manager
            profileManager = new PersonaProfileManager(Application.persistentDataPath + "/PersonaProfiles");

            // Try to load existing profile, or create a default one
            LoadOrCreateProfile();

            agent.Initialize(client.CreateClient(), MemoryProvider);
        }

        /// <summary>
        /// Loads an existing profile or creates a default one
        /// </summary>
        private void LoadOrCreateProfile()
        {
            // If we have a PersonaConfig, convert it to a profile
            if (agent.PersonaConfig != null)
            {
                agent.ConvertConfigToProfile();
                Debug.LogFormat("Converted PersonaConfig to profile for {0}", agent.PersonaConfig.Name ?? "Unknown");
                return;
            }

            // If no PersonaConfig is assigned, we need to create a default profile
            // Since we removed backward compatibility properties, we'll create a basic profile
            var defaultProfile = PersonaProfile.Create("default-persona", "Default NPC");
            defaultProfile.Description = "A helpful NPC";
            defaultProfile.SystemPrompt = "You are a helpful NPC.";
            defaultProfile.PersonalityTraits = "Friendly, helpful, knowledgeable";
            defaultProfile.Background = "A wise NPC who helps adventurers on their journey";

            profileManager.SaveProfile(defaultProfile);
            Debug.Log($"Created default profile for {defaultProfile.Name ?? "Unknown"}");

            // Note: The agent will need to be manually configured with a PersonaConfig
            // or the runtime profile will need to be set programmatically
        }

        /// <summary>
        /// Sends a message to the NPC.
        /// </summary>
        /// <param name="input">The input to say to the NPC.</param>
        public async void SayToNpc(string input)
        {
            var response = await agent.SendPlayerInputAsync(input);
            onNpcResponse?.Invoke(response);
        }

        /// <summary>
        /// Saves the current persona profile
        /// </summary>
        public void SaveProfile()
        {
            var profile = agent.RuntimeProfile;
            if (profile != null)
            {
                profileManager.SaveProfile(profile);
                Debug.Log($"Saved profile for {profile.Name ?? "Unknown"}");
            }
        }

        /// <summary>
        /// Loads a persona profile by ID
        /// </summary>
        /// <param name="personaId">The ID of the persona to load</param>
        public void LoadProfile(string personaId)
        {
            var profile = profileManager.LoadProfile(personaId);
            if (profile != null)
            {
                // Update the runtime profile
                var runtimeProfile = agent.RuntimeProfile;
                if (runtimeProfile == null)
                {
                    runtimeProfile = PersonaProfile.Create(profile.PersonaId, profile.Name);
                }

                runtimeProfile.Description = profile.Description;
                runtimeProfile.SystemPrompt = profile.SystemPrompt;
                runtimeProfile.PersonalityTraits = profile.PersonalityTraits;
                runtimeProfile.Background = profile.Background;

                // Copy metadata
                runtimeProfile.Metadata.Clear();
                foreach (var kvp in profile.Metadata)
                {
                    runtimeProfile.SetMetadata(kvp.Key, kvp.Value);
                }

                Debug.Log($"Loaded profile for {profile.Name ?? "Unknown"}");
            }
            else
            {
                Debug.LogWarning($"No profile found for persona ID: {personaId}");
            }
        }

        /// <summary>
        /// Gets all available profile IDs
        /// </summary>
        /// <returns>List of available persona IDs</returns>
        public List<string> GetAvailableProfiles()
        {
            return profileManager.GetAvailableProfileIds();
        }
    }
}
