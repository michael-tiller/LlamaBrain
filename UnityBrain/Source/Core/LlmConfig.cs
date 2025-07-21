namespace UnityBrain.Core
{
  /// <summary>
  /// Configuration for LLM generation parameters
  /// </summary>
  public class LlmConfig
  {
    /// <summary>
    /// The maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; set; } = 64;
    /// <summary>
    /// The temperature of the LLM
    /// </summary>
    public float Temperature { get; set; } = 0.7f;
    /// <summary>
    /// The top-p value for the LLM
    /// </summary>
    public float TopP { get; set; } = 0.9f;
    /// <summary>
    /// The top-k value for the LLM
    /// </summary>
    public int TopK { get; set; } = 40;
    /// <summary>
    /// The repeat penalty for the LLM
    /// </summary>
    public float RepeatPenalty { get; set; } = 1.1f;
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
  }
}