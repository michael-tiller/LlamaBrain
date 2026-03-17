using System;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Fluent builder for constructing <see cref="AuditRecord"/> instances.
  /// Provides convenient methods for setting properties and automatically
  /// computes hashes and timestamps.
  /// </summary>
  /// <remarks>
  /// Usage:
  /// <code>
  /// var record = new AuditRecordBuilder()
  ///   .WithNpcId("npc-1")
  ///   .WithInteractionCount(42)
  ///   .WithSeed(12345)
  ///   .WithPlayerInput("Hello!")
  ///   .WithOutput("Response text", "Parsed dialogue")
  ///   .Build();
  /// </code>
  /// </remarks>
  public sealed class AuditRecordBuilder
  {
    private string _npcId = "";
    private int _interactionCount;
    private int _seed;
    private long _snapshotTimeUtcTicks;
    private string _playerInput = "";
    private int _triggerReason;
    private string _triggerId = "";
    private string _sceneName = "";
    private string _memoryHashBefore = "";
    private string _promptHash = "";
    private string _constraintsHash = "";
    private string _constraintsSerialized = "";
    private string _rawOutput = "";
    private string _dialogueText = "";
    private bool _validationPassed;
    private int _violationCount;
    private int _mutationsApplied;
    private bool _fallbackUsed;
    private string _fallbackReason = "";
    private long _ttftMs;
    private long _totalTimeMs;
    private int _promptTokenCount;
    private int _generatedTokenCount;

    /// <summary>
    /// Sets the NPC identifier.
    /// </summary>
    /// <param name="npcId">The NPC/persona identifier.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithNpcId(string? npcId)
    {
      _npcId = npcId ?? "";
      return this;
    }

    /// <summary>
    /// Sets the interaction count (used as deterministic seed).
    /// </summary>
    /// <param name="count">The interaction count.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithInteractionCount(int count)
    {
      _interactionCount = count;
      return this;
    }

    /// <summary>
    /// Sets the seed value used for LLM generation.
    /// </summary>
    /// <param name="seed">The seed value.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithSeed(int seed)
    {
      _seed = seed;
      return this;
    }

    /// <summary>
    /// Sets the snapshot time UTC ticks.
    /// </summary>
    /// <param name="ticks">UTC ticks from the StateSnapshot.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithSnapshotTimeUtcTicks(long ticks)
    {
      _snapshotTimeUtcTicks = ticks;
      return this;
    }

    /// <summary>
    /// Sets the player's input message.
    /// </summary>
    /// <param name="input">The player input.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithPlayerInput(string? input)
    {
      _playerInput = input ?? "";
      return this;
    }

    /// <summary>
    /// Sets trigger information.
    /// </summary>
    /// <param name="triggerReason">Numeric trigger reason.</param>
    /// <param name="triggerId">Trigger zone/event identifier.</param>
    /// <param name="sceneName">Unity scene name.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithTriggerInfo(int triggerReason, string? triggerId, string? sceneName)
    {
      _triggerReason = triggerReason;
      _triggerId = triggerId ?? "";
      _sceneName = sceneName ?? "";
      return this;
    }

    /// <summary>
    /// Sets state hashes for drift detection.
    /// </summary>
    /// <param name="memoryHash">Hash of memory state before interaction.</param>
    /// <param name="promptHash">Hash of assembled prompt.</param>
    /// <param name="constraintsHash">Hash of constraints configuration.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithStateHashes(string? memoryHash, string? promptHash, string? constraintsHash)
    {
      _memoryHashBefore = memoryHash ?? "";
      _promptHash = promptHash ?? "";
      _constraintsHash = constraintsHash ?? "";
      return this;
    }

    /// <summary>
    /// Sets the serialized constraints.
    /// </summary>
    /// <param name="constraintsSerialized">JSON-serialized constraints.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithConstraints(string? constraintsSerialized)
    {
      _constraintsSerialized = constraintsSerialized ?? "";
      return this;
    }

    /// <summary>
    /// Sets the LLM output and automatically computes the output hash.
    /// </summary>
    /// <param name="rawOutput">Raw LLM response.</param>
    /// <param name="dialogueText">Parsed dialogue text.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithOutput(string? rawOutput, string? dialogueText)
    {
      _rawOutput = rawOutput ?? "";
      _dialogueText = dialogueText ?? "";
      return this;
    }

    /// <summary>
    /// Sets the validation outcome.
    /// </summary>
    /// <param name="passed">Whether validation passed.</param>
    /// <param name="violationCount">Number of violations detected.</param>
    /// <param name="mutationsApplied">Number of mutations applied.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithValidationOutcome(bool passed, int violationCount, int mutationsApplied)
    {
      _validationPassed = passed;
      _violationCount = violationCount;
      _mutationsApplied = mutationsApplied;
      return this;
    }

    /// <summary>
    /// Sets fallback information when a fallback response was used.
    /// </summary>
    /// <param name="reason">Reason why fallback was used.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithFallback(string? reason)
    {
      _fallbackUsed = true;
      _fallbackReason = reason ?? "";
      return this;
    }

    /// <summary>
    /// Sets performance metrics.
    /// </summary>
    /// <param name="ttftMs">Time to first token in milliseconds.</param>
    /// <param name="totalTimeMs">Total generation time in milliseconds.</param>
    /// <param name="promptTokenCount">Number of prompt tokens.</param>
    /// <param name="generatedTokenCount">Number of generated tokens.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder WithMetrics(long ttftMs, long totalTimeMs, int promptTokenCount, int generatedTokenCount)
    {
      _ttftMs = ttftMs;
      _totalTimeMs = totalTimeMs;
      _promptTokenCount = promptTokenCount;
      _generatedTokenCount = generatedTokenCount;
      return this;
    }

    /// <summary>
    /// Resets the builder to default state.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public AuditRecordBuilder Reset()
    {
      _npcId = "";
      _interactionCount = 0;
      _seed = 0;
      _snapshotTimeUtcTicks = 0;
      _playerInput = "";
      _triggerReason = 0;
      _triggerId = "";
      _sceneName = "";
      _memoryHashBefore = "";
      _promptHash = "";
      _constraintsHash = "";
      _constraintsSerialized = "";
      _rawOutput = "";
      _dialogueText = "";
      _validationPassed = false;
      _violationCount = 0;
      _mutationsApplied = 0;
      _fallbackUsed = false;
      _fallbackReason = "";
      _ttftMs = 0;
      _totalTimeMs = 0;
      _promptTokenCount = 0;
      _generatedTokenCount = 0;
      return this;
    }

    /// <summary>
    /// Builds the AuditRecord with the configured values.
    /// Automatically generates a record ID and captures the current UTC time.
    /// </summary>
    /// <returns>A new AuditRecord instance.</returns>
    public AuditRecord Build()
    {
      return new AuditRecord
      {
        RecordId = GenerateRecordId(),
        NpcId = _npcId,
        InteractionCount = _interactionCount,
        BufferIndex = 0, // Set by the ring buffer when exporting
        CapturedAtUtcTicks = DateTimeOffset.UtcNow.UtcTicks,
        SnapshotTimeUtcTicks = _snapshotTimeUtcTicks,
        Seed = _seed,
        PlayerInput = _playerInput,
        TriggerReason = _triggerReason,
        TriggerId = _triggerId,
        SceneName = _sceneName,
        MemoryHashBefore = _memoryHashBefore,
        PromptHash = _promptHash,
        ConstraintsHash = _constraintsHash,
        ConstraintsSerialized = _constraintsSerialized,
        RawOutput = _rawOutput,
        OutputHash = AuditHasher.ComputeSha256(_rawOutput),
        DialogueText = _dialogueText,
        ValidationPassed = _validationPassed,
        ViolationCount = _violationCount,
        MutationsApplied = _mutationsApplied,
        FallbackUsed = _fallbackUsed,
        FallbackReason = _fallbackReason,
        TtftMs = _ttftMs,
        TotalTimeMs = _totalTimeMs,
        PromptTokenCount = _promptTokenCount,
        GeneratedTokenCount = _generatedTokenCount
      };
    }

    /// <summary>
    /// Generates a unique record ID using a GUID.
    /// </summary>
    private static string GenerateRecordId()
    {
      return Guid.NewGuid().ToString("N").Substring(0, 16);
    }
  }
}
