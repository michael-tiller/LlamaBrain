using System;

namespace LlamaBrain.Persona.MemoryTypes
{
  /// <summary>
  /// Type of episodic memory event.
  /// </summary>
  public enum EpisodeType
  {
    /// <summary>Dialogue exchange with player.</summary>
    Dialogue,
    /// <summary>Observation of player action.</summary>
    Observation,
    /// <summary>Internal thought or reaction.</summary>
    Thought,
    /// <summary>Significant event that occurred.</summary>
    Event,
    /// <summary>Information learned from conversation.</summary>
    LearnedInfo
  }

  /// <summary>
  /// Conversation and event history memory.
  /// Can be added but not arbitrarily modified. May decay over time.
  /// Examples: "Player said hello", "We discussed the weather", "Player gave me a gift"
  /// </summary>
  [Serializable]
  public class EpisodicMemoryEntry : MemoryEntry
  {
    /// <summary>
    /// The description of what happened.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The type of episode.
    /// </summary>
    public EpisodeType EpisodeType { get; set; } = EpisodeType.Dialogue;

    /// <summary>
    /// Who was involved (e.g., "Player", "Guard Captain").
    /// </summary>
    public string? Participant { get; set; }

    /// <summary>
    /// Emotional significance (0.0 = neutral, 1.0 = highly significant).
    /// Higher significance memories are less likely to decay.
    /// </summary>
    public float Significance { get; set; } = 0.5f;

    /// <summary>
    /// Current strength of this memory (1.0 = fresh, decays toward 0).
    /// </summary>
    public float Strength { get; set; } = 1.0f;

    /// <summary>
    /// Game time when this episode occurred (for chronological ordering).
    /// </summary>
    public float GameTime { get; set; }

    /// <summary>
    /// Episodic memories have medium authority.
    /// </summary>
    public override MemoryAuthority Authority => MemoryAuthority.Episodic;

    /// <summary>
    /// Returns the episode description.
    /// </summary>
    public override string Content => Description;

    /// <summary>
    /// Creates a new episodic memory entry.
    /// </summary>
    /// <param name="description">What happened.</param>
    /// <param name="episodeType">The type of episode.</param>
    /// <exception cref="ArgumentNullException">Thrown when description is null</exception>
    public EpisodicMemoryEntry(string description, EpisodeType episodeType = EpisodeType.Dialogue)
    {
      Description = description ?? throw new ArgumentNullException(nameof(description));
      EpisodeType = episodeType;
      Source = MutationSource.ValidatedOutput;
    }

    /// <summary>
    /// Applies decay to this memory based on time passed.
    /// </summary>
    /// <param name="decayFactor">How much to decay (0-1). Higher significance resists decay.</param>
    public void ApplyDecay(float decayFactor)
    {
      // Significant memories decay slower
      var adjustedDecay = decayFactor * (1.0f - Significance * 0.5f);
      Strength = Math.Max(0, Strength - adjustedDecay);
    }

    /// <summary>
    /// Reinforces this memory (e.g., when it's referenced again).
    /// </summary>
    /// <param name="amount">How much to reinforce (0-1).</param>
    public void Reinforce(float amount = 0.2f)
    {
      Strength = Math.Min(1.0f, Strength + amount);
      MarkAccessed();
    }

    /// <summary>
    /// Whether this memory is still strong enough to be included in prompts.
    /// </summary>
    public bool IsActive => Strength > 0.1f;

    /// <summary>
    /// Creates an episodic memory for a dialogue exchange.
    /// </summary>
    /// <param name="speaker">Who spoke</param>
    /// <param name="content">What was said</param>
    /// <param name="significance">The emotional significance (0.0 to 1.0)</param>
    /// <returns>A new EpisodicMemoryEntry representing a dialogue exchange</returns>
    public static EpisodicMemoryEntry FromDialogue(string speaker, string content, float significance = 0.5f)
    {
      return new EpisodicMemoryEntry($"{speaker}: {content}", EpisodeType.Dialogue)
      {
        Participant = speaker,
        Significance = significance
      };
    }

    /// <summary>
    /// Creates an episodic memory for an observed event.
    /// </summary>
    /// <param name="observation">What was observed</param>
    /// <param name="significance">The emotional significance (0.0 to 1.0)</param>
    /// <returns>A new EpisodicMemoryEntry representing an observation</returns>
    public static EpisodicMemoryEntry FromObservation(string observation, float significance = 0.3f)
    {
      return new EpisodicMemoryEntry(observation, EpisodeType.Observation)
      {
        Significance = significance
      };
    }

    /// <summary>
    /// Creates an episodic memory for learned information.
    /// </summary>
    /// <param name="info">The information that was learned</param>
    /// <param name="source">Optional source of the information</param>
    /// <param name="significance">The emotional significance (0.0 to 1.0)</param>
    /// <returns>A new EpisodicMemoryEntry representing learned information</returns>
    public static EpisodicMemoryEntry FromLearnedInfo(string info, string? source = null, float significance = 0.6f)
    {
      var entry = new EpisodicMemoryEntry(info, EpisodeType.LearnedInfo)
      {
        Significance = significance
      };
      if (source != null) entry.Participant = source;
      return entry;
    }
  }
}
