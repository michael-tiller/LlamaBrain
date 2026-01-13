using System.Collections.Generic;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Interface for persisting audit records to durable storage.
  /// Implementations may use file system, database, or other storage backends.
  /// </summary>
  public interface IAuditPersistence
  {
    /// <summary>
    /// Persists an audit record to durable storage.
    /// This operation should be durable - the record should survive process restart.
    /// </summary>
    /// <param name="record">The audit record to persist</param>
    void Persist(AuditRecord record);

    /// <summary>
    /// Ensures all buffered records are written to storage.
    /// Call this before graceful shutdown to prevent data loss.
    /// </summary>
    void Flush();

    /// <summary>
    /// Gets the list of log file paths managed by this persistence instance.
    /// </summary>
    /// <returns>Ordered list of file paths, oldest first</returns>
    IReadOnlyList<string> GetLogFiles();

    /// <summary>
    /// Gets the total size in bytes of all log files.
    /// </summary>
    /// <returns>Total size in bytes</returns>
    long GetTotalSizeBytes();
  }
}
