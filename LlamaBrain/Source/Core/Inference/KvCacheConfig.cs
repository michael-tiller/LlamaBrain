namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// Defines where the static prefix boundary ends for KV cache optimization.
  /// Content before this boundary is cacheable; content after is per-request.
  /// </summary>
  public enum StaticPrefixBoundary
  {
    /// <summary>
    /// Static prefix includes only the system prompt.
    /// Most conservative - only system prompt is cached.
    /// </summary>
    AfterSystemPrompt = 0,

    /// <summary>
    /// Static prefix includes system prompt and canonical facts.
    /// Recommended default - facts are immutable within a session.
    /// </summary>
    AfterCanonicalFacts = 1,

    /// <summary>
    /// Static prefix includes system prompt, canonical facts, and world state.
    /// More aggressive caching - assumes world state is stable within session.
    /// </summary>
    AfterWorldState = 2,

    /// <summary>
    /// Static prefix includes everything up to constraints.
    /// Most aggressive - only dialogue/memories/input are dynamic.
    /// </summary>
    AfterConstraints = 3
  }

  /// <summary>
  /// Configuration for KV cache optimization in prompt assembly.
  /// </summary>
  /// <remarks>
  /// Effective KV cache utilization requires architectural discipline:
  /// - Static content (system prompt, canonical facts) must come first
  /// - Dynamic content (dialogue, timestamps) must come after
  /// - The boundary between static/dynamic determines cache efficiency
  ///
  /// Performance targets:
  /// - Cache hit: &lt; 200ms response time
  /// - Cache miss: &lt; 1.5s response time
  /// - Target hit rate: &gt; 80% for typical gameplay
  /// </remarks>
  public class KvCacheConfig
  {
    /// <summary>
    /// Whether KV caching is enabled.
    /// When true, prompts will be assembled with cache-aware ordering
    /// and the cachePrompt flag will be passed to the API client.
    /// Default: false.
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// Where the static prefix boundary ends.
    /// Content before this boundary is cacheable; content after is per-request.
    /// Default: AfterCanonicalFacts.
    /// </summary>
    public StaticPrefixBoundary Boundary { get; set; } = StaticPrefixBoundary.AfterCanonicalFacts;

    /// <summary>
    /// Whether to track cache efficiency metrics.
    /// When true, metrics will be recorded for cache hit/miss rates.
    /// Default: true when caching is enabled.
    /// </summary>
    public bool TrackMetrics { get; set; } = true;

    /// <summary>
    /// Whether to validate static prefix stability.
    /// When true, warns if dynamic content appears in the static prefix.
    /// Default: true in debug builds.
    /// </summary>
    public bool ValidatePrefixStability { get; set; } =
#if DEBUG
      true;
#else
      false;
#endif

    /// <summary>
    /// Number of tokens to keep during context shift (n_keep parameter).
    /// Protects the static prefix from being evicted when the context window fills.
    /// Set to the estimated token count of your static prefix, or use
    /// PromptWithCacheInfo.EstimatedStaticTokens for dynamic calculation.
    /// null = use server default (-1, keep all), N = keep first N tokens.
    /// </summary>
    /// <remarks>
    /// When the context window fills (e.g., 4096 tokens), llama.cpp performs a context shift,
    /// discarding the oldest tokens to make room for new ones. Without n_keep protection,
    /// this evicts your static prefix—destroying cache efficiency.
    ///
    /// Set this to the token count of your "Bedrock" layer (System Prompt + Canonical Facts)
    /// to ensure it survives context shifts.
    /// </remarks>
    public int? NKeepTokens { get; set; } = null;

    /// <summary>
    /// Creates a default configuration with caching disabled.
    /// </summary>
    /// <returns>A KvCacheConfig instance with caching disabled.</returns>
    public static KvCacheConfig Disabled() => new KvCacheConfig
    {
      EnableCaching = false,
      TrackMetrics = false,
      ValidatePrefixStability = false
    };

    /// <summary>
    /// Creates a default configuration with caching enabled.
    /// Uses AfterCanonicalFacts boundary (recommended).
    /// </summary>
    /// <returns>A KvCacheConfig instance with caching enabled and default settings.</returns>
    public static KvCacheConfig Default() => new KvCacheConfig
    {
      EnableCaching = true,
      Boundary = StaticPrefixBoundary.AfterCanonicalFacts,
      TrackMetrics = true,
      ValidatePrefixStability = true
    };

    /// <summary>
    /// Creates an aggressive caching configuration.
    /// Uses AfterWorldState boundary for maximum cache efficiency.
    /// Only use when world state is stable within sessions.
    /// </summary>
    /// <returns>A KvCacheConfig instance with aggressive caching settings.</returns>
    public static KvCacheConfig Aggressive() => new KvCacheConfig
    {
      EnableCaching = true,
      Boundary = StaticPrefixBoundary.AfterWorldState,
      TrackMetrics = true,
      ValidatePrefixStability = true
    };
  }
}
