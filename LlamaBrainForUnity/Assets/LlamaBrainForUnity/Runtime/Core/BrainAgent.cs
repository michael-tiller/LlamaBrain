using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using LlamaBrain.Core;
using LlamaBrain.Persona;

namespace LlamaBrain.Unity.Runtime.Core
{
    /// <summary>
    /// A LlamaBrain agent that can be used to interact with a LlamaBrain server.
    /// </summary>
    public class BrainAgent : MonoBehaviour
    {
        /// <summary>
        /// The persona configuration for this agent (Unity ScriptableObject).
        /// </summary>
        [Header("Persona Configuration")]
        public PersonaConfig PersonaConfig;

        /// <summary>
        /// The runtime persona profile (converted from config).
        /// </summary>
        [Header("Runtime Profile")]
        [SerializeField] private PersonaProfile runtimeProfile;

        /// <summary>
        /// The prompt composer settings for this agent.
        /// </summary>
        [Header("Prompt Settings")]
        public PromptComposerSettings PromptSettings;

        /// <summary>
        /// The memory provider for the persona.
        /// </summary>
        private PersonaMemoryStore memoryProvider;
        /// <summary>
        /// The client for the LlamaBrain server.
        /// </summary>
        private ApiClient client;
        /// <summary>
        /// The dialogue session for this agent.
        /// </summary>
        private DialogueSession dialogueSession;

        /// <summary>
        /// The current runtime profile (read-only)
        /// </summary>
        public PersonaProfile RuntimeProfile => runtimeProfile;


        /// <summary>
        /// The memories of the persona.
        /// </summary>
        public string Memories => Application.isPlaying ? string.Join("\n", memoryProvider.GetMemory(runtimeProfile)) : string.Empty;

        /// <summary>
        /// Initializes the LlamaBrain agent.
        /// </summary>
        public void Initialize(ApiClient client, PersonaMemoryStore memoryProvider)
        {
            this.client = client;
            this.memoryProvider = memoryProvider;

            // Convert config to runtime profile if available
            if (PersonaConfig != null && runtimeProfile == null)
            {
                runtimeProfile = PersonaConfig.ToProfile();
            }

            // Initialize dialogue session with persona ID
            dialogueSession = new DialogueSession(runtimeProfile?.Name ?? "Unknown");
        }

        /// <summary>
        /// Converts the current PersonaConfig to a runtime profile
        /// </summary>
        public void ConvertConfigToProfile()
        {
            if (PersonaConfig != null)
            {
                runtimeProfile = PersonaConfig.ToProfile();
            }
        }

        /// <summary>
        /// Updates the PersonaConfig from the current runtime profile
        /// </summary>
        public void UpdateConfigFromProfile()
        {
            if (PersonaConfig != null && runtimeProfile != null)
            {
                PersonaConfig.FromProfile(runtimeProfile);
            }
        }

        /// <summary>
        /// Sends a player input to the LlamaBrain server using the player name from settings.
        /// </summary>
        /// <param name="input">The input from the player.</param>
        /// <returns>The response from the LlamaBrain server.</returns>
        public async UniTask<string> SendPlayerInputAsync(string input)
        {
            return await SendPlayerInputAsync(null, input);
        }

        /// <summary>
        /// Sends a player input to the LlamaBrain server.
        /// </summary>
        /// <param name="playerName">The name of the player (optional, will use settings if not provided).</param>
        /// <param name="input">The input from the player.</param>
        /// <returns>The response from the LlamaBrain server.</returns>
        public async UniTask<string> SendPlayerInputAsync(string playerName, string input)
        {
            // Use player name from settings if not provided or empty
            if (string.IsNullOrEmpty(playerName) && PromptSettings != null)
            {
                playerName = PromptSettings.playerName;
            }

            // Fallback to a default name if still empty
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player";
            }

            // Ensure we have a runtime profile
            if (runtimeProfile == null && PersonaConfig != null)
            {
                runtimeProfile = PersonaConfig.ToProfile();
            }

            // Use PromptComposer for consistent prompt formatting with custom settings
            var prompt = PromptComposer.ComposeWithSettings(
                runtimeProfile?.Name ?? string.Empty,
                runtimeProfile?.Description ?? string.Empty,
                memoryProvider.GetMemory(runtimeProfile),
                dialogueSession.GetHistory(),
                playerName,
                input,
                PromptSettings != null ? new Dictionary<string, object>
                {
                    ["npcTemplate"] = PromptSettings.npcTemplate,
                    ["descriptionTemplate"] = PromptSettings.descriptionTemplate,
                    ["memoryHeaderTemplate"] = PromptSettings.memoryHeaderTemplate,
                    ["memoryItemTemplate"] = PromptSettings.memoryItemTemplate,
                    ["dialogueHeaderTemplate"] = PromptSettings.dialogueHeaderTemplate,
                    ["playerTemplate"] = PromptSettings.playerTemplate,
                    ["playerInputTemplate"] = PromptSettings.playerInputTemplate,
                    ["responsePromptTemplate"] = PromptSettings.responsePromptTemplate,
                    ["includeEmptyMemory"] = PromptSettings.includeEmptyMemory,
                    ["includeEmptyDialogue"] = PromptSettings.includeEmptyDialogue,
                    ["maxMemoryItems"] = PromptSettings.maxMemoryItems,
                    ["maxDialogueLines"] = PromptSettings.maxDialogueLines,
                    ["sectionSeparator"] = PromptSettings.sectionSeparator,
                    ["compactDialogueFormat"] = PromptSettings.compactDialogueFormat,
                    ["dialogueLinePrefix"] = PromptSettings.dialogueLinePrefix,
                    ["playerName"] = PromptSettings.playerName
                } : null
            );

            // Prepend system prompt if provided
            if (!string.IsNullOrEmpty(runtimeProfile?.SystemPrompt))
            {
                prompt = runtimeProfile.SystemPrompt + "\n\n" + prompt;
            }

            Debug.Log($"[LlamaBrainAgent] Built prompt:\n{prompt}");

            var response = await client.SendPromptAsync(prompt);
            Debug.Log($"[LlamaBrainAgent] Received response: '{response}'");

            dialogueSession.AppendPlayer(input);
            dialogueSession.AppendNpc(response);
            if (runtimeProfile?.UseMemory ?? false)
            {
                Debug.Log($"Memories: {Memories}");
                AddMemory(response);
            }
            return response;
        }

        /// <summary>
        /// Add important information to the NPC's memory
        /// </summary>
        /// <param name="memoryEntry">The memory entry to add</param>
        public void AddMemory(string memoryEntry)
        {
            Debug.Log($"Adding memory: {memoryEntry}");
            memoryProvider.AddMemory(runtimeProfile, memoryEntry);
        }

        /// <summary>
        /// Clear the dialogue history
        /// </summary>
        public void ClearDialogueHistory()
        {
            dialogueSession?.Clear();
        }
    }
}
