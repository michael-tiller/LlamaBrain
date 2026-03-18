using System;
using System.Collections.Generic;
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
  /// - Pending task tracking for deterministic test synchronization via FlushAsync()
  /// - Non-blocking: Memory mutations return immediately, embeddings happen asynchronously
  /// - Graceful failure: If embedding fails, logs the error but doesn't throw
  /// - IAsyncDisposable: Properly drains in-flight work and unsubscribes from events
  /// - IDisposable: Synchronous disposal with timeout for in-flight work
  /// </remarks>
  public sealed class MemoryEmbeddingService : IDisposable, IAsyncDisposable
  {
    private readonly AuthoritativeMemorySystem _memorySystem;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IMemoryVectorStore _vectorStore;
    private readonly Action<string>? _logger;
    private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();
    private readonly object _pendingLock = new object();
    private readonly List<Task> _pendingTasks = new List<Task>();

    private volatile bool _disposed;

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
    /// Event handler for memory mutations. Starts embedding generation as a tracked task.
    /// </summary>
    private void OnMemoryMutatedAsync(object? sender, MemoryMutatedEventArgs e)
    {
      if (_disposed)
        return;

      var task = ProcessEmbeddingAsync(e);
      TrackPendingTask(task);
    }

    /// <summary>
    /// Processes embedding generation for a memory mutation.
    /// </summary>
    private async Task ProcessEmbeddingAsync(MemoryMutatedEventArgs e)
    {
      try
      {
        // Check if embedding provider is available
        if (!_embeddingProvider.IsAvailable)
        {
          SafeLog($"[MemoryEmbedding] Embedding provider unavailable, skipping: {e.MemoryId}");
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
          SafeLog($"[MemoryEmbedding] Generated embedding for {e.MemoryType} memory: {e.MemoryId}");

          // Notify memory system of successful embedding (for statistics)
          NotifyEmbeddingGenerated(e.MemoryId);
        }
        else
        {
          SafeLog($"[MemoryEmbedding] Embedding generation returned null for {e.MemoryId}, falling back to keyword-only");
        }
      }
      catch (OperationCanceledException)
      {
        // Service is being disposed, ignore
      }
      catch (Exception ex)
      {
        // Swallow exception - graceful degradation to keyword-only retrieval
        SafeLog($"[MemoryEmbedding] Error generating embedding for {e.MemoryId}: {ex.Message}");
      }
    }

    /// <summary>
    /// Tracks a pending task for later synchronization via FlushAsync.
    /// </summary>
    private void TrackPendingTask(Task task)
    {
      lock (_pendingLock)
      {
        _pendingTasks.Add(task);
      }

      // Clean up completed task when done
      task.ContinueWith(_ =>
      {
        lock (_pendingLock)
        {
          _pendingTasks.Remove(task);
        }
      }, TaskContinuationOptions.ExecuteSynchronously);
    }

    /// <summary>
    /// Waits for all pending embedding operations to complete.
    /// Used for deterministic test synchronization.
    /// </summary>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <returns>A task that completes when all pending operations finish or timeout is reached.</returns>
    public async Task FlushAsync(TimeSpan? timeout = null)
    {
      var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
      Task[] snapshot;

      lock (_pendingLock)
      {
        snapshot = _pendingTasks.ToArray();
      }

      if (snapshot.Length == 0)
        return;

      var allTasks = Task.WhenAll(snapshot);
      var timeoutTask = Task.Delay(effectiveTimeout);

      var completedTask = await Task.WhenAny(allTasks, timeoutTask).ConfigureAwait(false);

      if (completedTask == allTasks)
      {
        // All tasks completed before timeout - await to propagate any exceptions
        await allTasks.ConfigureAwait(false);
      }
      // else: timeout reached, return without waiting further
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
    /// Safely invokes the logger callback, swallowing any exceptions to prevent
    /// logger failures from breaking the embedding flow.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void SafeLog(string message)
    {
      if (_logger == null)
        return;

      try
      {
        _logger.Invoke(message);
      }
      catch
      {
        // Swallow logger exceptions - logging failures should never break embedding
      }
    }

    /// <summary>
    /// Disposes the service synchronously, waiting up to 1 second for in-flight work to complete.
    /// For graceful shutdown with full drain, use <see cref="DisposeAsync"/> instead.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;

      // Unsubscribe first to prevent new work
      _memorySystem.MemoryMutated -= OnMemoryMutatedAsync;

      // Cancel in-flight work
      _disposeCts.Cancel();

      // Wait for pending tasks with timeout to avoid blocking forever
      Task[] snapshot;
      lock (_pendingLock)
      {
        snapshot = _pendingTasks.ToArray();
      }

      if (snapshot.Length > 0)
      {
        try
        {
          // Wait up to 1 second for in-flight work to complete
          Task.WaitAll(snapshot, TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
          // Tasks were cancelled or failed - expected during disposal
        }
      }

      _disposeCts.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the service, fully draining all in-flight embedding work.
    /// This is the preferred disposal method for graceful shutdown.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
      if (_disposed)
        return;

      _disposed = true;

      // Unsubscribe first to prevent new work
      _memorySystem.MemoryMutated -= OnMemoryMutatedAsync;

      // Cancel in-flight work
      _disposeCts.Cancel();

      // Wait for all pending tasks to complete (they should exit quickly due to cancellation)
      Task[] snapshot;
      lock (_pendingLock)
      {
        snapshot = _pendingTasks.ToArray();
      }

      if (snapshot.Length > 0)
      {
        try
        {
          await Task.WhenAll(snapshot).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
          // Expected during disposal
        }
        catch (AggregateException)
        {
          // Tasks failed - expected during disposal
        }
      }

      _disposeCts.Dispose();
    }
  }
}
