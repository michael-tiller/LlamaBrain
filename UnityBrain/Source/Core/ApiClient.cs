using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// API client for the LLM API
/// </summary>
namespace UnityBrain.Core
{
  /// <summary>
  /// API client for the LLM API
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
    /// Constructor
    /// </summary>
    /// <param name="host">The host of the API</param>
    /// <param name="port">The port of the API</param>
    /// <param name="model">The model to use</param>
    public ApiClient(string host, int port, string model)
    {
      _endpoint = $"http://{host}:{port}/api/generate";
      _model = model;
    }

    /// <summary>
    /// Send a prompt to the API
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="maxTokens">The maximum number of tokens to generate</param>
    /// <param name="temperature">The temperature to use</param>
    public async Task<string> SendPromptAsync(string prompt, int maxTokens = 128, float temperature = 0.7f)
    {
      var req = new GenerateRequest
      {
        model = _model,
        prompt = prompt,
        options = new Options
        {
          num_predict = maxTokens,
          temperature = temperature
        }
      };

      var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
      using var client = new HttpClient();
      var resp = await client.PostAsync(_endpoint, content);
      resp.EnsureSuccessStatusCode();
      var respJson = await resp.Content.ReadAsStringAsync();
      var response = JsonConvert.DeserializeObject<GenerateResponse>(respJson);
      return response?.response ?? string.Empty;
    }
  }
}