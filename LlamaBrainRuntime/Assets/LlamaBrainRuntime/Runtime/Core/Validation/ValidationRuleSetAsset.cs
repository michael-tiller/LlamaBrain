using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Runtime.Core.Validation
{
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
