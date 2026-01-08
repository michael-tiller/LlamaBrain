using System;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Represents a single audit record capturing the state and outcome of an NPC interaction.
  /// This record contains all information needed to reproduce a specific dialogue turn.
  /// </summary>
  /// <remarks>
  /// AuditRecord is designed for serialization and should remain a simple DTO.
  /// Use <see cref="AuditRecordBuilder"/> for constructing records with validation.
  ///
  /// All string properties are initialized to empty strings to avoid null reference issues
  /// during serialization and comparison.
  /// </remarks>
  [Serializable]
  public sealed class AuditRecord
  {
    /// <summary>
    /// Current version of the AuditRecord schema.
    /// Increment when making breaking changes to the record structure.
    /// </summary>
    public const int CurrentVersion = 1;

    #region Identification

    /// <summary>
    /// Unique identifier for this audit record.
    /// Generated at capture time, typically a GUID or hash-based ID.
    /// </summary>
    public string RecordId { get; set; } = "";

    /// <summary>
    /// Identifier of the NPC (persona) involved in this interaction.
    /// </summary>
    public string NpcId { get; set; } = "";

    /// <summary>
    /// Sequential interaction count for this NPC.
    /// Used as the deterministic seed for replay.
    /// </summary>
    public int InteractionCount { get; set; }

    /// <summary>
    /// Position of this record in the ring buffer (0 = oldest).
    /// Updated when exporting the buffer.
    /// </summary>
    public int BufferIndex { get; set; }

    #endregion

    #region Timing

    /// <summary>
    /// UTC ticks when this record was captured.
    /// Used for ordering records chronologically.
    /// </summary>
    public long CapturedAtUtcTicks { get; set; }

    /// <summary>
    /// UTC ticks from the StateSnapshot.
    /// Critical for deterministic recency calculations.
    /// </summary>
    public long SnapshotTimeUtcTicks { get; set; }

    #endregion

    #region Determinism Keys

    /// <summary>
    /// The seed value used for LLM generation.
    /// Same seed + same prompt = same output (assuming same model).
    /// </summary>
    public int Seed { get; set; }

    #endregion

    #region Input Context

    /// <summary>
    /// The player's input message that triggered this interaction.
    /// </summary>
    public string PlayerInput { get; set; } = "";

    /// <summary>
    /// Numeric value of the TriggerReason enum.
    /// Stored as int for serialization stability.
    /// </summary>
    public int TriggerReason { get; set; }

    /// <summary>
    /// Identifier of the trigger zone or event that initiated this interaction.
    /// </summary>
    public string TriggerId { get; set; } = "";

    /// <summary>
    /// Name of the Unity scene where this interaction occurred.
    /// </summary>
    public string SceneName { get; set; } = "";

    #endregion

    #region State Hashes (Drift Detection)

    /// <summary>
    /// SHA256 hash of the memory state before this interaction.
    /// Used to detect memory drift during replay.
    /// </summary>
    public string MemoryHashBefore { get; set; } = "";

    /// <summary>
    /// SHA256 hash of the assembled prompt.
    /// Used to detect prompt assembly drift during replay.
    /// </summary>
    public string PromptHash { get; set; } = "";

    /// <summary>
    /// SHA256 hash of the serialized constraints.
    /// Used to detect constraint configuration drift.
    /// </summary>
    public string ConstraintsHash { get; set; } = "";

    #endregion

    #region Full State (For Replay)

    /// <summary>
    /// JSON-serialized constraints used for this interaction.
    /// Stored fully to enable replay without constraint reconstruction.
    /// </summary>
    public string ConstraintsSerialized { get; set; } = "";

    #endregion

    #region Output

    /// <summary>
    /// Raw LLM response before parsing.
    /// </summary>
    public string RawOutput { get; set; } = "";

    /// <summary>
    /// SHA256 hash of the raw output.
    /// Primary mechanism for drift detection during replay.
    /// </summary>
    public string OutputHash { get; set; } = "";

    /// <summary>
    /// Parsed dialogue text from the LLM response.
    /// </summary>
    public string DialogueText { get; set; } = "";

    #endregion

    #region Outcome Metadata

    /// <summary>
    /// Whether the output passed all validation gates.
    /// </summary>
    public bool ValidationPassed { get; set; }

    /// <summary>
    /// Number of validation violations detected.
    /// </summary>
    public int ViolationCount { get; set; }

    /// <summary>
    /// Number of memory mutations applied from this interaction.
    /// </summary>
    public int MutationsApplied { get; set; }

    /// <summary>
    /// Whether a fallback response was used instead of the LLM output.
    /// </summary>
    public bool FallbackUsed { get; set; }

    /// <summary>
    /// Reason why fallback was used, if applicable.
    /// </summary>
    public string FallbackReason { get; set; } = "";

    #endregion

    #region Performance Metrics (Optional)

    /// <summary>
    /// Time to first token in milliseconds.
    /// </summary>
    public long TtftMs { get; set; }

    /// <summary>
    /// Total generation time in milliseconds.
    /// </summary>
    public long TotalTimeMs { get; set; }

    /// <summary>
    /// Number of tokens in the prompt.
    /// </summary>
    public int PromptTokenCount { get; set; }

    /// <summary>
    /// Number of tokens generated.
    /// </summary>
    public int GeneratedTokenCount { get; set; }

    #endregion
  }
}
