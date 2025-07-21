namespace UnityBrain.Core
{
  /// <summary>
  /// Request for the llama.cpp HTTP server API
  /// </summary>
  public sealed class CompletionRequest
  {
    /// <summary>
    /// The prompt to send
    /// </summary>
    public string? prompt { get; set; }
    /// <summary>
    /// The number of tokens to predict
    /// </summary>
    public int n_predict { get; set; } = 128;
    /// <summary>
    /// The temperature to use
    /// </summary>
    public float temperature { get; set; } = 0.7f;
    /// <summary>
    /// The top-p value for nucleus sampling
    /// </summary>
    public float top_p { get; set; } = 0.9f;
    /// <summary>
    /// The top-k value for top-k sampling
    /// </summary>
    public int top_k { get; set; } = 40;
    /// <summary>
    /// The repeat penalty
    /// </summary>
    public float repeat_penalty { get; set; } = 1.1f;
    /// <summary>
    /// The stop sequences
    /// </summary>
    public string[]? stop { get; set; }
    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool stream { get; set; } = false;
  }

  /// <summary>
  /// Timing information from the llama.cpp API response
  /// </summary>
  public sealed class Timings
  {
    /// <summary>
    /// Prediction time in milliseconds
    /// </summary>
    public long pred_ms { get; set; }
    /// <summary>
    /// Prompt processing time in milliseconds
    /// </summary>
    public long prompt_ms { get; set; }
    /// <summary>
    /// Total time in milliseconds
    /// </summary>
    public long total_ms { get; set; }
  }

  /// <summary>
  /// Response from the llama.cpp HTTP server API
  /// </summary>
  public sealed class CompletionResponse
  {
    /// <summary>
    /// The content of the response
    /// </summary>
    public string? content { get; set; }
    /// <summary>
    /// Whether the response is done
    /// </summary>
    public bool stop { get; set; }
    /// <summary>
    /// Timing information
    /// </summary>
    public Timings? timings { get; set; }
    /// <summary>
    /// The number of tokens predicted
    /// </summary>
    public int tokens_predicted { get; set; }
    /// <summary>
    /// The number of tokens cached
    /// </summary>
    public int tokens_cached { get; set; }
    /// <summary>
    /// The number of tokens evaluated
    /// </summary>
    public int tokens_evaluated { get; set; }
  }
}