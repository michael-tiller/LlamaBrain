using System;

namespace LlamaBrain.Persona.MemoryTypes
{
  /// <summary>
  /// Mutable game state that requires validation to change.
  /// Can only be modified through validated game events.
  /// Examples: "The door is open", "Player has 50 gold", "Quest 'Dragon Slayer' is active"
  /// </summary>
  [Serializable]
  public class WorldStateEntry : MemoryEntry
  {
    /// <summary>
    /// The state key (e.g., "door_castle_main", "player_gold").
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The current value of this state.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// When this state was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; private set; }

    /// <summary>
    /// Number of times this state has been modified.
    /// </summary>
    public int ModificationCount { get; private set; }

    /// <summary>
    /// World state has high authority (below canonical).
    /// </summary>
    public override MemoryAuthority Authority => MemoryAuthority.WorldState;

    /// <summary>
    /// Returns a formatted state description.
    /// </summary>
    public override string Content => $"{Key}: {Value}";

    /// <summary>
    /// Creates a new world state entry.
    /// </summary>
    /// <param name="key">The state key.</param>
    /// <param name="value">The initial value.</param>
    /// <exception cref="ArgumentNullException">Thrown when key or value is null</exception>
    public WorldStateEntry(string key, string value)
    {
      Key = key ?? throw new ArgumentNullException(nameof(key));
      Value = value ?? throw new ArgumentNullException(nameof(value));
      ModifiedAt = DateTime.UtcNow;
      Source = MutationSource.GameSystem;
    }

    /// <summary>
    /// Updates the value of this state entry.
    /// Only valid if the source has sufficient authority.
    /// </summary>
    /// <param name="newValue">The new value.</param>
    /// <param name="source">The source requesting the change.</param>
    /// <returns>True if the update was successful.</returns>
    public MutationResult UpdateValue(string newValue, MutationSource source)
    {
      // World state can only be modified by GameSystem or Designer
      if (source < MutationSource.GameSystem)
      {
        return MutationResult.Failed($"Source '{source}' lacks authority to modify world state. Required: GameSystem or higher.");
      }

      Value = newValue;
      ModifiedAt = DateTime.UtcNow;
      ModificationCount++;
      Source = source;

      return MutationResult.Succeeded(this);
    }

    /// <summary>
    /// Creates a world state entry with a specific ID.
    /// </summary>
    /// <param name="id">The unique identifier for the state entry</param>
    /// <param name="key">The state key</param>
    /// <param name="value">The initial value</param>
    /// <returns>A new WorldStateEntry with the specified ID</returns>
    public static WorldStateEntry Create(string id, string key, string value)
    {
      return new WorldStateEntry(key, value) { Id = id };
    }
  }
}
