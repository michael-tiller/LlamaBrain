using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityBrain.Utilities;

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
        var url = $"http://{_config.Host}:{_config.Port}/api/tags";
        var response = await _httpClient.GetAsync(url, token);
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
      Logger.Info("[Process] Waiting for process to be ready...");

      for (int i = 0; i < 30; i++) // Wait up to 15 seconds
      {
        if (token.IsCancellationRequested) break;

        if (await IsRunningAsync(token))
        {
          Logger.Info("[Process] Ready!");
          return;
        }

        await Task.Delay(500, token);
      }

      throw new InvalidOperationException("Process is not running. Please start it first.");
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