using System;
using System.Threading;
using LlamaBrain.Core;

namespace LlamaBrain.Core.Metrics
{
  /// <summary>
  /// Thread-safe metrics for KV cache efficiency tracking.
  /// Tracks cache hit/miss rates and token savings from caching.
  /// </summary>
  /// <remarks>
  /// Performance targets:
  /// - Cache hit rate: greater than 80% for typical gameplay
  /// - Token savings: Reduce re-evaluation of static prefix tokens by 80%+
  /// - Latency improvement: under 200ms for cached vs under 1.5s for uncached
  /// </remarks>
  public class CacheEfficiencyMetrics
  {
    private int _totalRequests;
    private int _cacheHitRequests;
    private int _cacheMissRequests;
    private long _totalCachedTokens;
    private long _totalPromptTokens;
    private long _totalStaticPrefixTokens;

    /// <summary>
    /// Total number of requests processed.
    /// </summary>
    public int TotalRequests => _totalRequests;

    /// <summary>
    /// Number of requests where cache was hit (CachedTokenCount > 0).
    /// </summary>
    public int CacheHitRequests => _cacheHitRequests;

    /// <summary>
    /// Number of requests where cache was missed (CachedTokenCount == 0).
    /// </summary>
    public int CacheMissRequests => _cacheMissRequests;

    /// <summary>
    /// Total number of tokens that were cached across all requests.
    /// </summary>
    public long TotalCachedTokens => _totalCachedTokens;

    /// <summary>
    /// Total number of prompt tokens across all requests.
    /// </summary>
    public long TotalPromptTokens => _totalPromptTokens;

    /// <summary>
    /// Total estimated tokens in static prefixes across all requests.
    /// </summary>
    public long TotalStaticPrefixTokens => _totalStaticPrefixTokens;

    /// <summary>
    /// Cache hit rate as a percentage (0-100).
    /// Returns 0 if no requests have been made.
    /// </summary>
    public double CacheHitRate
    {
      get
      {
        var total = _totalRequests;
        if (total == 0) return 0.0;
        return ((double)_cacheHitRequests / total) * 100.0;
      }
    }

    /// <summary>
    /// Token cache rate: percentage of prompt tokens that were cached.
    /// Returns 0 if no prompt tokens have been processed.
    /// </summary>
    public double TokenCacheRate
    {
      get
      {
        var total = _totalPromptTokens;
        if (total == 0) return 0.0;
        return ((double)_totalCachedTokens / total) * 100.0;
      }
    }

    /// <summary>
    /// Static prefix cache efficiency: percentage of static prefix tokens that were cached.
    /// A value close to 100% indicates optimal cache utilization.
    /// Returns 0 if no static prefix tokens have been tracked.
    /// Capped at 100% since cached tokens may include tokens beyond the static prefix.
    /// </summary>
    public double StaticPrefixCacheEfficiency
    {
      get
      {
        var total = _totalStaticPrefixTokens;
        if (total == 0) return 0.0;
        // Cap at 100% since _totalCachedTokens may include tokens beyond the static prefix
        var cachedStaticPrefixTokens = Math.Min(_totalCachedTokens, _totalStaticPrefixTokens);
        return ((double)cachedStaticPrefixTokens / total) * 100.0;
      }
    }

    /// <summary>
    /// Records a request with its cache metrics.
    /// </summary>
    /// <param name="promptTokens">Number of tokens in the prompt</param>
    /// <param name="cachedTokens">Number of tokens that were cached (from CompletionMetrics)</param>
    /// <param name="staticPrefixTokens">Estimated tokens in the static prefix (from PromptWithCacheInfo)</param>
    public void RecordRequest(int promptTokens, int cachedTokens, int staticPrefixTokens = 0)
    {
      Interlocked.Increment(ref _totalRequests);
      Interlocked.Add(ref _totalPromptTokens, promptTokens);
      Interlocked.Add(ref _totalCachedTokens, cachedTokens);
      Interlocked.Add(ref _totalStaticPrefixTokens, staticPrefixTokens);

      if (cachedTokens > 0)
      {
        Interlocked.Increment(ref _cacheHitRequests);
      }
      else
      {
        Interlocked.Increment(ref _cacheMissRequests);
      }
    }

    /// <summary>
    /// Records a request from CompletionMetrics.
    /// </summary>
    /// <param name="metrics">The completion metrics from the API client</param>
    /// <param name="staticPrefixTokens">Optional estimated tokens in the static prefix</param>
    public void RecordRequest(CompletionMetrics metrics, int staticPrefixTokens = 0)
    {
      if (metrics == null) return;
      RecordRequest(metrics.PromptTokenCount, metrics.CachedTokenCount, staticPrefixTokens);
    }

    /// <summary>
    /// Records a cache hit explicitly.
    /// </summary>
    public void RecordCacheHit()
    {
      Interlocked.Increment(ref _totalRequests);
      Interlocked.Increment(ref _cacheHitRequests);
    }

    /// <summary>
    /// Records a cache miss explicitly.
    /// </summary>
    public void RecordCacheMiss()
    {
      Interlocked.Increment(ref _totalRequests);
      Interlocked.Increment(ref _cacheMissRequests);
    }

    /// <summary>
    /// Resets all metrics to zero.
    /// </summary>
    public void Reset()
    {
      Interlocked.Exchange(ref _totalRequests, 0);
      Interlocked.Exchange(ref _cacheHitRequests, 0);
      Interlocked.Exchange(ref _cacheMissRequests, 0);
      Interlocked.Exchange(ref _totalCachedTokens, 0);
      Interlocked.Exchange(ref _totalPromptTokens, 0);
      Interlocked.Exchange(ref _totalStaticPrefixTokens, 0);
    }

    /// <summary>
    /// Returns a summary of cache efficiency metrics.
    /// </summary>
    /// <returns>A formatted string containing cache efficiency statistics.</returns>
    public override string ToString()
    {
      return $"CacheEfficiency[Requests={TotalRequests}, " +
             $"HitRate={CacheHitRate:F1}%, " +
             $"TokenCacheRate={TokenCacheRate:F1}%, " +
             $"PrefixEfficiency={StaticPrefixCacheEfficiency:F1}%]";
    }
  }
}
