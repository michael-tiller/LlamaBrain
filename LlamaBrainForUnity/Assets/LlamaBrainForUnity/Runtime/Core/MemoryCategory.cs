using UnityEngine;
using System.Collections.Generic;

namespace LlamaBrain.Unity.Runtime.Core
{
  /// <summary>
  /// A ScriptableObject that defines a memory category with its properties and extraction rules
  /// </summary>
  [CreateAssetMenu(fileName = "New Memory Category", menuName = "LlamaBrain/Memory Category")]
  public class MemoryCategory : ScriptableObject
  {
    [Header("Basic Information")]
    [SerializeField] private string categoryName = "New Category";
    [SerializeField] private string displayName = "New Category";
    [SerializeField] private Color categoryColor = Color.white;
    [SerializeField] private string description = "Description of this memory category";

    [Header("Extraction Rules")]
    [SerializeField] private List<string> triggerKeywords = new List<string>();
    [SerializeField] private List<string> extractionPatterns = new List<string>();
    [SerializeField] private bool autoExtract = true;
    [SerializeField] private int priority = 0; // Higher priority categories are checked first

    [Header("Memory Properties")]
    [SerializeField] private bool persistent = true; // Whether this memory persists across sessions
    [SerializeField] private float importance = 1.0f; // How important this type of memory is (0-1)
    [SerializeField] private int maxEntries = 10; // Maximum number of entries for this category

    /// <summary>
    /// The internal name of the category
    /// </summary>
    public string CategoryName => categoryName;

    /// <summary>
    /// The display name shown in the UI
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// The color used to display this category in the UI
    /// </summary>
    public Color CategoryColor => categoryColor;

    /// <summary>
    /// Description of what this category is for
    /// </summary>
    public string Description => description;

    /// <summary>
    /// Keywords that trigger extraction for this category
    /// </summary>
    public IReadOnlyList<string> TriggerKeywords => triggerKeywords;

    /// <summary>
    /// Regex patterns for extracting information
    /// </summary>
    public IReadOnlyList<string> ExtractionPatterns => extractionPatterns;

    /// <summary>
    /// Whether this category should be automatically extracted
    /// </summary>
    public bool AutoExtract => autoExtract;

    /// <summary>
    /// Priority for extraction (higher numbers are checked first)
    /// </summary>
    public int Priority => priority;

    /// <summary>
    /// Whether this memory persists across sessions
    /// </summary>
    public bool Persistent => persistent;

    /// <summary>
    /// How important this type of memory is (0-1)
    /// </summary>
    public float Importance => importance;

    /// <summary>
    /// Maximum number of entries for this category
    /// </summary>
    public int MaxEntries => maxEntries;

    /// <summary>
    /// Check if the given text contains trigger keywords for this category
    /// </summary>
    /// <param name="text">The text to check</param>
    /// <returns>True if any trigger keywords are found</returns>
    public bool ContainsTriggerKeywords(string text)
    {
      if (string.IsNullOrEmpty(text) || triggerKeywords.Count == 0)
        return false;

      var lowerText = text.ToLower();
      foreach (var keyword in triggerKeywords)
      {
        if (lowerText.Contains(keyword.ToLower()))
          return true;
      }
      return false;
    }

    /// <summary>
    /// Extract information from text using the category's patterns
    /// </summary>
    /// <param name="text">The text to extract from</param>
    /// <returns>List of extracted information</returns>
    public List<string> ExtractInformation(string text)
    {
      var extracted = new List<string>();

      if (string.IsNullOrEmpty(text) || extractionPatterns.Count == 0)
        return extracted;

      foreach (var pattern in extractionPatterns)
      {
        try
        {
          var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
          foreach (System.Text.RegularExpressions.Match match in matches)
          {
            if (match.Groups.Count > 1)
            {
              var extractedText = match.Groups[1].Value.Trim();
              if (!string.IsNullOrEmpty(extractedText))
              {
                extracted.Add(extractedText);
              }
            }
          }
        }
        catch (System.Exception ex)
        {
          Debug.LogWarning($"[MemoryCategory] Invalid regex pattern '{pattern}' in category '{categoryName}': {ex.Message}");
        }
      }

      return extracted;
    }

    /// <summary>
    /// Format a memory entry for this category
    /// </summary>
    /// <param name="content">The memory content</param>
    /// <returns>Formatted memory entry</returns>
    public string FormatMemoryEntry(string content)
    {
      return $"[{categoryName}] {content}";
    }

    /// <summary>
    /// Parse a memory entry to extract the content
    /// </summary>
    /// <param name="memoryEntry">The memory entry to parse</param>
    /// <returns>The content without the category prefix, or null if not for this category</returns>
    public string ParseMemoryEntry(string memoryEntry)
    {
      if (string.IsNullOrEmpty(memoryEntry) || !memoryEntry.StartsWith($"[{categoryName}]"))
        return null;

      return memoryEntry.Substring(categoryName.Length + 3).Trim(); // Remove "[CategoryName] "
    }

    private void OnValidate()
    {
      // Ensure category name is valid
      if (string.IsNullOrEmpty(categoryName))
      {
        categoryName = name;
      }

      // Ensure display name is set
      if (string.IsNullOrEmpty(displayName))
      {
        displayName = categoryName;
      }

      // Clamp importance between 0 and 1
      importance = Mathf.Clamp01(importance);

      // Ensure max entries is positive
      if (maxEntries <= 0)
      {
        maxEntries = 1;
      }
    }
  }
}