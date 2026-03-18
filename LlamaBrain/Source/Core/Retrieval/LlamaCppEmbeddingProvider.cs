using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Embedding provider using llama.cpp's OpenAI-compatible /v1/embeddings endpoint.
    /// Requires llama.cpp server running with an embedding model loaded.
    /// </summary>
    /// <remarks>
    /// Common embedding models: nomic-embed-text, all-MiniLM-L6-v2, bge-small-en-v1.5
    ///
    /// Start llama.cpp with: ./server -m your-embedding-model.gguf --port 8080 --embedding
    ///
    /// Test endpoint: curl http://localhost:8080/v1/embeddings -X POST -d '{"input":"test"}'
    /// </remarks>
    public sealed class LlamaCppEmbeddingProvider : IEmbeddingProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly string _baseUrl;
        private readonly string _modelName;
        private readonly int _embeddingDimension;
        private readonly TimeSpan _timeout;
        private bool _disposed;
        private bool? _isAvailable;

        /// <summary>
        /// Optional logging callback for debugging.
        /// </summary>
        public Action<string>? OnLog { get; set; }

        /// <inheritdoc />
        public int EmbeddingDimension => _embeddingDimension;

        /// <inheritdoc />
        public bool IsAvailable
        {
            get
            {
                // Cache the availability check result
                if (_isAvailable.HasValue)
                    return _isAvailable.Value;

                // Lazy check - will be set on first actual request
                // For now, assume available until proven otherwise
                return true;
            }
        }

        /// <summary>
        /// Creates a new llama.cpp embedding provider.
        /// </summary>
        /// <param name="baseUrl">Base URL of the llama.cpp server (e.g., "http://localhost:8080").</param>
        /// <param name="embeddingDimension">Expected embedding dimension from your model (e.g., 384, 768, 1536).</param>
        /// <param name="modelName">Optional model name for the request (some servers ignore this).</param>
        /// <param name="timeout">Request timeout. Default is 30 seconds.</param>
        public LlamaCppEmbeddingProvider(
            string baseUrl = "http://localhost:8080",
            int embeddingDimension = 384,
            string modelName = "nomic-embed-text",
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));
            if (embeddingDimension < 1)
                throw new ArgumentOutOfRangeException(nameof(embeddingDimension), "Embedding dimension must be at least 1");

            _baseUrl = baseUrl.TrimEnd('/');
            _embeddingDimension = embeddingDimension;
            _modelName = modelName ?? "nomic-embed-text";
            _timeout = timeout ?? TimeSpan.FromSeconds(30);

            _httpClient = new HttpClient
            {
                Timeout = _timeout
            };
            _ownsHttpClient = true;
        }

        /// <summary>
        /// Creates a new llama.cpp embedding provider with custom HttpClient.
        /// Useful for testing with mocked clients.
        /// Note: The provided HttpClient will NOT be disposed by this class.
        /// </summary>
        internal LlamaCppEmbeddingProvider(
            HttpClient httpClient,
            string baseUrl,
            int embeddingDimension,
            string modelName)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _embeddingDimension = embeddingDimension;
            _modelName = modelName ?? "nomic-embed-text";
            _timeout = httpClient.Timeout;
            _ownsHttpClient = false; // External HttpClient - caller owns it
        }

        /// <inheritdoc />
        public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
            {
                OnLog?.Invoke("[LlamaCppEmbedding] Empty text provided, returning null");
                return null;
            }

            try
            {
                var request = new EmbeddingRequest
                {
                    Input = text,
                    Model = _modelName
                };

                var jsonContent = JsonConvert.SerializeObject(request);
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/v1/embeddings",
                    content,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    OnLog?.Invoke($"[LlamaCppEmbedding] Request failed: {response.StatusCode} - {errorBody}");
                    _isAvailable = false;
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<EmbeddingResponse>(responseBody);

                if (result?.Data == null || result.Data.Length == 0 || result.Data[0].Embedding == null)
                {
                    OnLog?.Invoke("[LlamaCppEmbedding] Response had no embedding data");
                    return null;
                }

                var embedding = result.Data[0].Embedding;

                // Validate dimension
                if (embedding.Length != _embeddingDimension)
                {
                    OnLog?.Invoke($"[LlamaCppEmbedding] Dimension mismatch: expected {_embeddingDimension}, got {embedding.Length}");
                    // Still return it - the vector store will handle dimension validation
                }

                _isAvailable = true;
                return embedding;
            }
            catch (TaskCanceledException)
            {
                OnLog?.Invoke("[LlamaCppEmbedding] Request cancelled or timed out");
                return null;
            }
            catch (HttpRequestException ex)
            {
                OnLog?.Invoke($"[LlamaCppEmbedding] Connection error: {ex.Message}");
                _isAvailable = false;
                return null;
            }
            catch (JsonException ex)
            {
                OnLog?.Invoke($"[LlamaCppEmbedding] JSON parsing error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[LlamaCppEmbedding] Unexpected error: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<float[]?[]> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        {
            if (texts == null || texts.Count == 0)
            {
                return Array.Empty<float[]?>();
            }

            // Try batch request first (OpenAI-compatible format)
            var batchResult = await TryBatchRequestAsync(texts, cancellationToken);
            if (batchResult != null)
            {
                return batchResult;
            }

            // Fall back to sequential requests if batch not supported
            OnLog?.Invoke("[LlamaCppEmbedding] Batch request failed, falling back to sequential");
            var results = new float[]?[texts.Count];
            for (int i = 0; i < texts.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                results[i] = await GenerateEmbeddingAsync(texts[i], cancellationToken);
            }
            return results;
        }

        private async Task<float[]?[]?> TryBatchRequestAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
        {
            try
            {
                // OpenAI batch format: input is an array of strings
                var request = new BatchEmbeddingRequest
                {
                    Input = new List<string>(texts),
                    Model = _modelName
                };

                var jsonContent = JsonConvert.SerializeObject(request);
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/v1/embeddings",
                    content,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return null; // Will fall back to sequential
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<EmbeddingResponse>(responseBody);

                if (result?.Data == null || result.Data.Length != texts.Count)
                {
                    return null; // Will fall back to sequential
                }

                var embeddings = new float[]?[texts.Count];
                for (int i = 0; i < result.Data.Length; i++)
                {
                    embeddings[i] = result.Data[i].Embedding;
                }

                _isAvailable = true;
                return embeddings;
            }
            catch
            {
                return null; // Will fall back to sequential
            }
        }

        /// <summary>
        /// Tests connectivity to the embedding server.
        /// </summary>
        /// <returns>True if server is reachable and responds correctly.</returns>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await GenerateEmbeddingAsync("test", cancellationToken);
                _isAvailable = result != null;
                return _isAvailable.Value;
            }
            catch
            {
                _isAvailable = false;
                return false;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_ownsHttpClient)
                {
                    _httpClient.Dispose();
                }
                _disposed = true;
            }
        }

        #region Request/Response DTOs

        private class EmbeddingRequest
        {
            [JsonProperty("input")]
            public string Input { get; set; } = "";

            [JsonProperty("model")]
            public string Model { get; set; } = "";
        }

        private class BatchEmbeddingRequest
        {
            [JsonProperty("input")]
            public List<string> Input { get; set; } = new List<string>();

            [JsonProperty("model")]
            public string Model { get; set; } = "";
        }

        private class EmbeddingResponse
        {
            [JsonProperty("object")]
            public string Object { get; set; } = "";

            [JsonProperty("data")]
            public EmbeddingData[] Data { get; set; } = Array.Empty<EmbeddingData>();

            [JsonProperty("model")]
            public string Model { get; set; } = "";

            [JsonProperty("usage")]
            public UsageInfo? Usage { get; set; }
        }

        private class EmbeddingData
        {
            [JsonProperty("object")]
            public string Object { get; set; } = "";

            [JsonProperty("embedding")]
            public float[] Embedding { get; set; } = Array.Empty<float>();

            [JsonProperty("index")]
            public int Index { get; set; }
        }

        private class UsageInfo
        {
            [JsonProperty("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonProperty("total_tokens")]
            public int TotalTokens { get; set; }
        }

        #endregion
    }
}
