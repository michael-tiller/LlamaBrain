// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Michael Tiller and contributors

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using LlamaBrain.Utilities;
using LlamaBrain.Core.StructuredOutput;

/// <summary>
/// API client for the llama.cpp API
/// </summary>
namespace LlamaBrain.Core
{
  /// <summary>
  /// API client for the llama.cpp API
  /// </summary>
  public sealed class ApiClient : IApiClient, IDisposable
  {
    /// <summary>
    /// Event raised when performance metrics are available (for Unity/DLL integration)
    /// </summary>
    public event Action<CompletionMetrics>? OnMetricsAvailable;

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
    /// Default request timeout in seconds
    /// </summary>
    private const int DefaultRequestTimeoutSeconds = 30;
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    private readonly int requestTimeoutSeconds;
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
    /// <param name="requestTimeoutSeconds">Request timeout in seconds (default: 30)</param>
    public ApiClient(string host, int port, string model, LlmConfig? config = null, int requestTimeoutSeconds = DefaultRequestTimeoutSeconds)
      : this(host, port, model, config, requestTimeoutSeconds, null)
    {
    }

    /// <summary>
    /// Internal constructor for testing with custom HttpClient
    /// </summary>
    internal ApiClient(string host, int port, string model, LlmConfig? config, int requestTimeoutSeconds, HttpClient? httpClient)
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

      // Use 127.0.0.1 instead of localhost to avoid DNS resolution delays on Windows
      var resolvedHost = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : host;
      endpoint = $"http://{resolvedHost}:{port}/completion";
      this.model = model;
      this.config = ValidateAndSanitizeConfig(config ?? new LlmConfig());
      this.requestTimeoutSeconds = requestTimeoutSeconds > 0 ? requestTimeoutSeconds : DefaultRequestTimeoutSeconds;

      if (httpClient != null)
      {
        // Use provided HttpClient (for testing)
        this.httpClient = httpClient;
      }
      else
      {
        // Initialize HTTP client with optimized settings for low latency
        // Key optimizations:
        // 1. Disable proxy detection (major source of delays on Windows)
        // 2. Disable Expect: 100-continue header (avoids round-trip delay)
        // 3. Enable connection keep-alive for reuse
        var handler = new HttpClientHandler
        {
          UseProxy = false  // Disable proxy detection - major latency improvement
          // Note: Setting Proxy = null explicitly causes issues in Unity Mono runtime
        };

        this.httpClient = new HttpClient(handler)
        {
          Timeout = TimeSpan.FromSeconds(this.requestTimeoutSeconds)
        };

        // Disable Expect: 100-continue which causes an extra round-trip
        this.httpClient.DefaultRequestHeaders.ExpectContinue = false;
      }

      // Initialize rate limiting
      rateLimiter = new SemaphoreSlim(1, 1);
      requestHistory = new ConcurrentQueue<DateTime>();
    }

    /// <summary>
    /// Send a prompt to the API and return detailed metrics
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="maxTokens">The maximum number of tokens to generate (overrides config if specified)</param>
    /// <param name="temperature">The temperature to use (overrides config if specified)</param>
    /// <param name="cachePrompt">Whether to cache the prompt for KV cache reuse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed completion metrics</returns>
    public async Task<CompletionMetrics> SendPromptWithMetricsAsync(string prompt, int? maxTokens = null, float? temperature = null, bool cachePrompt = false, CancellationToken cancellationToken = default)
    {
      // Check if disposed
      if (disposed)
        throw new ObjectDisposedException(nameof(ApiClient));

      try
      {
        // Input validation
        if (string.IsNullOrWhiteSpace(prompt))
          return new CompletionMetrics { Content = "Error: Prompt cannot be null or empty" };

        if (prompt.Length > MaxPromptLength)
          return new CompletionMetrics { Content = $"Error: Prompt too long. Maximum length is {MaxPromptLength} characters" };

        // Rate limiting
        await EnforceRateLimitAsync();

        // Validate and sanitize parameters
        var validatedMaxTokens = ValidateMaxTokens(maxTokens ?? config.MaxTokens);
        var validatedTemperature = ValidateTemperature(temperature ?? config.Temperature);

        // Build stop sequences - primarily to enforce single-line constraint
        var stopSequences = new List<string> { "</s>" };

        // NOTE: We removed newline stop sequences (\n, \r\n) because they were causing
        // the model to stop immediately when the prompt ends with a newline (common in
        // dialogue formats). Instead, we rely on:
        // 1. Post-processing to extract the first line only (ValidateAndCleanResponse)
        // 2. Token limits to prevent overly long responses
        // 3. Validation to reject multi-line outputs
        // This allows the model to generate naturally while still enforcing single-line output.

        var req = new CompletionRequest
        {
          prompt = SanitizePrompt(prompt),
          n_predict = validatedMaxTokens,
          temperature = validatedTemperature,
          top_p = config.TopP,
          top_k = config.TopK,
          repeat_penalty = config.RepeatPenalty,
          stop = stopSequences.ToArray(),
          cache_prompt = cachePrompt
        };

        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

        // Add request to history for rate limiting
        requestHistory.Enqueue(DateTime.UtcNow);

        // Clean old requests from history
        CleanupRequestHistory();

        // Measure timings at the boundary (before/after the actual API call)
        var t0 = DateTime.UtcNow;
        var tBeforePost = DateTime.UtcNow;
        var resp = await httpClient.PostAsync(endpoint, content, cancellationToken);
        var tAfterPost = DateTime.UtcNow;
        var tBeforeRead = DateTime.UtcNow;
        var respJson = await resp.Content.ReadAsStringAsync();
        var tAfterRead = DateTime.UtcNow;
        var tDone = DateTime.UtcNow;
        var totalWallTimeMs = (long)(tDone - t0).TotalMilliseconds;

        // Debug: Log detailed timing breakdown
        try
        {
          var postTime = (tAfterPost - tBeforePost).TotalMilliseconds;
          var readTime = (tAfterRead - tBeforeRead).TotalMilliseconds;
          var otherTime = totalWallTimeMs - postTime - readTime;
          Logger.Info($"[ApiClient] Timing breakdown: PostAsync={postTime:F0}ms, ReadAsString={readTime:F0}ms, Other={otherTime:F0}ms, Total={totalWallTimeMs}ms");
        }
        catch { /* Logger may not be available */ }

        if (!resp.IsSuccessStatusCode)
        {
          return new CompletionMetrics { Content = $"Error: HTTP {resp.StatusCode} - {respJson}", TotalTimeMs = totalWallTimeMs };
        }

        if (string.IsNullOrEmpty(respJson))
          return new CompletionMetrics { Content = "Error: Empty response from server", TotalTimeMs = totalWallTimeMs };

        var response = JsonConvert.DeserializeObject<CompletionResponse>(respJson);

        if (response?.content == null)
          return new CompletionMetrics { Content = "Error: Invalid response format from server", TotalTimeMs = totalWallTimeMs };

        // Extract detailed metrics - use wall time as ground truth
        var metrics = new CompletionMetrics
        {
          Content = response.content,
          CachedTokenCount = response.tokens_cached,
          TotalTimeMs = totalWallTimeMs // Always use measured wall time
        };

        // Parse llama.cpp timing data if available
        // Field names match llama.cpp server JSON: prompt_ms, predicted_ms, prompt_n, predicted_n
        if (response.timings != null)
        {
          // Prefill time (prompt evaluation) - llama.cpp uses prompt_ms
          metrics.PrefillTimeMs = (long)response.timings.prompt_ms;

          // Decode time (token generation) - llama.cpp uses predicted_ms
          metrics.DecodeTimeMs = (long)response.timings.predicted_ms;

          // Token counts - llama.cpp uses prompt_n and predicted_n
          metrics.PromptTokenCount = response.timings.prompt_n > 0
            ? response.timings.prompt_n
            : (response.tokens_evaluated > 0 ? response.tokens_evaluated : 0);

          metrics.GeneratedTokenCount = response.timings.predicted_n > 0
            ? response.timings.predicted_n
            : (response.tokens_predicted > 0 ? response.tokens_predicted : 0);

          // TTFT is approximately prefill time (time until first token starts generating)
          metrics.TtftMs = metrics.PrefillTimeMs;

          // Sanity check: if decode time is 0 but we have generated tokens, fall back to wall time
          if (metrics.DecodeTimeMs == 0 && metrics.GeneratedTokenCount > 0)
          {
            // Estimate: if we have prefill time, decode is the remainder
            if (metrics.PrefillTimeMs > 0 && metrics.PrefillTimeMs < totalWallTimeMs)
            {
              metrics.DecodeTimeMs = totalWallTimeMs - metrics.PrefillTimeMs;
            }
            else
            {
              // Rough heuristic: 70% decode, 30% prefill
              metrics.DecodeTimeMs = (long)(totalWallTimeMs * 0.7);
              metrics.PrefillTimeMs = (long)(totalWallTimeMs * 0.3);
              metrics.TtftMs = metrics.PrefillTimeMs;
            }
          }
        }
        else
        {
          // No timing data from llama.cpp - estimate from wall time
          // Rough heuristic: 30% prefill, 70% decode
          metrics.PrefillTimeMs = (long)(totalWallTimeMs * 0.3);
          metrics.DecodeTimeMs = (long)(totalWallTimeMs * 0.7);
          metrics.TtftMs = metrics.PrefillTimeMs;

          // Estimate tokens (rough: ~4 chars per token)
          metrics.GeneratedTokenCount = response.content.Length / 4;
          metrics.PromptTokenCount = prompt.Length / 4;
        }

        // Validate response length
        if (metrics.Content.Length > MaxResponseLength)
        {
          metrics.Content = metrics.Content.Substring(0, MaxResponseLength) + "... [truncated]";
        }

        // Log detailed metrics (optional, for non-DLL contexts)
        try
        {
          Logger.Info($"[ApiClient] Generation metrics: " +
            $"Prompt={metrics.PromptTokenCount} tokens, " +
            $"Prefill={metrics.PrefillTimeMs}ms, " +
            $"Decode={metrics.DecodeTimeMs}ms, " +
            $"TTFT={metrics.TtftMs}ms, " +
            $"Generated={metrics.GeneratedTokenCount} tokens, " +
            $"Cached={metrics.CachedTokenCount} tokens, " +
            $"Speed={metrics.TokensPerSecond:F1} tokens/sec");
        }
        catch
        {
          // Logger may not be available in DLL context, ignore
        }

        // Raise event for Unity/DLL subscribers
        OnMetricsAvailable?.Invoke(metrics);

        return metrics;
      }
      catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return new CompletionMetrics { Content = "Error: Request was cancelled" };
      }
      catch (TaskCanceledException)
      {
        return new CompletionMetrics { Content = "Error: Request timed out" };
      }
      catch (HttpRequestException ex)
      {
        return new CompletionMetrics { Content = $"Error: Network error - {ex.Message}" };
      }
      catch (JsonException ex)
      {
        return new CompletionMetrics { Content = $"Error: Invalid JSON response - {ex.Message}" };
      }
      catch (System.Exception ex)
      {
        return new CompletionMetrics { Content = $"Error: Unexpected error - {ex.Message}" };
      }
    }

    /// <summary>
    /// Send a prompt to the API
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="maxTokens">The maximum number of tokens to generate (overrides config if specified)</param>
    /// <param name="temperature">The temperature to use (overrides config if specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated response text.</returns>
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
          stop = new string[] { "</s>" }, // Only keep the most basic stop sequence
          cache_prompt = false // Default to false for backward compatibility
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

        // Log detailed timing information if available (optional, for non-DLL contexts)
        // Field names match llama.cpp server JSON: prompt_ms, predicted_ms, prompt_n, predicted_n
        if (response.timings != null)
        {
          var prefillMs = (long)response.timings.prompt_ms;
          var decodeMs = (long)response.timings.predicted_ms;
          var promptTokens = response.timings.prompt_n > 0 ? response.timings.prompt_n : response.tokens_evaluated;
          var generatedTokens = response.timings.predicted_n > 0 ? response.timings.predicted_n : response.tokens_predicted;
          var ttftMs = prefillMs; // TTFT is approximately prefill time for first token

          // Try to log via Logger (works in non-DLL contexts)
          try
          {
            var speed = decodeMs > 0 ? (generatedTokens * 1000.0 / decodeMs) : 0.0;
            Logger.Info($"[ApiClient] Generation metrics: " +
              $"Prompt={promptTokens} tokens, " +
              $"Prefill={prefillMs}ms, " +
              $"Decode={decodeMs}ms, " +
              $"TTFT={ttftMs}ms, " +
              $"Generated={generatedTokens} tokens, " +
              $"Cached={response.tokens_cached} tokens, " +
              $"Speed={speed:F1} tokens/sec");
          }
          catch
          {
            // Logger may not be available in DLL context, ignore
          }
        }

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
    /// Send a prompt with structured output enforcement
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="jsonSchema">The JSON schema the response must conform to</param>
    /// <param name="format">The structured output format to use</param>
    /// <param name="maxTokens">The maximum number of tokens to generate</param>
    /// <param name="temperature">The temperature to use</param>
    /// <param name="cachePrompt">Whether to cache the prompt for KV cache reuse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The structured JSON response</returns>
    public async Task<string> SendStructuredPromptAsync(
      string prompt,
      string jsonSchema,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? maxTokens = null,
      float? temperature = null,
      bool cachePrompt = false,
      CancellationToken cancellationToken = default)
    {
      var metrics = await SendStructuredPromptWithMetricsAsync(
        prompt, jsonSchema, format, maxTokens, temperature, cachePrompt, cancellationToken);
      return metrics.Content;
    }

    /// <summary>
    /// Send a prompt with structured output enforcement and return detailed metrics
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="jsonSchema">The JSON schema the response must conform to</param>
    /// <param name="format">The structured output format to use</param>
    /// <param name="maxTokens">The maximum number of tokens to generate</param>
    /// <param name="temperature">The temperature to use</param>
    /// <param name="cachePrompt">Whether to cache the prompt for KV cache reuse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed completion metrics including the structured response</returns>
    public async Task<CompletionMetrics> SendStructuredPromptWithMetricsAsync(
      string prompt,
      string jsonSchema,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? maxTokens = null,
      float? temperature = null,
      bool cachePrompt = false,
      CancellationToken cancellationToken = default)
    {
      // Check if disposed
      if (disposed)
        throw new ObjectDisposedException(nameof(ApiClient));

      try
      {
        // Input validation
        if (string.IsNullOrWhiteSpace(prompt))
          return new CompletionMetrics { Content = "Error: Prompt cannot be null or empty" };

        if (prompt.Length > MaxPromptLength)
          return new CompletionMetrics { Content = $"Error: Prompt too long. Maximum length is {MaxPromptLength} characters" };

        if (string.IsNullOrWhiteSpace(jsonSchema))
          return new CompletionMetrics { Content = "Error: JSON schema cannot be null or empty" };

        // Validate schema
        var provider = LlamaCppStructuredOutputProvider.Instance;
        if (!provider.ValidateSchema(jsonSchema, out var schemaError))
          return new CompletionMetrics { Content = $"Error: Invalid JSON schema - {schemaError}" };

        // Rate limiting
        await EnforceRateLimitAsync();

        // Validate and sanitize parameters
        var validatedMaxTokens = ValidateMaxTokens(maxTokens ?? config.MaxTokens);
        var validatedTemperature = ValidateTemperature(temperature ?? config.Temperature);

        // Build the request with structured output parameters
        var req = new CompletionRequest
        {
          prompt = SanitizePrompt(prompt),
          n_predict = validatedMaxTokens,
          temperature = validatedTemperature,
          top_p = config.TopP,
          top_k = config.TopK,
          repeat_penalty = config.RepeatPenalty,
          stop = new string[] { "</s>" },
          cache_prompt = cachePrompt
        };

        // Apply structured output parameters based on format
        switch (format)
        {
          case StructuredOutputFormat.JsonSchema:
            req.json_schema = jsonSchema;
            break;

          case StructuredOutputFormat.Grammar:
            var parameters = provider.BuildParameters(jsonSchema, format);
            req.grammar = parameters.Grammar;
            break;

          case StructuredOutputFormat.ResponseFormat:
            req.response_format = ResponseFormat.JsonObject;
            break;

          case StructuredOutputFormat.None:
          default:
            // No structured output enforcement - will rely on prompt instructions
            break;
        }

        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

        // Add request to history for rate limiting
        requestHistory.Enqueue(DateTime.UtcNow);

        // Clean old requests from history
        CleanupRequestHistory();

        // Measure timings
        var t0 = DateTime.UtcNow;
        var resp = await httpClient.PostAsync(endpoint, content, cancellationToken);
        var respJson = await resp.Content.ReadAsStringAsync();
        var tDone = DateTime.UtcNow;
        var totalWallTimeMs = (long)(tDone - t0).TotalMilliseconds;

        if (!resp.IsSuccessStatusCode)
        {
          return new CompletionMetrics { Content = $"Error: HTTP {resp.StatusCode} - {respJson}", TotalTimeMs = totalWallTimeMs };
        }

        if (string.IsNullOrEmpty(respJson))
          return new CompletionMetrics { Content = "Error: Empty response from server", TotalTimeMs = totalWallTimeMs };

        var response = JsonConvert.DeserializeObject<CompletionResponse>(respJson);

        if (response?.content == null)
          return new CompletionMetrics { Content = "Error: Invalid response format from server", TotalTimeMs = totalWallTimeMs };

        // Build metrics
        var metrics = new CompletionMetrics
        {
          Content = response.content,
          CachedTokenCount = response.tokens_cached,
          TotalTimeMs = totalWallTimeMs
        };

        // Parse timing data if available
        if (response.timings != null)
        {
          metrics.PrefillTimeMs = (long)response.timings.prompt_ms;
          metrics.DecodeTimeMs = (long)response.timings.predicted_ms;
          metrics.PromptTokenCount = response.timings.prompt_n > 0
            ? response.timings.prompt_n
            : (response.tokens_evaluated > 0 ? response.tokens_evaluated : 0);
          metrics.GeneratedTokenCount = response.timings.predicted_n > 0
            ? response.timings.predicted_n
            : (response.tokens_predicted > 0 ? response.tokens_predicted : 0);
          metrics.TtftMs = metrics.PrefillTimeMs;
        }
        else
        {
          // Estimate from wall time
          metrics.PrefillTimeMs = (long)(totalWallTimeMs * 0.3);
          metrics.DecodeTimeMs = (long)(totalWallTimeMs * 0.7);
          metrics.TtftMs = metrics.PrefillTimeMs;
          metrics.GeneratedTokenCount = response.content.Length / 4;
          metrics.PromptTokenCount = prompt.Length / 4;
        }

        // Validate response length
        if (metrics.Content.Length > MaxResponseLength)
        {
          metrics.Content = metrics.Content.Substring(0, MaxResponseLength) + "... [truncated]";
        }

        // Log metrics
        try
        {
          Logger.Info($"[ApiClient] Structured output metrics ({format}): " +
            $"Prompt={metrics.PromptTokenCount} tokens, " +
            $"Generated={metrics.GeneratedTokenCount} tokens, " +
            $"Speed={metrics.TokensPerSecond:F1} tokens/sec");
        }
        catch
        {
          // Logger may not be available in DLL context
        }

        // Raise event for subscribers
        OnMetricsAvailable?.Invoke(metrics);

        return metrics;
      }
      catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return new CompletionMetrics { Content = "Error: Request was cancelled" };
      }
      catch (TaskCanceledException)
      {
        return new CompletionMetrics { Content = "Error: Request timed out" };
      }
      catch (HttpRequestException ex)
      {
        return new CompletionMetrics { Content = $"Error: Network error - {ex.Message}" };
      }
      catch (JsonException ex)
      {
        return new CompletionMetrics { Content = $"Error: Invalid JSON response - {ex.Message}" };
      }
      catch (System.Exception ex)
      {
        return new CompletionMetrics { Content = $"Error: Unexpected error - {ex.Message}" };
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
    internal LlmConfig ValidateAndSanitizeConfig(LlmConfig config)
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
    internal int ValidateMaxTokens(int maxTokens)
    {
      return Math.Max(1, Math.Min(maxTokens, 2048));
    }

    /// <summary>
    /// Validate temperature parameter
    /// </summary>
    internal float ValidateTemperature(float temperature)
    {
      return Math.Max(0.0f, Math.Min(temperature, 2.0f));
    }

    /// <summary>
    /// Sanitize host input
    /// </summary>
    internal string SanitizeHost(string host)
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
    internal string SanitizePrompt(string prompt)
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