using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.Metrics;
using LlamaBrain.Core.StructuredOutput;

namespace LlamaBrain.Tests.Performance
{
  /// <summary>
  /// Tests for KV cache metrics and efficiency tracking.
  /// </summary>
  [TestFixture]
  [Category("KvCache")]
  [Category("Performance")]
  public class KvCacheTests
  {
    #region CacheEfficiencyMetrics Tests

    [Test]
    public void CacheEfficiencyMetrics_Default_AllZeros()
    {
      // Act
      var metrics = new CacheEfficiencyMetrics();

      // Assert
      Assert.That(metrics.TotalRequests, Is.EqualTo(0));
      Assert.That(metrics.CacheHitRequests, Is.EqualTo(0));
      Assert.That(metrics.CacheMissRequests, Is.EqualTo(0));
      Assert.That(metrics.TotalCachedTokens, Is.EqualTo(0));
      Assert.That(metrics.TotalPromptTokens, Is.EqualTo(0));
      Assert.That(metrics.CacheHitRate, Is.EqualTo(0.0));
    }

    [Test]
    public void CacheEfficiencyMetrics_RecordRequest_CacheHit_IncrementsCounters()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();

      // Act - record a request with 100 cached tokens
      metrics.RecordRequest(promptTokens: 200, cachedTokens: 100);

      // Assert
      Assert.That(metrics.TotalRequests, Is.EqualTo(1));
      Assert.That(metrics.CacheHitRequests, Is.EqualTo(1));
      Assert.That(metrics.CacheMissRequests, Is.EqualTo(0));
      Assert.That(metrics.TotalCachedTokens, Is.EqualTo(100));
      Assert.That(metrics.TotalPromptTokens, Is.EqualTo(200));
    }

    [Test]
    public void CacheEfficiencyMetrics_RecordRequest_CacheMiss_IncrementsCounters()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();

      // Act - record a request with 0 cached tokens
      metrics.RecordRequest(promptTokens: 200, cachedTokens: 0);

      // Assert
      Assert.That(metrics.TotalRequests, Is.EqualTo(1));
      Assert.That(metrics.CacheHitRequests, Is.EqualTo(0));
      Assert.That(metrics.CacheMissRequests, Is.EqualTo(1));
      Assert.That(metrics.TotalCachedTokens, Is.EqualTo(0));
    }

    [Test]
    public void CacheEfficiencyMetrics_CacheHitRate_CalculatesCorrectly()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();

      // Act - 3 hits, 1 miss = 75% hit rate
      metrics.RecordRequest(promptTokens: 100, cachedTokens: 50);
      metrics.RecordRequest(promptTokens: 100, cachedTokens: 50);
      metrics.RecordRequest(promptTokens: 100, cachedTokens: 50);
      metrics.RecordRequest(promptTokens: 100, cachedTokens: 0);

      // Assert
      Assert.That(metrics.CacheHitRate, Is.EqualTo(75.0).Within(0.1));
    }

    [Test]
    public void CacheEfficiencyMetrics_TokenCacheRate_CalculatesCorrectly()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();

      // Act - 300 cached out of 400 total = 75%
      metrics.RecordRequest(promptTokens: 200, cachedTokens: 150);
      metrics.RecordRequest(promptTokens: 200, cachedTokens: 150);

      // Assert
      Assert.That(metrics.TokenCacheRate, Is.EqualTo(75.0).Within(0.1));
    }

    [Test]
    public void CacheEfficiencyMetrics_StaticPrefixCacheEfficiency_CalculatesCorrectly()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();

      // Act - 90 cached out of 100 static prefix tokens = 90% efficiency
      metrics.RecordRequest(promptTokens: 200, cachedTokens: 90, staticPrefixTokens: 100);

      // Assert
      Assert.That(metrics.StaticPrefixCacheEfficiency, Is.EqualTo(90.0).Within(0.1));
    }

    [Test]
    public void CacheEfficiencyMetrics_Reset_ClearsAllCounters()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();
      metrics.RecordRequest(promptTokens: 200, cachedTokens: 100, staticPrefixTokens: 50);

      // Act
      metrics.Reset();

      // Assert
      Assert.That(metrics.TotalRequests, Is.EqualTo(0));
      Assert.That(metrics.CacheHitRequests, Is.EqualTo(0));
      Assert.That(metrics.TotalCachedTokens, Is.EqualTo(0));
      Assert.That(metrics.TotalPromptTokens, Is.EqualTo(0));
      Assert.That(metrics.TotalStaticPrefixTokens, Is.EqualTo(0));
    }

    [Test]
    public void CacheEfficiencyMetrics_RecordFromCompletionMetrics_Works()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();
      var completionMetrics = new CompletionMetrics
      {
        PromptTokenCount = 200,
        CachedTokenCount = 80,
        GeneratedTokenCount = 50,
        TotalTimeMs = 500
      };

      // Act
      metrics.RecordRequest(completionMetrics, staticPrefixTokens: 100);

      // Assert
      Assert.That(metrics.TotalRequests, Is.EqualTo(1));
      Assert.That(metrics.CacheHitRequests, Is.EqualTo(1));
      Assert.That(metrics.TotalCachedTokens, Is.EqualTo(80));
      Assert.That(metrics.TotalPromptTokens, Is.EqualTo(200));
    }

    [Test]
    public void CacheEfficiencyMetrics_RecordCacheHit_ExplicitMethod()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();

      // Act
      metrics.RecordCacheHit();
      metrics.RecordCacheHit();
      metrics.RecordCacheMiss();

      // Assert
      Assert.That(metrics.TotalRequests, Is.EqualTo(3));
      Assert.That(metrics.CacheHitRequests, Is.EqualTo(2));
      Assert.That(metrics.CacheMissRequests, Is.EqualTo(1));
      Assert.That(metrics.CacheHitRate, Is.EqualTo(66.67).Within(0.1));
    }

    [Test]
    public void CacheEfficiencyMetrics_ToString_ContainsRelevantInfo()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();
      metrics.RecordRequest(promptTokens: 200, cachedTokens: 100);

      // Act
      var str = metrics.ToString();

      // Assert
      Assert.That(str, Does.Contain("Requests=1"));
      Assert.That(str, Does.Contain("HitRate=100"));
    }

    #endregion

    #region StructuredPipelineMetrics Cache Tests

    [Test]
    public void StructuredPipelineMetrics_RecordCacheHit_IncrementsCounter()
    {
      // Arrange
      var metrics = new StructuredPipelineMetrics();

      // Act
      metrics.RecordCacheHit();
      metrics.RecordCacheHit();

      // Assert
      Assert.That(metrics.CacheHitCount, Is.EqualTo(2));
    }

    [Test]
    public void StructuredPipelineMetrics_RecordCacheMiss_IncrementsCounter()
    {
      // Arrange
      var metrics = new StructuredPipelineMetrics();

      // Act
      metrics.RecordCacheMiss();

      // Assert
      Assert.That(metrics.CacheMissCount, Is.EqualTo(1));
    }

    [Test]
    public void StructuredPipelineMetrics_CacheHitRate_CalculatesCorrectly()
    {
      // Arrange
      var metrics = new StructuredPipelineMetrics();

      // Act - 2 hits, 2 misses = 50%
      metrics.RecordCacheHit();
      metrics.RecordCacheHit();
      metrics.RecordCacheMiss();
      metrics.RecordCacheMiss();

      // Assert
      Assert.That(metrics.CacheHitRate, Is.EqualTo(50.0f).Within(0.1f));
    }

    [Test]
    public void StructuredPipelineMetrics_RecordCacheResult_RecordsHitWhenCached()
    {
      // Arrange
      var metrics = new StructuredPipelineMetrics();

      // Act
      metrics.RecordCacheResult(cachedTokenCount: 100);

      // Assert
      Assert.That(metrics.CacheHitCount, Is.EqualTo(1));
      Assert.That(metrics.CacheMissCount, Is.EqualTo(0));
    }

    [Test]
    public void StructuredPipelineMetrics_RecordCacheResult_RecordsMissWhenNotCached()
    {
      // Arrange
      var metrics = new StructuredPipelineMetrics();

      // Act
      metrics.RecordCacheResult(cachedTokenCount: 0);

      // Assert
      Assert.That(metrics.CacheHitCount, Is.EqualTo(0));
      Assert.That(metrics.CacheMissCount, Is.EqualTo(1));
    }

    [Test]
    public void StructuredPipelineMetrics_Reset_ClearsCacheCounters()
    {
      // Arrange
      var metrics = new StructuredPipelineMetrics();
      metrics.RecordCacheHit();
      metrics.RecordCacheMiss();

      // Act
      metrics.Reset();

      // Assert
      Assert.That(metrics.CacheHitCount, Is.EqualTo(0));
      Assert.That(metrics.CacheMissCount, Is.EqualTo(0));
    }

    [Test]
    public void StructuredPipelineMetrics_ToString_IncludesCacheInfo()
    {
      // Arrange
      var metrics = new StructuredPipelineMetrics();
      metrics.RecordCacheHit();

      // Act
      var str = metrics.ToString();

      // Assert
      Assert.That(str, Does.Contain("KvCache"));
    }

    #endregion

    #region DialogueInteraction Cache Tests

    [Test]
    public void DialogueInteraction_DefaultCacheFields_AreZero()
    {
      // Act
      var interaction = new DialogueInteraction();

      // Assert
      Assert.That(interaction.CachedTokenCount, Is.EqualTo(0));
      Assert.That(interaction.StaticPrefixTokens, Is.EqualTo(0));
      Assert.That(interaction.CacheEfficiency, Is.EqualTo(0.0));
      Assert.That(interaction.KvCachingEnabled, Is.False);
    }

    [Test]
    public void DialogueInteraction_CacheEfficiency_CalculatesCorrectly()
    {
      // Arrange
      var interaction = new DialogueInteraction
      {
        CachedTokenCount = 80,
        StaticPrefixTokens = 100
      };

      // Assert
      Assert.That(interaction.CacheEfficiency, Is.EqualTo(0.8).Within(0.01));
    }

    [Test]
    public void DialogueInteraction_CacheEfficiency_ZeroWhenNoPrefixTokens()
    {
      // Arrange
      var interaction = new DialogueInteraction
      {
        CachedTokenCount = 100,
        StaticPrefixTokens = 0
      };

      // Assert
      Assert.That(interaction.CacheEfficiency, Is.EqualTo(0.0));
    }

    [Test]
    public void DialogueInteraction_KvCachingEnabled_CanBeSet()
    {
      // Act
      var interaction = new DialogueInteraction
      {
        KvCachingEnabled = true
      };

      // Assert
      Assert.That(interaction.KvCachingEnabled, Is.True);
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public void CacheEfficiencyMetrics_ConcurrentAccess_MaintainsConsistency()
    {
      // Arrange
      var metrics = new CacheEfficiencyMetrics();
      var tasks = new System.Threading.Tasks.Task[100];

      // Act - concurrent recording
      for (int i = 0; i < 100; i++)
      {
        var cachedTokens = i % 2 == 0 ? 50 : 0; // Alternating hits and misses
        tasks[i] = System.Threading.Tasks.Task.Run(() =>
          metrics.RecordRequest(promptTokens: 100, cachedTokens: cachedTokens));
      }
      System.Threading.Tasks.Task.WaitAll(tasks);

      // Assert - should have recorded all 100 requests
      Assert.That(metrics.TotalRequests, Is.EqualTo(100));
      Assert.That(metrics.CacheHitRequests + metrics.CacheMissRequests, Is.EqualTo(100));
    }

    [Test]
    public void StructuredPipelineMetrics_ConcurrentCacheRecording_MaintainsConsistency()
    {
      // Arrange
      var metrics = new StructuredPipelineMetrics();
      var tasks = new System.Threading.Tasks.Task[100];

      // Act - concurrent recording
      for (int i = 0; i < 100; i++)
      {
        var isHit = i % 2 == 0;
        tasks[i] = System.Threading.Tasks.Task.Run(() =>
        {
          if (isHit)
            metrics.RecordCacheHit();
          else
            metrics.RecordCacheMiss();
        });
      }
      System.Threading.Tasks.Task.WaitAll(tasks);

      // Assert
      Assert.That(metrics.CacheHitCount + metrics.CacheMissCount, Is.EqualTo(100));
    }

    #endregion
  }
}
