using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using LlamaBrain.Core;
using LlamaBrain.Persona;
using System.Linq;

namespace LlamaBrain.Unity.Runtime.Core
{
    /// <summary>
    /// A LlamaBrain agent that lives in Unity that can be used to interact with a LlamaBrain server.
    /// </summary>
    public class UnityBrainAgent : MonoBehaviour
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
        /// The memory category manager for this agent.
        /// </summary>
        [Header("Memory Settings")]
        public MemoryCategoryManager MemoryCategoryManager;

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
        /// Separate storage for conversation history (not mixed with memory)
        /// </summary>
        private List<DialogueEntry> conversationHistory = new List<DialogueEntry>();

        /// <summary>
        /// Maximum number of conversation history entries to keep
        /// </summary>
        [Header("Conversation Settings")]
        [SerializeField] private int maxConversationHistoryEntries = 20;

        /// <summary>
        /// Whether to store conversation history automatically
        /// </summary>
        [SerializeField] private bool storeConversationHistory = true;

        /// <summary>
        /// The current runtime profile
        /// </summary>
        public PersonaProfile RuntimeProfile
        {
            get => runtimeProfile;
            set => runtimeProfile = value;
        }


        /// <summary>
        /// The memories of the persona.
        /// </summary>
        public string Memories => Application.isPlaying ? string.Join("\n", memoryProvider.GetMemory(runtimeProfile)) : string.Empty;

        /// <summary>
        /// Initializes the LlamaBrain agent.
        /// </summary>
        public void Initialize(ApiClient client, PersonaMemoryStore memoryProvider)
        {
            initializationAttempted = true;
            Debug.Log("[UnityBrainAgent] Starting initialization...");

            if (client == null)
            {
                Debug.LogError("[UnityBrainAgent] Initialize failed: ApiClient is null");
                return;
            }

            if (memoryProvider == null)
            {
                Debug.LogError("[UnityBrainAgent] Initialize failed: PersonaMemoryStore is null");
                return;
            }

            try
            {
                this.client = client;
                this.memoryProvider = memoryProvider;

                Debug.Log("[UnityBrainAgent] Client and memory provider set successfully");

                // Convert config to runtime profile if available
                if (PersonaConfig != null && runtimeProfile == null)
                {
                    runtimeProfile = PersonaConfig.ToProfile();
                    Debug.Log($"[UnityBrainAgent] Converted PersonaConfig to profile: {runtimeProfile?.Name ?? "Unknown"}");
                }

                // Initialize dialogue session with persona ID
                var personaId = runtimeProfile?.PersonaId ?? runtimeProfile?.Name ?? "Unknown";
                dialogueSession = new DialogueSession(personaId, memoryProvider);
                Debug.Log($"[UnityBrainAgent] Dialogue session initialized with persona ID: {personaId}");

                Debug.Log($"[UnityBrainAgent] Initialization complete. IsInitialized: {IsInitialized}");
                Debug.Log($"[UnityBrainAgent] Final state - Client: {(client != null ? "Set" : "Null")}, MemoryProvider: {(memoryProvider != null ? "Set" : "Null")}, RuntimeProfile: {(runtimeProfile != null ? $"Set ({runtimeProfile.Name})" : "Null")}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UnityBrainAgent] Initialize failed with exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
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

            // Use PromptComposer for consistent prompt formatting
            var promptComposer = new PromptComposer();
            var prompt = promptComposer.ComposePrompt(runtimeProfile, dialogueSession, input);

            // Prepend system prompt if provided
            if (!string.IsNullOrEmpty(runtimeProfile?.SystemPrompt))
            {
                prompt = runtimeProfile.SystemPrompt + "\n\n" + prompt;
            }

            Debug.Log($"[UnityBrainAgent] Built prompt:\n{prompt}");

            if (client == null)
            {
                var errorMsg = "Cannot send prompt: Client is null. Make sure the agent is properly initialized.";
                Debug.LogError($"[UnityBrainAgent] {errorMsg}");
                return errorMsg;
            }

            try
            {
                var response = await client.SendPromptAsync(prompt);
                Debug.Log($"[UnityBrainAgent] Received response: '{response}'");

                // Store conversation history separately from memory
                if (storeConversationHistory)
                {
                    AddToConversationHistory("Player", input);
                    AddToConversationHistory("NPC", response);
                }

                // Keep DialogueSession for prompt composition but don't store history there
                dialogueSession.AppendPlayer(input);
                dialogueSession.AppendNpc(response);

                // Only extract memory if explicitly enabled and there's meaningful information
                if (runtimeProfile?.UseMemory ?? false)
                {
                    ExtractAndStoreImportantInformation(input, response);
                }
                return response;
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"Network error: {ex.Message}";
                Debug.LogError($"[UnityBrainAgent] {errorMsg}");
                return errorMsg;
            }
        }

        /// <summary>
        /// Add important information to the NPC's memory
        /// </summary>
        /// <param name="memoryEntry">The memory entry to add</param>
        public void AddMemory(string memoryEntry)
        {
            if (runtimeProfile == null || memoryProvider == null)
            {
                Debug.LogWarning("[UnityBrainAgent] Cannot add memory: Agent not initialized or runtime profile is null");
                return;
            }

            Debug.Log($"Adding memory: {memoryEntry}");
            memoryProvider.AddMemory(runtimeProfile, memoryEntry);
        }

        /// <summary>
        /// Add important information to the NPC's memory with a specific category
        /// </summary>
        /// <param name="category">The category of memory (e.g., "PlayerInfo", "WorldFacts", "Preferences")</param>
        /// <param name="memoryEntry">The memory entry to add</param>
        public void AddMemory(string category, string memoryEntry)
        {
            if (runtimeProfile == null || memoryProvider == null)
            {
                Debug.LogWarning("[UnityBrainAgent] Cannot add memory: Agent not initialized or runtime profile is null");
                return;
            }

            var categorizedMemory = $"[{category}] {memoryEntry}";
            Debug.Log($"Adding categorized memory: {categorizedMemory}");
            memoryProvider.AddMemory(runtimeProfile, categorizedMemory);
        }

        /// <summary>
        /// Extract and store important information from a conversation exchange
        /// </summary>
        /// <param name="playerInput">The player's input</param>
        /// <param name="npcResponse">The NPC's response</param>
        private void ExtractAndStoreImportantInformation(string playerInput, string npcResponse)
        {
            if (!(runtimeProfile?.UseMemory ?? false))
                return;

            // Only extract memory if the input seems meaningful (not just greetings, etc.)
            if (!IsMeaningfulInput(playerInput))
                return;

            // Use ScriptableObject-based extraction if available
            if (MemoryCategoryManager != null)
            {
                ExtractUsingScriptableObjectCategories(playerInput, npcResponse);
            }
            else
            {
                // Fallback to hardcoded extraction
                ExtractUsingHardcodedRules(playerInput, npcResponse);
            }
        }

        /// <summary>
        /// Check if the input contains meaningful information worth storing as memory
        /// </summary>
        /// <param name="input">The player input to check</param>
        /// <returns>True if the input contains meaningful information</returns>
        private bool IsMeaningfulInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var lowerInput = input.ToLower().Trim();

            // Skip very short inputs
            if (lowerInput.Length < 10)
                return false;

            // Skip common greetings and casual responses
            var casualPhrases = new[]
            {
                "hello", "hi", "hey", "goodbye", "bye", "thanks", "thank you",
                "yes", "no", "ok", "okay", "sure", "maybe", "i don't know",
                "what", "how", "why", "when", "where", "who",
                "please", "excuse me", "sorry", "pardon",
                "um", "uh", "ah", "oh", "hmm", "well",
                "you know", "i mean", "like", "sort of", "kind of",
                "good morning", "good afternoon", "good evening", "good night"
            };

            // If the input is just a casual phrase, don't extract memory
            if (casualPhrases.Any(phrase => lowerInput.Contains(phrase) && lowerInput.Length < 20))
                return false;

            // Look for indicators of meaningful information
            var meaningfulIndicators = new[]
            {
                "my name is", "i'm", "i am", "call me",
                "i like", "i love", "i prefer", "i hate", "i don't like",
                "i am a", "i'm a", "i work as", "i do",
                "remember", "don't forget", "important", "note",
                "quest", "mission", "task", "help", "save", "rescue",
                "family", "friend", "home", "town", "city", "village"
            };

            return meaningfulIndicators.Any(indicator => lowerInput.Contains(indicator));
        }

        /// <summary>
        /// Extract information using ScriptableObject-based memory categories
        /// </summary>
        /// <param name="playerInput">The player's input</param>
        /// <param name="npcResponse">The NPC's response</param>
        private void ExtractUsingScriptableObjectCategories(string playerInput, string npcResponse)
        {
            // Only extract from player input, not NPC responses (to avoid junk)
            var playerExtractions = MemoryCategoryManager.ExtractInformationFromText(playerInput);

            foreach (var kvp in playerExtractions)
            {
                var category = MemoryCategoryManager.GetCategory(kvp.Key);
                if (category != null && ShouldStoreMemory(category, kvp.Value))
                {
                    foreach (var extractedInfo in kvp.Value)
                    {
                        // Check if this memory already exists to avoid duplicates
                        if (!MemoryExists(category, extractedInfo))
                        {
                            var memoryEntry = category.FormatMemoryEntry(extractedInfo);
                            AddMemory(memoryEntry);
                            Debug.Log($"[UnityBrainAgent] Extracted important memory using category '{category.DisplayName}': {extractedInfo}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine if a memory should be stored based on category importance and content quality
        /// </summary>
        /// <param name="category">The memory category</param>
        /// <param name="extractedInfo">The extracted information</param>
        /// <returns>True if the memory should be stored</returns>
        private bool ShouldStoreMemory(MemoryCategory category, List<string> extractedInfo)
        {
            // Only store if category has high importance
            if (category.Importance < 0.5f)
                return false;

            // Check if we're at max entries for this category
            var existingMemories = GetMemoriesByCategory(category.CategoryName);
            if (existingMemories.Count >= category.MaxEntries)
                return false;

            // Filter out common junk phrases
            foreach (var info in extractedInfo)
            {
                var lowerInfo = info.ToLower();
                if (IsJunkPhrase(lowerInfo))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Check if a phrase is junk that shouldn't be stored as memory
        /// </summary>
        /// <param name="phrase">The phrase to check</param>
        /// <returns>True if it's junk</returns>
        private bool IsJunkPhrase(string phrase)
        {
            var junkPhrases = new[]
            {
                "hello", "hi", "hey", "goodbye", "bye", "thanks", "thank you",
                "yes", "no", "ok", "okay", "sure", "maybe", "i don't know",
                "what", "how", "why", "when", "where", "who",
                "please", "excuse me", "sorry", "pardon",
                "um", "uh", "ah", "oh", "hmm", "well",
                "you know", "i mean", "like", "sort of", "kind of"
            };

            return junkPhrases.Any(junk => phrase.Contains(junk));
        }

        /// <summary>
        /// Check if a memory already exists for this category and content
        /// </summary>
        /// <param name="category">The memory category</param>
        /// <param name="content">The content to check</param>
        /// <returns>True if the memory already exists</returns>
        private bool MemoryExists(MemoryCategory category, string content)
        {
            var existingMemories = GetMemoriesByCategory(category.CategoryName);
            var normalizedContent = content.ToLower().Trim();

            foreach (var memory in existingMemories)
            {
                var parsedContent = category.ParseMemoryEntry(memory);
                if (parsedContent != null && parsedContent.ToLower().Trim() == normalizedContent)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Extract information using hardcoded rules (fallback)
        /// </summary>
        /// <param name="playerInput">The player's input</param>
        /// <param name="npcResponse">The NPC's response</param>
        private void ExtractUsingHardcodedRules(string playerInput, string npcResponse)
        {
            var extractedFacts = new List<string>();

            // Extract player name if mentioned
            ExtractPlayerName(playerInput, npcResponse, extractedFacts);

            // Extract preferences and important facts
            ExtractPreferences(playerInput, npcResponse, extractedFacts);

            // Extract world knowledge and important events
            ExtractWorldKnowledge(playerInput, npcResponse, extractedFacts);

            // Store extracted facts as memory
            foreach (var fact in extractedFacts)
            {
                AddMemory(fact);
            }
        }

        /// <summary>
        /// Extract player name from conversation
        /// </summary>
        private void ExtractPlayerName(string playerInput, string npcResponse, List<string> extractedFacts)
        {
            // Look for patterns like "My name is X" or "I'm X" or "Call me X"
            var namePatterns = new[]
            {
                @"(?:my name is|i'm|i am|call me)\s+([a-zA-Z]+)",
                @"(?:name's|name is)\s+([a-zA-Z]+)"
            };

            foreach (var pattern in namePatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(playerInput.ToLower(), pattern);
                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    if (name.Length > 1) // Avoid single letters
                    {
                        extractedFacts.Add($"PlayerInfo: Player's name is {char.ToUpper(name[0]) + name.Substring(1)}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Extract preferences and important facts from conversation
        /// </summary>
        private void ExtractPreferences(string playerInput, string npcResponse, List<string> extractedFacts)
        {
            var input = playerInput.ToLower();

            // Extract preferences
            if (input.Contains("like") || input.Contains("love") || input.Contains("prefer"))
            {
                if (input.Contains("magic") || input.Contains("spell"))
                    extractedFacts.Add("Preferences: Player prefers magic/spells");
                if (input.Contains("weapon") || input.Contains("sword") || input.Contains("combat"))
                    extractedFacts.Add("Preferences: Player prefers weapons/combat");
                if (input.Contains("stealth") || input.Contains("sneak"))
                    extractedFacts.Add("Preferences: Player prefers stealth approach");
            }

            // Extract important facts about the player
            if (input.Contains("i am") || input.Contains("i'm"))
            {
                if (input.Contains("wizard") || input.Contains("mage"))
                    extractedFacts.Add("PlayerInfo: Player is a wizard/mage");
                if (input.Contains("warrior") || input.Contains("fighter"))
                    extractedFacts.Add("PlayerInfo: Player is a warrior/fighter");
                if (input.Contains("rogue") || input.Contains("thief"))
                    extractedFacts.Add("PlayerInfo: Player is a rogue/thief");
            }
        }

        /// <summary>
        /// Extract world knowledge and important events
        /// </summary>
        private void ExtractWorldKnowledge(string playerInput, string npcResponse, List<string> extractedFacts)
        {
            var input = playerInput.ToLower();
            var response = npcResponse.ToLower();

            // Look for important world events or knowledge
            if (input.Contains("quest") || input.Contains("mission"))
            {
                if (input.Contains("complete") || input.Contains("finished"))
                    extractedFacts.Add("WorldEvents: Player completed a quest");
                if (input.Contains("start") || input.Contains("begin"))
                    extractedFacts.Add("WorldEvents: Player started a quest");
            }

            // Extract location information
            if (input.Contains("town") || input.Contains("city") || input.Contains("village"))
            {
                var locationMatch = System.Text.RegularExpressions.Regex.Match(input, @"(?:in|at|to)\s+([a-zA-Z\s]+)(?:town|city|village)");
                if (locationMatch.Success)
                {
                    var location = locationMatch.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(location))
                        extractedFacts.Add($"WorldInfo: Player mentioned location {location}");
                }
            }
        }

        /// <summary>
        /// Manually add a specific memory entry with category
        /// </summary>
        /// <param name="category">The category of the memory</param>
        /// <param name="content">The memory content</param>
        public void AddSpecificMemory(string category, string content)
        {
            AddMemory(category, content);
        }

        /// <summary>
        /// Get memories by category
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>List of memories in the specified category</returns>
        public IReadOnlyList<string> GetMemoriesByCategory(string category)
        {
            var allMemories = GetMemories();
            var filteredMemories = new List<string>();

            foreach (var memory in allMemories)
            {
                if (memory.StartsWith($"[{category}]"))
                {
                    filteredMemories.Add(memory);
                }
            }

            return filteredMemories;
        }

        /// <summary>
        /// Add an entry to the conversation history
        /// </summary>
        /// <param name="speaker">The speaker (Player or NPC)</param>
        /// <param name="text">The text spoken</param>
        private void AddToConversationHistory(string speaker, string text)
        {
            var entry = new DialogueEntry
            {
                Speaker = speaker,
                Text = text,
                Timestamp = System.DateTime.Now
            };

            conversationHistory.Add(entry);

            // Keep only the most recent entries
            while (conversationHistory.Count > maxConversationHistoryEntries)
            {
                conversationHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Clear the conversation history
        /// </summary>
        public void ClearDialogueHistory()
        {
            conversationHistory.Clear();
            dialogueSession?.Clear();
        }

        /// <summary>
        /// Get all memories for the persona as a list
        /// </summary>
        /// <returns>List of all memories</returns>
        public IReadOnlyList<string> GetMemories()
        {
            if (runtimeProfile == null || memoryProvider == null)
                return new List<string>();

            return memoryProvider.GetMemory(runtimeProfile);
        }

        /// <summary>
        /// Clear all memories for the persona
        /// </summary>
        public void ClearMemories()
        {
            if (runtimeProfile == null || memoryProvider == null)
                return;

            Debug.Log($"[UnityBrainAgent] Clearing all memories for {runtimeProfile.Name}");
            memoryProvider.ClearMemory(runtimeProfile);
        }

        /// <summary>
        /// Get the conversation history as a list of strings
        /// </summary>
        /// <returns>List of conversation history entries</returns>
        public IReadOnlyList<string> GetConversationHistory()
        {
            var history = new List<string>();
            foreach (var entry in conversationHistory)
            {
                history.Add($"{entry.Speaker}: {entry.Text}");
            }
            return history;
        }

        /// <summary>
        /// Get recent conversation history entries
        /// </summary>
        /// <param name="count">Number of recent entries to retrieve</param>
        /// <returns>List of recent dialogue entries</returns>
        public IReadOnlyList<DialogueEntry> GetRecentHistory(int count)
        {
            var recentCount = Mathf.Min(count, conversationHistory.Count);
            var startIndex = conversationHistory.Count - recentCount;
            return conversationHistory.GetRange(startIndex, recentCount);
        }

        /// <summary>
        /// Get the number of memories stored
        /// </summary>
        public int MemoryCount => GetMemories().Count;

        /// <summary>
        /// Get the number of conversation history entries
        /// </summary>
        public int ConversationHistoryCount => conversationHistory.Count;

        /// <summary>
        /// Check if the persona has any memories
        /// </summary>
        public bool HasMemories => MemoryCount > 0;

        /// <summary>
        /// Check if the persona has any conversation history
        /// </summary>
        public bool HasConversationHistory => conversationHistory.Count > 0;

        /// <summary>
        /// Check if the agent is properly initialized
        /// </summary>
        public bool IsInitialized => client != null && memoryProvider != null && runtimeProfile != null;

        /// <summary>
        /// Check if the agent is connected to the server
        /// </summary>
        public bool IsConnected => client != null;

        /// <summary>
        /// Check if initialization was attempted (regardless of success)
        /// </summary>
        private bool initializationAttempted = false;
        public bool WasInitializationAttempted => initializationAttempted;

        /// <summary>
        /// Get the connection status message
        /// </summary>
        public string ConnectionStatus
        {
            get
            {
                if (!IsInitialized)
                    return "Agent not initialized";
                if (!IsConnected)
                    return "Not connected to server";
                return "Connected";
            }
        }

        /// <summary>
        /// Get setup instructions for the current state
        /// </summary>
        public string GetSetupInstructions()
        {
            if (!WasInitializationAttempted)
            {
                return "Initialization Issue:\n" +
                       "1. Initialize() method has never been called\n" +
                       "2. Check if NpcAgentExample.Start() is running\n" +
                       "3. Verify that the agent component is properly referenced\n" +
                       "4. Look for initialization errors in the console";
            }

            if (!IsInitialized)
            {
                return "Setup Instructions:\n" +
                       "1. Add a BrainServer component to your scene\n" +
                       "2. Configure BrainSettings with valid ExecutablePath and ModelPath\n" +
                       "3. Or use NpcAgentExample component which handles setup automatically\n" +
                       "4. Check console for initialization error messages";
            }

            if (!IsConnected)
            {
                return "Connection Issue:\n" +
                       "1. Check if BrainServer is running\n" +
                       "2. Verify BrainSettings configuration\n" +
                       "3. Ensure llama.cpp executable and model files exist\n" +
                       "4. Check server logs for startup errors";
            }

            return "Agent is ready to use!";
        }

        /// <summary>
        /// Debug method to log the current state of the agent
        /// </summary>
        [ContextMenu("Debug Agent State")]
        public void DebugAgentState()
        {
            Debug.Log($"[UnityBrainAgent] Debug State:");
            Debug.Log($"  - WasInitializationAttempted: {WasInitializationAttempted}");
            Debug.Log($"  - IsInitialized: {IsInitialized}");
            Debug.Log($"  - IsConnected: {IsConnected}");
            Debug.Log($"  - Client: {(client != null ? "Set" : "Null")}");
            Debug.Log($"  - MemoryProvider: {(memoryProvider != null ? "Set" : "Null")}");
            Debug.Log($"  - RuntimeProfile: {(runtimeProfile != null ? $"Set ({runtimeProfile.Name})" : "Null")}");
            Debug.Log($"  - PersonaConfig: {(PersonaConfig != null ? $"Set ({PersonaConfig.Name})" : "Null")}");
            Debug.Log($"  - DialogueSession: {(dialogueSession != null ? "Set" : "Null")}");
            Debug.Log($"  - ConnectionStatus: {ConnectionStatus}");
        }
    }
}
