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

