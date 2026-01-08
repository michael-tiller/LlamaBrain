using System;
using System.Collections.Generic;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Type of drift detected during replay.
  /// </summary>
  public enum DriftType
  {
    /// <summary>
    /// No drift detected. Replay matches original exactly.
    /// </summary>
    None = 0,

    /// <summary>
    /// Prompt assembly produced different hash than original.
    /// May indicate governance plane changes or template modifications.
    /// </summary>
    Prompt = 1,

    /// <summary>
    /// Memory state hash differs from original.
    /// May indicate different starting memory or memory mutation changes.
    /// </summary>
    Memory = 2,

    /// <summary>
    /// LLM output differs from original.
    /// Expected if model changed, or may indicate non-determinism.
    /// </summary>
    Output = 3,

    /// <summary>
    /// Validation/parsing outcome differs from original.
    /// May indicate validation rule changes.
    /// </summary>
    Validation = 4,

    /// <summary>
    /// Constraints hash differs from original.
    /// May indicate constraint configuration changes.
    /// </summary>
    Constraints = 5
  }

  /// <summary>
  /// Result of replaying a single audit record.
  /// </summary>
  [Serializable]
  public sealed class RecordReplayResult
  {
    /// <summary>
    /// The original audit record that was replayed.
    /// </summary>
    public AuditRecord OriginalRecord { get; set; } = new AuditRecord();

    /// <summary>
    /// The audit record produced during replay.
    /// </summary>
    public AuditRecord? ReplayedRecord { get; set; }

    /// <summary>
    /// Whether the replay succeeded without errors.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if replay failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Type of drift detected (if any).
    /// </summary>
    public DriftType DriftType { get; set; } = DriftType.None;

    /// <summary>
    /// Detailed description of the drift.
    /// </summary>
    public string? DriftDescription { get; set; }

    /// <summary>
    /// Whether the outputs match exactly (same OutputHash).
    /// </summary>
    public bool OutputMatches { get; set; }

    /// <summary>
    /// Whether the prompt hashes match.
    /// </summary>
    public bool PromptMatches { get; set; }

    /// <summary>
    /// Whether the memory hashes match.
    /// </summary>
    public bool MemoryMatches { get; set; }

    /// <summary>
    /// Whether the validation outcomes match.
    /// </summary>
    public bool ValidationMatches { get; set; }

    /// <summary>
    /// Creates a successful replay result with no drift.
    /// </summary>
    public static RecordReplayResult Succeeded(AuditRecord original, AuditRecord replayed)
    {
      return new RecordReplayResult
      {
        OriginalRecord = original,
        ReplayedRecord = replayed,
        Success = true,
        DriftType = DriftType.None,
        OutputMatches = true,
        PromptMatches = true,
        MemoryMatches = true,
        ValidationMatches = true
      };
    }

    /// <summary>
    /// Creates a failed replay result.
    /// </summary>
    public static RecordReplayResult Failed(AuditRecord original, string errorMessage)
    {
      return new RecordReplayResult
      {
        OriginalRecord = original,
        ReplayedRecord = null,
        Success = false,
        ErrorMessage = errorMessage
      };
    }
  }

  /// <summary>
  /// Result of replaying an entire debug package.
  /// </summary>
  [Serializable]
  public sealed class ReplayResult
  {
    /// <summary>
    /// The debug package that was replayed.
    /// </summary>
    public DebugPackage? Package { get; set; }

    /// <summary>
    /// Whether replay completed successfully (all records replayed).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if replay failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Results for each individual record.
    /// </summary>
    public List<RecordReplayResult> RecordResults { get; set; } = new List<RecordReplayResult>();

    /// <summary>
    /// Number of records with no drift.
    /// </summary>
    public int ExactMatches { get; set; }

    /// <summary>
    /// Number of records with prompt drift.
    /// </summary>
    public int PromptDrifts { get; set; }

    /// <summary>
    /// Number of records with output drift.
    /// </summary>
    public int OutputDrifts { get; set; }

    /// <summary>
    /// Number of records with memory drift.
    /// </summary>
    public int MemoryDrifts { get; set; }

    /// <summary>
    /// Number of records that failed to replay.
    /// </summary>
    public int Failures { get; set; }

    /// <summary>
    /// Total time taken to replay all records in milliseconds.
    /// </summary>
    public long ReplayDurationMs { get; set; }

    /// <summary>
    /// Whether all records matched exactly (no drift).
    /// </summary>
    public bool AllMatched => ExactMatches == RecordResults.Count && Failures == 0;

    /// <summary>
    /// Whether any drift was detected.
    /// </summary>
    public bool HasDrift => PromptDrifts > 0 || OutputDrifts > 0 || MemoryDrifts > 0;

    /// <summary>
    /// Model fingerprint validation result.
    /// </summary>
    public ModelFingerprintValidationResult? ModelValidation { get; set; }

    /// <summary>
    /// Updates statistics from the record results.
    /// </summary>
    public void UpdateStatistics()
    {
      ExactMatches = 0;
      PromptDrifts = 0;
      OutputDrifts = 0;
      MemoryDrifts = 0;
      Failures = 0;

      foreach (var result in RecordResults)
      {
        if (!result.Success)
        {
          Failures++;
          continue;
        }

        switch (result.DriftType)
        {
          case DriftType.None:
            ExactMatches++;
            break;
          case DriftType.Prompt:
            PromptDrifts++;
            break;
          case DriftType.Output:
            OutputDrifts++;
            break;
          case DriftType.Memory:
            MemoryDrifts++;
            break;
          default:
            // Other drift types counted as output drift for now
            OutputDrifts++;
            break;
        }
      }
    }

    /// <summary>
    /// Creates a successful replay result.
    /// </summary>
    public static ReplayResult Succeeded(DebugPackage package, List<RecordReplayResult> results)
    {
      var result = new ReplayResult
      {
        Package = package,
        Success = true,
        RecordResults = results
      };
      result.UpdateStatistics();
      return result;
    }

    /// <summary>
    /// Creates a failed replay result.
    /// </summary>
    public static ReplayResult Failed(DebugPackage? package, string errorMessage)
    {
      return new ReplayResult
      {
        Package = package,
        Success = false,
        ErrorMessage = errorMessage
      };
    }
  }
}
