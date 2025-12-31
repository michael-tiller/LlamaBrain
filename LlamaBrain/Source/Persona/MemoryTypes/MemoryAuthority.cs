namespace LlamaBrain.Persona.MemoryTypes
{
  /// <summary>
  /// Defines the authority level of a memory type.
  /// Higher authority memories cannot be overridden by lower authority sources.
  /// </summary>
  public enum MemoryAuthority
  {
    /// <summary>
    /// Canonical facts - immutable truths defined by game designers.
    /// Cannot be modified or contradicted by any source.
    /// Examples: "The king's name is Arthur", "Dragons breathe fire"
    /// </summary>
    Canonical = 100,

    /// <summary>
    /// World state - game state that can change but requires validation.
    /// Can only be modified through validated game events.
    /// Examples: "The door is open", "Player has 50 gold"
    /// </summary>
    WorldState = 75,

    /// <summary>
    /// Episodic memory - conversation and event history.
    /// Can be added but not arbitrarily modified. May decay over time.
    /// Examples: "Player said hello", "We discussed the weather"
    /// </summary>
    Episodic = 50,

    /// <summary>
    /// Belief/Relationship memory - NPC opinions and beliefs.
    /// Can be wrong, can change based on interactions.
    /// Examples: "I think the player is friendly", "I believe the treasure is hidden"
    /// </summary>
    Belief = 25
  }

  /// <summary>
  /// The source of a memory mutation request.
  /// Used to validate whether the source has authority to make changes.
  /// </summary>
  public enum MutationSource
  {
    /// <summary>
    /// Game designer/developer defined data.
    /// Has authority over all memory types.
    /// </summary>
    Designer = 100,

    /// <summary>
    /// Validated game system event.
    /// Has authority over WorldState and below.
    /// </summary>
    GameSystem = 75,

    /// <summary>
    /// Validated LLM output that passed the validation gate.
    /// Has authority over Episodic and Belief memories.
    /// </summary>
    ValidatedOutput = 50,

    /// <summary>
    /// Unvalidated LLM output.
    /// Can only suggest Belief-level changes (requires validation).
    /// </summary>
    LlmSuggestion = 25
  }
}
