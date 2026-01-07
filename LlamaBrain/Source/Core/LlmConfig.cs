using System;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Configuration for LLM generation parameters
  /// </summary>
  public class LlmConfig
  {
    private int maxTokens = 32; // Default to 32 tokens for snappy NPC responses (24-32 token range)
    private float temperature = 0.7f;
    private float topP = 0.9f;
    private int topK = 40;
    private float repeatPenalty = 1.1f;
    private int? seed = null; // null = no default seed (use per-call seed or server default)

    /// <summary>
    /// The maximum number of tokens to generate
    /// </summary>
    public int MaxTokens
    {
      get => maxTokens;
      set => maxTokens = Math.Max(1, Math.Min(value, 2048));
    }

    /// <summary>
    /// The temperature of the LLM
    /// </summary>
    public float Temperature
    {
      get => temperature;
      set => temperature = Math.Max(0.0f, Math.Min(value, 2.0f));
    }

    /// <summary>
    /// The top-p value for the LLM
    /// </summary>
    public float TopP
    {
      get => topP;
      set => topP = Math.Max(0.0f, Math.Min(value, 1.0f));
    }

    /// <summary>
    /// The top-k value for the LLM
    /// </summary>
    public int TopK
    {
      get => topK;
      set => topK = Math.Max(1, Math.Min(value, 100));
    }

    /// <summary>
    /// The repeat penalty for the LLM
    /// </summary>
    public float RepeatPenalty
    {
      get => repeatPenalty;
      set => repeatPenalty = Math.Max(0.0f, Math.Min(value, 2.0f));
    }

    /// <summary>
    /// Default random seed for deterministic generation.
    /// -1 = random (non-deterministic), 0+ = use this exact seed.
    /// null = no default (use per-call seed or server default).
    /// Per-call seed parameters take precedence over this default.
    /// </summary>
    public int? Seed
    {
      get => seed;
      set => seed = value;
    }

    /// <summary>
    /// The stop sequences for the LLM
    /// </summary>
    public string[] StopSequences { get; set; } = new string[]
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
    /// Validates the configuration and returns any validation errors
    /// </summary>
    /// <returns>Array of validation error messages, empty if valid</returns>
    public string[] Validate()
    {
      var errors = new List<string>();

      if (MaxTokens < 1 || MaxTokens > 2048)
        errors.Add($"MaxTokens must be between 1 and 2048, got {MaxTokens}");

      if (Temperature < 0.0f || Temperature > 2.0f)
        errors.Add($"Temperature must be between 0.0 and 2.0, got {Temperature}");

      if (TopP < 0.0f || TopP > 1.0f)
        errors.Add($"TopP must be between 0.0 and 1.0, got {TopP}");

      if (TopK < 1 || TopK > 100)
        errors.Add($"TopK must be between 1 and 100, got {TopK}");

      if (RepeatPenalty < 0.0f || RepeatPenalty > 2.0f)
        errors.Add($"RepeatPenalty must be between 0.0 and 2.0, got {RepeatPenalty}");

      if (StopSequences == null)
        errors.Add("StopSequences cannot be null");

      return errors.ToArray();
    }

    /// <summary>
    /// Creates a deep copy of this configuration
    /// </summary>
    /// <returns>A new LlmConfig instance with the same values</returns>
    public LlmConfig Clone()
    {
      return new LlmConfig
      {
        MaxTokens = this.MaxTokens,
        Temperature = this.Temperature,
        TopP = this.TopP,
        TopK = this.TopK,
        RepeatPenalty = this.RepeatPenalty,
        Seed = this.Seed,
        StopSequences = this.StopSequences?.ToArray() ?? new string[0]
      };
    }
  }
}