namespace LlamaBrain.Core.Expectancy
{
  /// <summary>
  /// The reason/type of interaction that triggered dialogue generation.
  /// </summary>
  public enum TriggerReason
  {
    /// <summary>Player initiated conversation.</summary>
    PlayerUtterance,
    /// <summary>NPC entered a trigger zone.</summary>
    ZoneTrigger,
    /// <summary>Time-based trigger (e.g., idle dialogue).</summary>
    TimeTrigger,
    /// <summary>Quest-related trigger.</summary>
    QuestTrigger,
    /// <summary>NPC-to-NPC interaction.</summary>
    NpcInteraction,
    /// <summary>World event triggered dialogue.</summary>
    WorldEvent,
    /// <summary>Unknown or custom trigger type.</summary>
    Custom
  }

  /// <summary>
  /// Captures the full context of an interaction for the Expectancy Engine.
  /// This is the input that determines which constraints apply.
  /// Engine-agnostic (works with Unity, Unreal, Godot, etc.)
  /// </summary>
  [System.Serializable]
  public class InteractionContext
  {
    /// <summary>
    /// The reason this interaction was triggered.
    /// </summary>
    public TriggerReason TriggerReason { get; set; } = TriggerReason.PlayerUtterance;

    /// <summary>
    /// The NPC involved in this interaction (their GameObject name or ID).
    /// </summary>
    public string? NpcId { get; set; }

    /// <summary>
    /// The trigger zone ID if this was a zone-triggered interaction.
    /// </summary>
    public string? TriggerId { get; set; }

    /// <summary>
    /// The player's input text (if PlayerUtterance trigger).
    /// </summary>
    public string? PlayerInput { get; set; }

    /// <summary>
    /// The prompt text from the trigger (for zone/quest triggers).
    /// </summary>
    public string? TriggerPrompt { get; set; }

    /// <summary>
    /// Current game time or world state timestamp.
    /// Set by the engine-specific layer (Unity Time.time, Unreal GetWorld()->GetTimeSeconds(), etc.)
    /// </summary>
    public float GameTime { get; set; }

    /// <summary>
    /// Current scene/level name.
    /// </summary>
    public string? SceneName { get; set; }

    /// <summary>
    /// Number of times this NPC has been interacted with in this session.
    /// </summary>
    public int InteractionCount { get; set; }

    /// <summary>
    /// Custom tags that can be used for rule matching.
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Creates an empty interaction context.
    /// </summary>
    public InteractionContext() { }

    /// <summary>
    /// Creates a context for a zone-triggered interaction.
    /// </summary>
    /// <param name="npcId">The NPC's identifier.</param>
    /// <param name="triggerId">The trigger zone identifier.</param>
    /// <param name="triggerPrompt">The prompt from the trigger.</param>
    /// <param name="gameTime">Current game time (from engine).</param>
    /// <returns>A new InteractionContext configured for a zone trigger</returns>
    public static InteractionContext FromZoneTrigger(string npcId, string triggerId, string triggerPrompt, float gameTime = 0f)
    {
      return new InteractionContext
      {
        TriggerReason = TriggerReason.ZoneTrigger,
        NpcId = npcId,
        TriggerId = triggerId,
        TriggerPrompt = triggerPrompt,
        GameTime = gameTime
      };
    }

    /// <summary>
    /// Creates a context for a player-initiated interaction.
    /// </summary>
    /// <param name="npcId">The NPC's identifier.</param>
    /// <param name="playerInput">The player's input text.</param>
    /// <param name="gameTime">Current game time (from engine).</param>
    /// <returns>A new InteractionContext configured for a player utterance</returns>
    public static InteractionContext FromPlayerUtterance(string npcId, string playerInput, float gameTime = 0f)
    {
      return new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = npcId,
        PlayerInput = playerInput,
        GameTime = gameTime
      };
    }

    /// <summary>
    /// Returns a string representation of this interaction context.
    /// </summary>
    /// <returns>A formatted string showing the trigger reason, NPC ID, and trigger ID</returns>
    public override string ToString()
    {
      return $"[InteractionContext] Trigger={TriggerReason}, NPC={NpcId}, TriggerId={TriggerId}";
    }
  }
}
