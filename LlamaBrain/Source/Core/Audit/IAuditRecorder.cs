using System;
using System.Collections.Generic;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Interface for recording and retrieving audit records of NPC interactions.
  /// Implementations should maintain per-NPC ring buffers with configurable capacity.
  /// </summary>
  /// <remarks>
  /// The audit recorder captures interaction state for debugging and bug reproduction.
  /// It uses a ring buffer strategy to maintain bounded memory usage while preserving
  /// the most recent interactions.
  /// </remarks>
  public interface IAuditRecorder
  {
    /// <summary>
    /// Gets the default buffer capacity for each NPC.
    /// </summary>
    int DefaultCapacity { get; }

    /// <summary>
    /// Gets the total number of records currently stored across all NPCs.
    /// </summary>
    int TotalRecordCount { get; }

    /// <summary>
    /// Gets the identifiers of all NPCs with audit records.
    /// </summary>
    IReadOnlyList<string> TrackedNpcIds { get; }

    /// <summary>
    /// Records an audit record for the specified NPC.
    /// </summary>
    /// <param name="record">The audit record to store.</param>
    /// <exception cref="ArgumentNullException">Thrown when record is null.</exception>
    void Record(AuditRecord record);

    /// <summary>
    /// Gets the number of records currently stored for the specified NPC.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    /// <returns>Number of records, or 0 if NPC has no records.</returns>
    int GetRecordCount(string npcId);

    /// <summary>
    /// Gets all records for the specified NPC, ordered from oldest to newest.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    /// <returns>Array of records, or empty array if NPC has no records.</returns>
    AuditRecord[] GetRecords(string npcId);

    /// <summary>
    /// Gets all records across all NPCs, ordered by capture time.
    /// </summary>
    /// <returns>Array of all records, ordered chronologically.</returns>
    AuditRecord[] GetAllRecords();

    /// <summary>
    /// Gets the most recent record for the specified NPC.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    /// <returns>The most recent record, or null if NPC has no records.</returns>
    AuditRecord? GetLatestRecord(string npcId);

    /// <summary>
    /// Clears all records for the specified NPC.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    void ClearRecords(string npcId);

    /// <summary>
    /// Clears all records for all NPCs.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Sets a custom buffer capacity for the specified NPC.
    /// Existing records may be truncated if the new capacity is smaller.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    /// <param name="capacity">The new buffer capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is less than 1.</exception>
    void SetCapacity(string npcId, int capacity);

    /// <summary>
    /// Gets the buffer capacity for the specified NPC.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    /// <returns>The buffer capacity, or DefaultCapacity if NPC has no custom setting.</returns>
    int GetCapacity(string npcId);
  }
}
