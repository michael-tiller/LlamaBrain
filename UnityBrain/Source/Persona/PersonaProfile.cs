namespace UnityBrain.Persona
{
  /// <summary>
  /// Profile for a persona
  /// </summary>
  public sealed class PersonaProfile
  {
    /// <summary>
    /// The ID of the persona
    /// </summary>
    public string? PersonaId { get; set; }
    /// <summary>
    /// The name of the persona
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// The description of the persona
    /// </summary>
    public string? Description { get; set; }
  }
}