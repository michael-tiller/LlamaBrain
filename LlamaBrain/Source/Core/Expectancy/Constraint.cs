using System.Collections.Generic;

namespace LlamaBrain.Core.Expectancy
{
  /// <summary>
  /// The type of constraint.
  /// </summary>
  public enum ConstraintType
  {
    /// <summary>Something the NPC must NOT do or say.</summary>
    Prohibition,
    /// <summary>Something the NPC MUST do or include.</summary>
    Requirement,
    /// <summary>Something the NPC is allowed to do (overrides default prohibitions).</summary>
    Permission
  }

  /// <summary>
  /// The severity of a constraint - affects retry behavior.
  /// </summary>
  public enum ConstraintSeverity
  {
    /// <summary>Soft constraint - violation triggers retry with warning.</summary>
    Soft,
    /// <summary>Hard constraint - violation always triggers retry.</summary>
    Hard,
    /// <summary>Critical constraint - violation uses fallback immediately.</summary>
    Critical
  }

  /// <summary>
  /// A single constraint that shapes NPC behavior.
  /// Used both for prompt injection and output validation.
  /// </summary>
  [System.Serializable]
  public class Constraint
  {
    /// <summary>
    /// Unique identifier for this constraint.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The type of constraint.
    /// </summary>
    public ConstraintType Type { get; set; }

    /// <summary>
    /// The severity of this constraint.
    /// </summary>
    public ConstraintSeverity Severity { get; set; } = ConstraintSeverity.Hard;

    /// <summary>
    /// Human-readable description of the constraint (for logging/debugging).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The text to inject into the prompt to communicate this constraint to the LLM.
    /// Example: "You must not reveal the location of the treasure."
    /// </summary>
    public string? PromptInjection { get; set; }

    /// <summary>
    /// Patterns to check in the output for violation detection.
    /// For prohibitions: if any pattern matches, constraint is violated.
    /// For requirements: if no pattern matches, constraint is violated.
    /// </summary>
    public List<string> ValidationPatterns { get; set; } = new List<string>();

    /// <summary>
    /// Whether this constraint uses regex patterns (vs simple contains).
    /// </summary>
    public bool UseRegex { get; set; } = false;

    /// <summary>
    /// The source rule that generated this constraint (for debugging).
    /// </summary>
    public string? SourceRule { get; set; }

    /// <summary>
    /// Creates a prohibition constraint.
    /// </summary>
    /// <param name="id">Unique identifier for the constraint</param>
    /// <param name="description">Human-readable description of the constraint</param>
    /// <param name="promptInjection">Text to inject into the prompt</param>
    /// <param name="patterns">Validation patterns to check for violations</param>
    /// <returns>A new prohibition constraint</returns>
    public static Constraint Prohibition(string id, string description, string promptInjection, params string[] patterns)
    {
      return new Constraint
      {
        Id = id,
        Type = ConstraintType.Prohibition,
        Description = description,
        PromptInjection = promptInjection,
        ValidationPatterns = new List<string>(patterns)
      };
    }

    /// <summary>
    /// Creates a requirement constraint.
    /// </summary>
    /// <param name="id">Unique identifier for the constraint</param>
    /// <param name="description">Human-readable description of the constraint</param>
    /// <param name="promptInjection">Text to inject into the prompt</param>
    /// <returns>A new requirement constraint</returns>
    public static Constraint Requirement(string id, string description, string promptInjection)
    {
      return new Constraint
      {
        Id = id,
        Type = ConstraintType.Requirement,
        Description = description,
        PromptInjection = promptInjection
      };
    }

    /// <summary>
    /// Creates a permission constraint.
    /// </summary>
    /// <param name="id">Unique identifier for the constraint</param>
    /// <param name="description">Human-readable description of the constraint</param>
    /// <param name="promptInjection">Text to inject into the prompt</param>
    /// <returns>A new permission constraint</returns>
    public static Constraint Permission(string id, string description, string promptInjection)
    {
      return new Constraint
      {
        Id = id,
        Type = ConstraintType.Permission,
        Description = description,
        PromptInjection = promptInjection
      };
    }

    /// <summary>
    /// Returns a string representation of this constraint.
    /// </summary>
    /// <returns>A formatted string showing the constraint type, severity, and description</returns>
    public override string ToString()
    {
      return $"[{Type}:{Severity}] {Description}";
    }
  }
}
