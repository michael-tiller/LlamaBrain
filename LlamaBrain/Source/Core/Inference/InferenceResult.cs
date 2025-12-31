using System;
using System.Collections.Generic;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// The outcome of a constraint validation check.
  /// </summary>
  public enum ValidationOutcome
  {
    /// <summary>All constraints were satisfied.</summary>
    Valid,
    /// <summary>A prohibition constraint was violated.</summary>
    ProhibitionViolated,
    /// <summary>A requirement constraint was not met.</summary>
    RequirementNotMet,
    /// <summary>Response was empty or malformed.</summary>
    InvalidFormat
  }

  /// <summary>
  /// Details about a constraint violation.
  /// </summary>
  public class ConstraintViolation
  {
    /// <summary>
    /// The constraint that was violated.
    /// </summary>
    public Constraint Constraint { get; }

    /// <summary>
    /// Description of how the constraint was violated.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The portion of the response that caused the violation.
    /// </summary>
    public string? ViolatingText { get; }

    /// <summary>
    /// Creates a new constraint violation.
    /// </summary>
    /// <param name="constraint">The constraint that was violated</param>
    /// <param name="description">Description of the violation</param>
    /// <param name="violatingText">The text that caused the violation (optional)</param>
    public ConstraintViolation(Constraint constraint, string description, string? violatingText = null)
    {
      Constraint = constraint;
      Description = description;
      ViolatingText = violatingText;
    }

    /// <summary>
    /// Returns a string representation of the violation.
    /// </summary>
    /// <returns>A string representation of the constraint violation</returns>
    public override string ToString()
    {
      return $"[{Constraint.Type}] {Description}";
    }
  }

  /// <summary>
  /// The result of an inference attempt, including validation status.
  /// </summary>
  public class InferenceResult
  {
    /// <summary>
    /// Whether the inference was successful and passed validation.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// The LLM's response text.
    /// </summary>
    public string Response { get; }

    /// <summary>
    /// The validation outcome.
    /// </summary>
    public ValidationOutcome Outcome { get; }

    /// <summary>
    /// List of constraint violations (empty if successful).
    /// </summary>
    public IReadOnlyList<ConstraintViolation> Violations { get; }

    /// <summary>
    /// The snapshot used for this inference.
    /// </summary>
    public StateSnapshot Snapshot { get; }

    /// <summary>
    /// Time taken for this inference attempt in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; }

    /// <summary>
    /// Any error message if the inference failed at the API level.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Token usage for this inference.
    /// </summary>
    public TokenUsage? TokenUsage { get; }

    private InferenceResult(
      bool success,
      string response,
      ValidationOutcome outcome,
      IReadOnlyList<ConstraintViolation> violations,
      StateSnapshot snapshot,
      long elapsedMs,
      string? errorMessage,
      TokenUsage? tokenUsage)
    {
      Success = success;
      Response = response;
      Outcome = outcome;
      Violations = violations;
      Snapshot = snapshot;
      ElapsedMilliseconds = elapsedMs;
      ErrorMessage = errorMessage;
      TokenUsage = tokenUsage;
    }

    /// <summary>
    /// Creates a successful inference result.
    /// </summary>
    /// <param name="response">The LLM response</param>
    /// <param name="snapshot">The snapshot used</param>
    /// <param name="elapsedMs">Time taken in milliseconds</param>
    /// <param name="tokenUsage">Optional token usage info</param>
    /// <returns>A successful inference result</returns>
    public static InferenceResult Succeeded(
      string response,
      StateSnapshot snapshot,
      long elapsedMs,
      TokenUsage? tokenUsage = null)
    {
      return new InferenceResult(
        success: true,
        response: response,
        outcome: ValidationOutcome.Valid,
        violations: Array.Empty<ConstraintViolation>(),
        snapshot: snapshot,
        elapsedMs: elapsedMs,
        errorMessage: null,
        tokenUsage: tokenUsage
      );
    }

    /// <summary>
    /// Creates a failed inference result due to constraint violations.
    /// </summary>
    /// <param name="response">The LLM response that failed validation</param>
    /// <param name="outcome">The validation outcome</param>
    /// <param name="violations">List of violations</param>
    /// <param name="snapshot">The snapshot used</param>
    /// <param name="elapsedMs">Time taken in milliseconds</param>
    /// <param name="tokenUsage">Optional token usage info</param>
    /// <returns>A failed inference result due to validation failures</returns>
    public static InferenceResult FailedValidation(
      string response,
      ValidationOutcome outcome,
      IReadOnlyList<ConstraintViolation> violations,
      StateSnapshot snapshot,
      long elapsedMs,
      TokenUsage? tokenUsage = null)
    {
      return new InferenceResult(
        success: false,
        response: response,
        outcome: outcome,
        violations: violations,
        snapshot: snapshot,
        elapsedMs: elapsedMs,
        errorMessage: null,
        tokenUsage: tokenUsage
      );
    }

    /// <summary>
    /// Creates a failed inference result due to API error.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="snapshot">The snapshot used</param>
    /// <param name="elapsedMs">Time taken in milliseconds</param>
    /// <returns>A failed inference result due to an API error</returns>
    public static InferenceResult FailedError(
      string errorMessage,
      StateSnapshot snapshot,
      long elapsedMs)
    {
      return new InferenceResult(
        success: false,
        response: "",
        outcome: ValidationOutcome.InvalidFormat,
        violations: Array.Empty<ConstraintViolation>(),
        snapshot: snapshot,
        elapsedMs: elapsedMs,
        errorMessage: errorMessage,
        tokenUsage: null
      );
    }

    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    /// <returns>A string representation of the inference result</returns>
    public override string ToString()
    {
      if (Success)
      {
        return $"InferenceResult[Success] Attempt {Snapshot.AttemptNumber + 1}, {ElapsedMilliseconds}ms";
      }
      else if (ErrorMessage != null)
      {
        return $"InferenceResult[Error] {ErrorMessage}";
      }
      else
      {
        return $"InferenceResult[{Outcome}] {Violations.Count} violations, Attempt {Snapshot.AttemptNumber + 1}";
      }
    }
  }

  /// <summary>
  /// Token usage information from an inference.
  /// </summary>
  public class TokenUsage
  {
    /// <summary>
    /// Number of input/prompt tokens.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Number of output/completion tokens.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
  }

  /// <summary>
  /// Aggregated result of an inference with retries.
  /// </summary>
  public class InferenceResultWithRetries
  {
    /// <summary>
    /// The final result (either successful or last failed attempt).
    /// </summary>
    public InferenceResult FinalResult { get; }

    /// <summary>
    /// All attempts made (including the final one).
    /// </summary>
    public IReadOnlyList<InferenceResult> AllAttempts { get; }

    /// <summary>
    /// Total time taken across all attempts in milliseconds.
    /// </summary>
    public long TotalElapsedMilliseconds { get; }

    /// <summary>
    /// Whether the inference eventually succeeded.
    /// </summary>
    public bool Success => FinalResult.Success;

    /// <summary>
    /// Number of attempts made.
    /// </summary>
    public int AttemptCount => AllAttempts.Count;

    /// <summary>
    /// Creates a new inference result with retries.
    /// </summary>
    /// <param name="finalResult">The final result</param>
    /// <param name="allAttempts">All attempts made</param>
    /// <param name="totalElapsedMs">Total time in milliseconds</param>
    public InferenceResultWithRetries(
      InferenceResult finalResult,
      IReadOnlyList<InferenceResult> allAttempts,
      long totalElapsedMs)
    {
      FinalResult = finalResult;
      AllAttempts = allAttempts;
      TotalElapsedMilliseconds = totalElapsedMs;
    }

    /// <summary>
    /// Gets the total token usage across all attempts.
    /// </summary>
    /// <returns>Total token usage across all attempts, or null if no token usage data is available</returns>
    public TokenUsage? GetTotalTokenUsage()
    {
      var hasUsage = false;
      var total = new TokenUsage();

      foreach (var attempt in AllAttempts)
      {
        if (attempt.TokenUsage != null)
        {
          hasUsage = true;
          total.PromptTokens += attempt.TokenUsage.PromptTokens;
          total.CompletionTokens += attempt.TokenUsage.CompletionTokens;
        }
      }

      return hasUsage ? total : null;
    }

    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    /// <returns>A string representation of the inference result with retries</returns>
    public override string ToString()
    {
      var status = Success ? "Success" : "Failed";
      return $"InferenceWithRetries[{status}] {AttemptCount} attempts, {TotalElapsedMilliseconds}ms total";
    }
  }
}
