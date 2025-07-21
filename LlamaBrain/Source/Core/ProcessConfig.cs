namespace LlamaBrain.Core
{
  /// <summary>
  /// Configuration for the process
  /// </summary>`
  public sealed class ProcessConfig
  {
    /// <summary>
    /// The host of the process
    /// </summary>
    public string Host { get; set; } = "localhost";
    /// <summary>
    /// The port of the process
    /// </summary>
    public int Port { get; set; } = 5000;
    /// <summary>
    /// The model to use
    /// </summary>
    public string Model { get; set; } = "";
    /// <summary>
    /// The path to the llama-server executable
    /// </summary>
    public string ExecutablePath { get; set; } = "";
    /// <summary>
    /// The context size for the model
    /// </summary>
    public int ContextSize { get; set; } = 2048;
    /// <summary>
    /// The LLM configuration for generation parameters
    /// </summary>
    public LlmConfig LlmConfig { get; set; } = new LlmConfig();
  }
}