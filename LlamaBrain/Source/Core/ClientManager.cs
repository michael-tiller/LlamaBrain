using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlamaBrain.Utilities;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Manager for the API client
  /// </summary>
  public sealed class ClientManager : IDisposable
  {
    /// <summary>
    /// The configuration for the process
    /// </summary>
    private readonly ProcessConfig config;
    /// <summary>
    /// The HTTP client
    /// </summary>
    private readonly HttpClient httpClient;
    /// <summary>
    /// Maximum number of retry attempts when waiting for server
    /// </summary>
    private readonly int maxRetries;
    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    private readonly int retryDelayMs;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="config">The configuration for the process</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 60)</param>
    /// <param name="retryDelayMs">Delay between retries in milliseconds (default: 500)</param>
    public ClientManager(ProcessConfig config, int maxRetries = 60, int retryDelayMs = 500)
    {
      this.config = config;
      this.maxRetries = maxRetries;
      this.retryDelayMs = retryDelayMs;
      httpClient = new HttpClient();
    }

    /// <summary>
    /// Create a new API client
    /// </summary>
    /// <returns>The API client</returns>
    public ApiClient CreateClient()
    {
      return new ApiClient(config.Host, config.Port, config.Model, config.LlmConfig);
    }

    /// <summary>
    /// Check if the process is running
    /// </summary>
    /// <param name="token">The cancellation token</param>
    /// <returns>True if the process is running, false otherwise</returns>
    public async Task<bool> IsRunningAsync(CancellationToken token = default)
    {
      try
      {
        var url = $"http://{config.Host}:{config.Port}/completion";

        // Send a minimal POST request to test if the server is ready
        var testRequest = new CompletionRequest
        {
          prompt = "test",
          n_predict = 1,
          stream = false
        };

        var content = new StringContent(
          Newtonsoft.Json.JsonConvert.SerializeObject(testRequest),
          System.Text.Encoding.UTF8,
          "application/json"
        );

        var response = await httpClient.PostAsync(url, content, token);
        return response.IsSuccessStatusCode;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Wait for the process to be ready
    /// </summary>
    /// <param name="token">The cancellation token</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task WaitForAsync(CancellationToken token = default)
    {
      Logger.Info("[Client] Waiting for server to be ready...");

      for (int i = 0; i < maxRetries; i++)
      {
        if (token.IsCancellationRequested) break;

        Logger.Info($"[Client] Attempt {i + 1}/{maxRetries}: Checking if server is ready...");

        if (await IsRunningAsync(token))
        {
          Logger.Info("[Client] Server is ready!");
          return;
        }

        await Task.Delay(retryDelayMs, token);
      }

      throw new InvalidOperationException("Server is not running. Please start it first.");
    }

    /// <summary>
    /// Dispose of the client manager
    /// </summary>
    public void Dispose()
    {
      httpClient?.Dispose();
    }
  }
}