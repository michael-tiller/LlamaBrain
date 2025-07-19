namespace UnityBrain.Core
{
  /// <summary>
  /// Configuration for the process
  /// </summary>
  public sealed class ProcessConfig
  {
    /// <summary>
    /// The host of the process
    /// </summary>
    public string Host { get; set; } = "localhost";
    /// <summary>
    /// The port of the process
    /// </summary>
    public int Port { get; set; } = 11434;
    /// <summary>
    /// The model to use
    /// </summary>
    public string Model { get; set; } = "llama2";
  }
}