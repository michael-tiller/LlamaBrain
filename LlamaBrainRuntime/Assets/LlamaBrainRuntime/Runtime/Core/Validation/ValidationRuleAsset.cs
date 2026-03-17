using System;
using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Runtime.Core.Validation
{
  /// <summary>
  /// The type of validation rule.
  /// </summary>
  public enum ValidationRuleType
  {
    /// <summary>Pattern must NOT be found in output.</summary>
    Prohibition,
    /// <summary>Pattern MUST be found in output.</summary>
    Requirement,
    /// <summary>Custom delegate-based validation.</summary>
    Custom
  }

  /// <summary>
  /// ScriptableObject wrapper for validation rules.
  /// Allows designers to create validation rules in the Unity inspector.
  /// </summary>
  [CreateAssetMenu(fileName = "NewValidationRule", menuName = "LlamaBrain/Validation Rule", order = 100)]
  public class ValidationRuleAsset : ScriptableObject
  {
    [Header("Rule Configuration")]
    [Tooltip("Unique identifier for this rule")]
    [SerializeField] private string ruleId;

    [Tooltip("Human-readable description")]
    [TextArea(2, 4)]
    [SerializeField] private string description = "";

    [Tooltip("Type of validation")]
    [SerializeField] private ValidationRuleType ruleType = ValidationRuleType.Prohibition;

    [Tooltip("Severity of violations")]
    [SerializeField] private ConstraintSeverity severity = ConstraintSeverity.Hard;

    [Header("Pattern Matching")]
    [Tooltip("The pattern to match (regex supported)")]
    [SerializeField] private string pattern = "";

    [Tooltip("Use case-insensitive matching")]
    [SerializeField] private bool caseInsensitive = true;

    [Tooltip("Additional patterns (OR - any match triggers)")]
    [SerializeField] private List<string> additionalPatterns = new List<string>();

    [Header("Context Conditions")]
    [Tooltip("Only apply this rule in specific scenes")]
    [SerializeField] private List<string> sceneFilter = new List<string>();

    [Tooltip("Only apply to specific NPC IDs")]
    [SerializeField] private List<string> npcIdFilter = new List<string>();

    [Tooltip("Only apply for specific trigger reasons")]
    [SerializeField] private List<TriggerReason> triggerReasonFilter = new List<TriggerReason>();

    /// <summary>
    /// The rule ID.
    /// </summary>
    public string RuleId => string.IsNullOrEmpty(ruleId) ? name : ruleId;

    /// <summary>
    /// The rule description.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// The rule type.
    /// </summary>
    public ValidationRuleType RuleType => ruleType;

    /// <summary>
    /// The severity of violations.
    /// </summary>
    public ConstraintSeverity Severity => severity;

    /// <summary>
    /// Converts this asset to a core ValidationRule.
    /// </summary>
    /// <returns>A ValidationRule instance created from this asset</returns>
    public ValidationRule ToValidationRule()
    {
      var rule = new PatternValidationRule
      {
        Id = RuleId,
        Description = Description,
        Pattern = BuildPattern(),
        IsProhibition = ruleType == ValidationRuleType.Prohibition,
        CaseInsensitive = caseInsensitive,
        Severity = severity
      };

      return rule;
    }

    /// <summary>
    /// Builds the regex pattern including additional patterns.
    /// </summary>
    private string BuildPattern()
    {
      if (additionalPatterns.Count == 0)
      {
        return pattern;
      }

      var patterns = new List<string> { pattern };
      patterns.AddRange(additionalPatterns);

      // Combine with OR
      return string.Join("|", patterns);
    }

    /// <summary>
    /// Checks if this rule applies to the given context.
    /// </summary>
    /// <param name="context">The interaction context to check</param>
    /// <returns>True if this rule applies to the given context, false otherwise</returns>
    public bool AppliesTo(InteractionContext context)
    {
      // Check scene filter
      if (sceneFilter.Count > 0 && !string.IsNullOrEmpty(context.SceneName))
      {
        if (!sceneFilter.Contains(context.SceneName))
        {
          return false;
        }
      }

      // Check NPC ID filter
      if (npcIdFilter.Count > 0 && !string.IsNullOrEmpty(context.NpcId))
      {
        if (!npcIdFilter.Contains(context.NpcId))
        {
          return false;
        }
      }

      // Check trigger reason filter
      if (triggerReasonFilter.Count > 0)
      {
        if (!triggerReasonFilter.Contains(context.TriggerReason))
        {
          return false;
        }
      }

      return true;
    }
  }
}
