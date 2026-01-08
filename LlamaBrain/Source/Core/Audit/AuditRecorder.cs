using System;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Records NPC interaction audit records using per-NPC ring buffers.
  /// Each NPC maintains its own ring buffer with configurable capacity.
  /// </summary>
  /// <remarks>
  /// Thread-safety: This implementation is NOT thread-safe. External synchronization
  /// is required for concurrent access from multiple threads.
  ///
  /// Memory usage: Approximately O(N * M) where N is the number of tracked NPCs
  /// and M is the average buffer capacity.
  /// </remarks>
  public sealed class AuditRecorder : IAuditRecorder
  {
    private readonly Dictionary<string, RingBuffer<AuditRecord>> _buffers;
    private readonly Dictionary<string, int> _customCapacities;
    private readonly int _defaultCapacity;

    /// <summary>
    /// Creates a new AuditRecorder with the specified default buffer capacity.
    /// </summary>
    /// <param name="defaultCapacity">Default buffer capacity per NPC. Default is 50.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is less than 1.</exception>
    public AuditRecorder(int defaultCapacity = 50)
    {
      if (defaultCapacity < 1)
        throw new ArgumentOutOfRangeException(nameof(defaultCapacity), defaultCapacity,
          "Capacity must be at least 1.");

      _defaultCapacity = defaultCapacity;
      _buffers = new Dictionary<string, RingBuffer<AuditRecord>>(StringComparer.Ordinal);
      _customCapacities = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public int DefaultCapacity => _defaultCapacity;

    /// <inheritdoc/>
    public int TotalRecordCount
    {
      get
      {
        int total = 0;
        foreach (var buffer in _buffers.Values)
          total += buffer.Count;
        return total;
      }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> TrackedNpcIds => _buffers.Keys.ToArray();

    /// <inheritdoc/>
    public void Record(AuditRecord record)
    {
      if (record == null)
        throw new ArgumentNullException(nameof(record));

      var npcId = record.NpcId ?? "";
      var buffer = GetOrCreateBuffer(npcId);
      buffer.Append(record);
    }

    /// <inheritdoc/>
    public int GetRecordCount(string npcId)
    {
      if (npcId == null)
        return 0;

      return _buffers.TryGetValue(npcId, out var buffer) ? buffer.Count : 0;
    }

    /// <inheritdoc/>
    public AuditRecord[] GetRecords(string npcId)
    {
      if (npcId == null || !_buffers.TryGetValue(npcId, out var buffer))
        return Array.Empty<AuditRecord>();

      var records = buffer.ToArray();

      // Set buffer indices
      for (int i = 0; i < records.Length; i++)
        records[i].BufferIndex = i;

      return records;
    }

    /// <inheritdoc/>
    public AuditRecord[] GetAllRecords()
    {
      var allRecords = new List<AuditRecord>();

      foreach (var kvp in _buffers)
      {
        var records = kvp.Value.ToArray();
        for (int i = 0; i < records.Length; i++)
          records[i].BufferIndex = i;
        allRecords.AddRange(records);
      }

      // Sort by capture time (deterministic ordering)
      allRecords.Sort((a, b) => a.CapturedAtUtcTicks.CompareTo(b.CapturedAtUtcTicks));

      return allRecords.ToArray();
    }

    /// <inheritdoc/>
    public AuditRecord? GetLatestRecord(string npcId)
    {
      if (npcId == null || !_buffers.TryGetValue(npcId, out var buffer))
        return null;

      if (buffer.IsEmpty)
        return null;

      return buffer.Newest;
    }

    /// <inheritdoc/>
    public void ClearRecords(string npcId)
    {
      if (npcId != null && _buffers.TryGetValue(npcId, out var buffer))
      {
        buffer.Clear();
        _buffers.Remove(npcId);
      }
    }

    /// <inheritdoc/>
    public void ClearAll()
    {
      _buffers.Clear();
      _customCapacities.Clear();
    }

    /// <inheritdoc/>
    public void SetCapacity(string npcId, int capacity)
    {
      if (capacity < 1)
        throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
          "Capacity must be at least 1.");

      npcId ??= "";
      _customCapacities[npcId] = capacity;

      // If buffer exists, resize it
      if (_buffers.TryGetValue(npcId, out var existingBuffer))
      {
        if (existingBuffer.Capacity != capacity)
        {
          // Create new buffer with new capacity, copy most recent records
          var newBuffer = new RingBuffer<AuditRecord>(capacity);
          var records = existingBuffer.ToArray();

          // Keep only the most recent records that fit
          var startIndex = Math.Max(0, records.Length - capacity);
          for (int i = startIndex; i < records.Length; i++)
            newBuffer.Append(records[i]);

          _buffers[npcId] = newBuffer;
        }
      }
    }

    /// <inheritdoc/>
    public int GetCapacity(string npcId)
    {
      if (npcId != null && _customCapacities.TryGetValue(npcId, out var capacity))
        return capacity;

      return _defaultCapacity;
    }

    /// <summary>
    /// Gets or creates a ring buffer for the specified NPC.
    /// </summary>
    private RingBuffer<AuditRecord> GetOrCreateBuffer(string npcId)
    {
      if (!_buffers.TryGetValue(npcId, out var buffer))
      {
        var capacity = GetCapacity(npcId);
        buffer = new RingBuffer<AuditRecord>(capacity);
        _buffers[npcId] = buffer;
      }

      return buffer;
    }
  }
}
