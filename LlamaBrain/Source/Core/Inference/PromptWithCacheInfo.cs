namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// Result of cache-aware prompt assembly.
  /// Splits the prompt into static (cacheable) and dynamic (per-request) sections.
  /// </summary>
  /// <remarks>
  /// The static prefix contains content that is byte-stable across requests:
  /// - System prompt
  /// - Canonical facts (world lore)
  ///
  /// The dynamic suffix contains content that varies per request:
  /// - World state (may change)
  /// - Episodic memories (varies by recency)
  /// - Dialogue history
  /// - Player input
  /// - NPC prompt
  ///
  /// When the same static prefix is used across requests, the LLM inference
  /// engine can reuse the KV cache for those tokens, significantly reducing
  /// latency (200ms cached vs 1.5s uncached for 2k+ token prefixes).
  /// </remarks>
  public sealed class PromptWithCacheInfo
  {
    /// <summary>
    /// The cacheable static prefix portion of the prompt.
    /// This content should be byte-stable across requests with the same configuration.
    /// </summary>
    public string StaticPrefix { get; }

    /// <summary>
    /// The per-request dynamic suffix portion of the prompt.
    /// This content varies based on dialogue history, player input, etc.
    /// </summary>
    public string DynamicSuffix { get; }

    /// <summary>
    /// The full assembled prompt (StaticPrefix + DynamicSuffix).
    /// </summary>
    public string FullPrompt { get; }

    /// <summary>
    /// Character count of the static prefix.
    /// </summary>
    public int StaticPrefixCharCount { get; }

    /// <summary>
    /// Character count of the dynamic suffix.
    /// </summary>
    public int DynamicSuffixCharCount { get; }

    /// <summary>
    /// Total character count of the full prompt.
    /// </summary>
    public int TotalCharCount { get; }

    /// <summary>
    /// Estimated token count for the static prefix (based on chars/token ratio).
    /// </summary>
    public int EstimatedStaticTokens { get; }

    /// <summary>
    /// Estimated token count for the dynamic suffix (based on chars/token ratio).
    /// </summary>
    public int EstimatedDynamicTokens { get; }

    /// <summary>
    /// Estimated total token count (based on chars/token ratio).
    /// </summary>
    public int EstimatedTotalTokens { get; }

    /// <summary>
    /// The boundary configuration used to determine the split point.
    /// </summary>
    public StaticPrefixBoundary Boundary { get; }

    /// <summary>
    /// Whether the prompt was truncated during assembly.
    /// </summary>
    public bool WasTruncated { get; }

    /// <summary>
    /// The underlying assembled prompt with section breakdown.
    /// </summary>
    public AssembledPrompt AssembledPrompt { get; }

    /// <summary>
    /// Creates a new PromptWithCacheInfo.
    /// </summary>
    /// <param name="staticPrefix">The cacheable static prefix</param>
    /// <param name="dynamicSuffix">The per-request dynamic suffix</param>
    /// <param name="boundary">The boundary configuration used</param>
    /// <param name="charsPerToken">Characters per token ratio for estimation</param>
    /// <param name="wasTruncated">Whether truncation occurred</param>
    /// <param name="assembledPrompt">The underlying assembled prompt</param>
    public PromptWithCacheInfo(
        string staticPrefix,
        string dynamicSuffix,
        StaticPrefixBoundary boundary,
        float charsPerToken,
        bool wasTruncated,
        AssembledPrompt assembledPrompt)
    {
      StaticPrefix = staticPrefix ?? "";
      DynamicSuffix = dynamicSuffix ?? "";
      FullPrompt = StaticPrefix + DynamicSuffix;
      Boundary = boundary;
      WasTruncated = wasTruncated;
      AssembledPrompt = assembledPrompt;

      StaticPrefixCharCount = StaticPrefix.Length;
      DynamicSuffixCharCount = DynamicSuffix.Length;
      TotalCharCount = FullPrompt.Length;

      EstimatedStaticTokens = EstimateTokens(StaticPrefixCharCount, charsPerToken);
      EstimatedDynamicTokens = EstimateTokens(DynamicSuffixCharCount, charsPerToken);
      EstimatedTotalTokens = EstimateTokens(TotalCharCount, charsPerToken);
    }

    private static int EstimateTokens(int chars, float charsPerToken)
    {
      if (charsPerToken <= 0) return 0;
      return (int)System.Math.Ceiling(chars / charsPerToken);
    }

    /// <summary>
    /// Returns a summary of the cache info.
    /// </summary>
    /// <returns>A formatted string containing cache information statistics.</returns>
    public override string ToString()
    {
      return $"PromptWithCacheInfo[Static={StaticPrefixCharCount}chars/{EstimatedStaticTokens}tok, " +
             $"Dynamic={DynamicSuffixCharCount}chars/{EstimatedDynamicTokens}tok, " +
             $"Boundary={Boundary}, Truncated={WasTruncated}]";
    }
  }
}
