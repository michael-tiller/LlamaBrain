using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// Configuration for working memory bounds.
  /// </summary>
  public class WorkingMemoryConfig
  {
    /// <summary>
    /// Maximum number of dialogue exchanges to include.
    /// Default: 5 (10 messages total - 5 player + 5 NPC).
    /// </summary>
    public int MaxDialogueExchanges { get; set; } = 5;

    /// <summary>
    /// Maximum number of episodic memories to include.
    /// Default: 5.
    /// </summary>
    public int MaxEpisodicMemories { get; set; } = 5;

    /// <summary>
    /// Maximum number of beliefs to include.
    /// Default: 3.
    /// </summary>
    public int MaxBeliefs { get; set; } = 3;

    /// <summary>
    /// Maximum total characters for all context (soft limit).
    /// Default: 2000 characters.
    /// </summary>
    public int MaxContextCharacters { get; set; } = 2000;

    /// <summary>
    /// Whether to always include all canonical facts (ignores character limit).
    /// Default: true.
    /// </summary>
    public bool AlwaysIncludeCanonicalFacts { get; set; } = true;

    /// <summary>
    /// Whether to always include all world state (ignores character limit).
    /// Default: true.
    /// </summary>
    public bool AlwaysIncludeWorldState { get; set; } = true;

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static WorkingMemoryConfig Default => new WorkingMemoryConfig();

    /// <summary>
    /// Creates a minimal configuration for constrained contexts.
    /// </summary>
    public static WorkingMemoryConfig Minimal => new WorkingMemoryConfig
    {
      MaxDialogueExchanges = 2,
      MaxEpisodicMemories = 2,
      MaxBeliefs = 1,
      MaxContextCharacters = 1000
    };

    /// <summary>
    /// Creates an expanded configuration for richer contexts.
    /// </summary>
    public static WorkingMemoryConfig Expanded => new WorkingMemoryConfig
    {
      MaxDialogueExchanges = 10,
      MaxEpisodicMemories = 10,
      MaxBeliefs = 5,
      MaxContextCharacters = 4000
    };
  }

  /// <summary>
  /// Short-lived memory assembled for a single inference.
  /// Contains bounded context extracted from the state snapshot.
  /// Designed to be created, used, and discarded within a single inference cycle.
  /// </summary>
  public class EphemeralWorkingMemory : IDisposable
  {
    private readonly WorkingMemoryConfig _config;
    private bool _disposed = false;

    /// <summary>
    /// The state snapshot this working memory was created from.
    /// </summary>
    public StateSnapshot SourceSnapshot { get; }

    /// <summary>
    /// Bounded dialogue history (most recent exchanges).
    /// </summary>
    public IReadOnlyList<string> DialogueHistory { get; private set; }

    /// <summary>
    /// Bounded canonical facts.
    /// </summary>
    public IReadOnlyList<string> CanonicalFacts { get; private set; }

    /// <summary>
    /// Bounded world state.
    /// </summary>
    public IReadOnlyList<string> WorldState { get; private set; }

    /// <summary>
    /// Bounded episodic memories.
    /// </summary>
    public IReadOnlyList<string> EpisodicMemories { get; private set; }

    /// <summary>
    /// Bounded beliefs.
    /// </summary>
    public IReadOnlyList<string> Beliefs { get; private set; }

    /// <summary>
    /// The system prompt.
    /// </summary>
    public string SystemPrompt { get; private set; }

    /// <summary>
    /// The player's current input.
    /// </summary>
    public string PlayerInput { get; private set; }

    /// <summary>
    /// The constraints to apply.
    /// </summary>
    public ConstraintSet Constraints { get; private set; }

    /// <summary>
    /// Total character count of all context.
    /// </summary>
    public int TotalCharacterCount { get; private set; }

    /// <summary>
    /// Whether any content was truncated due to limits.
    /// </summary>
    public bool WasTruncated { get; private set; }

    /// <summary>
    /// When this working memory was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Creates ephemeral working memory from a state snapshot.
    /// </summary>
    /// <param name="snapshot">The source state snapshot</param>
    /// <param name="config">Optional configuration (uses defaults if null)</param>
    public EphemeralWorkingMemory(StateSnapshot snapshot, WorkingMemoryConfig? config = null)
    {
      SourceSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
      _config = config ?? WorkingMemoryConfig.Default;
      CreatedAt = DateTime.UtcNow;

      // Initialize properties to avoid nullable warnings
      DialogueHistory = Array.Empty<string>();
      CanonicalFacts = Array.Empty<string>();
      WorldState = Array.Empty<string>();
      EpisodicMemories = Array.Empty<string>();
      Beliefs = Array.Empty<string>();
      SystemPrompt = "";
      PlayerInput = "";
      Constraints = new ConstraintSet();

      AssembleFromSnapshot(snapshot);
    }

    /// <summary>
    /// Assembles bounded working memory from the snapshot.
    /// </summary>
    private void AssembleFromSnapshot(StateSnapshot snapshot)
    {
      SystemPrompt = snapshot.SystemPrompt ?? "";
      PlayerInput = snapshot.PlayerInput ?? "";
      Constraints = snapshot.Constraints ?? new ConstraintSet();

      // Start with canonical facts (always included if configured)
      var canonicalFacts = new List<string>();
      if (_config.AlwaysIncludeCanonicalFacts)
      {
        canonicalFacts.AddRange(snapshot.CanonicalFacts);
      }
      CanonicalFacts = canonicalFacts;

      // World state (always included if configured)
      var worldState = new List<string>();
      if (_config.AlwaysIncludeWorldState)
      {
        worldState.AddRange(snapshot.WorldState);
      }
      WorldState = worldState;

      // Bound dialogue history (take most recent)
      var maxDialogueLines = _config.MaxDialogueExchanges * 2; // Each exchange = 2 lines
      var dialogueHistory = snapshot.DialogueHistory
        .Skip(Math.Max(0, snapshot.DialogueHistory.Count - maxDialogueLines))
        .ToList();
      DialogueHistory = dialogueHistory;

      // Bound episodic memories
      var episodicMemories = snapshot.EpisodicMemories
        .Take(_config.MaxEpisodicMemories)
        .ToList();
      EpisodicMemories = episodicMemories;

      // Bound beliefs
      var beliefs = snapshot.Beliefs
        .Take(_config.MaxBeliefs)
        .ToList();
      Beliefs = beliefs;

      // Calculate total character count
      TotalCharacterCount = CalculateTotalCharacters();

      // Check if we need to truncate further due to character limit
      if (TotalCharacterCount > _config.MaxContextCharacters)
      {
        TruncateToCharacterLimit();
      }
    }

    /// <summary>
    /// Calculates total character count of all context.
    /// </summary>
    private int CalculateTotalCharacters()
    {
      var total = SystemPrompt.Length + PlayerInput.Length;

      foreach (var fact in CanonicalFacts) total += fact.Length + 10; // + prefix
      foreach (var state in WorldState) total += state.Length + 10;
      foreach (var dialogue in DialogueHistory) total += dialogue.Length + 2;
      foreach (var memory in EpisodicMemories) total += memory.Length + 12;
      foreach (var belief in Beliefs) total += belief.Length + 2;

      // Add constraint text estimate
      total += Constraints.Count * 50;

      return total;
    }

    /// <summary>
    /// Truncates context to fit within character limit.
    /// Preserves canonical facts and world state, reduces other content.
    /// </summary>
    private void TruncateToCharacterLimit()
    {
      WasTruncated = true;

      // Calculate mandatory content size (facts + world state + system prompt + input)
      var mandatorySize = SystemPrompt.Length + PlayerInput.Length;
      foreach (var fact in CanonicalFacts) mandatorySize += fact.Length + 10;
      foreach (var state in WorldState) mandatorySize += state.Length + 10;
      mandatorySize += Constraints.Count * 50;

      var remainingBudget = _config.MaxContextCharacters - mandatorySize;
      if (remainingBudget <= 0)
      {
        // Not enough room for anything else
        DialogueHistory = new List<string>();
        EpisodicMemories = new List<string>();
        Beliefs = new List<string>();
        TotalCharacterCount = mandatorySize;
        return;
      }

      // Allocate remaining budget: 60% dialogue, 25% episodic, 15% beliefs
      var dialogueBudget = (int)(remainingBudget * 0.6);
      var episodicBudget = (int)(remainingBudget * 0.25);
      var beliefBudget = (int)(remainingBudget * 0.15);

      // Truncate dialogue to fit budget
      DialogueHistory = TruncateListToCharacterLimit(DialogueHistory.ToList(), dialogueBudget);

      // Truncate episodic to fit budget
      EpisodicMemories = TruncateListToCharacterLimit(EpisodicMemories.ToList(), episodicBudget);

      // Truncate beliefs to fit budget
      Beliefs = TruncateListToCharacterLimit(Beliefs.ToList(), beliefBudget);

      TotalCharacterCount = CalculateTotalCharacters();
    }

    /// <summary>
    /// Truncates a list of strings to fit within a character budget.
    /// Keeps items from the end (most recent).
    /// </summary>
    private List<string> TruncateListToCharacterLimit(List<string> items, int budget)
    {
      var result = new List<string>();
      var currentSize = 0;

      // Process from end to beginning (keep most recent)
      for (int i = items.Count - 1; i >= 0; i--)
      {
        var itemSize = items[i].Length + 2; // +2 for newline/separator
        if (currentSize + itemSize <= budget)
        {
          result.Insert(0, items[i]);
          currentSize += itemSize;
        }
        else
        {
          break;
        }
      }

      return result;
    }

    /// <summary>
    /// Gets all context as a single formatted string for prompt injection.
    /// </summary>
    /// <returns>Formatted context string</returns>
    public string GetFormattedContext()
    {
      var parts = new List<string>();

      // Canonical facts
      if (CanonicalFacts.Count > 0)
      {
        foreach (var fact in CanonicalFacts)
        {
          parts.Add($"[Fact] {fact}");
        }
      }

      // World state
      if (WorldState.Count > 0)
      {
        foreach (var state in WorldState)
        {
          parts.Add($"[State] {state}");
        }
      }

      // Episodic memories
      if (EpisodicMemories.Count > 0)
      {
        foreach (var memory in EpisodicMemories)
        {
          parts.Add($"[Memory] {memory}");
        }
      }

      // Beliefs
      if (Beliefs.Count > 0)
      {
        foreach (var belief in Beliefs)
        {
          parts.Add(belief);
        }
      }

      return string.Join("\n", parts);
    }

    /// <summary>
    /// Gets formatted dialogue history.
    /// </summary>
    /// <returns>Formatted dialogue string</returns>
    public string GetFormattedDialogue()
    {
      return string.Join("\n", DialogueHistory);
    }

    /// <summary>
    /// Gets statistics about this working memory.
    /// </summary>
    /// <returns>Statistics about the working memory contents</returns>
    public WorkingMemoryStats GetStats()
    {
      return new WorkingMemoryStats
      {
        DialogueCount = DialogueHistory.Count,
        CanonicalFactCount = CanonicalFacts.Count,
        WorldStateCount = WorldState.Count,
        EpisodicMemoryCount = EpisodicMemories.Count,
        BeliefCount = Beliefs.Count,
        ConstraintCount = Constraints.Count,
        TotalCharacters = TotalCharacterCount,
        WasTruncated = WasTruncated
      };
    }

    /// <summary>
    /// Returns a string representation of this working memory.
    /// </summary>
    /// <returns>A string representation of the working memory</returns>
    public override string ToString()
    {
      var stats = GetStats();
      var truncated = WasTruncated ? " (truncated)" : "";
      return $"WorkingMemory[{stats.TotalItems} items, {TotalCharacterCount} chars{truncated}]";
    }

    /// <summary>
    /// Disposes the working memory, clearing all references.
    /// </summary>
    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;

      // Clear all collections to help GC
      DialogueHistory = Array.Empty<string>();
      CanonicalFacts = Array.Empty<string>();
      WorldState = Array.Empty<string>();
      EpisodicMemories = Array.Empty<string>();
      Beliefs = Array.Empty<string>();
      SystemPrompt = "";
      PlayerInput = "";
      Constraints = new ConstraintSet();
    }
  }

  /// <summary>
  /// Statistics about working memory contents.
  /// </summary>
  public class WorkingMemoryStats
  {
    /// <summary>Number of dialogue lines.</summary>
    public int DialogueCount { get; set; }

    /// <summary>Number of canonical facts.</summary>
    public int CanonicalFactCount { get; set; }

    /// <summary>Number of world state entries.</summary>
    public int WorldStateCount { get; set; }

    /// <summary>Number of episodic memories.</summary>
    public int EpisodicMemoryCount { get; set; }

    /// <summary>Number of beliefs.</summary>
    public int BeliefCount { get; set; }

    /// <summary>Number of constraints.</summary>
    public int ConstraintCount { get; set; }

    /// <summary>Total characters in context.</summary>
    public int TotalCharacters { get; set; }

    /// <summary>Whether content was truncated.</summary>
    public bool WasTruncated { get; set; }

    /// <summary>Total number of context items.</summary>
    public int TotalItems => DialogueCount + CanonicalFactCount + WorldStateCount +
                             EpisodicMemoryCount + BeliefCount;

    /// <summary>
    /// Returns a string representation of the stats.
    /// </summary>
    /// <returns>A string representation of the working memory statistics</returns>
    public override string ToString()
    {
      return $"WorkingMemoryStats: {TotalItems} items, {TotalCharacters} chars, " +
             $"dialogue={DialogueCount}, facts={CanonicalFactCount}, state={WorldStateCount}, " +
             $"episodes={EpisodicMemoryCount}, beliefs={BeliefCount}, constraints={ConstraintCount}";
    }
  }
}
