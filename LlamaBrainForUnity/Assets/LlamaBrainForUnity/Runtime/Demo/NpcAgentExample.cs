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
    [RequireComponent(typeof(UnityBrainAgent))]
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
        public UnityBrainAgent agent;
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
        /// Whether the agent initialization is complete
        /// </summary>
        public bool IsInitializationComplete { get; private set; } = false;

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
            Debug.Log("[NpcAgentExample] Starting initialization...");
            InitializeAgentAsync().Forget();
        }

        /// <summary>
        /// Initializes the agent asynchronously
        /// </summary>
        private async UniTaskVoid InitializeAgentAsync()
        {
            try
            {
                if (agent == null)
                {
                    agent = GetComponent<UnityBrainAgent>();
                    Debug.Log("[NpcAgentExample] Found UnityBrainAgent component");
                }

                if (agent == null)
                {
                    Debug.LogError("[NpcAgentExample] No UnityBrainAgent component found on this GameObject");
                    return;
                }

                if (settings == null)
                {
                    Debug.LogError("[NpcAgentExample] BrainSettings is null. Please assign a BrainSettings asset.");
                    return;
                }

                if (client == null)
                {
                    Debug.Log("[NpcAgentExample] Creating ClientManager...");
                    var config = settings.ToProcessConfig();
                    if (config == null)
                    {
                        Debug.LogError("[NpcAgentExample] Failed to create ProcessConfig from settings");
                        return;
                    }
                    client = new ClientManager(config);
                    Debug.Log("[NpcAgentExample] ClientManager created successfully");
                }

                // Initialize profile manager
                Debug.Log("[NpcAgentExample] Initializing profile manager...");
                profileManager = new PersonaProfileManager(Application.persistentDataPath + "/PersonaProfiles");

                // Try to load existing profile, or create a default one
                LoadOrCreateProfile();

                Debug.Log("[NpcAgentExample] Creating API client and initializing agent...");
                var apiClient = client.CreateClient();
                if (apiClient == null)
                {
                    Debug.LogError("[NpcAgentExample] Failed to create API client from ClientManager");
                    return;
                }

                agent.Initialize(apiClient, MemoryProvider);

                // Wait a frame to ensure initialization is complete
                await UniTask.Yield();

                Debug.Log("[NpcAgentExample] Agent initialization completed");

                // Verify the agent is accessible
                if (agent != null && agent.IsInitialized)
                {
                    Debug.Log($"[NpcAgentExample] Agent is properly initialized and accessible. IsInitialized: {agent.IsInitialized}");
                    IsInitializationComplete = true;
                }
                else
                {
                    Debug.LogWarning("[NpcAgentExample] Agent initialization may have failed or agent is not accessible");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NpcAgentExample] InitializeAgentAsync() failed with exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
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
            defaultProfile.SetTrait("Personality", "Friendly, helpful, knowledgeable");
            defaultProfile.Background = "A wise NPC who helps adventurers on their journey";

            profileManager.SaveProfile(defaultProfile);
            Debug.Log($"Created default profile for {defaultProfile.Name ?? "Unknown"}");

            // Set the runtime profile directly on the agent
            agent.RuntimeProfile = defaultProfile;
            Debug.Log($"[NpcAgentExample] Set runtime profile to: {defaultProfile.Name}");
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
                // Copy traits
                runtimeProfile.Traits.Clear();
                foreach (var kvp in profile.Traits)
                {
                    runtimeProfile.SetTrait(kvp.Key, kvp.Value);
                }
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

        /// <summary>
        /// Gets the UnityBrainAgent component for external access
        /// </summary>
        /// <returns>The UnityBrainAgent component</returns>
        public UnityBrainAgent GetBrainAgent()
        {
            return agent;
        }

        /// <summary>
        /// Debug method to check the agent's initialization status
        /// </summary>
        [ContextMenu("Debug Agent Status")]
        public void DebugAgentStatus()
        {
            if (agent == null)
            {
                Debug.LogError("[NpcAgentExample] Agent is null!");
                return;
            }

            Debug.Log($"[NpcAgentExample] Agent Debug Status:");
            Debug.Log($"  - Agent Name: {agent.name}");
            Debug.Log($"  - WasInitializationAttempted: {agent.WasInitializationAttempted}");
            Debug.Log($"  - IsInitialized: {agent.IsInitialized}");
            Debug.Log($"  - IsConnected: {agent.IsConnected}");
            Debug.Log($"  - ConnectionStatus: {agent.ConnectionStatus}");

            // Call the agent's own debug method
            agent.DebugAgentState();
        }
    }
}
