using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// API client for the llama.cpp API
/// </summary>
namespace UnityBrain.Core
{
  /// <summary>
  /// API client for the llama.cpp API
  /// </summary>
  public sealed class ApiClient
  {
    /// <summary>
    /// The endpoint of the API
    /// </summary>
    private readonly string _endpoint;
    /// <summary>
    /// The model to use
    /// </summary>
    private readonly string _model;
    /// <summary>
    /// The LLM configuration
    /// </summary>
    private readonly LlmConfig _config;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="host">The host of the API</param>
    /// <param name="port">The port of the API</param>
    /// <param name="model">The model to use</param>
    /// <param name="config">The LLM configuration (optional)</param>
    public ApiClient(string host, int port, string model, LlmConfig? config = null)
    {
      _endpoint = $"http://{host}:{port}/completion";
      _model = model;
      _config = config ?? new LlmConfig();
    }

    /// <summary>
    /// Send a prompt to the API
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="maxTokens">The maximum number of tokens to generate (overrides config if specified)</param>
    /// <param name="temperature">The temperature to use (overrides config if specified)</param>
    public async Task<string> SendPromptAsync(string prompt, int? maxTokens = null, float? temperature = null)
    {
      try
      {
        var req = new CompletionRequest
        {
          prompt = prompt,
          n_predict = maxTokens ?? _config.MaxTokens,
          temperature = temperature ?? _config.Temperature,
          top_p = _config.TopP,
          top_k = _config.TopK,
          repeat_penalty = _config.RepeatPenalty,
          // Temporarily disable stop sequences to test if they're causing the issue
          // stop = _config.StopSequences
          stop = new string[] { "</s>" } // Only keep the most basic stop sequence
        };

        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
        using var client = new HttpClient();

        var resp = await client.PostAsync(_endpoint, content);

        if (!resp.IsSuccessStatusCode)
        {
          var errorContent = await resp.Content.ReadAsStringAsync();
          return $"Error: HTTP {resp.StatusCode} - {errorContent}";
        }

        var respJson = await resp.Content.ReadAsStringAsync();
        var response = JsonConvert.DeserializeObject<CompletionResponse>(respJson);

        return response?.content ?? string.Empty;
      }
      catch (System.Exception ex)
      {
        return $"Error: {ex.Message}";
      }
    }
  }
}