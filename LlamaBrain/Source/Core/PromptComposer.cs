using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Utility for assembling LLM prompts with persona and session context.
  /// </summary>
  public static class PromptComposer
  {
    /// <summary>
    /// Compose a prompt for an LLM with persona and session context using default settings.
    /// </summary>
    /// <param name="personaName">The name of the persona.</param>
    /// <param name="description">The description of the persona.</param>
    /// <param name="memory">The memory of the persona.</param>
    /// <param name="dialogueHistory">The dialogue history.</param>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="playerInput">The input from the player.</param>
    public static string Compose(
        string personaName,
        string description,
        IReadOnlyList<string> memory,
        IReadOnlyList<string> dialogueHistory,
        string playerName,
        string playerInput)
    {
      return Compose(personaName, description, memory, dialogueHistory, playerName, playerInput, null);
    }

    /// <summary>
    /// Compose a prompt for an LLM with persona and session context using a dictionary of settings.
    /// </summary>
    /// <param name="personaName">The name of the persona.</param>
    /// <param name="description">The description of the persona.</param>
    /// <param name="memory">The memory of the persona.</param>
    /// <param name="dialogueHistory">The dialogue history.</param>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="playerInput">The input from the player.</param>
    /// <param name="settings">Custom settings as a dictionary.</param>
    public static string ComposeWithSettings(
        string personaName,
        string description,
        IReadOnlyList<string> memory,
        IReadOnlyList<string> dialogueHistory,
        string playerName,
        string playerInput,
        Dictionary<string, object> settings)
    {
      var sb = new StringBuilder();

      // Use default settings if none provided
      var npcTemplate = "NPC: {personaName}";
      var descriptionTemplate = "Description: {description}";
      var memoryHeaderTemplate = "NPC Memory:";
      var memoryItemTemplate = "- {memory}";
      var dialogueHeaderTemplate = "Dialogue so far:";
      var playerTemplate = "Player: {playerName}";
      var playerInputTemplate = "Player says: {playerInput}";
      var responsePromptTemplate = "NPC responds:";
      var includeEmptyMemory = false;
      var includeEmptyDialogue = false;
      var maxMemoryItems = 10;
      var maxDialogueLines = 20;
      var sectionSeparator = "\n";
      var compactDialogueFormat = false;
      var dialogueLinePrefix = "";

      // Apply custom settings if provided
      if (settings != null)
      {
        npcTemplate = settings.TryGetValue("npcTemplate", out var npc) && npc is string npcStr ? npcStr : npcTemplate;
        descriptionTemplate = settings.TryGetValue("descriptionTemplate", out var desc) && desc is string descStr ? descStr : descriptionTemplate;
        memoryHeaderTemplate = settings.TryGetValue("memoryHeaderTemplate", out var memHeader) && memHeader is string memHeaderStr ? memHeaderStr : memoryHeaderTemplate;
        memoryItemTemplate = settings.TryGetValue("memoryItemTemplate", out var memItem) && memItem is string memItemStr ? memItemStr : memoryItemTemplate;
        dialogueHeaderTemplate = settings.TryGetValue("dialogueHeaderTemplate", out var dialogueHeader) && dialogueHeader is string dialogueHeaderStr ? dialogueHeaderStr : dialogueHeaderTemplate;
        playerTemplate = settings.TryGetValue("playerTemplate", out var player) && player is string playerStr ? playerStr : playerTemplate;
        playerInputTemplate = settings.TryGetValue("playerInputTemplate", out var playerInputSetting) && playerInputSetting is string playerInputSettingStr ? playerInputSettingStr : playerInputTemplate;
        responsePromptTemplate = settings.TryGetValue("responsePromptTemplate", out var response) && response is string responseStr ? responseStr : responsePromptTemplate;
        includeEmptyMemory = settings.TryGetValue("includeEmptyMemory", out var emptyMem) && emptyMem is bool emptyMemBool ? emptyMemBool : includeEmptyMemory;
        includeEmptyDialogue = settings.TryGetValue("includeEmptyDialogue", out var emptyDialogue) && emptyDialogue is bool emptyDialogueBool ? emptyDialogueBool : includeEmptyDialogue;
        maxMemoryItems = settings.TryGetValue("maxMemoryItems", out var maxMem) && maxMem is int maxMemInt ? maxMemInt : maxMemoryItems;
        maxDialogueLines = settings.TryGetValue("maxDialogueLines", out var maxDialogue) && maxDialogue is int maxDialogueInt ? maxDialogueInt : maxDialogueLines;
        sectionSeparator = settings.TryGetValue("sectionSeparator", out var separator) && separator is string separatorStr ? separatorStr : sectionSeparator;
        compactDialogueFormat = settings.TryGetValue("compactDialogueFormat", out var compact) && compact is bool compactBool ? compactBool : compactDialogueFormat;
        dialogueLinePrefix = settings.TryGetValue("dialogueLinePrefix", out var prefix) && prefix is string prefixStr ? prefixStr : dialogueLinePrefix;

        // Use player name from settings if available, otherwise fall back to parameter
        if (settings.TryGetValue("playerName", out var settingsPlayerName) && settingsPlayerName is string settingsPlayerNameStr && !string.IsNullOrEmpty(settingsPlayerNameStr))
        {
          playerName = settingsPlayerNameStr;
        }
      }

      // Build the prompt using the templates
      sb.AppendLine(FormatTemplate(npcTemplate, new { personaName }));
      sb.AppendLine(FormatTemplate(descriptionTemplate, new { description }));

      // Add memory section
      if (memory != null && memory.Count > 0)
      {
        sb.AppendLine(memoryHeaderTemplate);
        var memoryToInclude = memory.Count > maxMemoryItems ? memory.Skip(memory.Count - maxMemoryItems).Take(maxMemoryItems) : memory;
        foreach (var m in memoryToInclude)
        {
          sb.AppendLine(FormatTemplate(memoryItemTemplate, new { memory = m }));
        }
      }
      else if (includeEmptyMemory)
      {
        sb.AppendLine(memoryHeaderTemplate);
      }

      // Add dialogue history section
      if (dialogueHistory != null && dialogueHistory.Count > 0)
      {
        sb.AppendLine(dialogueHeaderTemplate);
        var historyToInclude = dialogueHistory.Count > maxDialogueLines ? dialogueHistory.Skip(dialogueHistory.Count - maxDialogueLines).Take(maxDialogueLines) : dialogueHistory;
        foreach (var line in historyToInclude)
        {
          if (compactDialogueFormat)
          {
            sb.AppendLine(dialogueLinePrefix + line);
          }
          else
          {
            sb.AppendLine(line);
          }
        }
      }
      else if (includeEmptyDialogue)
      {
        sb.AppendLine(dialogueHeaderTemplate);
      }

      sb.AppendLine(FormatTemplate(playerTemplate, new { playerName }));
      sb.AppendLine(FormatTemplate(playerInputTemplate, new { playerInput }));
      sb.AppendLine(FormatTemplate(responsePromptTemplate, new { personaName }));

      return sb.ToString();
    }

    /// <summary>
    /// Compose a prompt for an LLM with persona and session context using custom settings.
    /// </summary>
    /// <param name="personaName">The name of the persona.</param>
    /// <param name="description">The description of the persona.</param>
    /// <param name="memory">The memory of the persona.</param>
    /// <param name="dialogueHistory">The dialogue history.</param>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="playerInput">The input from the player.</param>
    /// <param name="settings">Custom settings for prompt composition (optional).</param>
    public static string Compose(
        string personaName,
        string description,
        IReadOnlyList<string> memory,
        IReadOnlyList<string> dialogueHistory,
        string playerName,
        string playerInput,
        object? settings)
    {
      var sb = new StringBuilder();

      // Use default settings if none provided
      var npcTemplate = "NPC: {personaName}";
      var descriptionTemplate = "Description: {description}";
      var memoryHeaderTemplate = "NPC Memory:";
      var memoryItemTemplate = "- {memory}";
      var dialogueHeaderTemplate = "Dialogue so far:";
      var playerTemplate = "Player: {playerName}";
      var playerInputTemplate = "Player says: {playerInput}";
      var responsePromptTemplate = "NPC responds:";
      var includeEmptyMemory = false;
      var includeEmptyDialogue = false;
      var maxMemoryItems = 10;
      var maxDialogueLines = 20;
      var sectionSeparator = "\n";
      var compactDialogueFormat = false;
      var dialogueLinePrefix = "";

      // Apply custom settings if provided
      if (settings != null)
      {
        var settingsType = settings.GetType();

        // Try to get properties from the settings object using reflection
        npcTemplate = GetPropertyValue(settings, "npcTemplate", npcTemplate);
        descriptionTemplate = GetPropertyValue(settings, "descriptionTemplate", descriptionTemplate);
        memoryHeaderTemplate = GetPropertyValue(settings, "memoryHeaderTemplate", memoryHeaderTemplate);
        memoryItemTemplate = GetPropertyValue(settings, "memoryItemTemplate", memoryItemTemplate);
        dialogueHeaderTemplate = GetPropertyValue(settings, "dialogueHeaderTemplate", dialogueHeaderTemplate);
        playerTemplate = GetPropertyValue(settings, "playerTemplate", playerTemplate);
        playerInputTemplate = GetPropertyValue(settings, "playerInputTemplate", playerInputTemplate);
        responsePromptTemplate = GetPropertyValue(settings, "responsePromptTemplate", responsePromptTemplate);
        includeEmptyMemory = GetPropertyValue(settings, "includeEmptyMemory", includeEmptyMemory);
        includeEmptyDialogue = GetPropertyValue(settings, "includeEmptyDialogue", includeEmptyDialogue);
        maxMemoryItems = GetPropertyValue(settings, "maxMemoryItems", maxMemoryItems);
        maxDialogueLines = GetPropertyValue(settings, "maxDialogueLines", maxDialogueLines);
        sectionSeparator = GetPropertyValue(settings, "sectionSeparator", sectionSeparator);
        compactDialogueFormat = GetPropertyValue(settings, "compactDialogueFormat", compactDialogueFormat);
        dialogueLinePrefix = GetPropertyValue(settings, "dialogueLinePrefix", dialogueLinePrefix);

        // Use player name from settings if available, otherwise fall back to parameter
        var settingsPlayerName = GetPropertyValue(settings, "playerName", "");
        if (!string.IsNullOrEmpty(settingsPlayerName))
        {
          playerName = settingsPlayerName;
        }
      }

      // Build the prompt using the templates
      sb.AppendLine(FormatTemplate(npcTemplate, new { personaName }));
      sb.AppendLine(FormatTemplate(descriptionTemplate, new { description }));

      // Add memory section
      if (memory != null && memory.Count > 0)
      {
        sb.AppendLine(memoryHeaderTemplate);
        var memoryToInclude = memory.Count > maxMemoryItems ? memory.Skip(memory.Count - maxMemoryItems).Take(maxMemoryItems) : memory;
        foreach (var m in memoryToInclude)
        {
          sb.AppendLine(FormatTemplate(memoryItemTemplate, new { memory = m }));
        }
      }
      else if (includeEmptyMemory)
      {
        sb.AppendLine(memoryHeaderTemplate);
      }

      // Add dialogue history section
      if (dialogueHistory != null && dialogueHistory.Count > 0)
      {
        sb.AppendLine(dialogueHeaderTemplate);
        var historyToInclude = dialogueHistory.Count > maxDialogueLines ? dialogueHistory.Skip(dialogueHistory.Count - maxDialogueLines).Take(maxDialogueLines) : dialogueHistory;
        foreach (var line in historyToInclude)
        {
          if (compactDialogueFormat)
          {
            sb.AppendLine(dialogueLinePrefix + line);
          }
          else
          {
            sb.AppendLine(line);
          }
        }
      }
      else if (includeEmptyDialogue)
      {
        sb.AppendLine(dialogueHeaderTemplate);
      }

      sb.AppendLine(FormatTemplate(playerTemplate, new { playerName }));
      sb.AppendLine(FormatTemplate(playerInputTemplate, new { playerInput }));
      sb.AppendLine(FormatTemplate(responsePromptTemplate, new { personaName }));

      return sb.ToString();
    }

    /// <summary>
    /// Format a template string by replacing placeholders with values from an anonymous object.
    /// </summary>
    private static string FormatTemplate(string template, object values)
    {
      var result = template;
      var properties = values.GetType().GetProperties();

      foreach (var property in properties)
      {
        var placeholder = "{" + property.Name + "}";
        var value = property.GetValue(values)?.ToString() ?? "";
        result = result.Replace(placeholder, value);
      }

      return result;
    }

    /// <summary>
    /// Get a property value from an object using reflection, with a fallback default value.
    /// </summary>
    private static T GetPropertyValue<T>(object obj, string propertyName, T defaultValue)
    {
      try
      {
        var property = obj.GetType().GetProperty(propertyName);
        if (property != null && property.CanRead)
        {
          var value = property.GetValue(obj);
          if (value is T typedValue)
          {
            return typedValue;
          }
        }
        else
        {
          // Debug: Log when property is not found or not readable
          System.Diagnostics.Debug.WriteLine($"Property '{propertyName}' not found or not readable on type {obj.GetType().Name}");
        }
      }
      catch (System.Exception ex)
      {
        // Debug: Log reflection errors
        System.Diagnostics.Debug.WriteLine($"Reflection error for property '{propertyName}': {ex.Message}");
      }

      return defaultValue;
    }
  }
}