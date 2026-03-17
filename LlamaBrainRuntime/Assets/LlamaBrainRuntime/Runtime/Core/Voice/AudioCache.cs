using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LlamaBrain.Runtime.Core.Voice
{
  /// <summary>
  /// LRU cache for generated TTS audio data.
  /// Caches float[] audio samples by text + voice configuration hash to avoid redundant inference.
  /// Thread-safe for concurrent access.
  /// </summary>
  public sealed class AudioCache : IDisposable
  {
    /// <summary>
    /// Statistics about audio cache usage.
    /// </summary>
    public readonly struct AudioCacheStatistics
    {
      /// <summary>Number of entries currently in the cache.</summary>
      public int EntryCount { get; init; }

      /// <summary>Maximum cache size in bytes.</summary>
      public long MaxSizeBytes { get; init; }

      /// <summary>Current cache size in bytes.</summary>
      public long CurrentSizeBytes { get; init; }

      /// <summary>Number of cache hits.</summary>
      public long HitCount { get; init; }

      /// <summary>Number of cache misses.</summary>
      public long MissCount { get; init; }

      /// <summary>Number of entries evicted due to size limit.</summary>
      public long EvictionCount { get; init; }

      /// <summary>Cache hit rate as a ratio (0.0 to 1.0).</summary>
      public float HitRate
      {
        get
        {
          var total = HitCount + MissCount;
          return total > 0 ? (float)HitCount / total : 0f;
        }
      }

      /// <inheritdoc/>
      public override string ToString()
      {
        return $"AudioCache: {EntryCount} entries, " +
               $"{CurrentSizeBytes / 1024 / 1024:F1}/{MaxSizeBytes / 1024 / 1024:F1} MB, " +
               $"Hit rate: {HitRate:P1}, " +
               $"Evictions: {EvictionCount}";
      }
    }

    private sealed class CacheEntry
    {
      public float[] AudioData { get; init; }
      public int SampleRate { get; init; }
      public long SizeBytes { get; init; }
    }

    private readonly Dictionary<string, CacheEntry> _cache;
    private readonly LinkedList<string> _lruList;
    private readonly object _lock = new();
    private readonly long _maxSizeBytes;

    private long _currentSizeBytes;
    private long _hitCount;
    private long _missCount;
    private long _evictionCount;
    private bool _disposed;

    /// <summary>
    /// Creates a new audio cache with the specified maximum size.
    /// </summary>
    /// <param name="maxSizeMB">Maximum cache size in megabytes. Default is 50 MB.</param>
    public AudioCache(int maxSizeMB = 50)
    {
      if (maxSizeMB <= 0)
        throw new ArgumentOutOfRangeException(nameof(maxSizeMB), "Max size must be positive");

      _maxSizeBytes = maxSizeMB * 1024L * 1024L;
      _cache = new Dictionary<string, CacheEntry>();
      _lruList = new LinkedList<string>();
    }

    /// <summary>
    /// Tries to get cached audio data for the given key.
    /// </summary>
    /// <param name="key">The cache key (use ComputeCacheKey to generate).</param>
    /// <param name="audioData">The cached audio samples if found.</param>
    /// <param name="sampleRate">The sample rate of the cached audio if found.</param>
    /// <returns>True if the cache contains the key, false otherwise.</returns>
    public bool TryGet(string key, out float[] audioData, out int sampleRate)
    {
      if (string.IsNullOrEmpty(key))
      {
        audioData = null;
        sampleRate = 0;
        return false;
      }

      lock (_lock)
      {
        if (_disposed)
        {
          audioData = null;
          sampleRate = 0;
          return false;
        }

        if (_cache.TryGetValue(key, out var entry))
        {
          // Move to front of LRU list (most recently used)
          _lruList.Remove(key);
          _lruList.AddFirst(key);

          _hitCount++;

          // Return a copy to prevent external modification
          audioData = (float[])entry.AudioData.Clone();
          sampleRate = entry.SampleRate;
          return true;
        }

        _missCount++;
        audioData = null;
        sampleRate = 0;
        return false;
      }
    }

    /// <summary>
    /// Stores audio data in the cache.
    /// </summary>
    /// <param name="key">The cache key (use ComputeCacheKey to generate).</param>
    /// <param name="audioData">The audio samples to cache.</param>
    /// <param name="sampleRate">The sample rate of the audio.</param>
    public void Set(string key, float[] audioData, int sampleRate)
    {
      if (string.IsNullOrEmpty(key) || audioData == null || audioData.Length == 0)
        return;

      // Calculate size: float = 4 bytes
      long entrySize = audioData.Length * sizeof(float);

      // Don't cache entries larger than max size
      if (entrySize > _maxSizeBytes)
        return;

      lock (_lock)
      {
        if (_disposed)
          return;

        // If key already exists, remove it first
        if (_cache.TryGetValue(key, out var existingEntry))
        {
          _currentSizeBytes -= existingEntry.SizeBytes;
          _cache.Remove(key);
          _lruList.Remove(key);
        }

        // Evict entries until we have room
        while (_currentSizeBytes + entrySize > _maxSizeBytes && _lruList.Count > 0)
        {
          EvictLRU();
        }

        // Store new entry (clone to prevent external modification)
        var entry = new CacheEntry
        {
          AudioData = (float[])audioData.Clone(),
          SampleRate = sampleRate,
          SizeBytes = entrySize
        };

        _cache[key] = entry;
        _lruList.AddFirst(key);
        _currentSizeBytes += entrySize;
      }
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
      lock (_lock)
      {
        if (_disposed)
          return;

        _cache.Clear();
        _lruList.Clear();
        _currentSizeBytes = 0;
        // Note: statistics are preserved (hits, misses, evictions)
      }
    }

    /// <summary>
    /// Gets current cache statistics.
    /// </summary>
    /// <returns>Statistics about cache usage.</returns>
    public AudioCacheStatistics GetStatistics()
    {
      lock (_lock)
      {
        return new AudioCacheStatistics
        {
          EntryCount = _cache.Count,
          MaxSizeBytes = _maxSizeBytes,
          CurrentSizeBytes = _currentSizeBytes,
          HitCount = _hitCount,
          MissCount = _missCount,
          EvictionCount = _evictionCount
        };
      }
    }

    /// <summary>
    /// Computes a cache key from text and voice configuration parameters.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="modelName">The voice model name.</param>
    /// <param name="lengthScale">Speech speed parameter.</param>
    /// <param name="noiseScale">Voice variation parameter.</param>
    /// <param name="noiseW">Additional noise parameter.</param>
    /// <returns>A hash string suitable for use as a cache key.</returns>
    public static string ComputeCacheKey(
        string text,
        string modelName,
        float lengthScale,
        float noiseScale,
        float noiseW)
    {
      if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(modelName))
        return null;

      // Use fixed precision for floats to avoid floating-point comparison issues
      var input = $"{text}|{modelName}|{lengthScale:F4}|{noiseScale:F4}|{noiseW:F4}";

      using var sha256 = SHA256.Create();
      var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
      return Convert.ToBase64String(hash);
    }

    private void EvictLRU()
    {
      // Remove least recently used (last in list)
      var lastNode = _lruList.Last;
      if (lastNode == null)
        return;

      var keyToRemove = lastNode.Value;
      if (_cache.TryGetValue(keyToRemove, out var entry))
      {
        _currentSizeBytes -= entry.SizeBytes;
        _cache.Remove(keyToRemove);
        _evictionCount++;
      }
      _lruList.RemoveLast();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
      lock (_lock)
      {
        if (_disposed)
          return;

        _disposed = true;
        _cache.Clear();
        _lruList.Clear();
        _currentSizeBytes = 0;
      }
    }
  }
}
