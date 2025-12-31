using System;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Core.Expectancy
{
  /// <summary>
  /// Callback for logging from the evaluator.
  /// Engine-specific layers can provide their own logging implementation.
  /// </summary>
  /// <param name="message">The log message.</param>
  public delegate void LogCallback(string message);

  /// <summary>
  /// Core expectancy rule evaluator.
  /// Engine-agnostic - can be used with Unity, Unreal, Godot, etc.
  /// Engine-specific layers should wrap this class.
  /// </summary>
  public class ExpectancyEvaluator
  {
    private readonly List<IExpectancyRule> _codeBasedRules = new List<IExpectancyRule>();

    /// <summary>
    /// Optional logging callback for debug output.
    /// </summary>
    public LogCallback? OnLog { get; set; }

    /// <summary>
    /// Registers a code-based rule with the evaluator.
    /// </summary>
    /// <param name="rule">The rule to register.</param>
    public void RegisterRule(IExpectancyRule rule)
    {
      if (rule != null && !_codeBasedRules.Any(r => r.RuleId == rule.RuleId))
      {
        _codeBasedRules.Add(rule);
        OnLog?.Invoke($"[ExpectancyEvaluator] Registered rule: {rule.RuleName}");
      }
    }

    /// <summary>
    /// Unregisters a code-based rule.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to unregister.</param>
    public void UnregisterRule(string ruleId)
    {
      _codeBasedRules.RemoveAll(r => r.RuleId == ruleId);
    }

    /// <summary>
    /// Gets all registered code-based rules.
    /// </summary>
    public IReadOnlyList<IExpectancyRule> CodeBasedRules => _codeBasedRules;

    /// <summary>
    /// Evaluates all rules against the given context.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="additionalRules">Additional rules to evaluate (e.g., NPC-specific rules).</param>
    /// <returns>The generated constraint set.</returns>
    public ConstraintSet Evaluate(InteractionContext context, IEnumerable<IExpectancyRule>? additionalRules = null)
    {
      var constraints = new ConstraintSet();

      // Collect all rules
      var allRules = new List<IExpectancyRule>(_codeBasedRules);
      if (additionalRules != null)
      {
        allRules.AddRange(additionalRules);
      }

      // Sort by priority (higher first)
      allRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));

      // Evaluate each rule
      foreach (var rule in allRules)
      {
        if (!rule.IsEnabled) continue;

        try
        {
          if (rule.Evaluate(context))
          {
            rule.GenerateConstraints(context, constraints);
            OnLog?.Invoke($"[ExpectancyEvaluator] Rule '{rule.RuleName}' matched, generated constraints");
          }
        }
        catch (Exception ex)
        {
          OnLog?.Invoke($"[ExpectancyEvaluator] Error evaluating rule '{rule.RuleName}': {ex.Message}");
        }
      }

      OnLog?.Invoke($"[ExpectancyEvaluator] Generated {constraints.Count} constraints for context: {context}");
      return constraints;
    }

    /// <summary>
    /// Clears all registered code-based rules.
    /// </summary>
    public void ClearRules()
    {
      _codeBasedRules.Clear();
    }
  }
}
