namespace UnityBrain.Core
{
  /// <summary>
  /// Request for the API
  /// </summary>
  public sealed class GenerateRequest
  {
    /// <summary>
    /// The model to use
    /// </summary>
    public string? model { get; set; }
    /// <summary>
    /// The prompt to send
    /// </summary>
    public string? prompt { get; set; }
    /// <summary>
    /// The options for the API
    /// </summary>
    public Options options { get; set; } = new Options();
    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool stream { get; set; } = false;
  }

  /// <summary>
  /// Options for the API
  /// </summary>
  public sealed class Options
  {
    /// <summary>
    /// The number of tokens to predict
    /// </summary>
    public int num_predict { get; set; } = 128;
    /// <summary>
    /// The temperature to use
    /// </summary>
    public float temperature { get; set; } = 0.7f;
  }

  /// <summary>
  /// Response from the API
  /// </summary>
  public sealed class GenerateResponse
  {
    /// <summary>
    /// The model that generated the response
    /// </summary>
    public string? model { get; set; }
    /// <summary>
    /// The response from the API
    /// </summary>
    public string? response { get; set; }
    /// <summary>
    /// Whether the response is done
    /// </summary>
    public bool done { get; set; }
  }
}