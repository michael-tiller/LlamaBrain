namespace LlamaBrain.Core
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
    /// Number of GPU layers to offload (0 = CPU only, 35+ = full GPU)
    /// </summary>
    public int GpuLayers { get; set; } = 35;
    /// <summary>
    /// Number of CPU threads (0 = auto-detect)
    /// </summary>
    public int Threads { get; set; } = 0;
    /// <summary>
    /// Batch size for prompt processing
    /// </summary>
    public int BatchSize { get; set; } = 512;
    /// <summary>
    /// Micro-batch size for generation
    /// </summary>
    public int UBatchSize { get; set; } = 128;
    /// <summary>
    /// Whether to lock model in RAM
    /// </summary>
    public bool UseMlock { get; set; } = true;
    /// <summary>
    /// Number of parallel request slots (1 = lowest latency, higher = more throughput)
    /// Default to 1 for game NPCs where latency matters more than throughput
    /// </summary>
    public int ParallelSlots { get; set; } = 1;
    /// <summary>
    /// Whether to use continuous batching (helps with multiple concurrent requests)
    /// </summary>
    public bool UseContinuousBatching { get; set; } = false;
    /// <summary>
    /// Whether to enable Flash Attention (faster attention on supported GPUs)
    /// Requires cuBLAS or Metal backend. Reduces memory usage and improves speed.
    /// </summary>
    public bool UseFlashAttention { get; set; } = true;
    /// <summary>
    /// The LLM configuration for generation parameters
    /// </summary>
    public LlmConfig LlmConfig { get; set; } = new LlmConfig();
  }
}