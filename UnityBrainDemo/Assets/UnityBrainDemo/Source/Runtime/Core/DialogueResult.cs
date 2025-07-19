/// <summary>
/// Represents a single LLM dialogue response and context.
/// </summary>
public class DialogueResult
{
    public string NpcResponse { get; set; }
    public string PlayerInput { get; set; }
    public string PersonaId { get; set; }
    public string[] DialogueHistory { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
}