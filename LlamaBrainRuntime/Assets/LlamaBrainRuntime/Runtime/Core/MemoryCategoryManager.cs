using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// A ScriptableObject that manages a collection of memory categories
  /// </summary>
  [CreateAssetMenu(fileName = "Memory Category Manager", menuName = "LlamaBrain/Memory Category Manager")]
  public class MemoryCategoryManager : ScriptableObject
  {
    [Header("Memory Categories")]
    [SerializeField] private List<MemoryCategory> memoryCategories = new List<MemoryCategory>();

    /// <summary>
    /// All memory categories in this manager
    /// </summary>
    public IReadOnlyList<MemoryCategory> MemoryCategories => memoryCategories;

    /// <summary>
    /// Get a memory category by name
    /// </summary>
    /// <param name="categoryName">The name of the category to find</param>
    /// <returns>The memory category, or null if not found</returns>
    public MemoryCategory GetCategory(string categoryName)
    {
      return memoryCategories.FirstOrDefault(cat => cat.CategoryName == categoryName);
    }

    /// <summary>
    /// Get all categories that should be automatically extracted
    /// </summary>
    /// <returns>List of auto-extract categories, sorted by priority</returns>
    public List<MemoryCategory> GetAutoExtractCategories()
    {
      return memoryCategories
          .Where(cat => cat.AutoExtract)
          .OrderByDescending(cat => cat.Priority)
          .ToList();
    }

    /// <summary>
    /// Extract information from text using all applicable categories
    /// </summary>
    /// <param name="text">The text to extract from</param>
    /// <returns>Dictionary of category name to list of extracted information</returns>
    public Dictionary<string, List<string>> ExtractInformationFromText(string text)
    {
      var results = new Dictionary<string, List<string>>();

      if (string.IsNullOrEmpty(text))
        return results;

      var autoExtractCategories = GetAutoExtractCategories();

      foreach (var category in autoExtractCategories)
      {
        if (category.ContainsTriggerKeywords(text))
        {
          var extracted = category.ExtractInformation(text);
          if (extracted.Count > 0)
          {
            results[category.CategoryName] = extracted;
          }
        }
      }

      return results;
    }

    /// <summary>
    /// Get all category names for UI dropdowns
    /// </summary>
    /// <returns>List of category display names</returns>
    public List<string> GetCategoryDisplayNames()
    {
      var names = new List<string> { "All Categories" };
      names.AddRange(memoryCategories.Select(cat => cat.DisplayName));
      return names;
    }

    /// <summary>
    /// Get all category names (internal names)
    /// </summary>
    /// <returns>List of category internal names</returns>
    public List<string> GetCategoryNames()
    {
      return memoryCategories.Select(cat => cat.CategoryName).ToList();
    }

    /// <summary>
    /// Add a memory category to the manager
    /// </summary>
    /// <param name="category">The category to add</param>
    public void AddCategory(MemoryCategory category)
    {
      if (category != null && !memoryCategories.Contains(category))
      {
        memoryCategories.Add(category);
      }
    }

    /// <summary>
    /// Remove a memory category from the manager
    /// </summary>
    /// <param name="category">The category to remove</param>
    public void RemoveCategory(MemoryCategory category)
    {
      if (category != null)
      {
        memoryCategories.Remove(category);
      }
    }

    /// <summary>
    /// Clear all memory categories
    /// </summary>
    public void ClearCategories()
    {
      memoryCategories.Clear();
    }

    /// <summary>
    /// Get categories by importance threshold
    /// </summary>
    /// <param name="minImportance">Minimum importance threshold</param>
    /// <returns>List of categories with importance >= threshold</returns>
    public List<MemoryCategory> GetCategoriesByImportance(float minImportance)
    {
      return memoryCategories
          .Where(cat => cat.Importance >= minImportance)
          .OrderByDescending(cat => cat.Importance)
          .ToList();
    }

    /// <summary>
    /// Get persistent categories (those that persist across sessions)
    /// </summary>
    /// <returns>List of persistent categories</returns>
    public List<MemoryCategory> GetPersistentCategories()
    {
      return memoryCategories
          .Where(cat => cat.Persistent)
          .ToList();
    }

    private void OnValidate()
    {
      // Remove any null entries
      memoryCategories.RemoveAll(cat => cat == null);

      // Ensure unique category names
      var uniqueCategories = new List<MemoryCategory>();
      var seenNames = new HashSet<string>();

      foreach (var category in memoryCategories)
      {
        if (category != null && !string.IsNullOrEmpty(category.CategoryName))
        {
          if (!seenNames.Contains(category.CategoryName))
          {
            seenNames.Add(category.CategoryName);
            uniqueCategories.Add(category);
          }
          else
          {
            Debug.LogWarning($"[MemoryCategoryManager] Duplicate category name found: {category.CategoryName}");
          }
        }
      }

      memoryCategories = uniqueCategories;
    }
  }
}