using System.Collections.Generic;

namespace LlamaBrain.Persona
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
    /// The personality traits dictionary of the persona
    /// </summary>
    public Dictionary<string, string> Traits { get; set; } = new Dictionary<string, string>();

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
        Traits = new Dictionary<string, string>(),
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

    /// <summary>
    /// Gets a trait value by key
    /// </summary>
    /// <param name="key">The trait key</param>
    /// <returns>The trait value, or null if not found</returns>
    public string? GetTrait(string key)
    {
      return Traits.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Sets a trait value
    /// </summary>
    /// <param name="key">The trait key</param>
    /// <param name="value">The trait value</param>
    public void SetTrait(string key, string value)
    {
      Traits[key] = value;
    }

    /// <summary>
    /// Removes a trait entry
    /// </summary>
    /// <param name="key">The trait key to remove</param>
    /// <returns>True if the trait was removed, false if it didn't exist</returns>
    public bool RemoveTrait(string key)
    {
      return Traits.Remove(key);
    }

    /// <summary>
    /// Converts the traits dictionary to a formatted string (for backward compatibility)
    /// </summary>
    /// <returns>A formatted string representation of all traits</returns>
    public string GetTraitsAsString()
    {
      if (Traits.Count == 0)
        return string.Empty;

      var traitStrings = new List<string>();
      foreach (var trait in Traits)
      {
        traitStrings.Add($"{trait.Key}: {trait.Value}");
      }
      return string.Join("; ", traitStrings);
    }
  }
}