using System;
using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Runtime.Core.Expectancy
{
  /// <summary>
  /// Defines when a rule should be applied.
  /// </summary>
  [Serializable]
  public class RuleCondition
  {
    /// <summary>
    /// The type of condition to check.
    /// </summary>
    public enum ConditionType
    {
      /// <summary>Always apply this rule.</summary>
      Always,
      /// <summary>Apply when trigger reason matches.</summary>
      TriggerReason,
      /// <summary>Apply when NPC ID matches.</summary>
      NpcId,
      /// <summary>Apply when trigger ID matches.</summary>
      TriggerId,
      /// <summary>Apply when scene name matches.</summary>
      SceneName,
      /// <summary>Apply when context has specific tag.</summary>
      HasTag,
      /// <summary>Apply when interaction count is in range.</summary>
      InteractionCount
    }

    /// <summary>
    /// The type of condition to evaluate.
    /// </summary>
    public ConditionType Type = ConditionType.Always;

    /// <summary>
    /// The value to match against (for string conditions).
    /// </summary>
    public string MatchValue = "";

    /// <summary>
    /// For TriggerReason condition - which trigger types to match.
    /// </summary>
    public TriggerReason TriggerReasonMatch = TriggerReason.PlayerUtterance;

    /// <summary>
    /// For InteractionCount condition - minimum count.
    /// </summary>
    public int MinInteractionCount = 0;

    /// <summary>
    /// For InteractionCount condition - maximum count (-1 = unlimited).
    /// </summary>
    public int MaxInteractionCount = -1;

    /// <summary>
    /// Whether to invert this condition (NOT).
    /// </summary>
    public bool Invert = false;

    /// <summary>
    /// Evaluates this condition against the given context.
    /// </summary>
    /// <param name="context">The interaction context to evaluate</param>
    /// <returns>True if the condition matches, false otherwise</returns>
    public bool Evaluate(InteractionContext context)
    {
      bool result = Type switch
      {
        ConditionType.Always => true,
        ConditionType.TriggerReason => context.TriggerReason == TriggerReasonMatch,
        ConditionType.NpcId => string.Equals(context.NpcId, MatchValue, StringComparison.OrdinalIgnoreCase),
        ConditionType.TriggerId => string.Equals(context.TriggerId, MatchValue, StringComparison.OrdinalIgnoreCase),
        ConditionType.SceneName => string.Equals(context.SceneName, MatchValue, StringComparison.OrdinalIgnoreCase),
        ConditionType.HasTag => context.Tags != null && Array.Exists(context.Tags, t => string.Equals(t, MatchValue, StringComparison.OrdinalIgnoreCase)),
        ConditionType.InteractionCount => context.InteractionCount >= MinInteractionCount && (MaxInteractionCount < 0 || context.InteractionCount <= MaxInteractionCount),
        _ => false
      };

      return Invert ? !result : result;
    }
  }

  /// <summary>
  /// Defines a constraint to generate when rule conditions are met.
  /// </summary>
  [Serializable]
  public class RuleConstraintDefinition
  {
    /// <summary>
    /// The type of constraint (Prohibition, Requirement, or Permission).
    /// </summary>
    public ConstraintType Type = ConstraintType.Prohibition;
    /// <summary>
    /// The severity of the constraint (affects retry behavior).
    /// </summary>
    public ConstraintSeverity Severity = ConstraintSeverity.Hard;

    /// <summary>
    /// Human-readable description of the constraint.
    /// </summary>
    [TextArea(2, 4)]
    public string Description = "";

    /// <summary>
    /// The text injected into the prompt to enforce this constraint.
    /// </summary>
    [TextArea(2, 4)]
    [Tooltip("The text injected into the prompt to enforce this constraint.")]
    public string PromptInjection = "";

    /// <summary>
    /// Patterns to detect violations. For prohibitions: any match = violation. For requirements: no match = violation.
    /// </summary>
    [Tooltip("Patterns to detect violations. For prohibitions: any match = violation. For requirements: no match = violation.")]
    public List<string> ValidationPatterns = new List<string>();

    /// <summary>
    /// Whether validation patterns should be interpreted as regex patterns.
    /// </summary>
    public bool UseRegex = false;

    /// <summary>
    /// Converts this definition to a Constraint object.
    /// </summary>
    /// <param name="ruleId">The ID of the rule that generated this constraint</param>
    /// <param name="index">The index of this constraint within the rule</param>
    /// <returns>A new Constraint object with the configured properties</returns>
    public Constraint ToConstraint(string ruleId, int index)
    {
      return new Constraint
      {
        Id = $"{ruleId}_constraint_{index}",
        Type = Type,
        Severity = Severity,
        Description = Description,
        PromptInjection = PromptInjection,
        ValidationPatterns = new List<string>(ValidationPatterns),
        UseRegex = UseRegex,
        SourceRule = ruleId
      };
    }
  }

  /// <summary>
  /// ScriptableObject-based expectancy rule for declarative rule definition in the Unity Editor.
  /// Create these in your project to define rules without writing code.
  /// </summary>
  [CreateAssetMenu(fileName = "New Expectancy Rule", menuName = "LlamaBrain/Expectancy Rule")]
  public class ExpectancyRuleAsset : ScriptableObject, IExpectancyRule
  {
    [Header("Rule Identity")]
    [Tooltip("Unique identifier for this rule.")]
    [SerializeField] private string ruleId;

    [Tooltip("Human-readable name.")]
    [SerializeField] private string ruleName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Header("Rule Settings")]
    [Tooltip("Whether this rule is active.")]
    [SerializeField] private bool isEnabled = true;

    [Tooltip("Higher priority rules are evaluated first.")]
    [SerializeField] private int priority = 0;

    [Header("Conditions")]
    [Tooltip("How multiple conditions should be combined.")]
    [SerializeField] private ConditionCombineMode conditionMode = ConditionCombineMode.All;

    [SerializeField] private List<RuleCondition> conditions = new List<RuleCondition>();

    [Header("Constraints")]
    [SerializeField] private List<RuleConstraintDefinition> constraints = new List<RuleConstraintDefinition>();

    /// <summary>
    /// How multiple conditions should be combined when evaluating the rule.
    /// </summary>
    public enum ConditionCombineMode
    {
      /// <summary>All conditions must be true (AND).</summary>
      All,
      /// <summary>Any condition must be true (OR).</summary>
      Any
    }

    // IExpectancyRule implementation
    /// <summary>
    /// Gets the unique identifier for this rule.
    /// </summary>
    public string RuleId => string.IsNullOrEmpty(ruleId) ? name : ruleId;
    /// <summary>
    /// Gets the human-readable name of this rule.
    /// </summary>
    public string RuleName => string.IsNullOrEmpty(ruleName) ? name : ruleName;
    /// <summary>
    /// Gets whether this rule is currently enabled.
    /// </summary>
    public bool IsEnabled => isEnabled;
    /// <summary>
    /// Gets the priority of this rule (higher priority rules are evaluated first).
    /// </summary>
    public int Priority => priority;

    /// <summary>
    /// Evaluates whether this rule's conditions match the given interaction context.
    /// </summary>
    /// <param name="context">The interaction context to evaluate</param>
    /// <returns>True if the rule conditions are met, false otherwise</returns>
    public bool Evaluate(InteractionContext context)
    {
      if (!isEnabled) return false;
      if (conditions.Count == 0) return true; // No conditions = always apply

      if (conditionMode == ConditionCombineMode.All)
      {
        foreach (var condition in conditions)
        {
          if (!condition.Evaluate(context)) return false;
        }
        return true;
      }
      else // Any
      {
        foreach (var condition in conditions)
        {
          if (condition.Evaluate(context)) return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Generates constraints from this rule and adds them to the constraint set.
    /// </summary>
    /// <param name="context">The interaction context (used for constraint generation)</param>
    /// <param name="constraintSet">The constraint set to add generated constraints to</param>
    public void GenerateConstraints(InteractionContext context, ConstraintSet constraintSet)
    {
      for (int i = 0; i < constraints.Count; i++)
      {
        var constraint = constraints[i].ToConstraint(RuleId, i);
        constraintSet.Add(constraint);
      }
    }

    private void Reset()
    {
      ruleId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
      ruleName = "New Rule";
    }
  }
}
