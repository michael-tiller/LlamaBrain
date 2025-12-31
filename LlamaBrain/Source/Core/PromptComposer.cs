using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Persona;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Composes prompts for LLM interactions using persona profiles and dialogue context
  /// </summary>
  public sealed class PromptComposer
  {
    /// <summary>
    /// Maximum context length in characters
    /// </summary>
    private const int MaxContextLength = 8000;

    /// <summary>
    /// Maximum history entries to include
    /// </summary>
    private const int MaxHistoryEntries = 10;

    /// <summary>
    /// Composes a complete prompt from a persona profile and dialogue session
    /// </summary>
    /// <param name="profile">The persona profile to use</param>
    /// <param name="session">The dialogue session with conversation history</param>
    /// <param name="userInput">The current user input</param>
    /// <param name="includeTraits">Whether to include personality traits in the prompt (default: true)</param>
    /// <returns>A formatted prompt ready for the LLM</returns>
    public string ComposePrompt(PersonaProfile profile, DialogueSession session, string userInput, bool includeTraits = true)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof(profile));

      if (session == null)
        throw new ArgumentNullException(nameof(session));

      if (string.IsNullOrWhiteSpace(userInput))
        throw new ArgumentException("User input cannot be null or empty", nameof(userInput));

      var prompt = new StringBuilder();

      // Add system prompt
      if (!string.IsNullOrWhiteSpace(profile.SystemPrompt))
      {
        prompt.AppendLine($"System: {profile.SystemPrompt}");
        prompt.AppendLine();
      }

      // Add persona description
      if (!string.IsNullOrWhiteSpace(profile.Description))
      {
        prompt.AppendLine($"You are {profile.Name}, {profile.Description}");
        prompt.AppendLine();
      }

      // Add personality traits (only if requested)
      if (includeTraits && profile.Traits.Count > 0)
      {
        prompt.AppendLine("Your personality traits:");
        foreach (var trait in profile.Traits)
        {
          prompt.AppendLine($"- {trait.Key}: {trait.Value}");
        }
        prompt.AppendLine();
      }

      // Add background story
      if (!string.IsNullOrWhiteSpace(profile.Background))
      {
        prompt.AppendLine($"Background: {profile.Background}");
        prompt.AppendLine();
      }

      // Add conversation history
      var history = session.GetHistory();
      if (history.Count > 0)
      {
        prompt.AppendLine("Conversation history:");

        // Take the last N entries to stay within context limits
        var recentHistory = history.Count > MaxHistoryEntries
          ? history.Skip(history.Count - MaxHistoryEntries).Take(MaxHistoryEntries)
          : history;

        foreach (var entry in recentHistory)
        {
          prompt.AppendLine(entry);
        }
        prompt.AppendLine();
      }

      // Add current user input
      prompt.AppendLine($"Player: {userInput}");
      prompt.AppendLine($"{profile.Name}:");

      // Truncate if too long
      var result = prompt.ToString();
      if (result.Length > MaxContextLength)
      {
        result = result.Substring(0, MaxContextLength) + "...";
      }

      return result;
    }

    /// <summary>
    /// Composes a simple prompt from just a persona profile and user input
    /// </summary>
    /// <param name="profile">The persona profile to use</param>
    /// <param name="userInput">The current user input</param>
    /// <param name="includeTraits">Whether to include personality traits in the prompt (default: true)</param>
    /// <returns>A formatted prompt ready for the LLM</returns>
    public string ComposeSimplePrompt(PersonaProfile profile, string userInput, bool includeTraits = true)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof(profile));

      if (string.IsNullOrWhiteSpace(userInput))
        throw new ArgumentException("User input cannot be null or empty", nameof(userInput));

      var prompt = new StringBuilder();

      // Add system prompt if available
      if (!string.IsNullOrWhiteSpace(profile.SystemPrompt))
      {
        prompt.AppendLine($"System: {profile.SystemPrompt}");
        prompt.AppendLine();
      }

      // Add persona description
      if (!string.IsNullOrWhiteSpace(profile.Description))
      {
        prompt.AppendLine($"You are {profile.Name}, {profile.Description}");
        prompt.AppendLine();
      }

      // Add personality traits (only if requested)
      if (includeTraits && profile.Traits.Count > 0)
      {
        prompt.AppendLine("Your personality traits:");
        foreach (var trait in profile.Traits)
        {
          prompt.AppendLine($"- {trait.Key}: {trait.Value}");
        }
        prompt.AppendLine();
      }

      // Add user input
      prompt.AppendLine($"Player: {userInput}");
      prompt.AppendLine($"{profile.Name}:");

      var result = prompt.ToString();
      if (result.Length > MaxContextLength)
      {
        result = result.Substring(0, MaxContextLength) + "...";
      }

      return result;
    }

    /// <summary>
    /// Static method to compose a prompt with custom settings
    /// </summary>
    /// <param name="personaName">The name of the persona</param>
    /// <param name="description">The description of the persona</param>
    /// <param name="memory">The memory items for the persona</param>
    /// <param name="dialogueHistory">The dialogue history</param>
    /// <param name="playerName">The name of the player (will be overridden by settings if provided)</param>
    /// <param name="playerInput">The player's input</param>
    /// <param name="settings">Optional custom settings dictionary</param>
    /// <returns>A formatted prompt ready for the LLM</returns>
    public static string ComposeWithSettings(string personaName, string description, IReadOnlyList<string> memory,
        IReadOnlyList<string> dialogueHistory, string playerName, string playerInput, Dictionary<string, object>? settings = null)
    {
      var prompt = new StringBuilder();

      // Get settings with defaults
      var npcTemplate = GetSettingValue(settings, "npcTemplate", "NPC: {personaName}");
      var descriptionTemplate = GetSettingValue(settings, "descriptionTemplate", "Description: {description}");
      var memoryHeaderTemplate = GetSettingValue(settings, "memoryHeaderTemplate", "NPC Memory:");
      var memoryItemTemplate = GetSettingValue(settings, "memoryItemTemplate", "- {memory}");
      var dialogueHeaderTemplate = GetSettingValue(settings, "dialogueHeaderTemplate", "Dialogue so far:");
      var playerTemplate = GetSettingValue(settings, "playerTemplate", "Player: {playerName}");
      var playerInputTemplate = GetSettingValue(settings, "playerInputTemplate", "Player says: {playerInput}");
      var responsePromptTemplate = GetSettingValue(settings, "responsePromptTemplate", "NPC responds:");
      var includeEmptyMemory = GetSettingValue(settings, "includeEmptyMemory", false);
      var includeEmptyDialogue = GetSettingValue(settings, "includeEmptyDialogue", false);
      var maxMemoryItems = GetSettingValue(settings, "maxMemoryItems", 10);
      var maxDialogueLines = GetSettingValue(settings, "maxDialogueLines", 10);
      var sectionSeparator = GetSettingValue(settings, "sectionSeparator", "\n");
      var compactDialogueFormat = GetSettingValue(settings, "compactDialogueFormat", false);
      var dialogueLinePrefix = GetSettingValue(settings, "dialogueLinePrefix", "> ");

      // Use player name from settings if available, otherwise use the parameter
      var effectivePlayerName = GetSettingValue(settings, "playerName", playerName);

      // Add NPC section
      prompt.AppendLine(npcTemplate.Replace("{personaName}", personaName));
      prompt.AppendLine();

      // Add description
      if (!string.IsNullOrWhiteSpace(description))
      {
        prompt.AppendLine(descriptionTemplate.Replace("{description}", description));
        prompt.AppendLine();
      }

      // Add memory section
      if (memory.Count > 0 || includeEmptyMemory)
      {
        prompt.AppendLine(memoryHeaderTemplate);
        if (memory.Count > 0)
        {
          var memoryToInclude = memory.Count > maxMemoryItems
            ? memory.Skip(memory.Count - maxMemoryItems).Take(maxMemoryItems)
            : memory;
          foreach (var memoryItem in memoryToInclude)
          {
            prompt.AppendLine(memoryItemTemplate.Replace("{memory}", memoryItem));
          }
        }
        prompt.AppendLine();
      }

      // Add dialogue history
      if (dialogueHistory.Count > 0 || includeEmptyDialogue)
      {
        prompt.AppendLine(dialogueHeaderTemplate);
        if (dialogueHistory.Count > 0)
        {
          var historyToInclude = dialogueHistory.Count > maxDialogueLines
            ? dialogueHistory.Skip(dialogueHistory.Count - maxDialogueLines).Take(maxDialogueLines)
            : dialogueHistory;
          foreach (var dialogueLine in historyToInclude)
          {
            if (compactDialogueFormat)
            {
              prompt.AppendLine(dialogueLinePrefix + dialogueLine);
            }
            else
            {
              prompt.AppendLine(dialogueLine);
            }
          }
        }
        prompt.AppendLine();
      }

      // Add player section
      prompt.AppendLine(playerTemplate.Replace("{playerName}", effectivePlayerName));
      prompt.AppendLine();

      // Add player input
      prompt.AppendLine(playerInputTemplate.Replace("{playerInput}", playerInput));
      prompt.AppendLine();

      // Add response prompt
      prompt.AppendLine(responsePromptTemplate.Replace("{personaName}", personaName));

      return prompt.ToString();
    }

    /// <summary>
    /// Helper method to get setting values with defaults
    /// </summary>
    private static T GetSettingValue<T>(Dictionary<string, object>? settings, string key, T defaultValue)
    {
      if (settings != null && settings.TryGetValue(key, out var value))
      {
        if (value is T typedValue)
        {
          return typedValue;
        }
      }
      return defaultValue;
    }

    /// <summary>
    /// Composes a prompt for a specific task or instruction
    /// </summary>
    /// <param name="profile">The persona profile to use</param>
    /// <param name="instruction">The specific instruction or task</param>
    /// <param name="context">Optional additional context</param>
    /// <returns>A formatted prompt ready for the LLM</returns>
    public string ComposeInstructionPrompt(PersonaProfile profile, string instruction, string? context = null)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof(profile));

      if (string.IsNullOrWhiteSpace(instruction))
        throw new ArgumentException("Instruction cannot be null or empty", nameof(instruction));

      var prompt = new StringBuilder();

      // Add system prompt if available
      if (!string.IsNullOrWhiteSpace(profile.SystemPrompt))
      {
        prompt.AppendLine($"System: {profile.SystemPrompt}");
        prompt.AppendLine();
      }

      // Add persona description
      if (!string.IsNullOrWhiteSpace(profile.Description))
      {
        prompt.AppendLine($"You are {profile.Name}, {profile.Description}");
        prompt.AppendLine();
      }

      // Add personality traits
      if (profile.Traits.Count > 0)
      {
        prompt.AppendLine("Your personality traits:");
        foreach (var trait in profile.Traits)
        {
          prompt.AppendLine($"- {trait.Key}: {trait.Value}");
        }
        prompt.AppendLine();
      }

      // Add instruction
      prompt.AppendLine($"Instruction: {instruction}");

      // Add context if provided
      if (!string.IsNullOrWhiteSpace(context))
      {
        prompt.AppendLine($"Context: {context}");
      }

      prompt.AppendLine();
      prompt.AppendLine($"{profile.Name}:");

      var result = prompt.ToString();
      if (result.Length > MaxContextLength)
      {
        result = result.Substring(0, MaxContextLength) + "...";
      }

      return result;
    }

    /// <summary>
    /// Composes a prompt for structured JSON output with schema validation
    /// </summary>
    /// <param name="profile">The persona profile to use</param>
    /// <param name="instruction">The specific instruction or task</param>
    /// <param name="jsonSchema">The JSON schema to follow</param>
    /// <param name="context">Optional additional context</param>
    /// <returns>A formatted prompt ready for the LLM with JSON structure requirements</returns>
    public string ComposeStructuredJsonPrompt(PersonaProfile profile, string instruction, string jsonSchema, string? context = null)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof(profile));

      if (string.IsNullOrWhiteSpace(instruction))
        throw new ArgumentException("Instruction cannot be null or empty", nameof(instruction));

      if (string.IsNullOrWhiteSpace(jsonSchema))
        throw new ArgumentException("JSON schema cannot be null or empty", nameof(jsonSchema));

      var prompt = new StringBuilder();

      // Add system prompt if available
      if (!string.IsNullOrWhiteSpace(profile.SystemPrompt))
      {
        prompt.AppendLine($"System: {profile.SystemPrompt}");
        prompt.AppendLine();
      }

      // Add structured output instructions
      prompt.AppendLine("IMPORTANT: You must respond with valid JSON only. Do not include any text before or after the JSON.");
      prompt.AppendLine("Your response must be a single JSON object that follows this exact schema:");
      prompt.AppendLine();
      prompt.AppendLine(jsonSchema);
      prompt.AppendLine();

      // Add persona description
      if (!string.IsNullOrWhiteSpace(profile.Description))
      {
        prompt.AppendLine($"You are {profile.Name}, {profile.Description}");
        prompt.AppendLine();
      }

      // Add personality traits
      if (profile.Traits.Count > 0)
      {
        prompt.AppendLine("Your personality traits:");
        foreach (var trait in profile.Traits)
        {
          prompt.AppendLine($"- {trait.Key}: {trait.Value}");
        }
        prompt.AppendLine();
      }

      // Add instruction
      prompt.AppendLine($"Task: {instruction}");

      // Add context if provided
      if (!string.IsNullOrWhiteSpace(context))
      {
        prompt.AppendLine($"Context: {context}");
      }

      prompt.AppendLine();
      prompt.AppendLine("Respond with JSON only:");

      var result = prompt.ToString();
      if (result.Length > MaxContextLength)
      {
        result = result.Substring(0, MaxContextLength) + "...";
      }

      return result;
    }

    /// <summary>
    /// Composes a prompt for structured JSON output with conversation context
    /// </summary>
    /// <param name="profile">The persona profile to use</param>
    /// <param name="session">The dialogue session with conversation history</param>
    /// <param name="userInput">The current user input</param>
    /// <param name="jsonSchema">The JSON schema to follow</param>
    /// <returns>A formatted prompt ready for the LLM with JSON structure requirements</returns>
    public string ComposeStructuredJsonConversationPrompt(PersonaProfile profile, DialogueSession session, string userInput, string jsonSchema)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof(profile));

      if (session == null)
        throw new ArgumentNullException(nameof(session));

      if (string.IsNullOrWhiteSpace(userInput))
        throw new ArgumentException("User input cannot be null or empty", nameof(userInput));

      if (string.IsNullOrWhiteSpace(jsonSchema))
        throw new ArgumentException("JSON schema cannot be null or empty", nameof(jsonSchema));

      var prompt = new StringBuilder();

      // Add system prompt
      if (!string.IsNullOrWhiteSpace(profile.SystemPrompt))
      {
        prompt.AppendLine($"System: {profile.SystemPrompt}");
        prompt.AppendLine();
      }

      // Add structured output instructions
      prompt.AppendLine("IMPORTANT: You must respond with valid JSON only. Do not include any text before or after the JSON.");
      prompt.AppendLine("Your response must be a single JSON object that follows this exact schema:");
      prompt.AppendLine();
      prompt.AppendLine(jsonSchema);
      prompt.AppendLine();

      // Add persona description
      if (!string.IsNullOrWhiteSpace(profile.Description))
      {
        prompt.AppendLine($"You are {profile.Name}, {profile.Description}");
        prompt.AppendLine();
      }

      // Add personality traits
      if (profile.Traits.Count > 0)
      {
        prompt.AppendLine("Your personality traits:");
        foreach (var trait in profile.Traits)
        {
          prompt.AppendLine($"- {trait.Key}: {trait.Value}");
        }
        prompt.AppendLine();
      }

      // Add background story
      if (!string.IsNullOrWhiteSpace(profile.Background))
      {
        prompt.AppendLine($"Background: {profile.Background}");
        prompt.AppendLine();
      }

      // Add conversation history
      var history = session.GetHistory();
      if (history.Count > 0)
      {
        prompt.AppendLine("Conversation history:");

        // Take the last N entries to stay within context limits
        var recentHistory = history.Count > MaxHistoryEntries
          ? history.Skip(history.Count - MaxHistoryEntries).Take(MaxHistoryEntries)
          : history;

        foreach (var entry in recentHistory)
        {
          prompt.AppendLine(entry);
        }
        prompt.AppendLine();
      }

      // Add current user input
      prompt.AppendLine($"Player: {userInput}");
      prompt.AppendLine();
      prompt.AppendLine("Respond with JSON only:");

      // Truncate if too long
      var result = prompt.ToString();
      if (result.Length > MaxContextLength)
      {
        result = result.Substring(0, MaxContextLength) + "...";
      }

      return result;
    }

    /// <summary>
    /// Composes a prompt split into static prefix (for KV cache) and dynamic suffix.
    /// The prefix contains static content that can be cached, while the suffix contains dynamic content that changes with each request.
    /// </summary>
    /// <param name="profile">The persona profile to use.</param>
    /// <param name="session">The dialogue session with conversation history.</param>
    /// <param name="userInput">The current user input.</param>
    /// <param name="includeTraits">Whether to include personality traits in the prompt (default: true).</param>
    /// <returns>A tuple containing the static prefix string (can be cached for KV cache optimization) and the dynamic suffix string (changes with each request). The prefix is the first element and the suffix is the second element.</returns>
    /// <remarks>This method returns a tuple to enable efficient KV cache usage by separating static and dynamic prompt content.</remarks>
    public (string prefix, string suffix) ComposePromptWithPrefix(PersonaProfile profile, DialogueSession session, string userInput, bool includeTraits = true)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof(profile));

      if (session == null)
        throw new ArgumentNullException(nameof(session));

      if (string.IsNullOrWhiteSpace(userInput))
        throw new ArgumentException("User input cannot be null or empty", nameof(userInput));

      var prefixBuilder = new StringBuilder();
      var suffixBuilder = new StringBuilder();

      // Static prefix: system prompt, persona description, traits, background
      // This part rarely changes and can be cached
      if (!string.IsNullOrWhiteSpace(profile.SystemPrompt))
      {
        prefixBuilder.AppendLine($"System: {profile.SystemPrompt}");
        prefixBuilder.AppendLine();
      }

      if (!string.IsNullOrWhiteSpace(profile.Description))
      {
        prefixBuilder.AppendLine($"You are {profile.Name}, {profile.Description}");
        prefixBuilder.AppendLine();
      }

      if (includeTraits && profile.Traits.Count > 0)
      {
        prefixBuilder.AppendLine("Your personality traits:");
        foreach (var trait in profile.Traits)
        {
          prefixBuilder.AppendLine($"- {trait.Key}: {trait.Value}");
        }
        prefixBuilder.AppendLine();
      }

      if (!string.IsNullOrWhiteSpace(profile.Background))
      {
        prefixBuilder.AppendLine($"Background: {profile.Background}");
        prefixBuilder.AppendLine();
      }

      // Dynamic suffix: conversation history + current input
      // This changes every request
      var history = session.GetHistory();
      if (history.Count > 0)
      {
        suffixBuilder.AppendLine("Conversation history:");

        var recentHistory = history.Count > MaxHistoryEntries
          ? history.Skip(history.Count - MaxHistoryEntries).Take(MaxHistoryEntries)
          : history;

        foreach (var entry in recentHistory)
        {
          suffixBuilder.AppendLine(entry);
        }
        suffixBuilder.AppendLine();
      }

      suffixBuilder.AppendLine($"Player: {userInput}");
      suffixBuilder.AppendLine($"{profile.Name}:");

      var prefix = prefixBuilder.ToString();
      var suffix = suffixBuilder.ToString();

      // Truncate if too long
      var totalLength = prefix.Length + suffix.Length;
      if (totalLength > MaxContextLength)
      {
        var availableForSuffix = MaxContextLength - prefix.Length;
        if (availableForSuffix > 0)
        {
          suffix = suffix.Length > availableForSuffix
            ? suffix.Substring(0, availableForSuffix) + "..."
            : suffix;
        }
        else
        {
          // Prefix itself is too long, truncate it
          prefix = prefix.Substring(0, MaxContextLength) + "...";
          suffix = string.Empty;
        }
      }

      return (prefix, suffix);
    }
  }
}