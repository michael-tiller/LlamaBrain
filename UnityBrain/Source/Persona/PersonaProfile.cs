using System.Collections.Generic;

namespace UnityBrain.Persona
{
  /// <summary>
  /// Profile data for a persona - POCO for serialization and runtime use
  /// </summary>
  public sealed class PersonaProfile
  {
    /// <summary>
    /// The ID of the persona
    /// </summary>
    public string PersonaId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the persona
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the persona
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The system prompt for the persona
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// The personality traits of the persona
    /// </summary>
    public string PersonalityTraits { get; set; } = string.Empty;

    /// <summary>
    /// The background story of the persona
    /// </summary>
    public string Background { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use memory for the persona
    /// </summary>
    public bool UseMemory { get; set; } = true;

    /// <summary>
    /// Custom metadata for the persona
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Creates a new PersonaProfile with the specified ID and name
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <param name="name">The name of the persona</param>
    /// <returns>A new PersonaProfile instance</returns>
    public static PersonaProfile Create(string personaId, string name)
    {
      return new PersonaProfile
      {
        PersonaId = personaId,
        Name = name,
        Metadata = new Dictionary<string, string>()
      };
    }

    /// <summary>
    /// Gets a metadata value by key
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <returns>The metadata value, or null if not found</returns>
    public string? GetMetadata(string key)
    {
      return Metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Sets a metadata value
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    public void SetMetadata(string key, string value)
    {
      Metadata[key] = value;
    }

    /// <summary>
    /// Removes a metadata entry
    /// </summary>
    /// <param name="key">The metadata key to remove</param>
    /// <returns>True if the entry was removed, false if it didn't exist</returns>
    public bool RemoveMetadata(string key)
    {
      return Metadata.Remove(key);
    }
  }
}