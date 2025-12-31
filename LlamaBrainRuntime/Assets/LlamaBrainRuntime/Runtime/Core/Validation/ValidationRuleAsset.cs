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

  /// <summary>
  /// Collection of validation rules that can be assigned to NPCs or applied globally.
  /// </summary>
  [CreateAssetMenu(fileName = "ValidationRuleSet", menuName = "LlamaBrain/Validation Rule Set", order = 101)]
  public class ValidationRuleSetAsset : ScriptableObject
  {
    [Header("Rule Set")]
    [Tooltip("The validation rules in this set")]
    [SerializeField] private List<ValidationRuleAsset> rules = new List<ValidationRuleAsset>();

    [Tooltip("Whether this rule set is enabled")]
    [SerializeField] private bool enabled = true;

    /// <summary>
    /// Whether this rule set is enabled.
    /// </summary>
    public bool Enabled => enabled;

    /// <summary>
    /// Gets all rules in this set.
    /// </summary>
    public IReadOnlyList<ValidationRuleAsset> Rules => rules;

    /// <summary>
    /// Gets rules that apply to the given context.
    /// </summary>
    /// <param name="context">The interaction context to filter rules for</param>
    /// <returns>An enumerable of validation rules that apply to the given context</returns>
    public IEnumerable<ValidationRule> GetApplicableRules(InteractionContext context)
    {
      if (!enabled) yield break;

      foreach (var rule in rules)
      {
        if (rule != null && rule.AppliesTo(context))
        {
          yield return rule.ToValidationRule();
        }
      }
    }

    /// <summary>
    /// Converts all rules to core ValidationRules.
    /// </summary>
    /// <returns>An enumerable of all validation rules in this set</returns>
    public IEnumerable<ValidationRule> ToValidationRules()
    {
      if (!enabled) yield break;

      foreach (var rule in rules)
      {
        if (rule != null)
        {
          yield return rule.ToValidationRule();
        }
      }
    }
  }
}
