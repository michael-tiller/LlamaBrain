using System;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Context data captured during an NPC interaction for audit recording.
  /// This struct carries all the information needed to create an AuditRecord.
  /// </summary>
  /// <remarks>
  /// AuditCaptureContext is raised via an event after each interaction completes.
  /// The audit recorder subscribes to this event and creates AuditRecords.
  ///
  /// All fields should be populated at capture time - no lazy evaluation.
  /// </remarks>
  public struct AuditCaptureContext
  {
    #region Identification

    /// <summary>
    /// Identifier of the NPC (persona) involved in this interaction.
    /// </summary>
    public string NpcId { get; set; }

    /// <summary>
    /// Sequential interaction count for this NPC.
    /// Used as the deterministic seed for replay.
    /// </summary>
    public int InteractionCount { get; set; }

    #endregion

    #region Determinism Keys

    /// <summary>
    /// The seed value used for LLM generation.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// UTC ticks from the StateSnapshot.
    /// Critical for deterministic recency calculations.
    /// </summary>
    public long SnapshotTimeUtcTicks { get; set; }

    #endregion

    #region Input

    /// <summary>
    /// The player's input message that triggered this interaction.
    /// </summary>
    public string PlayerInput { get; set; }

    /// <summary>
    /// Numeric value of the TriggerReason enum.
    /// </summary>
    public int TriggerReason { get; set; }

    /// <summary>
    /// Identifier of the trigger zone or event.
    /// </summary>
    public string TriggerId { get; set; }

    /// <summary>
    /// Name of the scene where this interaction occurred.
    /// </summary>
    public string SceneName { get; set; }

    #endregion

    #region State Hashes

    /// <summary>
    /// SHA256 hash of the memory state before this interaction.
    /// </summary>
    public string MemoryHashBefore { get; set; }

    /// <summary>
    /// SHA256 hash of the assembled prompt.
    /// </summary>
    public string PromptHash { get; set; }

    /// <summary>
    /// SHA256 hash of the serialized constraints.
    /// </summary>
    public string ConstraintsHash { get; set; }

    /// <summary>
    /// JSON-serialized constraints used for this interaction.
    /// </summary>
    public string ConstraintsSerialized { get; set; }

    #endregion

    #region Output

    /// <summary>
    /// Raw LLM response before parsing.
    /// </summary>
    public string RawOutput { get; set; }

    /// <summary>
    /// Parsed dialogue text from the LLM response.
    /// </summary>
    public string DialogueText { get; set; }

    #endregion

    #region Outcome

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
    /// Whether a fallback response was used.
    /// </summary>
    public bool FallbackUsed { get; set; }

    /// <summary>
    /// Reason why fallback was used, if applicable.
    /// </summary>
    public string FallbackReason { get; set; }

    #endregion

    #region Performance Metrics

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

    /// <summary>
    /// Creates an AuditRecord from this capture context.
    /// </summary>
    /// <returns>A new AuditRecord populated with the context data.</returns>
    public AuditRecord ToAuditRecord()
    {
      var builder = new AuditRecordBuilder()
        .WithNpcId(NpcId)
        .WithInteractionCount(InteractionCount)
        .WithSeed(Seed)
        .WithSnapshotTimeUtcTicks(SnapshotTimeUtcTicks)
        .WithPlayerInput(PlayerInput)
        .WithTriggerInfo(TriggerReason, TriggerId, SceneName)
        .WithStateHashes(MemoryHashBefore, PromptHash, ConstraintsHash)
        .WithConstraints(ConstraintsSerialized)
        .WithOutput(RawOutput, DialogueText)
        .WithValidationOutcome(ValidationPassed, ViolationCount, MutationsApplied)
        .WithMetrics(TtftMs, TotalTimeMs, PromptTokenCount, GeneratedTokenCount);

      if (FallbackUsed)
      {
        builder.WithFallback(FallbackReason);
      }

      return builder.Build();
    }
  }
}
