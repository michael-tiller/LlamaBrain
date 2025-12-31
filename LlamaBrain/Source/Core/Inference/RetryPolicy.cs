using System;
using System.Collections.Generic;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// How to escalate constraints on retry.
  /// </summary>
  public enum ConstraintEscalation
  {
    /// <summary>Keep same constraints, just retry.</summary>
    None,
    /// <summary>Add explicit prohibition about the specific violation.</summary>
    AddSpecificProhibition,
    /// <summary>Convert soft requirements to hard requirements.</summary>
    HardenRequirements,
    /// <summary>Add both specific prohibition and harden requirements.</summary>
    Full
  }

  /// <summary>
  /// Configuration for retry behavior when inference fails validation.
  /// </summary>
  public class RetryPolicy
  {
    /// <summary>
    /// Maximum number of retry attempts (not including the first attempt).
    /// Default: 2 (so 3 total attempts).
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// How to escalate constraints on each retry.
    /// </summary>
    public ConstraintEscalation Escalation { get; set; } = ConstraintEscalation.AddSpecificProhibition;

    /// <summary>
    /// Whether to include the previous response in retry context.
    /// Helps the LLM understand what went wrong.
    /// </summary>
    public bool IncludePreviousResponse { get; set; } = true;

    /// <summary>
    /// Whether to include explicit violation feedback.
    /// </summary>
    public bool IncludeViolationFeedback { get; set; } = true;

    /// <summary>
    /// Delay between retries in milliseconds.
    /// Default: 0 (no delay).
    /// </summary>
    public int RetryDelayMs { get; set; } = 0;

    /// <summary>
    /// Maximum total time for all attempts in milliseconds.
    /// Default: 30000 (30 seconds).
    /// </summary>
    public int MaxTotalTimeMs { get; set; } = 30000;

    /// <summary>
    /// Optional callback when a retry is about to happen.
    /// </summary>
    public Action<int, InferenceResult>? OnRetry { get; set; }

    /// <summary>
    /// Creates a default retry policy.
    /// </summary>
    public static RetryPolicy Default => new RetryPolicy();

    /// <summary>
    /// Creates a policy with no retries.
    /// </summary>
    public static RetryPolicy NoRetry => new RetryPolicy { MaxRetries = 0 };

    /// <summary>
    /// Creates a policy with aggressive retry settings.
    /// </summary>
    public static RetryPolicy Aggressive => new RetryPolicy
    {
      MaxRetries = 3,
      Escalation = ConstraintEscalation.Full,
      IncludePreviousResponse = true,
      IncludeViolationFeedback = true
    };

    /// <summary>
    /// Total maximum attempts (first attempt + retries).
    /// </summary>
    public int MaxAttempts => MaxRetries + 1;

    /// <summary>
    /// Generates additional constraints for a retry based on violations.
    /// </summary>
    /// <param name="violations">The violations from the failed attempt</param>
    /// <param name="attemptNumber">Current attempt number (0-based)</param>
    /// <returns>Additional constraints to add for the retry</returns>
    public ConstraintSet GenerateRetryConstraints(
      IReadOnlyList<ConstraintViolation> violations,
      int attemptNumber)
    {
      var constraints = new ConstraintSet();

      if (Escalation == ConstraintEscalation.None)
      {
        return constraints;
      }

      foreach (var violation in violations)
      {
        if (Escalation == ConstraintEscalation.AddSpecificProhibition ||
            Escalation == ConstraintEscalation.Full)
        {
          // Add specific prohibition based on the violation
          var description = GenerateProhibitionFromViolation(violation);
          var promptInjection = description;
          var id = $"RetryEscalation_{violation.Constraint.Id ?? "Unknown"}_{attemptNumber}";
          
          // Extract patterns from violating text if available
          if (!string.IsNullOrEmpty(violation.ViolatingText))
          {
            constraints.Add(Constraint.Prohibition(id, description, promptInjection, violation.ViolatingText));
          }
          else
          {
            constraints.Add(Constraint.Prohibition(id, description, promptInjection));
          }
        }

        if ((Escalation == ConstraintEscalation.HardenRequirements ||
             Escalation == ConstraintEscalation.Full) &&
            violation.Constraint.Type == ConstraintType.Requirement)
        {
          // Re-add the requirement with stronger emphasis
          // Use a different ID suffix to avoid collision with the prohibition created above
          var strengthened = $"MUST {violation.Constraint.Description ?? "meet this requirement"}";
          var promptInjection = strengthened;
          var id = $"RetryEscalation_{violation.Constraint.Id ?? "Unknown"}_Req_{attemptNumber}";
          constraints.Add(Constraint.Requirement(id, strengthened, promptInjection));
        }
      }

      return constraints;
    }

    /// <summary>
    /// Generates a prohibition description from a violation.
    /// </summary>
    private string GenerateProhibitionFromViolation(ConstraintViolation violation)
    {
      if (!string.IsNullOrEmpty(violation.ViolatingText))
      {
        return $"Do not say or imply: \"{Truncate(violation.ViolatingText, 100)}\"";
      }

      // Generate based on constraint type
      if (violation.Constraint.Type == ConstraintType.Prohibition)
      {
        return $"STRICTLY {violation.Constraint.Description}";
      }
      else
      {
        return $"Do not fail to: {violation.Constraint.Description}";
      }
    }

    /// <summary>
    /// Generates retry feedback text for inclusion in the prompt.
    /// </summary>
    /// <param name="previousResult">The previous failed result</param>
    /// <returns>Feedback text to include in retry prompt</returns>
    public string GenerateRetryFeedback(InferenceResult previousResult)
    {
      var parts = new List<string>();

      parts.Add($"[RETRY ATTEMPT {previousResult.Snapshot.AttemptNumber + 2}]");

      if (IncludeViolationFeedback && previousResult.Violations.Count > 0)
      {
        parts.Add("Your previous response violated the following constraints:");
        foreach (var violation in previousResult.Violations)
        {
          parts.Add($"- {violation.Description}");
        }
      }

      if (IncludePreviousResponse && !string.IsNullOrEmpty(previousResult.Response))
      {
        parts.Add($"Previous response (rejected): \"{Truncate(previousResult.Response, 200)}\"");
      }

      parts.Add("Please provide a new response that satisfies ALL constraints.");

      return string.Join("\n", parts);
    }

    private static string Truncate(string text, int maxLength)
    {
      if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
      return text.Substring(0, maxLength) + "...";
    }
  }
}
