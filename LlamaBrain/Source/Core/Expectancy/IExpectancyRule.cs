namespace LlamaBrain.Core.Expectancy
{
  /// <summary>
  /// Interface for expectancy rules that generate constraints.
  /// Implement this for complex rules that need C# logic.
  /// Engine-agnostic - can be used with Unity, Unreal, Godot, etc.
  /// </summary>
  public interface IExpectancyRule
  {
    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Human-readable name for this rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Whether this rule is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Evaluation priority (higher = evaluated first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Evaluates whether this rule applies to the given context.
    /// </summary>
    /// <param name="context">The interaction context to evaluate.</param>
    /// <returns>True if this rule should generate constraints for this context.</returns>
    bool Evaluate(InteractionContext context);

    /// <summary>
    /// Generates constraints for the given context.
    /// Only called if Evaluate() returns true.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="constraints">The constraint set to add constraints to.</param>
    void GenerateConstraints(InteractionContext context, ConstraintSet constraints);
  }
}
