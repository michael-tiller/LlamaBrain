using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Newtonsoft.Json;
using System;
using System.Linq;

/// <summary>
/// API client for the llama.cpp API
/// </summary>
namespace LlamaBrain.Core
{
  /// <summary>
  /// API client for the llama.cpp API
  /// </summary>
  public sealed class ApiClient : IDisposable
  {
    /// <summary>
    /// The endpoint of the API
    /// </summary>
    private readonly string endpoint;
    /// <summary>
    /// The model to use
    /// </summary>
    private readonly string model;
    /// <summary>
    /// The LLM configuration
    /// </summary>
    private readonly LlmConfig config;
    /// <summary>
    /// HTTP client for making requests
    /// </summary>
    private readonly HttpClient httpClient;
    /// <summary>
    /// Rate limiting semaphore
    /// </summary>
    private readonly SemaphoreSlim rateLimiter;
    /// <summary>
    /// Request history for rate limiting
    /// </summary>
    private readonly ConcurrentQueue<DateTime> requestHistory;
    /// <summary>
    /// Maximum requests per minute
    /// </summary>
    private const int MaxRequestsPerMinute = 60;
    /// <summary>
    /// Maximum prompt length in characters
    /// </summary>
    private const int MaxPromptLength = 10000;
    /// <summary>
    /// Maximum response length in characters
    /// </summary>
    private const int MaxResponseLength = 50000;
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    private const int RequestTimeoutSeconds = 30;
    /// <summary>
    /// Whether the client has been disposed
    /// </summary>
    private bool disposed = false;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="host">The host of the API</param>
    /// <param name="port">The port of the API</param>
    /// <param name="model">The model to use</param>
    /// <param name="config">The LLM configuration (optional)</param>
    public ApiClient(string host, int port, string model, LlmConfig? config = null)
    {
      // Input validation
      if (string.IsNullOrWhiteSpace(host))
        throw new ArgumentException("Host cannot be null or empty", nameof(host));

      if (port <= 0 || port > 65535)
        throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

      if (string.IsNullOrWhiteSpace(model))
        throw new ArgumentException("Model cannot be null or empty", nameof(model));

      // Sanitize host input
      host = SanitizeHost(host);

      endpoint = $"http://{host}:{port}/completion";
      this.model = model;
      this.config = ValidateAndSanitizeConfig(config ?? new LlmConfig());

      // Initialize HTTP client with timeout
      httpClient = new HttpClient
      {
        Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds)
      };

      // Initialize rate limiting
      rateLimiter = new SemaphoreSlim(1, 1);
      requestHistory = new ConcurrentQueue<DateTime>();
    }

    /// <summary>
    /// Send a prompt to the API
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="maxTokens">The maximum number of tokens to generate (overrides config if specified)</param>
    /// <param name="temperature">The temperature to use (overrides config if specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<string> SendPromptAsync(string prompt, int? maxTokens = null, float? temperature = null, CancellationToken cancellationToken = default)
    {
      // Check if disposed
      if (disposed)
        throw new ObjectDisposedException(nameof(ApiClient));

      try
      {
        // Input validation
        if (string.IsNullOrWhiteSpace(prompt))
          return "Error: Prompt cannot be null or empty";

        if (prompt.Length > MaxPromptLength)
          return $"Error: Prompt too long. Maximum length is {MaxPromptLength} characters";

        // Rate limiting
        await EnforceRateLimitAsync();

        // Validate and sanitize parameters
        var validatedMaxTokens = ValidateMaxTokens(maxTokens ?? config.MaxTokens);
        var validatedTemperature = ValidateTemperature(temperature ?? config.Temperature);

        var req = new CompletionRequest
        {
          prompt = SanitizePrompt(prompt),
          n_predict = validatedMaxTokens,
          temperature = validatedTemperature,
          top_p = config.TopP,
          top_k = config.TopK,
          repeat_penalty = config.RepeatPenalty,
          stop = new string[] { "</s>" } // Only keep the most basic stop sequence
        };

        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

        // Add request to history for rate limiting
        requestHistory.Enqueue(DateTime.UtcNow);

        // Clean old requests from history
        CleanupRequestHistory();

        var resp = await httpClient.PostAsync(endpoint, content, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
          var errorContent = await resp.Content.ReadAsStringAsync();
          return $"Error: HTTP {resp.StatusCode} - {errorContent}";
        }

        var respJson = await resp.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(respJson))
          return "Error: Empty response from server";

        var response = JsonConvert.DeserializeObject<CompletionResponse>(respJson);

        if (response?.content == null)
          return "Error: Invalid response format from server";

        // Validate response length
        if (response.content.Length > MaxResponseLength)
        {
          response.content = response.content.Substring(0, MaxResponseLength) + "... [truncated]";
        }

        return response.content;
      }
      catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return "Error: Request was cancelled";
      }
      catch (TaskCanceledException)
      {
        return "Error: Request timed out";
      }
      catch (HttpRequestException ex)
      {
        return $"Error: Network error - {ex.Message}";
      }
      catch (JsonException ex)
      {
        return $"Error: Invalid JSON response - {ex.Message}";
      }
      catch (System.Exception ex)
      {
        return $"Error: Unexpected error - {ex.Message}";
      }
    }

    /// <summary>
    /// Enforce rate limiting
    /// </summary>
    private async Task EnforceRateLimitAsync()
    {
      await rateLimiter.WaitAsync();
      try
      {
        var now = DateTime.UtcNow;
        var oneMinuteAgo = now.AddMinutes(-1);

        // Count requests in the last minute
        var recentRequests = requestHistory.ToArray().Count(x => x > oneMinuteAgo);

        if (recentRequests >= MaxRequestsPerMinute)
        {
          var oldestRequest = requestHistory.ToArray().Where(x => x > oneMinuteAgo).Min();
          var waitTime = oldestRequest.AddMinutes(1) - now;
          if (waitTime > TimeSpan.Zero)
          {
            await Task.Delay(waitTime);
          }
        }
      }
      finally
      {
        rateLimiter.Release();
      }
    }

    /// <summary>
    /// Clean up old requests from history
    /// </summary>
    private void CleanupRequestHistory()
    {
      var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
      var tempQueue = new ConcurrentQueue<DateTime>();

      while (requestHistory.TryDequeue(out var requestTime))
      {
        if (requestTime > oneMinuteAgo)
        {
          tempQueue.Enqueue(requestTime);
        }
      }

      // Restore recent requests
      while (tempQueue.TryDequeue(out var requestTime))
      {
        requestHistory.Enqueue(requestTime);
      }
    }

    /// <summary>
    /// Validate and sanitize configuration
    /// </summary>
    private LlmConfig ValidateAndSanitizeConfig(LlmConfig config)
    {
      if (config == null)
        throw new ArgumentNullException(nameof(config));

      // Validate and clamp values to safe ranges
      config.MaxTokens = Math.Max(1, Math.Min(config.MaxTokens, 2048));
      config.Temperature = Math.Max(0.0f, Math.Min(config.Temperature, 2.0f));
      config.TopP = Math.Max(0.0f, Math.Min(config.TopP, 1.0f));
      config.TopK = Math.Max(1, Math.Min(config.TopK, 100));
      config.RepeatPenalty = Math.Max(0.0f, Math.Min(config.RepeatPenalty, 2.0f));

      return config;
    }

    /// <summary>
    /// Validate max tokens parameter
    /// </summary>
    private int ValidateMaxTokens(int maxTokens)
    {
      return Math.Max(1, Math.Min(maxTokens, 2048));
    }

    /// <summary>
    /// Validate temperature parameter
    /// </summary>
    private float ValidateTemperature(float temperature)
    {
      return Math.Max(0.0f, Math.Min(temperature, 2.0f));
    }

    /// <summary>
    /// Sanitize host input
    /// </summary>
    private string SanitizeHost(string host)
    {
      // Remove any protocol prefixes
      host = host.Replace("http://", "").Replace("https://", "");

      // Remove any path components
      var slashIndex = host.IndexOf('/');
      if (slashIndex >= 0)
        host = host.Substring(0, slashIndex);

      // Remove any port numbers (we handle port separately)
      var colonIndex = host.LastIndexOf(':');
      if (colonIndex >= 0)
        host = host.Substring(0, colonIndex);

      return host.Trim();
    }

    /// <summary>
    /// Sanitize prompt input
    /// </summary>
    private string SanitizePrompt(string prompt)
    {
      if (string.IsNullOrEmpty(prompt))
        return string.Empty;

      // Remove null characters and other potentially dangerous characters
      var sanitized = prompt.Replace("\0", "")
                           .Replace("\r", "")
                           .Trim();

      // Limit length
      if (sanitized.Length > MaxPromptLength)
        sanitized = sanitized.Substring(0, MaxPromptLength);

      return sanitized;
    }

    /// <summary>
    /// Dispose the client
    /// </summary>
    public void Dispose()
    {
      if (!disposed)
      {
        httpClient?.Dispose();
        rateLimiter?.Dispose();
        disposed = true;
      }
    }
  }
}