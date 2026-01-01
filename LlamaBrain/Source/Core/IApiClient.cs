using System;
using System.Threading;
using System.Threading.Tasks;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Interface for API client to enable testing
  /// </summary>
  public interface IApiClient
  {
    /// <summary>
    /// Event raised when performance metrics are available (for Unity/DLL integration)
    /// </summary>
    event Action<CompletionMetrics>? OnMetricsAvailable;

    /// <summary>
    /// Send a prompt to the API and return detailed metrics
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="maxTokens">The maximum number of tokens to generate (overrides config if specified)</param>
    /// <param name="temperature">The temperature to use (overrides config if specified)</param>
    /// <param name="cachePrompt">Whether to cache the prompt for KV cache reuse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed completion metrics</returns>
    Task<CompletionMetrics> SendPromptWithMetricsAsync(string prompt, int? maxTokens = null, float? temperature = null, bool cachePrompt = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt to the LLM and returns the response
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="maxTokens">Optional maximum tokens</param>
    /// <param name="temperature">Optional temperature</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The response from the LLM</returns>
    Task<string> SendPromptAsync(string prompt, int? maxTokens = null, float? temperature = null, CancellationToken cancellationToken = default);
  }
}

