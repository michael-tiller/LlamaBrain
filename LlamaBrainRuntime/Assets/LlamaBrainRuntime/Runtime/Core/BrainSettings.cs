using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Core;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// Settings for the LlamaBrain server.
  /// </summary>
  [CreateAssetMenu(menuName = "LlamaBrain/BrainSettings")]
  public class BrainSettings : ScriptableObject
  {
    /// <summary>
    /// The path to the llama.cpp executable
    /// </summary>
    [Header("Server Configuration")]
    public string ExecutablePath;

    /// <summary>
    /// The path to the llama.cpp model
    /// </summary>
    public string ModelPath;

    /// <summary>
    /// The port to use for the llama.cpp server
    /// </summary>
    public int Port = 5000;

    /// <summary>
    /// The context size to use for the llama.cpp server
    /// </summary>
    public int ContextSize = 2048;

    /// <summary>
    /// Number of GPU layers to offload (0 = CPU only, 35+ = full GPU offload)
    /// CRITICAL FOR PERFORMANCE: Set to 35+ if you have an NVIDIA GPU
    /// </summary>
    [Header("Performance Settings")]
    [Tooltip("GPU layers to offload. 0=CPU only (slow), 35+=GPU accelerated (fast). Requires CUDA-enabled llama.cpp build.")]
    [Range(0, 100)]
    public int GpuLayers = 35;

    /// <summary>
    /// Number of CPU threads to use (0 = auto-detect)
    /// </summary>
    [Tooltip("CPU threads for inference. 0=auto, recommended: CPU cores - 2")]
    [Range(0, 32)]
    public int Threads = 0;

    /// <summary>
    /// Batch size for prompt processing
    /// </summary>
    [Tooltip("Batch size for prompt eval. Higher=faster but more VRAM. Default: 512")]
    [Range(128, 2048)]
    public int BatchSize = 512;

    /// <summary>
    /// Micro-batch size for generation
    /// </summary>
    [Tooltip("Micro-batch size for token generation. Default: 128")]
    [Range(32, 512)]
    public int UBatchSize = 128;

    /// <summary>
    /// Whether to lock model in RAM (prevents swapping, improves stability)
    /// </summary>
    [Tooltip("Lock model in RAM. Prevents swapping, improves performance. Recommended: enabled")]
    public bool UseMlock = true;

    /// <summary>
    /// Number of parallel request slots (1 = lowest latency, higher = more throughput)
    /// </summary>
    [Tooltip("Parallel slots. 1=lowest latency (best for single NPC), 4+=higher throughput (multiple NPCs). Default: 1")]
    [Range(1, 8)]
    public int ParallelSlots = 1;

    /// <summary>
    /// Whether to use continuous batching (helps with multiple concurrent requests)
    /// </summary>
    [Tooltip("Enable continuous batching. Helps when multiple NPCs request simultaneously. Default: off")]
    public bool UseContinuousBatching = false;

    /// <summary>
    /// The maximum number of tokens to generate
    /// </summary>
    [Header("LLM Generation Settings")]
    [Tooltip("Maximum number of tokens to generate")]
    public int MaxTokens = 64;

    /// <summary>
    /// The temperature to use for the llama.cpp server
    /// </summary>
    [Tooltip("Controls randomness in generation (0.0 = deterministic, 1.0 = very random)")]
    [Range(0.0f, 2.0f)]
    public float Temperature = 0.7f;

    /// <summary>
    /// The top-p value to use for the llama.cpp server
    /// </summary>
    [Tooltip("Controls diversity via nucleus sampling (0.0 = deterministic, 1.0 = very diverse)")]
    [Range(0.0f, 1.0f)]
    public float TopP = 0.9f;

    /// <summary>
    /// Limits the number of tokens considered for each step (0 = disabled).
    /// Higher values allow more diverse outputs but may reduce quality.
    /// </summary>
    [Tooltip("Limits the number of tokens considered for each step (0 = disabled)")]
    [Range(0, 100)]
    public int TopK = 40;

    /// <summary>
    /// Penalty for repeating tokens (1.0 = no penalty, higher values = stronger penalty).
    /// Helps prevent the model from getting stuck in repetitive loops.
    /// </summary>
    [Tooltip("Penalty for repeating tokens (1.0 = no penalty, higher values = stronger penalty)")]
    [Range(1.0f, 2.0f)]
    public float RepeatPenalty = 1.1f;

    /// <summary>
    /// Sequences that will stop generation when encountered.
    /// When the model generates any of these sequences, it will stop generating further tokens.
    /// </summary>
    [Header("Stop Sequences")]
    [Tooltip("Sequences that will stop generation when encountered")]
    public string[] StopSequences = new string[]
    {
            "</s>",
            "Human:",
            "Assistant:",
            "Player:",
            "NPC:",
            "Step 1:",
            "Step 2:",
            "Step 3:",
            "\n\n",
            "\nPlayer:",
            "\nNPC:"
    };

    /// <summary>
    /// Converts the current settings to a ProcessConfig
    /// </summary>
    /// <returns>The ProcessConfig</returns>
    public ProcessConfig ToProcessConfig()
    {
      var config = new ProcessConfig();
      config.Host = "localhost";
      config.Port = Port;
      config.Model = ModelPath ?? "";
      config.ExecutablePath = ExecutablePath ?? "";
      config.ContextSize = ContextSize;

      // Performance settings
      config.GpuLayers = GpuLayers;
      config.Threads = Threads;
      config.BatchSize = BatchSize;
      config.UBatchSize = UBatchSize;
      config.UseMlock = UseMlock;
      config.ParallelSlots = ParallelSlots;
      config.UseContinuousBatching = UseContinuousBatching;

      // Create LLM configuration from settings
      config.LlmConfig = ToLlmConfig();

      return config;
    }

    /// <summary>
    /// Creates a LlmConfig from the current settings
    /// </summary>
    /// <returns>The LLM configuration</returns>
    public LlmConfig ToLlmConfig()
    {
      return new LlmConfig
      {
        MaxTokens = MaxTokens,
        Temperature = Temperature,
        TopP = TopP,
        TopK = TopK,
        RepeatPenalty = RepeatPenalty,
        StopSequences = StopSequences
      };
    }
  }
}
