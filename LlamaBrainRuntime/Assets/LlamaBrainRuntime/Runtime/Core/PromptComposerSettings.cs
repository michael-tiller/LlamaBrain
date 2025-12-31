using System;
using UnityEngine;

/// <summary>
/// This namespace contains the core components for the LlamaBrain for Unity project.
/// </summary>
namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// Settings for configuring the PromptComposer behavior and templates.
  /// </summary>
  [CreateAssetMenu(fileName = "PromptComposerSettings", menuName = "LlamaBrain/Prompt Composer Settings")]
  public class PromptComposerSettings : ScriptableObject
  {
    /// <summary>
    /// The template for the NPC section.
    /// </summary>
    [Header("Prompt Template")]
    [TextArea(3, 6)]
    [Tooltip("Template for the NPC section. Use {personaName} as placeholder.")]
    public string npcTemplate = "NPC: {personaName}";

    /// <summary>
    /// The template for the description section.
    /// </summary>
    [TextArea(3, 6)]
    [Tooltip("Template for the description section. Use {description} as placeholder.")]
    public string descriptionTemplate = "Description: {description}";

    /// <summary>
    /// The template for the memory section header.
    /// </summary>
    [TextArea(3, 6)]
    [Tooltip("Template for the memory section header. Use {memoryCount} as placeholder for number of memories.")]
    public string memoryHeaderTemplate = "NPC Memory:";

    /// <summary>
    /// The template for individual memory items.
    /// </summary>
    [TextArea(3, 6)]
    [Tooltip("Template for individual memory items. Use {memory} as placeholder.")]
    public string memoryItemTemplate = "- {memory}";

    /// <summary>
    /// The template for the dialogue history section header.
    /// </summary>
    [TextArea(3, 6)]
    [Tooltip("Template for the dialogue history section header.")]
    public string dialogueHeaderTemplate = "Dialogue so far:";

    /// <summary>
    /// The template for the player section.
    /// </summary>
    [TextArea(3, 6)]
    [Tooltip("Template for the player section. Use {playerName} as placeholder.")]
    public string playerTemplate = "Player: {playerName}";

    /// <summary>
    /// The default name for the player.
    /// </summary>
    [Header("Player Configuration")]
    [Tooltip("The default name for the player.")]
    public string playerName = "Adventurer";

    /// <summary>
    /// The template for the player input section.
    /// </summary>
    [TextArea(3, 6)]
    [Tooltip("Template for the player input section. Use {playerInput} as placeholder.")]
    public string playerInputTemplate = "Player says: {playerInput}";

    /// <summary>
    /// The template for the NPC response prompt.
    /// </summary>
    [TextArea(3, 6)]
    [Tooltip("Template for the NPC response prompt. Use {personaName} as placeholder.")]
    public string responsePromptTemplate = "NPC responds:";

    /// <summary>
    /// Whether to include the memory section if there are no memories.
    /// </summary>
    [Header("Formatting Options")]
    [Tooltip("Whether to include the memory section if there are no memories.")]
    public bool includeEmptyMemory = false;

    /// <summary>
    /// Whether to include the dialogue history section if there's no history.
    /// </summary>
    [Tooltip("Whether to include the dialogue history section if there's no history.")]
    public bool includeEmptyDialogue = false;

    /// <summary>
    /// The maximum number of memory items to include in the prompt.
    /// </summary>
    [Tooltip("Maximum number of memory items to include in the prompt.")]
    [Range(1, 50)]
    public int maxMemoryItems = 10;

    /// <summary>
    /// The maximum number of dialogue history lines to include in the prompt.
    /// </summary>
    [Tooltip("Maximum number of dialogue history lines to include in the prompt.")]
    [Range(1, 100)]
    public int maxDialogueLines = 20;

    /// <summary>
    /// The separator between sections.
    /// </summary>
    [Tooltip("Separator between sections.")]
    public string sectionSeparator = "\n";

    /// <summary>
    /// Whether to use a more compact format for dialogue history.
    /// </summary>
    [Header("Advanced Options")]
    [Tooltip("Whether to use a more compact format for dialogue history.")]
    public bool compactDialogueFormat = false;

    /// <summary>
    /// The custom prefix for dialogue lines when using compact format.
    /// </summary>
    [Tooltip("Custom prefix for dialogue lines when using compact format.")]
    public string dialogueLinePrefix = "";

    /// <summary>
    /// Whether to include personality traits in prompts sent to the LLM.
    /// </summary>
    [Tooltip("Whether to include personality traits in prompts sent to the LLM. Disable to prevent trait information from appearing in responses.")]
    public bool includePersonalityTraits = true;

    /// <summary>
    /// Creates a default PromptComposerSettings instance.
    /// </summary>
    /// <returns>A new PromptComposerSettings instance with default values</returns>
    public static PromptComposerSettings CreateDefault()
    {
      var settings = CreateInstance<PromptComposerSettings>();
      settings.name = "Default Prompt Composer Settings";
      return settings;
    }

    /// <summary>
    /// Creates a compact PromptComposerSettings instance for shorter prompts.
    /// </summary>
    /// <returns>A new PromptComposerSettings instance optimized for compact prompts</returns>
    public static PromptComposerSettings CreateCompact()
    {
      var settings = CreateInstance<PromptComposerSettings>();
      settings.name = "Compact Prompt Composer Settings";

      settings.npcTemplate = "NPC: {personaName}";
      settings.descriptionTemplate = "{description}";
      settings.memoryHeaderTemplate = "Memory:";
      settings.memoryItemTemplate = "- {memory}";
      settings.dialogueHeaderTemplate = "History:";
      settings.playerTemplate = "Player: {playerName}";
      settings.playerInputTemplate = "> {playerInput}";
      settings.responsePromptTemplate = "{personaName}:";
      settings.playerName = "Adventurer";
      settings.includeEmptyMemory = false;
      settings.includeEmptyDialogue = false;
      settings.maxMemoryItems = 5;
      settings.maxDialogueLines = 10;
      settings.sectionSeparator = "\n";
      settings.compactDialogueFormat = true;
      settings.dialogueLinePrefix = "";
      settings.includePersonalityTraits = false;

      return settings;
    }

    /// <summary>
    /// Creates a detailed PromptComposerSettings instance for more verbose prompts.
    /// </summary>
    /// <returns>A new PromptComposerSettings instance optimized for detailed prompts</returns>
    public static PromptComposerSettings CreateDetailed()
    {
      var settings = CreateInstance<PromptComposerSettings>();
      settings.name = "Detailed Prompt Composer Settings";

      settings.npcTemplate = "Character: {personaName}";
      settings.descriptionTemplate = "Background: {description}";
      settings.memoryHeaderTemplate = "Character Memory (Recent Events):";
      settings.memoryItemTemplate = "• {memory}";
      settings.dialogueHeaderTemplate = "Conversation History:";
      settings.playerTemplate = "Player Character: {playerName}";
      settings.playerInputTemplate = "Player says: \"{playerInput}\"";
      settings.responsePromptTemplate = "How does {personaName} respond?";
      settings.playerName = "Adventurer";
      settings.includeEmptyMemory = true;
      settings.includeEmptyDialogue = true;
      settings.maxMemoryItems = 15;
      settings.maxDialogueLines = 30;
      settings.sectionSeparator = "\n\n";
      settings.compactDialogueFormat = false;
      settings.dialogueLinePrefix = "";
      settings.includePersonalityTraits = true;

      return settings;
    }

    /// <summary>
    /// Validates the settings and returns any validation errors.
    /// </summary>
    /// <returns>An array of validation error messages, or an empty array if validation passes</returns>
    public string[] ValidateSettings()
    {
      var errors = new System.Collections.Generic.List<string>();

      if (string.IsNullOrEmpty(npcTemplate))
        errors.Add("NPC template cannot be empty");

      if (string.IsNullOrEmpty(descriptionTemplate))
        errors.Add("Description template cannot be empty");

      if (string.IsNullOrEmpty(memoryHeaderTemplate))
        errors.Add("Memory header template cannot be empty");

      if (string.IsNullOrEmpty(memoryItemTemplate))
        errors.Add("Memory item template cannot be empty");

      if (string.IsNullOrEmpty(dialogueHeaderTemplate))
        errors.Add("Dialogue header template cannot be empty");

      if (string.IsNullOrEmpty(playerTemplate))
        errors.Add("Player template cannot be empty");

      if (string.IsNullOrEmpty(playerInputTemplate))
        errors.Add("Player input template cannot be empty");

      if (string.IsNullOrEmpty(responsePromptTemplate))
        errors.Add("Response prompt template cannot be empty");

      if (maxMemoryItems < 1)
        errors.Add("Max memory items must be at least 1");

      if (maxDialogueLines < 1)
        errors.Add("Max dialogue lines must be at least 1");

      return errors.ToArray();
    }
  }
}