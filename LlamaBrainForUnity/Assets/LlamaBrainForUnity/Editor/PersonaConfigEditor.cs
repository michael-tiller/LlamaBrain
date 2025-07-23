#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LlamaBrain.Unity.Runtime.Core;

namespace LlamaBrain.Unity.Editor
{
  /// <summary>
  /// Custom editor for PersonaConfig to provide better trait management UI
  /// </summary>
  [CustomEditor(typeof(PersonaConfig))]
  public class PersonaConfigEditor : UnityEditor.Editor
  {
    private PersonaConfig _config;
    private bool _showTraitManagement = true;
    private bool _showMetadata = true;
    private Vector2 _scrollPosition;

    private void OnEnable()
    {
      _config = (PersonaConfig)target;
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Persona Configuration", EditorStyles.boldLabel);
      EditorGUILayout.Space();

      // Basic Information
      EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(serializedObject.FindProperty("PersonaId"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("Name"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));
      EditorGUILayout.Space();

      // AI Configuration
      EditorGUILayout.LabelField("AI Configuration", EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(serializedObject.FindProperty("SystemPrompt"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("Background"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("UseMemory"));
      EditorGUILayout.Space();

      // Trait Management
      _showTraitManagement = EditorGUILayout.Foldout(_showTraitManagement, "Trait Assignments", true);
      if (_showTraitManagement)
      {
        DrawTraitManagement();
      }

      // Metadata
      _showMetadata = EditorGUILayout.Foldout(_showMetadata, "Custom Metadata", true);
      if (_showMetadata)
      {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Metadata"));
      }

      EditorGUILayout.Space();

      // Validation and Actions
      DrawValidationAndActions();

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawTraitManagement()
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      // Trait assignment list
      var traitAssignmentsProperty = serializedObject.FindProperty("TraitAssignments");

      // Add trait button
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Add Trait Assignment", GUILayout.Width(150)))
      {
        traitAssignmentsProperty.arraySize++;
        var newElement = traitAssignmentsProperty.GetArrayElementAtIndex(traitAssignmentsProperty.arraySize - 1);
        newElement.FindPropertyRelative("IsEnabled").boolValue = true;
      }

      if (GUILayout.Button("Clear All", GUILayout.Width(100)))
      {
        if (EditorUtility.DisplayDialog("Clear Trait Assignments",
            "Are you sure you want to clear all trait assignments?", "Yes", "No"))
        {
          traitAssignmentsProperty.ClearArray();
        }
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space();

      // Draw trait assignments
      for (int i = 0; i < traitAssignmentsProperty.arraySize; i++)
      {
        var element = traitAssignmentsProperty.GetArrayElementAtIndex(i);
        DrawTraitAssignment(element, i);
      }

      if (traitAssignmentsProperty.arraySize == 0)
      {
        EditorGUILayout.HelpBox("No trait assignments. Click 'Add Trait Assignment' to add traits to this persona.", MessageType.Info);
      }

      EditorGUILayout.EndVertical();
    }

    private void DrawTraitAssignment(SerializedProperty element, int index)
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      EditorGUILayout.BeginHorizontal();

      // Enable/disable toggle
      var isEnabledProperty = element.FindPropertyRelative("IsEnabled");
      isEnabledProperty.boolValue = EditorGUILayout.Toggle(isEnabledProperty.boolValue, GUILayout.Width(20));

      // Trait field
      var traitProperty = element.FindPropertyRelative("Trait");
      EditorGUILayout.PropertyField(traitProperty, GUIContent.none);

      // Remove button
      if (GUILayout.Button("Remove", GUILayout.Width(60)))
      {
        var traitAssignmentsProperty = serializedObject.FindProperty("TraitAssignments");
        traitAssignmentsProperty.DeleteArrayElementAtIndex(index);
        return;
      }

      EditorGUILayout.EndHorizontal();

      // Only show custom value if trait is assigned and enabled
      if (isEnabledProperty.boolValue && traitProperty.objectReferenceValue != null)
      {
        var customValueProperty = element.FindPropertyRelative("CustomValue");
        var trait = (PersonaTrait)traitProperty.objectReferenceValue;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Custom Value:", GUILayout.Width(100));

        if (string.IsNullOrEmpty(customValueProperty.stringValue))
        {
          EditorGUILayout.LabelField($"Using default: {trait.DefaultValue}", EditorStyles.miniLabel);
        }
        else
        {
          EditorGUILayout.PropertyField(customValueProperty, GUIContent.none);
        }

        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
          customValueProperty.stringValue = string.Empty;
        }

        EditorGUILayout.EndHorizontal();

        // Show trait info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Category:", GUILayout.Width(100));
        EditorGUILayout.LabelField(trait.Category, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(trait.Description))
        {
          EditorGUILayout.BeginHorizontal();
          EditorGUILayout.LabelField("Description:", GUILayout.Width(100));
          EditorGUILayout.LabelField(trait.Description, EditorStyles.miniLabel);
          EditorGUILayout.EndHorizontal();
        }
      }

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space();
    }

    private void DrawValidationAndActions()
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Validation & Actions", EditorStyles.boldLabel);

      // Validation
      var issues = ValidateConfig();
      if (issues.Count > 0)
      {
        EditorGUILayout.HelpBox("Configuration Issues:", MessageType.Warning);
        foreach (var issue in issues)
        {
          EditorGUILayout.LabelField($"• {issue}", EditorStyles.miniLabel);
        }
      }
      else
      {
        EditorGUILayout.HelpBox("Configuration is valid!", MessageType.Info);
      }

      EditorGUILayout.Space();

      // Actions
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Create Test Profile"))
      {
        var profile = _config.ToProfile();
        Debug.Log($"Created profile: {profile.Name} with {profile.Traits.Count} traits");
      }

      if (GUILayout.Button("Export as JSON"))
      {
        var profile = _config.ToProfile();
        var json = JsonUtility.ToJson(profile, true);
        Debug.Log($"Profile JSON:\n{json}");
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.EndVertical();
    }

    private List<string> ValidateConfig()
    {
      var issues = new List<string>();

      if (string.IsNullOrEmpty(_config.PersonaId))
        issues.Add("PersonaId is required");

      if (string.IsNullOrEmpty(_config.Name))
        issues.Add("Name is required");

      if (string.IsNullOrEmpty(_config.Description))
        issues.Add("Description is recommended");

      // Check for duplicate trait assignments
      var traitNames = _config.TraitAssignments
        .Where(ta => ta.IsEnabled && ta.Trait != null)
        .Select(ta => ta.Trait.DisplayName)
        .ToList();

      var duplicates = traitNames.GroupBy(n => n)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key);

      foreach (var duplicate in duplicates)
      {
        issues.Add($"Duplicate trait assignment: {duplicate}");
      }

      return issues;
    }
  }
}
#endif