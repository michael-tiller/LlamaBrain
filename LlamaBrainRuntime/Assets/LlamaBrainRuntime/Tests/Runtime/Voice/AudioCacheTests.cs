using System;
using NUnit.Framework;
using LlamaBrain.Runtime.Core.Voice;

namespace LlamaBrain.Runtime.Tests.Voice
{
  [TestFixture]
  public class AudioCacheTests
  {
    private AudioCache _cache;

    [SetUp]
    public void SetUp()
    {
      _cache = new AudioCache(maxSizeMB: 1); // 1 MB for tests
    }

    [TearDown]
    public void TearDown()
    {
      _cache?.Dispose();
    }

    [Test]
    public void TryGet_EmptyCache_ReturnsFalse()
    {
      // Act
      var result = _cache.TryGet("nonexistent", out var audioData, out var sampleRate);

      // Assert
      Assert.That(result, Is.False);
      Assert.That(audioData, Is.Null);
      Assert.That(sampleRate, Is.EqualTo(0));
    }

    [Test]
    public void TryGet_NullKey_ReturnsFalse()
    {
      // Act
      var result = _cache.TryGet(null, out var audioData, out var sampleRate);

      // Assert
      Assert.That(result, Is.False);
      Assert.That(audioData, Is.Null);
    }

    [Test]
    public void TryGet_EmptyKey_ReturnsFalse()
    {
      // Act
      var result = _cache.TryGet("", out var audioData, out var sampleRate);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void Set_ThenGet_ReturnsCachedData()
    {
      // Arrange
      var key = "test-key";
      var audioData = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
      var sampleRate = 22050;

      // Act
      _cache.Set(key, audioData, sampleRate);
      var result = _cache.TryGet(key, out var retrieved, out var retrievedRate);

      // Assert
      Assert.That(result, Is.True);
      Assert.That(retrieved, Is.EqualTo(audioData));
      Assert.That(retrievedRate, Is.EqualTo(sampleRate));
    }

    [Test]
    public void Set_NullKey_DoesNotThrow()
    {
      // Act & Assert - should not throw
      Assert.DoesNotThrow(() => _cache.Set(null, new float[] { 1f }, 22050));
    }

    [Test]
    public void Set_NullAudioData_DoesNotThrow()
    {
      // Act & Assert - should not throw
      Assert.DoesNotThrow(() => _cache.Set("key", null, 22050));
    }

    [Test]
    public void Set_EmptyAudioData_DoesNotCache()
    {
      // Arrange
      var key = "test-key";

      // Act
      _cache.Set(key, Array.Empty<float>(), 22050);
      var result = _cache.TryGet(key, out _, out _);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void Set_ReturnsClonedData()
    {
      // Arrange
      var key = "test-key";
      var originalData = new float[] { 0.1f, 0.2f, 0.3f };

      // Act
      _cache.Set(key, originalData, 22050);
      _cache.TryGet(key, out var retrieved, out _);

      // Modify original after caching
      originalData[0] = 999f;

      // Assert - cached data should not be affected
      Assert.That(retrieved[0], Is.EqualTo(0.1f));
    }

    [Test]
    public void TryGet_ReturnsClonedData()
    {
      // Arrange
      var key = "test-key";
      _cache.Set(key, new float[] { 0.1f, 0.2f, 0.3f }, 22050);

      // Act
      _cache.TryGet(key, out var retrieved1, out _);
      retrieved1[0] = 999f;
      _cache.TryGet(key, out var retrieved2, out _);

      // Assert - modifying retrieved data should not affect cache
      Assert.That(retrieved2[0], Is.EqualTo(0.1f));
    }

    [Test]
    public void Set_ExceedsMaxSize_EvictsLRU()
    {
      // Arrange - 1 MB = 1024*1024 bytes, float = 4 bytes
      // So we can store ~262,144 floats max
      var smallArray = new float[50000]; // ~200 KB each
      var key1 = "key1";
      var key2 = "key2";
      var key3 = "key3";
      var key4 = "key4";
      var key5 = "key5";
      var key6 = "key6";

      // Act - add entries that will exceed 1 MB
      _cache.Set(key1, smallArray, 22050);
      _cache.Set(key2, smallArray, 22050);
      _cache.Set(key3, smallArray, 22050);
      _cache.Set(key4, smallArray, 22050);
      _cache.Set(key5, smallArray, 22050);
      _cache.Set(key6, smallArray, 22050); // This should evict key1

      // Assert
      var hasKey1 = _cache.TryGet(key1, out _, out _);
      var hasKey6 = _cache.TryGet(key6, out _, out _);

      Assert.That(hasKey1, Is.False, "Key1 should have been evicted");
      Assert.That(hasKey6, Is.True, "Key6 should still be in cache");
    }

    [Test]
    public void TryGet_UpdatesLRUOrder()
    {
      // Arrange
      var smallArray = new float[50000]; // ~200 KB each
      var key1 = "key1";
      var key2 = "key2";
      var key3 = "key3";
      var key4 = "key4";
      var key5 = "key5";

      // Add entries
      _cache.Set(key1, smallArray, 22050);
      _cache.Set(key2, smallArray, 22050);
      _cache.Set(key3, smallArray, 22050);
      _cache.Set(key4, smallArray, 22050);

      // Access key1 to make it most recently used
      _cache.TryGet(key1, out _, out _);

      // Add one more entry that will cause eviction
      _cache.Set(key5, smallArray, 22050);

      // Assert - key2 should be evicted (was least recently used), key1 should remain
      var hasKey1 = _cache.TryGet(key1, out _, out _);
      var hasKey2 = _cache.TryGet(key2, out _, out _);

      Assert.That(hasKey1, Is.True, "Key1 should remain (was accessed recently)");
      Assert.That(hasKey2, Is.False, "Key2 should have been evicted (was LRU)");
    }

    [Test]
    public void ComputeCacheKey_SameParams_SameKey()
    {
      // Act
      var key1 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.667f, 0.8f);
      var key2 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.667f, 0.8f);

      // Assert
      Assert.That(key1, Is.EqualTo(key2));
    }

    [Test]
    public void ComputeCacheKey_DifferentText_DifferentKeys()
    {
      // Act
      var key1 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.667f, 0.8f);
      var key2 = AudioCache.ComputeCacheKey("Goodbye world", "en_US-lessac-high", 1.0f, 0.667f, 0.8f);

      // Assert
      Assert.That(key1, Is.Not.EqualTo(key2));
    }

    [Test]
    public void ComputeCacheKey_DifferentModel_DifferentKeys()
    {
      // Act
      var key1 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.667f, 0.8f);
      var key2 = AudioCache.ComputeCacheKey("Hello world", "en_US-ljspeech-high", 1.0f, 0.667f, 0.8f);

      // Assert
      Assert.That(key1, Is.Not.EqualTo(key2));
    }

    [Test]
    public void ComputeCacheKey_DifferentLengthScale_DifferentKeys()
    {
      // Act
      var key1 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.667f, 0.8f);
      var key2 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.5f, 0.667f, 0.8f);

      // Assert
      Assert.That(key1, Is.Not.EqualTo(key2));
    }

    [Test]
    public void ComputeCacheKey_DifferentNoiseScale_DifferentKeys()
    {
      // Act
      var key1 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.5f, 0.8f);
      var key2 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.667f, 0.8f);

      // Assert
      Assert.That(key1, Is.Not.EqualTo(key2));
    }

    [Test]
    public void ComputeCacheKey_DifferentNoiseW_DifferentKeys()
    {
      // Act
      var key1 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.667f, 0.5f);
      var key2 = AudioCache.ComputeCacheKey("Hello world", "en_US-lessac-high", 1.0f, 0.667f, 0.8f);

      // Assert
      Assert.That(key1, Is.Not.EqualTo(key2));
    }

    [Test]
    public void ComputeCacheKey_NullText_ReturnsNull()
    {
      // Act
      var key = AudioCache.ComputeCacheKey(null, "en_US-lessac-high", 1.0f, 0.667f, 0.8f);

      // Assert
      Assert.That(key, Is.Null);
    }

    [Test]
    public void ComputeCacheKey_NullModel_ReturnsNull()
    {
      // Act
      var key = AudioCache.ComputeCacheKey("Hello world", null, 1.0f, 0.667f, 0.8f);

      // Assert
      Assert.That(key, Is.Null);
    }

    [Test]
    public void GetStatistics_InitialState_ZeroCounts()
    {
      // Act
      var stats = _cache.GetStatistics();

      // Assert
      Assert.That(stats.EntryCount, Is.EqualTo(0));
      Assert.That(stats.HitCount, Is.EqualTo(0));
      Assert.That(stats.MissCount, Is.EqualTo(0));
      Assert.That(stats.EvictionCount, Is.EqualTo(0));
      Assert.That(stats.CurrentSizeBytes, Is.EqualTo(0));
    }

    [Test]
    public void GetStatistics_AfterHit_IncrementsHitCount()
    {
      // Arrange
      _cache.Set("key", new float[] { 1f, 2f, 3f }, 22050);

      // Act
      _cache.TryGet("key", out _, out _);
      _cache.TryGet("key", out _, out _);
      var stats = _cache.GetStatistics();

      // Assert
      Assert.That(stats.HitCount, Is.EqualTo(2));
      Assert.That(stats.MissCount, Is.EqualTo(0));
    }

    [Test]
    public void GetStatistics_AfterMiss_IncrementsMissCount()
    {
      // Act
      _cache.TryGet("nonexistent1", out _, out _);
      _cache.TryGet("nonexistent2", out _, out _);
      var stats = _cache.GetStatistics();

      // Assert
      Assert.That(stats.HitCount, Is.EqualTo(0));
      Assert.That(stats.MissCount, Is.EqualTo(2));
    }

    [Test]
    public void GetStatistics_HitRate_CalculatedCorrectly()
    {
      // Arrange
      _cache.Set("key", new float[] { 1f, 2f, 3f }, 22050);

      // Act - 2 hits, 2 misses = 50% hit rate
      _cache.TryGet("key", out _, out _);
      _cache.TryGet("key", out _, out _);
      _cache.TryGet("nonexistent1", out _, out _);
      _cache.TryGet("nonexistent2", out _, out _);
      var stats = _cache.GetStatistics();

      // Assert
      Assert.That(stats.HitRate, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void GetStatistics_CurrentSizeBytes_TrackedCorrectly()
    {
      // Arrange
      var audioData = new float[1000]; // 4000 bytes

      // Act
      _cache.Set("key1", audioData, 22050);
      _cache.Set("key2", audioData, 22050);
      var stats = _cache.GetStatistics();

      // Assert
      Assert.That(stats.CurrentSizeBytes, Is.EqualTo(8000)); // 2 * 1000 * 4
    }

    [Test]
    public void Clear_RemovesAllEntries()
    {
      // Arrange
      _cache.Set("key1", new float[] { 1f }, 22050);
      _cache.Set("key2", new float[] { 2f }, 22050);

      // Act
      _cache.Clear();

      // Assert
      Assert.That(_cache.TryGet("key1", out _, out _), Is.False);
      Assert.That(_cache.TryGet("key2", out _, out _), Is.False);
      Assert.That(_cache.GetStatistics().EntryCount, Is.EqualTo(0));
      Assert.That(_cache.GetStatistics().CurrentSizeBytes, Is.EqualTo(0));
    }

    [Test]
    public void Clear_PreservesStatistics()
    {
      // Arrange
      _cache.Set("key", new float[] { 1f }, 22050);
      _cache.TryGet("key", out _, out _); // hit
      _cache.TryGet("miss", out _, out _); // miss

      // Act
      _cache.Clear();
      var stats = _cache.GetStatistics();

      // Assert - statistics preserved after clear
      Assert.That(stats.HitCount, Is.EqualTo(1));
      Assert.That(stats.MissCount, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_ClearsCache()
    {
      // Arrange
      _cache.Set("key", new float[] { 1f }, 22050);

      // Act
      _cache.Dispose();

      // Assert - after dispose, operations should return defaults
      var result = _cache.TryGet("key", out var data, out var rate);
      Assert.That(result, Is.False);
      Assert.That(data, Is.Null);
    }

    [Test]
    public void Dispose_MultipleCallsDoNotThrow()
    {
      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        _cache.Dispose();
        _cache.Dispose();
        _cache.Dispose();
      });
    }

    [Test]
    public void Constructor_InvalidMaxSize_Throws()
    {
      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => new AudioCache(0));
      Assert.Throws<ArgumentOutOfRangeException>(() => new AudioCache(-1));
    }

    [Test]
    public void Set_EntryLargerThanMaxSize_DoesNotCache()
    {
      // Arrange - cache is 1 MB, create entry larger than that
      var cache = new AudioCache(1); // 1 MB
      var largeArray = new float[300000]; // ~1.2 MB

      // Act
      cache.Set("large", largeArray, 22050);
      var result = cache.TryGet("large", out _, out _);

      // Assert
      Assert.That(result, Is.False);
      cache.Dispose();
    }

    [Test]
    public void Set_OverwriteExistingKey_UpdatesEntry()
    {
      // Arrange
      var key = "test-key";
      var data1 = new float[] { 1f, 2f, 3f };
      var data2 = new float[] { 4f, 5f, 6f, 7f };

      // Act
      _cache.Set(key, data1, 22050);
      _cache.Set(key, data2, 44100);
      _cache.TryGet(key, out var retrieved, out var rate);

      // Assert
      Assert.That(retrieved, Is.EqualTo(data2));
      Assert.That(rate, Is.EqualTo(44100));
    }

    [Test]
    public void Statistics_ToString_FormatsCorrectly()
    {
      // Arrange
      _cache.Set("key", new float[10000], 22050); // 40 KB
      _cache.TryGet("key", out _, out _);

      // Act
      var stats = _cache.GetStatistics();
      var str = stats.ToString();

      // Assert
      Assert.That(str, Does.Contain("AudioCache"));
      Assert.That(str, Does.Contain("1 entries"));
      Assert.That(str, Does.Contain("Hit rate"));
    }
  }
}
