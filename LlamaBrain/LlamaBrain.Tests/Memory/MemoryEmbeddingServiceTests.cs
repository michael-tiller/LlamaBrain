using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Core.Retrieval;

namespace LlamaBrain.Tests.Memory
{
  /// <summary>
  /// Tests for the MemoryEmbeddingService.
  /// </summary>
  [TestFixture]
  public class MemoryEmbeddingServiceTests
  {
    private AuthoritativeMemorySystem _memorySystem = null!;
    private InMemoryVectorStore _vectorStore = null!;

    [SetUp]
    public void SetUp()
    {
      _memorySystem = new AuthoritativeMemorySystem { NpcId = "test_npc" };
      _vectorStore = new InMemoryVectorStore(768); // Standard embedding dimension
    }

    #region Basic Embedding Tests

    [Test]
    [Category("RAG")]
    public async Task OnMemoryMutated_GeneratesEmbedding_AddsToVectorStore()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      _memorySystem.AddCanonicalFact("fact_001", "The king's name is Arthur");
      await service.FlushAsync();

      // Assert
      var stats = _vectorStore.GetStatistics();
      Assert.That(stats.TotalVectors, Is.EqualTo(1));
      Assert.That(stats.CanonicalFactVectors, Is.EqualTo(1));
    }

    [Test]
    [Category("RAG")]
    public async Task OnMemoryMutated_EmbeddingFails_VectorStoreRemainsSafe()
    {
      // Arrange - provider returns null to simulate failure
      var mockProvider = new MockEmbeddingProvider(null);
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      _memorySystem.AddCanonicalFact("fact_001", "Test fact");
      await service.FlushAsync();

      // Assert
      var stats = _vectorStore.GetStatistics();
      Assert.That(stats.TotalVectors, Is.EqualTo(0), "Vector store should remain empty on embedding failure");

      // Memory should still exist in AuthoritativeMemorySystem
      Assert.That(_memorySystem.GetCanonicalFact("fact_001"), Is.Not.Null);
    }

    [Test]
    [Category("RAG")]
    public async Task OnMemoryMutated_ProviderUnavailable_SkipsEmbedding()
    {
      // Arrange - provider reports unavailable
      var mockProvider = new MockEmbeddingProvider(new float[768]) { IsAvailable = false };
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      _memorySystem.AddCanonicalFact("fact_001", "Test fact");
      await service.FlushAsync();

      // Assert
      var stats = _vectorStore.GetStatistics();
      Assert.That(stats.TotalVectors, Is.EqualTo(0), "Should skip embedding when provider unavailable");
    }

    [Test]
    [Category("RAG")]
    public async Task OnMemoryMutated_ProviderThrows_HandlesGracefully()
    {
      // Arrange - provider throws exception
      var mockProvider = new ThrowingEmbeddingProvider();
      var logs = new List<string>();
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore, msg => logs.Add(msg));

      // Act - should not throw
      _memorySystem.AddCanonicalFact("fact_001", "Test fact");
      await service.FlushAsync();

      // Assert
      var stats = _vectorStore.GetStatistics();
      Assert.That(stats.TotalVectors, Is.EqualTo(0));
      Assert.That(logs.Exists(l => l.Contains("Error")), Is.True, "Should log the error");
    }

    #endregion

    #region Multiple Memory Types Tests

    [Test]
    [Category("RAG")]
    public async Task OnMemoryMutated_MultipleMemories_AllEmbedded()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      _memorySystem.AddCanonicalFact("fact_001", "First fact");
      _memorySystem.SetWorldState("door_1", "open", MutationSource.GameSystem);
      _memorySystem.AddDialogue("Player", "Hello");
      _memorySystem.SetBelief("belief_001", BeliefMemoryEntry.CreateBelief("x", "y"), MutationSource.ValidatedOutput);
      await service.FlushAsync();

      // Assert
      var stats = _vectorStore.GetStatistics();
      Assert.That(stats.TotalVectors, Is.EqualTo(4));
      Assert.That(stats.CanonicalFactVectors, Is.EqualTo(1));
      Assert.That(stats.WorldStateVectors, Is.EqualTo(1));
      Assert.That(stats.EpisodicVectors, Is.EqualTo(1));
      Assert.That(stats.BeliefVectors, Is.EqualTo(1));
    }

    [Test]
    [Category("RAG")]
    public async Task OnMemoryMutated_EpisodicMemory_UsesNpcId()
    {
      // Arrange
      _memorySystem.NpcId = "wizard_001";
      var mockProvider = new MockEmbeddingProvider(new float[768]);
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      _memorySystem.AddDialogue("Player", "Hello wizard!");
      await service.FlushAsync();

      // Assert - search for this NPC should find the memory
      var queryEmbedding = new float[768];
      var results = _vectorStore.FindSimilar(queryEmbedding, 10, "wizard_001");
      Assert.That(results.Count, Is.EqualTo(1));
      Assert.That(results[0].NpcId, Is.EqualTo("wizard_001"));
    }

    [Test]
    [Category("RAG")]
    public async Task OnMemoryMutated_CanonicalFact_HasNullNpcId()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      _memorySystem.AddCanonicalFact("fact_001", "The sky is blue");
      await service.FlushAsync();

      // Assert - search for any NPC should find canonical facts (shared)
      var queryEmbedding = new float[768];
      var results = _vectorStore.FindSimilar(queryEmbedding, 10, "any_npc");
      Assert.That(results.Count, Is.EqualTo(1));
      Assert.That(results[0].NpcId, Is.Null);
    }

    #endregion

    #region Dispose Tests

    [Test]
    [Category("RAG")]
    public async Task Dispose_UnsubscribesFromEvent()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);
      var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      service.Dispose();
      _memorySystem.AddCanonicalFact("fact_001", "Test fact");
      await service.FlushAsync();

      // Assert
      var stats = _vectorStore.GetStatistics();
      Assert.That(stats.TotalVectors, Is.EqualTo(0), "Should not generate embedding after disposal");
    }

    [Test]
    [Category("RAG")]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);
      var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act & Assert - should not throw
      Assert.DoesNotThrow(() =>
      {
        service.Dispose();
        service.Dispose();
        service.Dispose();
      });
    }

    #endregion

    #region Statistics Tests

    [Test]
    [Category("RAG")]
    public async Task EmbeddingStatistics_TracksCoverage()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      _memorySystem.AddCanonicalFact("fact_001", "Test fact");
      await service.FlushAsync();

      // Assert
      var stats = _memorySystem.GetStatistics();
      Assert.That(stats.TotalMemoriesCreated, Is.EqualTo(1));
      Assert.That(stats.EmbeddedMemories, Is.EqualTo(1));
      Assert.That(stats.EmbeddingCoverage, Is.EqualTo(1.0f));
    }

    [Test]
    [Category("RAG")]
    public async Task EmbeddingStatistics_PartialCoverage_WhenSomeFail()
    {
      // Arrange - provider fails every other time
      var mockProvider = new AlternatingEmbeddingProvider(768);
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);

      // Act
      _memorySystem.AddCanonicalFact("fact_001", "First fact");  // Succeeds
      _memorySystem.AddCanonicalFact("fact_002", "Second fact"); // Fails
      _memorySystem.AddCanonicalFact("fact_003", "Third fact");  // Succeeds
      _memorySystem.AddCanonicalFact("fact_004", "Fourth fact"); // Fails
      await service.FlushAsync();

      // Assert
      var stats = _memorySystem.GetStatistics();
      Assert.That(stats.TotalMemoriesCreated, Is.EqualTo(4));
      Assert.That(stats.EmbeddedMemories, Is.EqualTo(2));
      Assert.That(stats.EmbeddingCoverage, Is.EqualTo(0.5f));
    }

    [Test]
    [Category("RAG")]
    public void EmbeddingStatistics_ZeroMemories_ReturnsZeroCoverage()
    {
      // Arrange - no memories

      // Act
      var stats = _memorySystem.GetStatistics();

      // Assert
      Assert.That(stats.TotalMemoriesCreated, Is.EqualTo(0));
      Assert.That(stats.EmbeddedMemories, Is.EqualTo(0));
      Assert.That(stats.EmbeddingCoverage, Is.EqualTo(0f));
    }

    [Test]
    [Category("RAG")]
    public async Task EmbeddingStatistics_ClearAll_ResetsCounts()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);
      using var service = new MemoryEmbeddingService(_memorySystem, mockProvider, _vectorStore);
      _memorySystem.AddCanonicalFact("fact_001", "Test fact");
      await service.FlushAsync();

      // Act
      _memorySystem.ClearAll();

      // Assert
      var stats = _memorySystem.GetStatistics();
      Assert.That(stats.TotalMemoriesCreated, Is.EqualTo(0));
      Assert.That(stats.EmbeddedMemories, Is.EqualTo(0));
    }

    #endregion

    #region Constructor Validation Tests

    [Test]
    [Category("RAG")]
    public void Constructor_NullMemorySystem_ThrowsArgumentNullException()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() =>
        new MemoryEmbeddingService(null!, mockProvider, _vectorStore));
    }

    [Test]
    [Category("RAG")]
    public void Constructor_NullEmbeddingProvider_ThrowsArgumentNullException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() =>
        new MemoryEmbeddingService(_memorySystem, null!, _vectorStore));
    }

    [Test]
    [Category("RAG")]
    public void Constructor_NullVectorStore_ThrowsArgumentNullException()
    {
      // Arrange
      var mockProvider = new MockEmbeddingProvider(new float[768]);

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() =>
        new MemoryEmbeddingService(_memorySystem, mockProvider, null!));
    }

    #endregion

    #region Mock Embedding Providers

    /// <summary>
    /// Mock embedding provider that returns a fixed embedding or null.
    /// </summary>
    private sealed class MockEmbeddingProvider : IEmbeddingProvider
    {
      private readonly float[]? _returns;

      public MockEmbeddingProvider(float[]? returns)
      {
        _returns = returns;
        IsAvailable = true;
      }

      public int EmbeddingDimension => _returns?.Length ?? 768;
      public bool IsAvailable { get; set; }

      public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
      {
        return Task.FromResult(_returns);
      }

      public Task<float[]?[]> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
      {
        var results = new float[]?[texts.Count];
        for (int i = 0; i < texts.Count; i++)
        {
          results[i] = _returns;
        }
        return Task.FromResult(results);
      }
    }

    /// <summary>
    /// Mock embedding provider that throws an exception.
    /// </summary>
    private sealed class ThrowingEmbeddingProvider : IEmbeddingProvider
    {
      public int EmbeddingDimension => 768;
      public bool IsAvailable => true;

      public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
      {
        throw new InvalidOperationException("Test exception");
      }

      public Task<float[]?[]> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
      {
        throw new InvalidOperationException("Test exception");
      }
    }

    /// <summary>
    /// Mock embedding provider that alternates between success and failure.
    /// </summary>
    private sealed class AlternatingEmbeddingProvider : IEmbeddingProvider
    {
      private readonly int _dimension;
      private int _callCount = 0;

      public AlternatingEmbeddingProvider(int dimension)
      {
        _dimension = dimension;
      }

      public int EmbeddingDimension => _dimension;
      public bool IsAvailable => true;

      public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
      {
        _callCount++;
        // Succeed on odd calls (1, 3, 5...), fail on even calls (2, 4, 6...)
        if (_callCount % 2 == 1)
        {
          return Task.FromResult<float[]?>(new float[_dimension]);
        }
        return Task.FromResult<float[]?>(null);
      }

      public Task<float[]?[]> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
      {
        var results = new float[]?[texts.Count];
        for (int i = 0; i < texts.Count; i++)
        {
          _callCount++;
          results[i] = _callCount % 2 == 1 ? new float[_dimension] : null;
        }
        return Task.FromResult(results);
      }
    }

    #endregion
  }
}
