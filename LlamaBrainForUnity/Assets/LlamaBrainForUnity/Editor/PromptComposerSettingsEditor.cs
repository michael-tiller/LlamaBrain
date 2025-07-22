#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using LlamaBrain.Unity.Runtime.Core;

/// <summary>
/// This namespace contains the editor components for the LlamaBrain for Unity project.
/// </summary>
namespace LlamaBrain.Unity.Editor
{
  /// <summary>
  /// Custom editor for PromptComposerSettings to provide better UI and validation.
  /// </summary>
  [CustomEditor(typeof(PromptComposerSettings))]
  public class PromptComposerSettingsEditor : UnityEditor.Editor
  {
    /// <summary>
    /// Whether to show the templates section
    /// </summary>
    private bool showTemplates = true;

    /// <summary>
    /// Whether to show the formatting section
    /// </summary>
    private bool showFormatting = true;

    /// <summary>
    /// Whether to show the advanced section
    /// </summary>
    private bool showAdvanced = false;

    /// <summary>
    /// Whether to show the validation section
    /// </summary>
    private bool showValidation = false;

    /// <summary>
    /// Draws the inspector GUI
    /// </summary>
    public override void OnInspectorGUI()
    {
      var settings = (PromptComposerSettings)target;

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Prompt Composer Settings", EditorStyles.boldLabel);
      EditorGUILayout.Space();

      // Templates Section
      showTemplates = EditorGUILayout.Foldout(showTemplates, "Prompt Templates", true);
      if (showTemplates)
      {
        EditorGUI.indentLevel++;

        settings.npcTemplate = EditorGUILayout.TextField("NPC Template", settings.npcTemplate);
        EditorGUILayout.HelpBox("Use {personaName} as placeholder", MessageType.Info);

        settings.descriptionTemplate = EditorGUILayout.TextField("Description Template", settings.descriptionTemplate);
        EditorGUILayout.HelpBox("Use {description} as placeholder", MessageType.Info);

        settings.memoryHeaderTemplate = EditorGUILayout.TextField("Memory Header", settings.memoryHeaderTemplate);
        settings.memoryItemTemplate = EditorGUILayout.TextField("Memory Item", settings.memoryItemTemplate);
        EditorGUILayout.HelpBox("Use {memory} as placeholder", MessageType.Info);

        settings.dialogueHeaderTemplate = EditorGUILayout.TextField("Dialogue Header", settings.dialogueHeaderTemplate);

        settings.playerTemplate = EditorGUILayout.TextField("Player Template", settings.playerTemplate);
        EditorGUILayout.HelpBox("Use {playerName} as placeholder", MessageType.Info);

        settings.playerInputTemplate = EditorGUILayout.TextField("Player Input", settings.playerInputTemplate);
        EditorGUILayout.HelpBox("Use {playerInput} as placeholder", MessageType.Info);

        settings.responsePromptTemplate = EditorGUILayout.TextField("Response Prompt", settings.responsePromptTemplate);
        EditorGUILayout.HelpBox("Use {personaName} as placeholder", MessageType.Info);

        EditorGUI.indentLevel--;
      }

      EditorGUILayout.Space();

      // Formatting Options Section
      showFormatting = EditorGUILayout.Foldout(showFormatting, "Formatting Options", true);
      if (showFormatting)
      {
        EditorGUI.indentLevel++;

        settings.includeEmptyMemory = EditorGUILayout.Toggle("Include Empty Memory", settings.includeEmptyMemory);
        settings.includeEmptyDialogue = EditorGUILayout.Toggle("Include Empty Dialogue", settings.includeEmptyDialogue);

        settings.maxMemoryItems = EditorGUILayout.IntSlider("Max Memory Items", settings.maxMemoryItems, 1, 50);
        settings.maxDialogueLines = EditorGUILayout.IntSlider("Max Dialogue Lines", settings.maxDialogueLines, 1, 100);

        settings.sectionSeparator = EditorGUILayout.TextField("Section Separator", settings.sectionSeparator);

        EditorGUI.indentLevel--;
      }

      EditorGUILayout.Space();

      // Advanced Options Section
      showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Options", true);
      if (showAdvanced)
      {
        EditorGUI.indentLevel++;

        settings.compactDialogueFormat = EditorGUILayout.Toggle("Compact Dialogue Format", settings.compactDialogueFormat);
        if (settings.compactDialogueFormat)
        {
          settings.dialogueLinePrefix = EditorGUILayout.TextField("Dialogue Line Prefix", settings.dialogueLinePrefix);
        }

        EditorGUI.indentLevel--;
      }

      EditorGUILayout.Space();

      // Quick Presets
      EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button("Default"))
      {
        ApplyDefaultPreset(settings);
      }

      if (GUILayout.Button("Compact"))
      {
        ApplyCompactPreset(settings);
      }

      if (GUILayout.Button("Detailed"))
      {
        ApplyDetailedPreset(settings);
      }

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space();

      // Validation Section
      showValidation = EditorGUILayout.Foldout(showValidation, "Validation", true);
      if (showValidation)
      {
        var errors = settings.ValidateSettings();
        if (errors.Length > 0)
        {
          EditorGUILayout.HelpBox("Validation Errors:", MessageType.Error);
          foreach (var error in errors)
          {
            EditorGUILayout.HelpBox(error, MessageType.Error);
          }
        }
        else
        {
          EditorGUILayout.HelpBox("All settings are valid!", MessageType.Info);
        }
      }

      // Apply changes
      if (GUI.changed)
      {
        EditorUtility.SetDirty(settings);
      }
    }

    /// <summary>
    /// Applies the default preset to the settings
    /// </summary>
    /// <param name="settings">The settings to apply the preset to</param>
    private void ApplyDefaultPreset(PromptComposerSettings settings)
    {
      settings.npcTemplate = "NPC: {personaName}";
      settings.descriptionTemplate = "Description: {description}";
      settings.memoryHeaderTemplate = "NPC Memory:";
      settings.memoryItemTemplate = "- {memory}";
      settings.dialogueHeaderTemplate = "Dialogue so far:";
      settings.playerTemplate = "Player: {playerName}";
      settings.playerInputTemplate = "Player says: {playerInput}";
      settings.responsePromptTemplate = "NPC responds:";
      settings.includeEmptyMemory = false;
      settings.includeEmptyDialogue = false;
      settings.maxMemoryItems = 10;
      settings.maxDialogueLines = 20;
      settings.sectionSeparator = "\n";
      settings.compactDialogueFormat = false;
      settings.dialogueLinePrefix = "";

      EditorUtility.SetDirty(settings);
    }

    /// <summary>
    /// Applies the compact preset to the settings
    /// </summary>
    /// <param name="settings">The settings to apply the preset to</param>
    private void ApplyCompactPreset(PromptComposerSettings settings)
    {
      settings.npcTemplate = "NPC: {personaName}";
      settings.descriptionTemplate = "{description}";
      settings.memoryHeaderTemplate = "Memory:";
      settings.memoryItemTemplate = "- {memory}";
      settings.dialogueHeaderTemplate = "History:";
      settings.playerTemplate = "Player: {playerName}";
      settings.playerInputTemplate = "> {playerInput}";
      settings.responsePromptTemplate = "{personaName}:";
      settings.includeEmptyMemory = false;
      settings.includeEmptyDialogue = false;
      settings.maxMemoryItems = 5;
      settings.maxDialogueLines = 10;
      settings.sectionSeparator = "\n";
      settings.compactDialogueFormat = true;
      settings.dialogueLinePrefix = "";

      EditorUtility.SetDirty(settings);
    }

    /// <summary>
    /// Applies the detailed preset to the settings
    /// </summary>
    /// <param name="settings">The settings to apply the preset to</param>
    private void ApplyDetailedPreset(PromptComposerSettings settings)
    {
      settings.npcTemplate = "Character: {personaName}";
      settings.descriptionTemplate = "Background: {description}";
      settings.memoryHeaderTemplate = "Character Memory (Recent Events):";
      settings.memoryItemTemplate = "• {memory}";
      settings.dialogueHeaderTemplate = "Conversation History:";
      settings.playerTemplate = "Player Character: {playerName}";
      settings.playerInputTemplate = "Player says: \"{playerInput}\"";
      settings.responsePromptTemplate = "How does {personaName} respond?";
      settings.includeEmptyMemory = true;
      settings.includeEmptyDialogue = true;
      settings.maxMemoryItems = 15;
      settings.maxDialogueLines = 30;
      settings.sectionSeparator = "\n\n";
      settings.compactDialogueFormat = false;
      settings.dialogueLinePrefix = "";

      EditorUtility.SetDirty(settings);
    }
  }
}
#endif