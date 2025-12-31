using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// Validates LLM responses against constraints.
  /// </summary>
  public class ResponseValidator
  {
    /// <summary>
    /// Optional logging callback.
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// Validates a response against the constraints in a snapshot.
    /// </summary>
    /// <param name="response">The LLM response to validate</param>
    /// <param name="snapshot">The snapshot containing constraints</param>
    /// <returns>Validation result with any violations</returns>
    public ValidationResult Validate(string response, StateSnapshot snapshot)
    {
      return Validate(response, snapshot.Constraints);
    }

    /// <summary>
    /// Validates a response against a constraint set.
    /// </summary>
    /// <param name="response">The LLM response to validate</param>
    /// <param name="constraints">The constraints to validate against</param>
    /// <returns>Validation result with any violations</returns>
    public ValidationResult Validate(string response, ConstraintSet constraints)
    {
      var violations = new List<ConstraintViolation>();

      // Check for empty/null response
      if (string.IsNullOrWhiteSpace(response))
      {
        return new ValidationResult(
          ValidationOutcome.InvalidFormat,
          violations,
          "Response is empty or whitespace"
        );
      }

      var responseLower = response.ToLowerInvariant();

      // Check prohibitions
      foreach (var prohibition in constraints.Prohibitions)
      {
        var violation = CheckProhibition(response, responseLower, prohibition);
        if (violation != null)
        {
          violations.Add(violation);
          OnLog?.Invoke($"[Validator] Prohibition violated: {prohibition.Description}");
        }
      }

      // Check requirements
      foreach (var requirement in constraints.Requirements)
      {
        var violation = CheckRequirement(response, responseLower, requirement);
        if (violation != null)
        {
          violations.Add(violation);
          OnLog?.Invoke($"[Validator] Requirement not met: {requirement.Description}");
        }
      }

      // Determine outcome
      if (violations.Count == 0)
      {
        return new ValidationResult(ValidationOutcome.Valid, violations);
      }

      // Check if any prohibition was violated
      var hasProhibitionViolation = violations.Exists(v => v.Constraint.Type == ConstraintType.Prohibition);
      var outcome = hasProhibitionViolation
        ? ValidationOutcome.ProhibitionViolated
        : ValidationOutcome.RequirementNotMet;

      return new ValidationResult(outcome, violations);
    }

    /// <summary>
    /// Checks if a prohibition constraint is violated.
    /// </summary>
    private ConstraintViolation? CheckProhibition(string response, string responseLower, Constraint prohibition)
    {
      // Use explicit validation patterns if provided, otherwise extract from description
      var patterns = prohibition.ValidationPatterns != null && prohibition.ValidationPatterns.Count > 0
        ? prohibition.ValidationPatterns
        : ExtractPatterns(prohibition.Description ?? "");

      foreach (var pattern in patterns)
      {
        // Try regex match first
        if (pattern.StartsWith("/") && pattern.EndsWith("/"))
        {
          var regexPattern = pattern.Substring(1, pattern.Length - 2);
          try
          {
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            var match = regex.Match(response);
            if (match.Success)
            {
              return new ConstraintViolation(
                prohibition,
                $"Response contains prohibited pattern: {prohibition.Description}",
                match.Value
              );
            }
          }
          catch
          {
            // Invalid regex, fall through to keyword check
          }
        }

        // Check for keyword/phrase presence
        var patternLower = pattern.ToLowerInvariant();
        if (responseLower.Contains(patternLower))
        {
          var index = responseLower.IndexOf(patternLower);
          var violatingText = response.Substring(index, Math.Min(pattern.Length + 20, response.Length - index));
          return new ConstraintViolation(
            prohibition,
            $"Response contains prohibited content: {prohibition.Description}",
            violatingText
          );
        }
      }

      return null;
    }

    /// <summary>
    /// Checks if a requirement constraint is met.
    /// </summary>
    private ConstraintViolation? CheckRequirement(string response, string responseLower, Constraint requirement)
    {
      // Use explicit validation patterns if provided, otherwise extract from description
      var patterns = requirement.ValidationPatterns != null && requirement.ValidationPatterns.Count > 0
        ? requirement.ValidationPatterns
        : ExtractPatterns(requirement.Description ?? "");

      // For requirements, we need at least one pattern to match
      if (patterns.Count == 0)
      {
        // No specific patterns - requirement is descriptive only
        // These can only be checked semantically (future enhancement)
        return null;
      }

      foreach (var pattern in patterns)
      {
        var patternLower = pattern.ToLowerInvariant();

        // Try regex match
        if (pattern.StartsWith("/") && pattern.EndsWith("/"))
        {
          var regexPattern = pattern.Substring(1, pattern.Length - 2);
          try
          {
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            if (regex.IsMatch(response))
            {
              return null; // Requirement met
            }
          }
          catch
          {
            // Invalid regex
          }
        }
        else if (responseLower.Contains(patternLower))
        {
          return null; // Requirement met
        }
      }

      return new ConstraintViolation(
        requirement,
        $"Response does not meet requirement: {requirement.Description}"
      );
    }

    /// <summary>
    /// Extracts patterns from a constraint description.
    /// Looks for quoted text and explicit patterns.
    /// </summary>
    private List<string> ExtractPatterns(string description)
    {
      var patterns = new List<string>();

      // Extract quoted strings
      var quoteRegex = new Regex("\"([^\"]+)\"|'([^']+)'");
      var matches = quoteRegex.Matches(description);
      foreach (Match match in matches)
      {
        var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        if (!string.IsNullOrWhiteSpace(value))
        {
          patterns.Add(value);
        }
      }

      // Extract words after "about" or "mention" or "say"
      var keywordRegex = new Regex(@"(?:about|mention|say|discuss|reveal|tell)\s+(\w+)", RegexOptions.IgnoreCase);
      var keywordMatches = keywordRegex.Matches(description);
      foreach (Match match in keywordMatches)
      {
        var keyword = match.Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > 2)
        {
          patterns.Add(keyword);
        }
      }

      return patterns;
    }
  }

  /// <summary>
  /// Result of response validation.
  /// </summary>
  public class ValidationResult
  {
    /// <summary>
    /// The validation outcome.
    /// </summary>
    public ValidationOutcome Outcome { get; }

    /// <summary>
    /// List of violations found (empty if valid).
    /// </summary>
    public IReadOnlyList<ConstraintViolation> Violations { get; }

    /// <summary>
    /// Whether the response passed validation.
    /// </summary>
    public bool IsValid => Outcome == ValidationOutcome.Valid;

    /// <summary>
    /// Optional error message for format issues.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a new validation result.
    /// </summary>
    /// <param name="outcome">The validation outcome</param>
    /// <param name="violations">List of violations</param>
    /// <param name="errorMessage">Optional error message</param>
    public ValidationResult(
      ValidationOutcome outcome,
      IReadOnlyList<ConstraintViolation> violations,
      string? errorMessage = null)
    {
      Outcome = outcome;
      Violations = violations;
      ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    /// <returns>A string representation of the validation result</returns>
    public override string ToString()
    {
      if (IsValid)
      {
        return "ValidationResult[Valid]";
      }
      return $"ValidationResult[{Outcome}] {Violations.Count} violations";
    }
  }
}
