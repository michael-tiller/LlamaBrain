using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityBrain.Utilities;
using System.Text;
using Newtonsoft.Json;

namespace UnityBrain.Core
{
  /// <summary>
  /// Manager for the API client
  /// </summary>
  public sealed class ClientManager : IDisposable
  {
    /// <summary>
    /// The configuration for the process
    /// </summary>
    private readonly ProcessConfig _config;
    /// <summary>
    /// The HTTP client
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="config">The configuration for the process</param>
    public ClientManager(ProcessConfig config)
    {
      _config = config;
      _httpClient = new HttpClient();
    }

    /// <summary>
    /// Create a new API client
    /// </summary>
    /// <returns>The API client</returns>
    public ApiClient CreateClient()
    {
      return new ApiClient(_config.Host, _config.Port, _config.Model);
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
        var url = $"http://{_config.Host}:{_config.Port}/completion";

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

        var response = await _httpClient.PostAsync(url, content, token);
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
    public async Task WaitForAsync(CancellationToken token = default)
    {
      Logger.Info("[Client] Waiting for server to be ready...");

      for (int i = 0; i < 60; i++) // Wait up to 30 seconds (increased from 15)
      {
        if (token.IsCancellationRequested) break;

        Logger.Info($"[Client] Attempt {i + 1}/60: Checking if server is ready...");

        if (await IsRunningAsync(token))
        {
          Logger.Info("[Client] Server is ready!");
          return;
        }

        await Task.Delay(500, token);
      }

      throw new InvalidOperationException("Server is not running. Please start it first.");
    }

    /// <summary>
    /// Dispose of the client manager
    /// </summary>
    public void Dispose()
    {
      _httpClient?.Dispose();
    }
  }
}