using System;

namespace LlamaBrain.Persona.MemoryTypes
{
  /// <summary>
  /// Base class for all memory entries in the structured memory system.
  /// </summary>
  [Serializable]
  public abstract class MemoryEntry
  {
    /// <summary>
    /// Unique identifier for this memory entry.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// The authority level of this memory.
    /// </summary>
    public abstract MemoryAuthority Authority { get; }

    /// <summary>
    /// When this memory was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this memory was last accessed/used.
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The content of this memory as a string for prompt injection.
    /// </summary>
    public abstract string Content { get; }

    /// <summary>
    /// Optional category/tag for organizing memories.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Source that created this memory.
    /// </summary>
    public MutationSource Source { get; set; } = MutationSource.Designer;

    /// <summary>
    /// Marks this memory as accessed, updating LastAccessedAt.
    /// </summary>
    public void MarkAccessed()
    {
      LastAccessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the memory content for prompt injection.
    /// </summary>
    /// <returns>The content of this memory entry as a string</returns>
    public override string ToString() => Content;
  }

  /// <summary>
  /// Result of a memory mutation attempt.
  /// </summary>
  public class MutationResult
  {
    /// <summary>
    /// Whether the mutation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Reason for failure if not successful.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// The memory entry that was affected (if any).
    /// </summary>
    public MemoryEntry? AffectedEntry { get; set; }

    /// <summary>
    /// Creates a successful mutation result.
    /// </summary>
    /// <param name="entry">The memory entry that was affected</param>
    /// <returns>A MutationResult indicating success</returns>
    public static MutationResult Succeeded(MemoryEntry entry) => new MutationResult()
    {
      Success = true,
      AffectedEntry = entry
    };

    /// <summary>
    /// Creates a failed mutation result.
    /// </summary>
    /// <param name="reason">The reason for the failure</param>
    /// <returns>A MutationResult indicating failure with the specified reason</returns>
    public static MutationResult Failed(string reason) => new MutationResult()
    {
      Success = false,
      FailureReason = reason
    };
  }
}
