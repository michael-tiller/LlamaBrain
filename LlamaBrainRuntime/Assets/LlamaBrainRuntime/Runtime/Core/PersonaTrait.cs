using UnityEngine;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// A reusable trait definition for personas - can be shared across multiple personas
  /// </summary>
  [CreateAssetMenu(fileName = "New Persona Trait", menuName = "LlamaBrain/Persona Trait")]
  public sealed class PersonaTrait : ScriptableObject
  {
    /// <summary>
    /// The unique identifier for this trait
    /// </summary>
    [Header("Trait Information")]
    public string TraitId;

    /// <summary>
    /// The display name of the trait
    /// </summary>
    public string DisplayName;

    /// <summary>
    /// The description of what this trait means
    /// </summary>
    [TextArea(2, 4)]
    public string Description;

    /// <summary>
    /// The category this trait belongs to (e.g., "Personality", "Skills", "Background")
    /// </summary>
    public string Category;

    /// <summary>
    /// The default value for this trait
    /// </summary>
    [TextArea(2, 3)]
    public string DefaultValue;

    /// <summary>
    /// Whether this trait is required for personas
    /// </summary>
    [Header("Configuration")]
    public bool IsRequired;

    /// <summary>
    /// Whether this trait should be included in prompts by default
    /// </summary>
    public bool IncludeInPrompts = true;

    /// <summary>
    /// The order this trait should appear in (lower numbers appear first)
    /// </summary>
    [Range(0, 100)]
    public int DisplayOrder;

    /// <summary>
    /// Automatically generates a GUID for TraitId if it's empty
    /// </summary>
    private void OnEnable()
    {
      if (string.IsNullOrEmpty(TraitId))
      {
        TraitId = System.Guid.NewGuid().ToString();
      }
    }

    /// <summary>
    /// Gets the trait as a key-value pair for the PersonaProfile
    /// </summary>
    /// <param name="value">The custom value for this trait</param>
    /// <returns>A key-value pair representing this trait</returns>
    public System.Collections.Generic.KeyValuePair<string, string> ToKeyValuePair(string value = null)
    {
      var traitValue = string.IsNullOrEmpty(value) ? DefaultValue : value;
      return new System.Collections.Generic.KeyValuePair<string, string>(DisplayName, traitValue);
    }

    /// <summary>
    /// Gets a formatted description of this trait
    /// </summary>
    /// <returns>A formatted string describing the trait</returns>
    public string GetFormattedDescription()
    {
      if (string.IsNullOrEmpty(Description))
        return DisplayName;

      return $"{DisplayName}: {Description}";
    }
  }
}