using System;
using System.Threading;
using System.Threading.Tasks;
using LlamaBrain.Core.Retrieval;

namespace LlamaBrain.Persona
{
  /// <summary>
  /// Automatically generates embeddings for new memories and adds them to a vector store.
  /// Subscribes to the AuthoritativeMemorySystem.MemoryMutated event and processes
  /// embeddings asynchronously in the background.
  ///
  /// This service enables RAG-based retrieval by automatically populating the vector store
  /// as memories are created, without blocking memory mutations.
  /// </summary>
  /// <remarks>
  /// Key design decisions:
  /// - Async event handler (async void) for fire-and-forget embedding generation
  /// - Non-blocking: Memory mutations return immediately, embeddings happen asynchronously
  /// - Graceful failure: If embedding fails, logs the error but doesn't throw
  /// - IDisposable: Properly unsubscribes from events to prevent memory leaks
  /// </remarks>
  public sealed class MemoryEmbeddingService : IDisposable
  {
    private readonly AuthoritativeMemorySystem _memorySystem;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IMemoryVectorStore _vectorStore;
    private readonly Action<string>? _logger;
    private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

    private bool _disposed;

    /// <summary>
    /// Creates a new MemoryEmbeddingService that automatically generates embeddings
    /// for memories created in the specified AuthoritativeMemorySystem.
    /// </summary>
    /// <param name="memorySystem">The memory system to monitor for mutations.</param>
    /// <param name="embeddingProvider">The provider to use for generating embeddings.</param>
    /// <param name="vectorStore">The vector store to add embeddings to.</param>
    /// <param name="logger">Optional logging callback for debugging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public MemoryEmbeddingService(
      AuthoritativeMemorySystem memorySystem,
      IEmbeddingProvider embeddingProvider,
      IMemoryVectorStore vectorStore,
      Action<string>? logger = null)
    {
      _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
      _embeddingProvider = embeddingProvider ?? throw new ArgumentNullException(nameof(embeddingProvider));
      _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
      _logger = logger;

      _memorySystem.MemoryMutated += OnMemoryMutatedAsync;
    }

    /// <summary>
    /// Event handler for memory mutations. Generates embeddings asynchronously.
    /// Uses async void pattern for fire-and-forget (acceptable for event handlers).
    /// </summary>
    private async void OnMemoryMutatedAsync(object? sender, MemoryMutatedEventArgs e)
    {
      if (_disposed)
        return;

      try
      {
        // Check if embedding provider is available
        if (!_embeddingProvider.IsAvailable)
        {
          _logger?.Invoke($"[MemoryEmbedding] Embedding provider unavailable, skipping: {e.MemoryId}");
          return;
        }

        // Generate embedding asynchronously
        var embedding = await _embeddingProvider.GenerateEmbeddingAsync(e.Content, _disposeCts.Token);

        if (_disposed)
          return;

        if (embedding != null)
        {
          // Add to vector store
          _vectorStore.Upsert(e.MemoryId, e.NpcId, e.MemoryType, embedding, e.SequenceNumber);
          _logger?.Invoke($"[MemoryEmbedding] Generated embedding for {e.MemoryType} memory: {e.MemoryId}");

          // Notify memory system of successful embedding (for statistics)
          NotifyEmbeddingGenerated(e.MemoryId);
        }
        else
        {
          _logger?.Invoke($"[MemoryEmbedding] Embedding generation returned null for {e.MemoryId}, falling back to keyword-only");
        }
      }
      catch (OperationCanceledException)
      {
        // Service is being disposed, ignore
      }
      catch (Exception ex)
      {
        // Swallow exception - graceful degradation to keyword-only retrieval
        _logger?.Invoke($"[MemoryEmbedding] Error generating embedding for {e.MemoryId}: {ex.Message}");
      }
    }

    /// <summary>
    /// Notifies that an embedding was successfully generated.
    /// Called by OnMemoryMutatedAsync after successful embedding generation.
    /// </summary>
    /// <param name="memoryId">The ID of the memory that was embedded.</param>
    private void NotifyEmbeddingGenerated(string memoryId)
    {
      // Statistics tracking is done in AuthoritativeMemorySystem
      // This is called to update the embedded count
      _memorySystem.NotifyEmbeddingGenerated(memoryId);
    }

    /// <summary>
    /// Disposes the service, unsubscribing from events and canceling pending operations.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;
      _disposeCts.Cancel();
      _memorySystem.MemoryMutated -= OnMemoryMutatedAsync;
      _disposeCts.Dispose();
    }
  }
}
