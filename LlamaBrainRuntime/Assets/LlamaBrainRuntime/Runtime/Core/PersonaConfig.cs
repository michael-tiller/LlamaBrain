using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LlamaBrain.Persona;
using LlamaBrain.Config;

namespace LlamaBrain.Runtime.Core
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
    /// Prompt variants for A/B testing (optional).
    /// If set, variants will be selected deterministically based on InteractionCount.
    /// </summary>
    [Header("A/B Testing (Optional)")]
    [Tooltip("Prompt variants for A/B testing. If empty, uses default SystemPrompt.")]
    public List<PromptVariantConfig> SystemPromptVariants = new List<PromptVariantConfig>();

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

    /// <summary>
    /// Validates that variant traffic percentages sum to 100% (only for active variants).
    /// Returns an array of error messages (empty if valid).
    /// </summary>
    /// <returns>Array of validation errors (empty if valid)</returns>
    public string[] ValidateVariantTraffic()
    {
      if (SystemPromptVariants == null || SystemPromptVariants.Count == 0)
      {
        return Array.Empty<string>(); // No variants = no validation needed
      }

      var errors = new List<string>();

      // Check for duplicate names
      var names = SystemPromptVariants.Select(v => v.VariantName).ToList();
      var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
      if (duplicates.Count > 0)
      {
        errors.Add($"Duplicate variant names found: {string.Join(", ", duplicates)}");
      }

      // Check that active variants sum to 100%
      var activeVariants = SystemPromptVariants.Where(v => v.IsActive).ToList();
      if (activeVariants.Count > 0)
      {
        var totalTraffic = activeVariants.Sum(v => v.TrafficPercentage);
        if (Math.Abs(totalTraffic - 100f) > 0.01f) // Allow for floating point precision
        {
          errors.Add($"Active variant traffic percentages must sum to 100%, but sum to {totalTraffic:F1}%");
        }
      }

      return errors.ToArray();
    }

    /// <summary>
    /// Converts Unity PromptVariantConfig to LlamaBrain.Config.PromptVariant for use with PromptVariantManager.
    /// </summary>
    /// <returns>List of PromptVariant instances</returns>
    public List<PromptVariant> ToPromptVariants()
    {
      if (SystemPromptVariants == null || SystemPromptVariants.Count == 0)
      {
        return new List<PromptVariant>();
      }

      var variants = new List<PromptVariant>();
      foreach (var config in SystemPromptVariants)
      {
        variants.Add(new PromptVariant
        {
          Name = config.VariantName,
          SystemPrompt = config.SystemPrompt,
          TrafficPercentage = config.TrafficPercentage,
          IsActive = config.IsActive
        });
      }

      return variants;
    }
  }

  /// <summary>
  /// A key-value pair for persona metadata in Unity Inspector
  /// </summary>
  /// <summary>
  /// Represents a key-value metadata entry for persona configuration.
  /// </summary>
  [System.Serializable]
  public class PersonaMetadataEntry
  {
    /// <summary>
    /// The metadata key.
    /// </summary>
    public string Key;
    /// <summary>
    /// The metadata value.
    /// </summary>
    public string Value;
  }

  /// <summary>
  /// Represents a prompt variant configuration for A/B testing.
  /// Unity-serializable version of LlamaBrain.Config.PromptVariant.
  /// </summary>
  [System.Serializable]
  public class PromptVariantConfig
  {
    /// <summary>
    /// The name of the variant (e.g., "Control", "Experimental").
    /// </summary>
    [Tooltip("The name of the variant (e.g., 'Control', 'Experimental')")]
    public string VariantName = "";

    /// <summary>
    /// The system prompt for this variant.
    /// </summary>
    [TextArea(3, 8)]
    [Tooltip("The system prompt for this variant")]
    public string SystemPrompt = "";

    /// <summary>
    /// The percentage of traffic this variant should receive (0.0-100.0).
    /// All active variants must sum to 100%.
    /// </summary>
    [Range(0f, 100f)]
    [Tooltip("Traffic percentage (0-100). Active variants must sum to 100%.")]
    public float TrafficPercentage = 50f;

    /// <summary>
    /// Whether this variant is currently active.
    /// Inactive variants are skipped during selection.
    /// </summary>
    [Tooltip("Whether this variant is active. Inactive variants are skipped.")]
    public bool IsActive = true;
  }
}