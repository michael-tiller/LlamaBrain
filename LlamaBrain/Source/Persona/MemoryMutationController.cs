using System;
using System.Collections.Generic;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Persona
{
  /// <summary>
  /// Result of executing a single mutation.
  /// </summary>
  [Serializable]
  public class MutationExecutionResult
  {
    /// <summary>
    /// Whether the mutation was executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The original mutation that was attempted.
    /// </summary>
    public ProposedMutation Mutation { get; set; } = null!;

    /// <summary>
    /// Error message if the mutation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The memory entry that was created or modified (if any).
    /// </summary>
    public MemoryEntry? AffectedEntry { get; set; }

    /// <summary>
    /// Creates a successful mutation result.
    /// </summary>
    /// <param name="mutation">The mutation that was executed successfully.</param>
    /// <param name="entry">Optional memory entry that was created or modified.</param>
    /// <returns>A <see cref="MutationExecutionResult"/> indicating successful execution.</returns>
    public static MutationExecutionResult Succeeded(ProposedMutation mutation, MemoryEntry? entry = null)
    {
      return new MutationExecutionResult
      {
        Success = true,
        Mutation = mutation,
        AffectedEntry = entry
      };
    }

    /// <summary>
    /// Creates a failed mutation result.
    /// </summary>
    /// <param name="mutation">The mutation that failed to execute.</param>
    /// <param name="error">The error message describing why the mutation failed.</param>
    /// <returns>A <see cref="MutationExecutionResult"/> indicating failed execution.</returns>
    public static MutationExecutionResult Failed(ProposedMutation mutation, string error)
    {
      return new MutationExecutionResult
      {
        Success = false,
        Mutation = mutation,
        ErrorMessage = error
      };
    }

    /// <inheritdoc/>
    /// <returns>A string representation of the mutation execution result.</returns>
    public override string ToString()
    {
      return Success
        ? $"[OK] {Mutation.Type}: {Mutation.Content}"
        : $"[FAILED] {Mutation.Type}: {ErrorMessage}";
    }
  }

  /// <summary>
  /// Result of executing all mutations from a validation gate result.
  /// </summary>
  [Serializable]
  public class MutationBatchResult
  {
    /// <summary>
    /// Individual results for each mutation.
    /// </summary>
    public List<MutationExecutionResult> Results { get; set; } = new List<MutationExecutionResult>();

    /// <summary>
    /// World intents that were emitted.
    /// </summary>
    public List<WorldIntent> EmittedIntents { get; set; } = new List<WorldIntent>();

    /// <summary>
    /// Total number of mutations attempted.
    /// </summary>
    public int TotalAttempted => Results.Count;

    /// <summary>
    /// Number of successful mutations.
    /// </summary>
    public int SuccessCount => Results.FindAll(r => r.Success).Count;

    /// <summary>
    /// Number of failed mutations.
    /// </summary>
    public int FailureCount => Results.FindAll(r => !r.Success).Count;

    /// <summary>
    /// Whether all mutations succeeded.
    /// </summary>
    public bool AllSucceeded => FailureCount == 0;

    /// <summary>
    /// Gets all failed results.
    /// </summary>
    public IEnumerable<MutationExecutionResult> Failures
    {
      get
      {
        foreach (var result in Results)
        {
          if (!result.Success) yield return result;
        }
      }
    }

    /// <inheritdoc/>
    /// <returns>A string representation of the mutation batch result.</returns>
    public override string ToString()
    {
      return $"MutationBatch: {SuccessCount}/{TotalAttempted} succeeded, {EmittedIntents.Count} intents emitted";
    }
  }

  /// <summary>
  /// Statistics about mutation controller operations.
  /// </summary>
  [Serializable]
  public class MutationStatistics
  {
    /// <summary>
    /// Total mutations attempted.
    /// </summary>
    public int TotalAttempted { get; set; }

    /// <summary>
    /// Total successful mutations.
    /// </summary>
    public int TotalSucceeded { get; set; }

    /// <summary>
    /// Total failed mutations.
    /// </summary>
    public int TotalFailed { get; set; }

    /// <summary>
    /// Number of episodic memories appended.
    /// </summary>
    public int EpisodicAppended { get; set; }

    /// <summary>
    /// Number of beliefs transformed.
    /// </summary>
    public int BeliefsTransformed { get; set; }

    /// <summary>
    /// Number of relationships transformed.
    /// </summary>
    public int RelationshipsTransformed { get; set; }

    /// <summary>
    /// Number of world intents emitted.
    /// </summary>
    public int IntentsEmitted { get; set; }

    /// <summary>
    /// Number of canonical fact mutation attempts (always blocked).
    /// </summary>
    public int CanonicalMutationAttempts { get; set; }

    /// <summary>
    /// Number of authority violations (source lacked authority to modify target).
    /// Tracked when ValidateMutation returns false.
    /// </summary>
    public int AuthorityViolations { get; set; }

    /// <summary>
    /// Success rate as a percentage.
    /// </summary>
    public float SuccessRate => TotalAttempted > 0 ? (float)TotalSucceeded / TotalAttempted * 100f : 0f;

    /// <inheritdoc/>
    /// <returns>A string representation of the mutation statistics.</returns>
    public override string ToString()
    {
      return $"Mutations: {TotalSucceeded}/{TotalAttempted} ({SuccessRate:F1}%), " +
             $"Episodic: {EpisodicAppended}, Beliefs: {BeliefsTransformed}, " +
             $"Relationships: {RelationshipsTransformed}, Intents: {IntentsEmitted}";
    }
  }

  /// <summary>
  /// Event arguments for world intent events.
  /// </summary>
  public class WorldIntentEventArgs : EventArgs
  {
    /// <summary>
    /// The world intent that was emitted.
    /// </summary>
    public WorldIntent Intent { get; }

    /// <summary>
    /// The NPC ID that emitted this intent (if known).
    /// </summary>
    public string? NpcId { get; }

    /// <summary>
    /// Creates a new WorldIntentEventArgs.
    /// </summary>
    /// <param name="intent">The world intent that was emitted.</param>
    /// <param name="npcId">Optional NPC ID that emitted this intent.</param>
    public WorldIntentEventArgs(WorldIntent intent, string? npcId = null)
    {
      Intent = intent;
      NpcId = npcId;
    }
  }

  /// <summary>
  /// Configuration for the mutation controller.
  /// </summary>
  [Serializable]
  public class MutationControllerConfig
  {
    /// <summary>
    /// Default significance for episodic memories created from mutations.
    /// </summary>
    public float DefaultEpisodicSignificance { get; set; } = 0.5f;

    /// <summary>
    /// Default sentiment for relationship mutations when not specified.
    /// </summary>
    public float DefaultRelationshipSentiment { get; set; } = 0f;

    /// <summary>
    /// Whether to log all mutation attempts.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Whether to track statistics.
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Default configuration.
    /// </summary>
    public static MutationControllerConfig Default => new MutationControllerConfig();
  }

  /// <summary>
  /// Controls memory mutations based on validated outputs from the validation gate.
  /// Only validated outputs can trigger mutations. Enforces authority boundaries
  /// and prevents modifications to canonical facts.
  /// </summary>
  public class MemoryMutationController
  {
    private readonly MutationControllerConfig config;
    private readonly MutationStatistics statistics = new MutationStatistics();

    /// <summary>
    /// Optional logging callback.
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// Event fired when a world intent is emitted.
    /// Game systems can subscribe to handle NPC desires.
    /// </summary>
    public event EventHandler<WorldIntentEventArgs>? OnWorldIntentEmitted;

    /// <summary>
    /// Gets the current mutation statistics.
    /// </summary>
    public MutationStatistics Statistics => statistics;

    /// <summary>
    /// Test-only counter for world intent emissions (for policy boundary testing).
    /// Incremented whenever EmitWorldIntent is called.
    /// Always available to ensure cross-assembly test access.
    /// </summary>
    public int WorldIntentEmitsForTests { get; private set; }

    /// <summary>
    /// Creates a new mutation controller with default configuration.
    /// </summary>
    public MemoryMutationController() : this(MutationControllerConfig.Default) { }

    /// <summary>
    /// Creates a new mutation controller with specified configuration.
    /// </summary>
    /// <param name="config">The configuration to use for the mutation controller.</param>
    public MemoryMutationController(MutationControllerConfig config)
    {
      this.config = config ?? MutationControllerConfig.Default;
    }

    /// <summary>
    /// Executes all approved mutations from a gate result against the memory system.
    /// </summary>
    /// <param name="gateResult">The validated gate result containing approved mutations and intents.</param>
    /// <param name="memorySystem">The authoritative memory system to modify.</param>
    /// <param name="npcId">Optional NPC ID for logging and intent attribution.</param>
    /// <returns>A batch result containing individual mutation results.</returns>
    public MutationBatchResult ExecuteMutations(
      GateResult gateResult,
      AuthoritativeMemorySystem memorySystem,
      string? npcId = null)
    {
      if (gateResult == null)
        throw new ArgumentNullException(nameof(gateResult));
      if (memorySystem == null)
        throw new ArgumentNullException(nameof(memorySystem));

      var batchResult = new MutationBatchResult();

      // Only execute mutations if the gate passed
      if (!gateResult.Passed)
      {
        Log($"[MutationController] Skipping mutations - gate did not pass");
        return batchResult;
      }

      // Execute each approved mutation
      // Note: Statistics are tracked in ExecuteSingleMutation
      foreach (var mutation in gateResult.ApprovedMutations)
      {
        var result = ExecuteSingleMutation(mutation, memorySystem, npcId);
        batchResult.Results.Add(result);
      }

      // Emit approved world intents
      foreach (var intent in gateResult.ApprovedIntents)
      {
        EmitWorldIntent(intent, npcId);
        batchResult.EmittedIntents.Add(intent);

        if (config.EnableStatistics)
        {
          statistics.IntentsEmitted++;
        }
      }

      Log($"[MutationController] Batch complete: {batchResult}");
      return batchResult;
    }

    /// <summary>
    /// Executes a single mutation against the memory system.
    /// </summary>
    /// <param name="mutation">The mutation to execute.</param>
    /// <param name="memorySystem">The authoritative memory system to modify.</param>
    /// <param name="npcId">Optional NPC ID for logging.</param>
    /// <returns>The result of the mutation execution.</returns>
    public MutationExecutionResult ExecuteSingleMutation(
      ProposedMutation mutation,
      AuthoritativeMemorySystem memorySystem,
      string? npcId = null)
    {
      if (mutation == null)
        throw new ArgumentNullException(nameof(mutation));
      if (memorySystem == null)
        throw new ArgumentNullException(nameof(memorySystem));

      Log($"[MutationController] Executing {mutation.Type}: {mutation.Content}");

      MutationExecutionResult result;
      try
      {
        switch (mutation.Type)
        {
          case MutationType.AppendEpisodic:
            result = ExecuteAppendEpisodic(mutation, memorySystem);
            break;

          case MutationType.TransformBelief:
            result = ExecuteTransformBelief(mutation, memorySystem);
            break;

          case MutationType.TransformRelationship:
            result = ExecuteTransformRelationship(mutation, memorySystem);
            break;

          case MutationType.EmitWorldIntent:
            result = ExecuteEmitWorldIntent(mutation, npcId);
            break;

          default:
            result = MutationExecutionResult.Failed(mutation, $"Unknown mutation type: {mutation.Type}");
            break;
        }
      }
      catch (Exception ex)
      {
        Log($"[MutationController] Exception executing mutation: {ex.Message}");
        result = MutationExecutionResult.Failed(mutation, $"Exception: {ex.Message}");
      }

      // Update total statistics
      if (config.EnableStatistics)
      {
        statistics.TotalAttempted++;
        if (result.Success)
          statistics.TotalSucceeded++;
        else
          statistics.TotalFailed++;
      }

      return result;
    }

    /// <summary>
    /// Validates whether a mutation can be executed (pre-flight check).
    /// Does not actually execute the mutation.
    /// </summary>
    /// <param name="mutation">The mutation to validate.</param>
    /// <param name="memorySystem">The authoritative memory system.</param>
    /// <returns>Null if valid, error message if invalid.</returns>
    public string? ValidateMutation(ProposedMutation mutation, AuthoritativeMemorySystem memorySystem)
    {
      if (mutation == null) return "Mutation is null";
      if (memorySystem == null) return "Memory system is null";

      // Check for canonical fact mutation attempts
      if (mutation.Target != null && memorySystem.IsCanonicalFact(mutation.Target))
      {
        if (config.EnableStatistics)
          statistics.CanonicalMutationAttempts++;
        return $"Cannot modify canonical fact: {mutation.Target}";
      }

      // Validate mutation type-specific requirements
      switch (mutation.Type)
      {
        case MutationType.AppendEpisodic:
          if (string.IsNullOrWhiteSpace(mutation.Content))
            return "Episodic memory content cannot be empty";
          break;

        case MutationType.TransformBelief:
          if (string.IsNullOrWhiteSpace(mutation.Target))
            return "Belief mutation requires a target (belief ID)";
          if (string.IsNullOrWhiteSpace(mutation.Content))
            return "Belief content cannot be empty";
          break;

        case MutationType.TransformRelationship:
          if (string.IsNullOrWhiteSpace(mutation.Target))
            return "Relationship mutation requires a target (entity ID)";
          if (string.IsNullOrWhiteSpace(mutation.Content))
            return "Relationship content cannot be empty";
          break;

        case MutationType.EmitWorldIntent:
          if (string.IsNullOrWhiteSpace(mutation.Target))
            return "World intent requires an intent type (target)";
          break;
      }

      return null;
    }

    /// <summary>
    /// Resets statistics.
    /// </summary>
    public void ResetStatistics()
    {
      statistics.TotalAttempted = 0;
      statistics.TotalSucceeded = 0;
      statistics.TotalFailed = 0;
      statistics.EpisodicAppended = 0;
      statistics.BeliefsTransformed = 0;
      statistics.RelationshipsTransformed = 0;
      statistics.IntentsEmitted = 0;
      statistics.CanonicalMutationAttempts = 0;
      statistics.AuthorityViolations = 0;
    }

    #region Private Execution Methods

    private MutationExecutionResult ExecuteAppendEpisodic(
      ProposedMutation mutation,
      AuthoritativeMemorySystem memorySystem)
    {
      // Validate content
      if (string.IsNullOrWhiteSpace(mutation.Content))
      {
        return MutationExecutionResult.Failed(mutation, "Episodic memory content cannot be empty");
      }

      // Create episodic memory entry
      var entry = new EpisodicMemoryEntry(mutation.Content, EpisodeType.LearnedInfo)
      {
        Significance = config.DefaultEpisodicSignificance
      };

      // If source text is available, might increase significance
      if (!string.IsNullOrEmpty(mutation.SourceText))
      {
        entry.Significance = Math.Min(1.0f, entry.Significance + 0.1f);
      }

      // Execute mutation with ValidatedOutput authority
      var result = memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);

      if (result.Success)
      {
        if (config.EnableStatistics)
          statistics.EpisodicAppended++;
        Log($"[MutationController] Appended episodic memory: {mutation.Content}");
        return MutationExecutionResult.Succeeded(mutation, entry);
      }

      // Track authority violations
      if (config.EnableStatistics && result.FailureReason?.Contains("lacks authority") == true)
        statistics.AuthorityViolations++;

      return MutationExecutionResult.Failed(mutation, result.FailureReason ?? "Unknown error");
    }

    private MutationExecutionResult ExecuteTransformBelief(
      ProposedMutation mutation,
      AuthoritativeMemorySystem memorySystem)
    {
      // Validate target and content
      if (string.IsNullOrWhiteSpace(mutation.Target))
      {
        return MutationExecutionResult.Failed(mutation, "Belief mutation requires a target (belief ID)");
      }
      if (string.IsNullOrWhiteSpace(mutation.Content))
      {
        return MutationExecutionResult.Failed(mutation, "Belief content cannot be empty");
      }

      // Check for canonical fact protection
      if (memorySystem.IsCanonicalFact(mutation.Target))
      {
        if (config.EnableStatistics)
          statistics.CanonicalMutationAttempts++;
        return MutationExecutionResult.Failed(mutation, $"Cannot modify canonical fact: {mutation.Target}");
      }

      // Create or update belief
      var entry = new BeliefMemoryEntry(mutation.Target, mutation.Content, BeliefType.Belief)
      {
        Confidence = mutation.Confidence,
        Evidence = mutation.SourceText
      };

      var result = memorySystem.SetBelief(mutation.Target, entry, MutationSource.ValidatedOutput);

      if (result.Success)
      {
        if (config.EnableStatistics)
          statistics.BeliefsTransformed++;
        Log($"[MutationController] Transformed belief '{mutation.Target}': {mutation.Content}");
        return MutationExecutionResult.Succeeded(mutation, entry);
      }

      // Track authority violations
      if (config.EnableStatistics && result.FailureReason?.Contains("lacks authority") == true)
        statistics.AuthorityViolations++;

      return MutationExecutionResult.Failed(mutation, result.FailureReason ?? "Unknown error");
    }

    private MutationExecutionResult ExecuteTransformRelationship(
      ProposedMutation mutation,
      AuthoritativeMemorySystem memorySystem)
    {
      // Validate target and content
      if (string.IsNullOrWhiteSpace(mutation.Target))
      {
        return MutationExecutionResult.Failed(mutation, "Relationship mutation requires a target (entity ID)");
      }
      if (string.IsNullOrWhiteSpace(mutation.Content))
      {
        return MutationExecutionResult.Failed(mutation, "Relationship content cannot be empty");
      }

      // Create relationship belief ID
      var beliefId = $"relationship_{mutation.Target}";

      // Check for canonical fact protection
      if (memorySystem.IsCanonicalFact(beliefId))
      {
        if (config.EnableStatistics)
          statistics.CanonicalMutationAttempts++;
        return MutationExecutionResult.Failed(mutation, $"Cannot modify canonical relationship: {beliefId}");
      }

      // Create relationship belief entry
      var entry = BeliefMemoryEntry.CreateRelationship(
        mutation.Target,
        mutation.Content,
        config.DefaultRelationshipSentiment
      );
      entry.Evidence = mutation.SourceText;
      entry.Confidence = mutation.Confidence;

      var result = memorySystem.SetBelief(beliefId, entry, MutationSource.ValidatedOutput);

      if (result.Success)
      {
        if (config.EnableStatistics)
          statistics.RelationshipsTransformed++;
        Log($"[MutationController] Transformed relationship with '{mutation.Target}': {mutation.Content}");
        return MutationExecutionResult.Succeeded(mutation, entry);
      }

      // Track authority violations
      if (config.EnableStatistics && result.FailureReason?.Contains("lacks authority") == true)
        statistics.AuthorityViolations++;

      return MutationExecutionResult.Failed(mutation, result.FailureReason ?? "Unknown error");
    }

    private MutationExecutionResult ExecuteEmitWorldIntent(
      ProposedMutation mutation,
      string? npcId)
    {
      // Validate intent type
      if (string.IsNullOrWhiteSpace(mutation.Target))
      {
        return MutationExecutionResult.Failed(mutation, "World intent requires an intent type (target)");
      }

      // Create world intent from mutation
      var intent = WorldIntent.Create(mutation.Target, npcId, 0);
      intent.SourceText = mutation.SourceText;

      // Add content as a parameter
      if (!string.IsNullOrWhiteSpace(mutation.Content))
      {
        intent.Parameters["content"] = mutation.Content;
      }

      // Emit the intent
      EmitWorldIntent(intent, npcId);

      Log($"[MutationController] Emitted world intent: {intent.IntentType}");
      return MutationExecutionResult.Succeeded(mutation);
    }

    private void EmitWorldIntent(WorldIntent intent, string? npcId)
    {
      WorldIntentEmitsForTests++;
      OnWorldIntentEmitted?.Invoke(this, new WorldIntentEventArgs(intent, npcId));
    }

    /// <summary>
    /// Raises the OnWorldIntentEmitted event for testing purposes.
    /// This method is intended for use in tests only.
    /// </summary>
    /// <param name="intent">The world intent to emit.</param>
    /// <param name="npcId">The optional NPC identifier associated with the intent.</param>
    public void RaiseWorldIntentEmittedForTests(WorldIntent intent, string? npcId)
    {
      WorldIntentEmitsForTests++;
      OnWorldIntentEmitted?.Invoke(this, new WorldIntentEventArgs(intent, npcId));
    }

    private void Log(string message)
    {
      if (config.EnableLogging)
      {
        OnLog?.Invoke(message);
      }
    }

    #endregion
  }
}
