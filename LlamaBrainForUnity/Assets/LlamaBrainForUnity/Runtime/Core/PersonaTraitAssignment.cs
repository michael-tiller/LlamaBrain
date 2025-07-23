using System;
using UnityEngine;

namespace LlamaBrain.Unity.Runtime.Core
{
  /// <summary>
  /// Represents a trait assignment with a custom value for a specific persona
  /// </summary>
  [Serializable]
  public sealed class PersonaTraitAssignment
  {
    /// <summary>
    /// The trait being assigned
    /// </summary>
    [Header("Trait Assignment")]
    public PersonaTrait Trait;

    /// <summary>
    /// The custom value for this trait (if empty, uses the trait's default value)
    /// </summary>
    [TextArea(2, 3)]
    public string CustomValue;

    /// <summary>
    /// Whether this trait assignment is enabled for this persona
    /// </summary>
    public bool IsEnabled = true;

    /// <summary>
    /// Gets the effective value for this trait (custom value or default)
    /// </summary>
    /// <returns>The effective value for this trait</returns>
    public string GetEffectiveValue()
    {
      if (!IsEnabled || Trait == null)
        return string.Empty;

      return string.IsNullOrEmpty(CustomValue) ? Trait.DefaultValue : CustomValue;
    }

    /// <summary>
    /// Gets the display name for this trait
    /// </summary>
    /// <returns>The display name, or empty string if trait is null</returns>
    public string GetDisplayName()
    {
      return Trait?.DisplayName ?? string.Empty;
    }

    /// <summary>
    /// Gets the category for this trait
    /// </summary>
    /// <returns>The category, or empty string if trait is null</returns>
    public string GetCategory()
    {
      return Trait?.Category ?? string.Empty;
    }

    /// <summary>
    /// Gets the display order for this trait
    /// </summary>
    /// <returns>The display order, or 0 if trait is null</returns>
    public int GetDisplayOrder()
    {
      return Trait?.DisplayOrder ?? 0;
    }

    /// <summary>
    /// Gets whether this trait should be included in prompts
    /// </summary>
    /// <returns>True if the trait should be included in prompts</returns>
    public bool ShouldIncludeInPrompts()
    {
      return IsEnabled && Trait != null && Trait.IncludeInPrompts;
    }

    /// <summary>
    /// Converts this assignment to a key-value pair for the PersonaProfile
    /// </summary>
    /// <returns>A key-value pair representing this trait assignment</returns>
    public System.Collections.Generic.KeyValuePair<string, string> ToKeyValuePair()
    {
      if (!IsEnabled || Trait == null)
        return new System.Collections.Generic.KeyValuePair<string, string>(string.Empty, string.Empty);

      return new System.Collections.Generic.KeyValuePair<string, string>(Trait.DisplayName, GetEffectiveValue());
    }

    /// <summary>
    /// Gets a formatted description of this trait assignment
    /// </summary>
    /// <returns>A formatted string describing the trait assignment</returns>
    public string GetFormattedDescription()
    {
      if (!IsEnabled || Trait == null)
        return string.Empty;

      var value = GetEffectiveValue();
      return string.IsNullOrEmpty(value) ? Trait.DisplayName : $"{Trait.DisplayName}: {value}";
    }
  }
}