using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Core.Validation
{
  /// <summary>
  /// The reason validation failed.
  /// </summary>
  public enum ValidationFailureReason
  {
    /// <summary>No failure - validation passed.</summary>
    None,
    /// <summary>A prohibition constraint was violated.</summary>
    ProhibitionViolated,
    /// <summary>A requirement constraint was not met.</summary>
    RequirementNotMet,
    /// <summary>Output contradicts a canonical fact.</summary>
    CanonicalFactContradiction,
    /// <summary>Output reveals knowledge the NPC shouldn't have.</summary>
    KnowledgeBoundaryViolation,
    /// <summary>Proposed mutation would modify canonical facts.</summary>
    CanonicalMutationAttempt,
    /// <summary>Output contains invalid format or structure.</summary>
    InvalidFormat,
    /// <summary>Custom validation rule failed.</summary>
    CustomRuleFailed
  }

  /// <summary>
  /// Details about a specific validation failure.
  /// </summary>
  [Serializable]
  public class ValidationFailure
  {
    /// <summary>
    /// The reason for the failure.
    /// </summary>
    public ValidationFailureReason Reason { get; set; }

    /// <summary>
    /// Human-readable description of the failure.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// The specific text that caused the failure (if applicable).
    /// </summary>
    public string? ViolatingText { get; set; }

    /// <summary>
    /// The rule or constraint that was violated (if applicable).
    /// </summary>
    public string? ViolatedRule { get; set; }

    /// <summary>
    /// The severity of this failure.
    /// </summary>
    public ConstraintSeverity Severity { get; set; } = ConstraintSeverity.Hard;

    /// <summary>
    /// Creates a prohibition violation failure.
    /// </summary>
    /// <param name="description">Human-readable description of the violation</param>
    /// <param name="violatingText">The specific text that caused the violation</param>
    /// <param name="rule">The rule or constraint that was violated</param>
    /// <returns>A ValidationFailure representing a prohibition violation</returns>
    public static ValidationFailure ProhibitionViolated(string description, string? violatingText = null, string? rule = null)
    {
      return new ValidationFailure
      {
        Reason = ValidationFailureReason.ProhibitionViolated,
        Description = description,
        ViolatingText = violatingText,
        ViolatedRule = rule
      };
    }

    /// <summary>
    /// Creates a requirement not met failure.
    /// </summary>
    /// <param name="description">Human-readable description of why the requirement was not met</param>
    /// <param name="rule">The rule or constraint that was not met</param>
    /// <returns>A ValidationFailure representing a requirement not met</returns>
    public static ValidationFailure RequirementNotMet(string description, string? rule = null)
    {
      return new ValidationFailure
      {
        Reason = ValidationFailureReason.RequirementNotMet,
        Description = description,
        ViolatedRule = rule
      };
    }

    /// <summary>
    /// Creates a canonical fact contradiction failure.
    /// </summary>
    /// <param name="factId">The ID of the canonical fact that was contradicted</param>
    /// <param name="factContent">The content of the canonical fact</param>
    /// <param name="violatingText">The specific text that contradicts the fact</param>
    /// <returns>A ValidationFailure representing a canonical fact contradiction</returns>
    public static ValidationFailure CanonicalContradiction(string factId, string factContent, string? violatingText = null)
    {
      return new ValidationFailure
      {
        Reason = ValidationFailureReason.CanonicalFactContradiction,
        Description = $"Output contradicts canonical fact '{factId}': {factContent}",
        ViolatingText = violatingText,
        ViolatedRule = factId,
        Severity = ConstraintSeverity.Critical
      };
    }

    /// <summary>
    /// Creates a knowledge boundary violation failure.
    /// </summary>
    /// <param name="description">Human-readable description of the knowledge boundary violation</param>
    /// <param name="violatingText">The specific text that violates the knowledge boundary</param>
    /// <returns>A ValidationFailure representing a knowledge boundary violation</returns>
    public static ValidationFailure KnowledgeBoundary(string description, string? violatingText = null)
    {
      return new ValidationFailure
      {
        Reason = ValidationFailureReason.KnowledgeBoundaryViolation,
        Description = description,
        ViolatingText = violatingText
      };
    }

    /// <summary>
    /// Creates a canonical mutation attempt failure.
    /// </summary>
    /// <param name="factId">The ID of the canonical fact that was attempted to be mutated</param>
    /// <returns>A ValidationFailure representing an attempt to mutate a canonical fact</returns>
    public static ValidationFailure CanonicalMutation(string factId)
    {
      return new ValidationFailure
      {
        Reason = ValidationFailureReason.CanonicalMutationAttempt,
        Description = $"Attempted to mutate canonical fact: {factId}",
        ViolatedRule = factId,
        Severity = ConstraintSeverity.Critical
      };
    }

    /// <summary>
    /// Returns a string representation of this validation failure.
    /// </summary>
    /// <returns>A string representation of the validation failure</returns>
    public override string ToString()
    {
      return $"[{Reason}:{Severity}] {Description}";
    }
  }

  /// <summary>
  /// The result of validation gate processing.
  /// </summary>
  [Serializable]
  public class GateResult
  {
    /// <summary>
    /// Whether validation passed (all gates cleared).
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// The validated output (if passed).
    /// </summary>
    public ParsedOutput? ValidatedOutput { get; set; }

    /// <summary>
    /// List of validation failures (if any).
    /// </summary>
    public List<ValidationFailure> Failures { get; set; } = new List<ValidationFailure>();

    /// <summary>
    /// Mutations that were approved for execution.
    /// </summary>
    public List<ProposedMutation> ApprovedMutations { get; set; } = new List<ProposedMutation>();

    /// <summary>
    /// Mutations that were rejected.
    /// </summary>
    public List<ProposedMutation> RejectedMutations { get; set; } = new List<ProposedMutation>();

    /// <summary>
    /// World intents that were approved.
    /// </summary>
    public List<WorldIntent> ApprovedIntents { get; set; } = new List<WorldIntent>();

    /// <summary>
    /// Whether any critical failures occurred (immediate fallback required).
    /// </summary>
    public bool HasCriticalFailure => Failures.Exists(f => f.Severity == ConstraintSeverity.Critical);

    /// <summary>
    /// Whether retry is recommended based on failure types.
    /// </summary>
    public bool ShouldRetry => !Passed && !HasCriticalFailure && Failures.Count > 0;

    /// <summary>
    /// Creates a passed result.
    /// </summary>
    /// <param name="output">The validated output that passed all gates</param>
    /// <returns>A GateResult indicating successful validation</returns>
    public static GateResult Pass(ParsedOutput output)
    {
      return new GateResult
      {
        Passed = true,
        ValidatedOutput = output,
        ApprovedMutations = new List<ProposedMutation>(output.ProposedMutations),
        ApprovedIntents = new List<WorldIntent>(output.WorldIntents)
      };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="failures">The validation failures that caused the gate to fail</param>
    /// <returns>A GateResult indicating failed validation with the specified failures</returns>
    public static GateResult Fail(params ValidationFailure[] failures)
    {
      return new GateResult
      {
        Passed = false,
        Failures = new List<ValidationFailure>(failures)
      };
    }

    /// <summary>
    /// Returns a string representation of this gate result.
    /// </summary>
    /// <returns>A string representation of the gate result</returns>
    public override string ToString()
    {
      if (Passed)
      {
        return $"GateResult[PASS] {ApprovedMutations.Count} mutations, {ApprovedIntents.Count} intents";
      }
      return $"GateResult[FAIL] {Failures.Count} failures (critical: {HasCriticalFailure})";
    }
  }

  /// <summary>
  /// A custom validation rule that can be added to the gate.
  /// </summary>
  public abstract class ValidationRule
  {
    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Human-readable description of the rule.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// The severity of violations of this rule.
    /// </summary>
    public ConstraintSeverity Severity { get; set; } = ConstraintSeverity.Hard;

    /// <summary>
    /// Validates the parsed output against this rule.
    /// </summary>
    /// <param name="output">The parsed output to validate</param>
    /// <param name="context">Optional validation context</param>
    /// <returns>Validation failure if rule is violated, null otherwise</returns>
    public abstract ValidationFailure? Validate(ParsedOutput output, ValidationContext? context);
  }

  /// <summary>
  /// A pattern-based validation rule.
  /// </summary>
  public class PatternValidationRule : ValidationRule
  {
    /// <summary>
    /// The regex pattern to match.
    /// </summary>
    public string Pattern { get; set; } = "";

    /// <summary>
    /// Whether the pattern should NOT be found (prohibition) or MUST be found (requirement).
    /// </summary>
    public bool IsProhibition { get; set; } = true;

    /// <summary>
    /// Whether to use case-insensitive matching.
    /// </summary>
    public bool CaseInsensitive { get; set; } = true;

    /// <summary>
    /// Validates the parsed output against this pattern-based rule.
    /// </summary>
    /// <param name="output">The parsed output to validate</param>
    /// <param name="context">Optional validation context</param>
    /// <returns>Validation failure if rule is violated, null otherwise</returns>
    public override ValidationFailure? Validate(ParsedOutput output, ValidationContext? context)
    {
      var options = CaseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None;
      var isMatch = Regex.IsMatch(output.DialogueText, Pattern, options);

      if (IsProhibition && isMatch)
      {
        var match = Regex.Match(output.DialogueText, Pattern, options);
        return new ValidationFailure
        {
          Reason = ValidationFailureReason.CustomRuleFailed,
          Description = Description,
          ViolatingText = match.Value,
          ViolatedRule = Id,
          Severity = Severity
        };
      }

      if (!IsProhibition && !isMatch)
      {
        return new ValidationFailure
        {
          Reason = ValidationFailureReason.RequirementNotMet,
          Description = Description,
          ViolatedRule = Id,
          Severity = Severity
        };
      }

      return null;
    }
  }

  /// <summary>
  /// Context information for validation.
  /// </summary>
  public class ValidationContext
  {
    /// <summary>
    /// The memory system for canonical fact checking.
    /// </summary>
    public AuthoritativeMemorySystem? MemorySystem { get; set; }

    /// <summary>
    /// The constraint set from the expectancy engine.
    /// </summary>
    public ConstraintSet? Constraints { get; set; }

    /// <summary>
    /// The NPC's knowledge boundaries (topics they shouldn't know about).
    /// </summary>
    public List<string> ForbiddenKnowledge { get; set; } = new List<string>();

    /// <summary>
    /// The current state snapshot.
    /// </summary>
    public StateSnapshot? Snapshot { get; set; }
  }

  /// <summary>
  /// Configuration for the validation gate.
  /// </summary>
  [Serializable]
  public class ValidationGateConfig
  {
    /// <summary>
    /// Whether to check constraints from the expectancy engine.
    /// </summary>
    public bool CheckConstraints { get; set; } = true;

    /// <summary>
    /// Whether to check for canonical fact contradictions.
    /// </summary>
    public bool CheckCanonicalFacts { get; set; } = true;

    /// <summary>
    /// Whether to check knowledge boundaries.
    /// </summary>
    public bool CheckKnowledgeBoundaries { get; set; } = true;

    /// <summary>
    /// Whether to validate proposed mutations.
    /// </summary>
    public bool ValidateMutations { get; set; } = true;

    /// <summary>
    /// Default configuration.
    /// </summary>
    public static ValidationGateConfig Default => new ValidationGateConfig();

    /// <summary>
    /// Minimal validation (constraints only).
    /// </summary>
    public static ValidationGateConfig Minimal => new ValidationGateConfig
    {
      CheckCanonicalFacts = false,
      CheckKnowledgeBoundaries = false,
      ValidateMutations = false
    };
  }

  /// <summary>
  /// Validates parsed output against constraints, canonical facts, and knowledge boundaries.
  /// This is the central gate that determines whether output can proceed to memory mutation.
  /// </summary>
  public class ValidationGate
  {
    private readonly ValidationGateConfig config;
    private readonly List<ValidationRule> customRules = new List<ValidationRule>();
    private readonly ResponseValidator constraintValidator;

    /// <summary>
    /// Optional logging callback.
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// Creates a new validation gate with default configuration.
    /// </summary>
    public ValidationGate() : this(ValidationGateConfig.Default) { }

    /// <summary>
    /// Creates a new validation gate with specified configuration.
    /// </summary>
    /// <param name="config">The configuration to use for validation. If null, uses default configuration.</param>
    public ValidationGate(ValidationGateConfig config)
    {
      this.config = config ?? ValidationGateConfig.Default;
      this.constraintValidator = new ResponseValidator();
    }

    /// <summary>
    /// Adds a custom validation rule to the gate.
    /// </summary>
    /// <param name="rule">The validation rule to add</param>
    public void AddRule(ValidationRule rule)
    {
      customRules.Add(rule);
    }

    /// <summary>
    /// Removes a custom validation rule by ID.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to remove</param>
    /// <returns>True if the rule was found and removed, false otherwise</returns>
    public bool RemoveRule(string ruleId)
    {
      return customRules.RemoveAll(r => r.Id == ruleId) > 0;
    }

    /// <summary>
    /// Clears all custom validation rules.
    /// </summary>
    public void ClearRules()
    {
      customRules.Clear();
    }

    /// <summary>
    /// Validates parsed output through all gate checks.
    /// </summary>
    /// <param name="output">The parsed output to validate</param>
    /// <param name="context">Validation context with memory and constraints</param>
    /// <returns>Gate result indicating pass/fail and any failures</returns>
    public GateResult Validate(ParsedOutput output, ValidationContext? context = null)
    {
      if (!output.Success)
      {
        return GateResult.Fail(new ValidationFailure
        {
          Reason = ValidationFailureReason.InvalidFormat,
          Description = output.ErrorMessage ?? "Parsing failed"
        });
      }

      var failures = new List<ValidationFailure>();
      var approvedMutations = new List<ProposedMutation>();
      var rejectedMutations = new List<ProposedMutation>();
      var approvedIntents = new List<WorldIntent>(output.WorldIntents);

      // Gate 1: Constraint validation (expectancy rules)
      if (config.CheckConstraints && context?.Constraints != null)
      {
        var constraintFailures = ValidateConstraints(output.DialogueText, context.Constraints);
        failures.AddRange(constraintFailures);
      }

      // Gate 2: Canonical fact contradiction check
      if (config.CheckCanonicalFacts && context?.MemorySystem != null)
      {
        var canonicalFailures = ValidateCanonicalFacts(output.DialogueText, context.MemorySystem);
        failures.AddRange(canonicalFailures);
      }

      // Gate 3: Knowledge boundary check
      if (config.CheckKnowledgeBoundaries && context != null && context.ForbiddenKnowledge.Count > 0)
      {
        var knowledgeFailures = ValidateKnowledgeBoundaries(output.DialogueText, context.ForbiddenKnowledge);
        failures.AddRange(knowledgeFailures);
      }

      // Gate 4: Mutation validation
      if (config.ValidateMutations && output.ProposedMutations.Count > 0)
      {
        foreach (var mutation in output.ProposedMutations)
        {
          var mutationResult = ValidateMutation(mutation, context);
          if (mutationResult == null)
          {
            approvedMutations.Add(mutation);
          }
          else
          {
            failures.Add(mutationResult);
            rejectedMutations.Add(mutation);
          }
        }
      }
      else
      {
        approvedMutations.AddRange(output.ProposedMutations);
      }

      // Gate 5: Custom rules
      foreach (var rule in customRules)
      {
        var ruleFailure = rule.Validate(output, context);
        if (ruleFailure != null)
        {
          failures.Add(ruleFailure);
        }
      }

      // Determine result
      if (failures.Count == 0)
      {
        OnLog?.Invoke($"[ValidationGate] PASSED: {output}");
        return new GateResult
        {
          Passed = true,
          ValidatedOutput = output,
          ApprovedMutations = approvedMutations,
          ApprovedIntents = approvedIntents
        };
      }

      OnLog?.Invoke($"[ValidationGate] FAILED: {failures.Count} failures");
      foreach (var failure in failures)
      {
        OnLog?.Invoke($"  - {failure}");
      }

      return new GateResult
      {
        Passed = false,
        Failures = failures,
        ApprovedMutations = approvedMutations,
        RejectedMutations = rejectedMutations,
        ApprovedIntents = approvedIntents
      };
    }

    /// <summary>
    /// Validates output against constraint set.
    /// </summary>
    private List<ValidationFailure> ValidateConstraints(string text, ConstraintSet constraints)
    {
      var failures = new List<ValidationFailure>();
      var result = constraintValidator.Validate(text, constraints);

      foreach (var violation in result.Violations)
      {
        var reason = violation.Constraint.Type == ConstraintType.Prohibition
          ? ValidationFailureReason.ProhibitionViolated
          : ValidationFailureReason.RequirementNotMet;

        failures.Add(new ValidationFailure
        {
          Reason = reason,
          Description = violation.Description,
          ViolatingText = violation.ViolatingText,
          ViolatedRule = violation.Constraint.Id,
          Severity = violation.Constraint.Severity
        });
      }

      return failures;
    }

    /// <summary>
    /// Validates output against canonical facts (detects contradictions).
    /// </summary>
    private List<ValidationFailure> ValidateCanonicalFacts(string text, AuthoritativeMemorySystem memorySystem)
    {
      var failures = new List<ValidationFailure>();
      var textLower = text.ToLowerInvariant();

      foreach (var fact in memorySystem.GetCanonicalFacts())
      {
        var factContent = fact.Content.ToLowerInvariant();

        // Check for explicit negation patterns - both prefix and inline
        var negationPatterns = new[]
        {
          $"not {factContent}",
          $"isn't {factContent}",
          $"is not {factContent}",
          $"wasn't {factContent}",
          $"was not {factContent}",
          $"don't {factContent}",
          $"doesn't {factContent}",
          $"never {factContent}"
        };

        foreach (var pattern in negationPatterns)
        {
          if (textLower.Contains(pattern))
          {
            failures.Add(ValidationFailure.CanonicalContradiction(fact.FactId, fact.Content, pattern));
            break;
          }
        }

        // Also check for inline negation (e.g., "X is Y" vs "X is not Y")
        // Transform "is " to "is not " and check if output matches the negated version
        if (factContent.Contains(" is "))
        {
          var negatedFact = factContent.Replace(" is ", " is not ");
          if (textLower.Contains(negatedFact))
          {
            failures.Add(ValidationFailure.CanonicalContradiction(fact.FactId, fact.Content, negatedFact));
          }
        }

        // Also check for "isn't" variant
        if (factContent.Contains(" is "))
        {
          var negatedFact = factContent.Replace(" is ", " isn't ");
          if (textLower.Contains(negatedFact))
          {
            failures.Add(ValidationFailure.CanonicalContradiction(fact.FactId, fact.Content, negatedFact));
          }
        }

        // Check for direct contradiction keywords
        if (fact.ContradictionKeywords != null)
        {
          foreach (var keyword in fact.ContradictionKeywords)
          {
            if (textLower.Contains(keyword.ToLowerInvariant()))
            {
              failures.Add(ValidationFailure.CanonicalContradiction(fact.FactId, fact.Content, keyword));
              break;
            }
          }
        }
      }

      return failures;
    }

    /// <summary>
    /// Validates output against knowledge boundaries.
    /// </summary>
    private List<ValidationFailure> ValidateKnowledgeBoundaries(string text, List<string> forbiddenKnowledge)
    {
      var failures = new List<ValidationFailure>();
      var textLower = text.ToLowerInvariant();

      foreach (var forbidden in forbiddenKnowledge)
      {
        var forbiddenLower = forbidden.ToLowerInvariant();
        if (textLower.Contains(forbiddenLower))
        {
          failures.Add(ValidationFailure.KnowledgeBoundary(
            $"NPC revealed forbidden knowledge: '{forbidden}'",
            forbiddenLower
          ));
        }
      }

      return failures;
    }

    /// <summary>
    /// Validates a proposed mutation.
    /// </summary>
    private ValidationFailure? ValidateMutation(ProposedMutation mutation, ValidationContext? context)
    {
      // Check if mutation targets a canonical fact
      if (context?.MemorySystem != null && mutation.Target != null)
      {
        var isCanonical = context.MemorySystem.IsCanonicalFact(mutation.Target);
        if (isCanonical)
        {
          return ValidationFailure.CanonicalMutation(mutation.Target);
        }
      }

      // Additional mutation validation can be added here
      return null;
    }
  }
}
