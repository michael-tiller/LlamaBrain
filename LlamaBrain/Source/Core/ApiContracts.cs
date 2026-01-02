namespace LlamaBrain.Core
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
    /// <summary>
    /// Cache the prompt to reuse KV cache (for prefix caching)
    /// </summary>
    public bool cache_prompt { get; set; } = false;

    // --- Structured Output Parameters (llama.cpp native support) ---

    /// <summary>
    /// JSON schema to enforce structured output.
    /// When set, the LLM output will be constrained to match this schema.
    /// Maps to llama.cpp json_schema parameter.
    /// </summary>
    public string? json_schema { get; set; }

    /// <summary>
    /// GBNF grammar to constrain output format.
    /// More flexible than json_schema, can enforce non-JSON formats.
    /// Maps to llama.cpp grammar parameter.
    /// </summary>
    public string? grammar { get; set; }

    /// <summary>
    /// Response format specification for JSON mode.
    /// When set to json_object type, forces valid JSON output.
    /// Maps to llama.cpp response_format parameter.
    /// </summary>
    public ResponseFormat? response_format { get; set; }
  }

  /// <summary>
  /// Response format specification for llama.cpp structured output.
  /// Used with the response_format parameter to request JSON mode.
  /// </summary>
  public sealed class ResponseFormat
  {
    /// <summary>
    /// The type of response format. Use "json_object" for JSON mode.
    /// </summary>
    public string type { get; set; } = "json_object";

    /// <summary>
    /// Optional JSON schema for the response format.
    /// Some providers support embedding the schema in response_format.
    /// </summary>
    public object? schema { get; set; }

    /// <summary>
    /// Creates a ResponseFormat for JSON object mode.
    /// </summary>
    public static ResponseFormat JsonObject => new ResponseFormat { type = "json_object" };

    /// <summary>
    /// Creates a ResponseFormat with an embedded schema.
    /// </summary>
    /// <param name="schema">The JSON schema object to embed in the response format.</param>
    /// <returns>A new ResponseFormat instance configured for JSON object mode with the specified schema.</returns>
    public static ResponseFormat WithSchema(object schema)
    {
      return new ResponseFormat
      {
        type = "json_object",
        schema = schema
      };
    }
  }

  /// <summary>
  /// Timing information from the llama.cpp API response
  /// Field names match llama.cpp server JSON output format
  /// </summary>
  public sealed class Timings
  {
    // --- Prompt evaluation (prefill) ---
    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    public int prompt_n { get; set; }
    /// <summary>
    /// Prompt processing time in milliseconds (prefill time)
    /// </summary>
    public double prompt_ms { get; set; }
    /// <summary>
    /// Prompt processing time per token in milliseconds
    /// </summary>
    public double prompt_per_token_ms { get; set; }
    /// <summary>
    /// Prompt processing speed (tokens per second)
    /// </summary>
    public double prompt_per_second { get; set; }

    // --- Token generation (decode) ---
    /// <summary>
    /// Number of tokens generated
    /// </summary>
    public int predicted_n { get; set; }
    /// <summary>
    /// Token generation time in milliseconds (decode time)
    /// </summary>
    public double predicted_ms { get; set; }
    /// <summary>
    /// Token generation time per token in milliseconds
    /// </summary>
    public double predicted_per_token_ms { get; set; }
    /// <summary>
    /// Token generation speed (tokens per second)
    /// </summary>
    public double predicted_per_second { get; set; }
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

  /// <summary>
  /// Detailed performance metrics from a completion request
  /// </summary>
  public sealed class CompletionMetrics
  {
    /// <summary>
    /// The generated response content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    public int PromptTokenCount { get; set; }
    /// <summary>
    /// Prefill time in milliseconds (prompt evaluation)
    /// </summary>
    public long PrefillTimeMs { get; set; }
    /// <summary>
    /// Decode time in milliseconds (token generation)
    /// </summary>
    public long DecodeTimeMs { get; set; }
    /// <summary>
    /// Time to first token (TTFT) in milliseconds
    /// </summary>
    public long TtftMs { get; set; }
    /// <summary>
    /// Number of tokens generated
    /// </summary>
    public int GeneratedTokenCount { get; set; }
    /// <summary>
    /// Number of tokens cached (from KV cache reuse)
    /// </summary>
    public int CachedTokenCount { get; set; }
    /// <summary>
    /// Total time in milliseconds
    /// </summary>
    public long TotalTimeMs { get; set; }
    /// <summary>
    /// Tokens per second (decode speed)
    /// </summary>
    public double TokensPerSecond => DecodeTimeMs > 0 ? (GeneratedTokenCount * 1000.0) / DecodeTimeMs : 0;
  }
}