using System;
using System.Text;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Detects drift between original audit records and replayed results.
  /// Compares hashes to identify where divergence occurred.
  /// </summary>
  public sealed class DriftDetector
  {
    /// <summary>
    /// Compares an original audit record with a replayed record to detect drift.
    /// </summary>
    /// <param name="original">The original audit record from the debug package.</param>
    /// <param name="replayed">The record produced during replay.</param>
    /// <returns>Record replay result with drift information.</returns>
    public RecordReplayResult Compare(AuditRecord original, AuditRecord replayed)
    {
      if (original == null)
        throw new ArgumentNullException(nameof(original));

      if (replayed == null)
        throw new ArgumentNullException(nameof(replayed));

      var result = new RecordReplayResult
      {
        OriginalRecord = original,
        ReplayedRecord = replayed
      };

      // Compare each hash in order of replay sequence
      result.MemoryMatches = CompareHashes(original.MemoryHashBefore, replayed.MemoryHashBefore);
      result.PromptMatches = CompareHashes(original.PromptHash, replayed.PromptHash);
      result.OutputMatches = CompareHashes(original.OutputHash, replayed.OutputHash);
      result.ValidationMatches = original.ValidationPassed == replayed.ValidationPassed;

      // Determine drift type based on first mismatch in sequence
      result.DriftType = DetermineDriftType(result);
      result.DriftDescription = GenerateDriftDescription(original, replayed, result);

      // Success means the comparison completed without errors.
      // Drift detection is separate - check DriftType or IsExactMatch for that.
      result.Success = true;

      return result;
    }

    /// <summary>
    /// Compares two hash values, treating empty/null as equal to each other.
    /// </summary>
    private static bool CompareHashes(string? hash1, string? hash2)
    {
      var h1 = string.IsNullOrEmpty(hash1) ? "" : hash1;
      var h2 = string.IsNullOrEmpty(hash2) ? "" : hash2;
      return h1 == h2;
    }

    /// <summary>
    /// Determines the drift type based on comparison results.
    /// Returns the first drift type in the replay sequence.
    /// </summary>
    private static DriftType DetermineDriftType(RecordReplayResult result)
    {
      // Check in order of replay sequence
      if (!result.MemoryMatches)
        return DriftType.Memory;

      if (!result.PromptMatches)
        return DriftType.Prompt;

      if (!result.OutputMatches)
        return DriftType.Output;

      if (!result.ValidationMatches)
        return DriftType.Validation;

      return DriftType.None;
    }

    /// <summary>
    /// Generates a human-readable description of the drift.
    /// </summary>
    private static string? GenerateDriftDescription(
      AuditRecord original,
      AuditRecord replayed,
      RecordReplayResult result)
    {
      if (result.DriftType == DriftType.None)
        return null;

      var sb = new StringBuilder();

      switch (result.DriftType)
      {
        case DriftType.Memory:
          sb.Append("Memory state differs from original. ");
          if (!string.IsNullOrEmpty(original.MemoryHashBefore) && !string.IsNullOrEmpty(replayed.MemoryHashBefore))
          {
            sb.Append($"Original: {Truncate(original.MemoryHashBefore, 8)}, ");
            sb.Append($"Replayed: {Truncate(replayed.MemoryHashBefore, 8)}");
          }
          break;

        case DriftType.Prompt:
          sb.Append("Prompt assembly differs from original. ");
          if (!string.IsNullOrEmpty(original.PromptHash) && !string.IsNullOrEmpty(replayed.PromptHash))
          {
            sb.Append($"Original: {Truncate(original.PromptHash, 8)}, ");
            sb.Append($"Replayed: {Truncate(replayed.PromptHash, 8)}");
          }
          sb.Append(" May indicate template or governance changes.");
          break;

        case DriftType.Output:
          sb.Append("LLM output differs from original. ");
          if (!string.IsNullOrEmpty(original.OutputHash) && !string.IsNullOrEmpty(replayed.OutputHash))
          {
            sb.Append($"Original: {Truncate(original.OutputHash, 8)}, ");
            sb.Append($"Replayed: {Truncate(replayed.OutputHash, 8)}");
          }
          sb.Append(" May indicate model change or non-determinism.");
          break;

        case DriftType.Validation:
          sb.Append($"Validation outcome differs. ");
          sb.Append($"Original: {(original.ValidationPassed ? "passed" : "failed")}, ");
          sb.Append($"Replayed: {(replayed.ValidationPassed ? "passed" : "failed")}");
          break;

        case DriftType.Constraints:
          sb.Append("Constraints configuration differs from original.");
          break;
      }

      return sb.ToString();
    }

    /// <summary>
    /// Truncates a string to the specified length.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
      if (string.IsNullOrEmpty(value))
        return "";

      return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    /// <summary>
    /// Compares constraints hashes.
    /// </summary>
    /// <param name="originalHash">Hash from original record.</param>
    /// <param name="replayedHash">Hash from replayed record.</param>
    /// <returns>True if constraints match.</returns>
    public bool CompareConstraints(string? originalHash, string? replayedHash)
    {
      return CompareHashes(originalHash, replayedHash);
    }

    /// <summary>
    /// Creates a quick summary of drift across multiple records.
    /// </summary>
    /// <param name="results">The replay results to summarize.</param>
    /// <returns>Summary string.</returns>
    public string CreateDriftSummary(ReplayResult results)
    {
      if (results == null)
        throw new ArgumentNullException(nameof(results));

      var sb = new StringBuilder();

      if (results.AllMatched)
      {
        sb.Append($"All {results.RecordResults.Count} records matched exactly.");
        return sb.ToString();
      }

      sb.AppendLine($"Replay summary for {results.RecordResults.Count} records:");
      sb.AppendLine($"  Exact matches: {results.ExactMatches}");

      if (results.PromptDrifts > 0)
        sb.AppendLine($"  Prompt drifts: {results.PromptDrifts}");

      if (results.MemoryDrifts > 0)
        sb.AppendLine($"  Memory drifts: {results.MemoryDrifts}");

      if (results.OutputDrifts > 0)
        sb.AppendLine($"  Output drifts: {results.OutputDrifts}");

      if (results.Failures > 0)
        sb.AppendLine($"  Failures: {results.Failures}");

      return sb.ToString().TrimEnd();
    }
  }
}
