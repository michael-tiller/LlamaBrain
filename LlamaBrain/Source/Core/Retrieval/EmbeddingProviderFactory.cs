using System;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Factory for creating embedding providers from configuration.
    /// </summary>
    public static class EmbeddingProviderFactory
    {
        /// <summary>
        /// Creates an embedding provider based on the given configuration.
        /// </summary>
        /// <param name="config">The embedding configuration specifying provider type and settings.</param>
        /// <returns>An IEmbeddingProvider instance configured according to the config.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        /// <exception cref="ArgumentException">Thrown when config validation fails.</exception>
        /// <exception cref="NotSupportedException">Thrown when provider type is not supported.</exception>
        public static IEmbeddingProvider Create(EmbeddingConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Validate configuration
            var errors = config.Validate();
            if (errors.Length > 0)
            {
                throw new ArgumentException(
                    $"Invalid embedding configuration: {string.Join("; ", errors)}",
                    nameof(config));
            }

            switch (config.ProviderType)
            {
                case EmbeddingProviderType.Null:
                    return new NullEmbeddingProvider();

                case EmbeddingProviderType.LlamaCpp:
                    return new LlamaCppEmbeddingProvider(
                        baseUrl: config.LlamaCppBaseUrl,
                        embeddingDimension: config.EmbeddingDimension,
                        modelName: config.LlamaCppModelName,
                        timeout: TimeSpan.FromSeconds(config.RequestTimeoutSeconds));

                case EmbeddingProviderType.OpenAI:
                    throw new NotSupportedException(
                        "OpenAI embedding provider is not yet implemented. " +
                        "Use LlamaCpp with a local server or implement IEmbeddingProvider manually.");

                default:
                    throw new NotSupportedException(
                        $"Unknown embedding provider type: {config.ProviderType}");
            }
        }

        /// <summary>
        /// Creates a keyword-only provider (NullEmbeddingProvider).
        /// Convenience method for common case of no semantic retrieval.
        /// </summary>
        /// <returns>A NullEmbeddingProvider instance.</returns>
        public static IEmbeddingProvider CreateKeywordOnly()
        {
            return new NullEmbeddingProvider();
        }

        /// <summary>
        /// Creates a llama.cpp provider with default settings.
        /// Convenience method for common localhost setup.
        /// </summary>
        /// <param name="baseUrl">Base URL of the llama.cpp server. Defaults to "http://localhost:8080".</param>
        /// <param name="embeddingDimension">Expected embedding dimension. Defaults to 384.</param>
        /// <param name="modelName">Model name for requests. Defaults to "nomic-embed-text".</param>
        /// <returns>A LlamaCppEmbeddingProvider instance.</returns>
        public static IEmbeddingProvider CreateLlamaCpp(
            string baseUrl = "http://localhost:8080",
            int embeddingDimension = 384,
            string modelName = "nomic-embed-text")
        {
            return new LlamaCppEmbeddingProvider(baseUrl, embeddingDimension, modelName);
        }

        /// <summary>
        /// Attempts to create a provider, returning null on failure instead of throwing.
        /// Useful for graceful degradation scenarios.
        /// </summary>
        /// <param name="config">The embedding configuration.</param>
        /// <param name="provider">The created provider, or null on failure.</param>
        /// <param name="error">Error message if creation failed, or null on success.</param>
        /// <returns>True if provider was created successfully, false otherwise.</returns>
        public static bool TryCreate(EmbeddingConfig config, out IEmbeddingProvider? provider, out string? error)
        {
            provider = null;
            error = null;

            if (config == null)
            {
                error = "Configuration is null";
                return false;
            }

            var errors = config.Validate();
            if (errors.Length > 0)
            {
                error = string.Join("; ", errors);
                return false;
            }

            try
            {
                provider = Create(config);
                return true;
            }
            catch (NotSupportedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = $"Failed to create provider: {ex.Message}";
                return false;
            }
        }
    }
}
