/// <summary>
/// This namespace contains the core classes for the UnityBrain library.
/// </summary>
namespace UnityBrain.Core
{
  /// <summary>
  /// Represents a single LLM dialogue response and context.
  /// </summary>
  public class DialogueResult
  {
    /// <summary>
    /// The NPC response.
    /// </summary>
    public string NpcResponse { get; set; } = "";
    /// <summary>
    /// The player input.
    /// </summary>
    public string PlayerInput { get; set; } = "";
    /// <summary>   
    /// The ID of the persona.
    /// </summary>
    public string PersonaId { get; set; } = "";
    /// <summary>
    /// Whether the dialogue was successful.
    /// </summary>
    public bool Success { get; set; } = false;
    /// <summary>
    /// The error message if the dialogue was not successful.
    /// </summary>
    public string Error { get; set; } = "";
  }
}