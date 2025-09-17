using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// A collection of persona traits organized by category - useful for managing trait libraries
  /// </summary>
  [CreateAssetMenu(fileName = "New Trait Collection", menuName = "LlamaBrain/Trait Collection")]
  public sealed class PersonaTraitCollection : ScriptableObject
  {
    /// <summary>
    /// All traits in this collection
    /// </summary>
    [Header("Trait Collection")]
    public List<PersonaTrait> Traits = new List<PersonaTrait>();

    /// <summary>
    /// Gets all traits in a specific category
    /// </summary>
    /// <param name="category">The category to filter by</param>
    /// <returns>List of traits in the specified category</returns>
    public List<PersonaTrait> GetTraitsByCategory(string category)
    {
      if (string.IsNullOrEmpty(category))
        return new List<PersonaTrait>();

      return Traits.Where(t => t.Category.Equals(category, System.StringComparison.OrdinalIgnoreCase))
                   .OrderBy(t => t.DisplayOrder)
                   .ToList();
    }

    /// <summary>
    /// Gets all unique categories in this collection
    /// </summary>
    /// <returns>List of unique category names</returns>
    public List<string> GetCategories()
    {
      return Traits.Select(t => t.Category)
                   .Where(c => !string.IsNullOrEmpty(c))
                   .Distinct()
                   .OrderBy(c => c)
                   .ToList();
    }

    /// <summary>
    /// Gets a trait by its display name
    /// </summary>
    /// <param name="displayName">The display name to search for</param>
    /// <returns>The trait with the matching display name, or null if not found</returns>
    public PersonaTrait GetTraitByDisplayName(string displayName)
    {
      if (string.IsNullOrEmpty(displayName))
        return null;

      return Traits.FirstOrDefault(t => t.DisplayName.Equals(displayName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a trait by its ID
    /// </summary>
    /// <param name="traitId">The trait ID to search for</param>
    /// <returns>The trait with the matching ID, or null if not found</returns>
    public PersonaTrait GetTraitById(string traitId)
    {
      if (string.IsNullOrEmpty(traitId))
        return null;

      return Traits.FirstOrDefault(t => t.TraitId.Equals(traitId, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all required traits
    /// </summary>
    /// <returns>List of required traits</returns>
    public List<PersonaTrait> GetRequiredTraits()
    {
      return Traits.Where(t => t.IsRequired)
                   .OrderBy(t => t.DisplayOrder)
                   .ToList();
    }

    /// <summary>
    /// Gets all traits that should be included in prompts
    /// </summary>
    /// <returns>List of traits that should be included in prompts</returns>
    public List<PersonaTrait> GetPromptTraits()
    {
      return Traits.Where(t => t.IncludeInPrompts)
                   .OrderBy(t => t.DisplayOrder)
                   .ToList();
    }

    /// <summary>
    /// Adds a trait to the collection if it doesn't already exist
    /// </summary>
    /// <param name="trait">The trait to add</param>
    /// <returns>True if the trait was added, false if it already exists</returns>
    public bool AddTrait(PersonaTrait trait)
    {
      if (trait == null)
        return false;

      if (Traits.Any(t => t.TraitId == trait.TraitId))
        return false;

      Traits.Add(trait);
      return true;
    }

    /// <summary>
    /// Removes a trait from the collection
    /// </summary>
    /// <param name="trait">The trait to remove</param>
    /// <returns>True if the trait was removed, false if it wasn't found</returns>
    public bool RemoveTrait(PersonaTrait trait)
    {
      if (trait == null)
        return false;

      return Traits.Remove(trait);
    }

    /// <summary>
    /// Validates the collection and returns any issues
    /// </summary>
    /// <returns>List of validation issues, empty if valid</returns>
    public List<string> Validate()
    {
      var issues = new List<string>();

      // Check for duplicate trait IDs
      var duplicateIds = Traits.GroupBy(t => t.TraitId)
                               .Where(g => g.Count() > 1)
                               .Select(g => g.Key);

      foreach (var duplicateId in duplicateIds)
      {
        issues.Add($"Duplicate trait ID found: {duplicateId}");
      }

      // Check for duplicate display names
      var duplicateNames = Traits.GroupBy(t => t.DisplayName)
                                 .Where(g => g.Count() > 1)
                                 .Select(g => g.Key);

      foreach (var duplicateName in duplicateNames)
      {
        issues.Add($"Duplicate display name found: {duplicateName}");
      }

      // Check for traits without display names
      var traitsWithoutNames = Traits.Where(t => string.IsNullOrEmpty(t.DisplayName));
      foreach (var trait in traitsWithoutNames)
      {
        issues.Add($"Trait {trait.name} has no display name");
      }

      return issues;
    }
  }
}