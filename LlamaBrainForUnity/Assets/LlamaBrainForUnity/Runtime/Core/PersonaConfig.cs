using System;
using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Persona;

namespace LlamaBrain.Unity.Runtime.Core
{
  /// <summary>
  /// Unity ScriptableObject configuration for a persona - easy to configure in Inspector
  /// </summary>
  [CreateAssetMenu(fileName = "New Persona Config", menuName = "LlamaBrain/Persona Config")]
  public sealed class PersonaConfig : ScriptableObject
  {
    /// <summary>
    /// The ID of the persona
    /// </summary>
    [Header("Basic Information")]
    public string PersonaId;

    /// <summary>
    /// The name of the persona
    /// </summary>
    public string Name;

    /// <summary>
    /// The description of the persona
    /// </summary>
    [TextArea(3, 5)]
    public string Description;

    /// <summary>
    /// The system prompt for the persona
    /// </summary>
    [Header("AI Configuration")]
    [TextArea(3, 8)]
    public string SystemPrompt;



    /// <summary>
    /// The background story of the persona
    /// </summary>
    [TextArea(3, 6)]
    public string Background;

    /// <summary>
    /// Whether to use memory for the persona
    /// </summary>
    public bool UseMemory;

    /// <summary>
    /// Trait assignments for this persona
    /// </summary>
    [Header("Trait Assignments")]
    public List<PersonaTraitAssignment> TraitAssignments = new List<PersonaTraitAssignment>();

    /// <summary>
    /// Custom metadata for the persona
    /// </summary>
    [Header("Custom Metadata")]
    public List<PersonaMetadataEntry> Metadata = new List<PersonaMetadataEntry>();

    /// <summary>
    /// Automatically generates a GUID for PersonaId if it's empty
    /// </summary>
    private void OnEnable()
    {
      if (string.IsNullOrEmpty(PersonaId))
      {
        PersonaId = Guid.NewGuid().ToString();
      }
    }

    /// <summary>
    /// Converts this config to a PersonaProfile
    /// </summary>
    /// <returns>A PersonaProfile with the data from this config</returns>
    public PersonaProfile ToProfile()
    {
      var profile = PersonaProfile.Create(PersonaId, Name);
      profile.Description = Description;
      profile.SystemPrompt = SystemPrompt;
      profile.Background = Background;
      profile.UseMemory = UseMemory;

      // Convert trait assignments to traits dictionary
      foreach (var assignment in TraitAssignments)
      {
        if (assignment.ShouldIncludeInPrompts())
        {
          var kvp = assignment.ToKeyValuePair();
          if (!string.IsNullOrEmpty(kvp.Key))
          {
            profile.SetTrait(kvp.Key, kvp.Value);
          }
        }
      }

      // Convert metadata entries to dictionary
      foreach (var entry in Metadata)
      {
        if (!string.IsNullOrEmpty(entry.Key))
        {
          profile.SetMetadata(entry.Key, entry.Value);
        }
      }

      return profile;
    }

    /// <summary>
    /// Updates this config from a PersonaProfile
    /// </summary>
    /// <param name="profile">The profile to copy data from</param>
    public void FromProfile(PersonaProfile profile)
    {
      if (profile == null) return;

      PersonaId = profile.PersonaId;
      Name = profile.Name;
      Description = profile.Description;
      SystemPrompt = profile.SystemPrompt;
      Background = profile.Background;
      UseMemory = profile.UseMemory;

      // Convert traits dictionary to trait assignments
      TraitAssignments.Clear();
      foreach (var kvp in profile.Traits)
      {
        var assignment = new PersonaTraitAssignment
        {
          CustomValue = kvp.Value,
          IsEnabled = true
        };
        TraitAssignments.Add(assignment);
      }

      // Convert dictionary to metadata entries
      Metadata.Clear();
      foreach (var kvp in profile.Metadata)
      {
        Metadata.Add(new PersonaMetadataEntry { Key = kvp.Key, Value = kvp.Value });
      }
    }
  }

  /// <summary>
  /// A key-value pair for persona metadata in Unity Inspector
  /// </summary>
  [System.Serializable]
  public class PersonaMetadataEntry
  {
    public string Key;
    public string Value;
  }
}