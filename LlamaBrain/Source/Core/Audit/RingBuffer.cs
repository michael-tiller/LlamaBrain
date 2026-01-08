using System;
using System.Collections;
using System.Collections.Generic;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// A generic fixed-capacity circular buffer with O(1) append and O(1) access.
  /// When the buffer is full, new items overwrite the oldest items.
  /// </summary>
  /// <typeparam name="T">The type of elements in the buffer.</typeparam>
  /// <remarks>
  /// This implementation is NOT thread-safe. External synchronization is required
  /// for concurrent access.
  ///
  /// Performance guarantees:
  /// - Append: O(1) - constant time regardless of buffer size
  /// - Access by index: O(1) - constant time regardless of position
  /// - Clear: O(n) - clears all references for GC
  /// - ToArray: O(n) - copies all elements
  /// </remarks>
  public sealed class RingBuffer<T> : IEnumerable<T>
  {
    private readonly T[] _buffer;
    private int _head; // Index where next item will be written
    private int _count; // Current number of items in buffer
    private long _totalAppended; // Total items ever appended (for statistics)

    /// <summary>
    /// Creates a new ring buffer with the specified capacity.
    /// </summary>
    /// <param name="capacity">Maximum number of items the buffer can hold. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is less than 1.</exception>
    public RingBuffer(int capacity)
    {
      if (capacity < 1)
        throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 1.");

      _buffer = new T[capacity];
      _head = 0;
      _count = 0;
      _totalAppended = 0;
    }

    /// <summary>
    /// Gets the maximum number of items this buffer can hold.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Gets the current number of items in the buffer.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets whether the buffer is empty.
    /// </summary>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// Gets whether the buffer is at full capacity.
    /// </summary>
    public bool IsFull => _count == _buffer.Length;

    /// <summary>
    /// Gets the total number of items ever appended to this buffer.
    /// This count includes items that have been overwritten.
    /// </summary>
    public long TotalAppended => _totalAppended;

    /// <summary>
    /// Gets the newest (most recently appended) item.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the buffer is empty.</exception>
    public T Newest
    {
      get
      {
        if (_count == 0)
          throw new InvalidOperationException("Buffer is empty.");

        // The newest item is at (head - 1), wrapped
        var newestIndex = (_head - 1 + _buffer.Length) % _buffer.Length;
        return _buffer[newestIndex];
      }
    }

    /// <summary>
    /// Gets the oldest item still in the buffer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the buffer is empty.</exception>
    public T Oldest
    {
      get
      {
        if (_count == 0)
          throw new InvalidOperationException("Buffer is empty.");

        // The oldest item is at (head - count), wrapped
        var oldestIndex = (_head - _count + _buffer.Length) % _buffer.Length;
        return _buffer[oldestIndex];
      }
    }

    /// <summary>
    /// Gets the item at the specified logical index (0 = oldest, Count-1 = newest).
    /// </summary>
    /// <param name="index">Logical index from 0 (oldest) to Count-1 (newest).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public T this[int index]
    {
      get
      {
        if (index < 0 || index >= _count)
          throw new ArgumentOutOfRangeException(nameof(index), index,
            $"Index must be between 0 and {_count - 1}.");

        // Map logical index to physical index
        // Oldest is at (head - count), so index 0 maps to (head - count)
        var physicalIndex = (_head - _count + index + _buffer.Length) % _buffer.Length;
        return _buffer[physicalIndex];
      }
    }

    /// <summary>
    /// Appends an item to the buffer. If the buffer is full, the oldest item is overwritten.
    /// </summary>
    /// <param name="item">The item to append.</param>
    public void Append(T item)
    {
      _buffer[_head] = item;
      _head = (_head + 1) % _buffer.Length;

      if (_count < _buffer.Length)
        _count++;

      _totalAppended++;
    }

    /// <summary>
    /// Clears all items from the buffer.
    /// </summary>
    public void Clear()
    {
      // Clear references to allow GC (important for reference types)
      Array.Clear(_buffer, 0, _buffer.Length);
      _head = 0;
      _count = 0;
      _totalAppended = 0;
    }

    /// <summary>
    /// Tries to get the item at the specified logical index.
    /// </summary>
    /// <param name="index">Logical index from 0 (oldest) to Count-1 (newest).</param>
    /// <param name="item">The item if found, otherwise default.</param>
    /// <returns>True if the index was valid and item was retrieved.</returns>
    public bool TryGetAt(int index, out T item)
    {
      if (index < 0 || index >= _count)
      {
        item = default!;
        return false;
      }

      var physicalIndex = (_head - _count + index + _buffer.Length) % _buffer.Length;
      item = _buffer[physicalIndex];
      return true;
    }

    /// <summary>
    /// Gets a range of items starting at the specified index.
    /// </summary>
    /// <param name="startIndex">Starting logical index (0 = oldest).</param>
    /// <param name="count">Number of items to retrieve.</param>
    /// <returns>Array containing the requested items in order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when range is invalid.</exception>
    public T[] GetRange(int startIndex, int count)
    {
      if (startIndex < 0)
        throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, "Start index cannot be negative.");

      if (count < 0)
        throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");

      if (count == 0)
        return Array.Empty<T>();

      if (startIndex >= _count)
        throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex,
          $"Start index must be less than {_count}.");

      if (startIndex + count > _count)
        throw new ArgumentOutOfRangeException(nameof(count), count,
          $"Range [{startIndex}, {startIndex + count}) exceeds buffer bounds [0, {_count}).");

      var result = new T[count];
      for (int i = 0; i < count; i++)
      {
        var physicalIndex = (_head - _count + startIndex + i + _buffer.Length) % _buffer.Length;
        result[i] = _buffer[physicalIndex];
      }

      return result;
    }

    /// <summary>
    /// Creates an array containing all items in order (oldest to newest).
    /// </summary>
    /// <returns>New array with all items.</returns>
    public T[] ToArray()
    {
      if (_count == 0)
        return Array.Empty<T>();

      var result = new T[_count];
      for (int i = 0; i < _count; i++)
      {
        var physicalIndex = (_head - _count + i + _buffer.Length) % _buffer.Length;
        result[i] = _buffer[physicalIndex];
      }

      return result;
    }

    /// <summary>
    /// Enumerates all items from oldest to newest.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
      for (int i = 0; i < _count; i++)
      {
        var physicalIndex = (_head - _count + i + _buffer.Length) % _buffer.Length;
        yield return _buffer[physicalIndex];
      }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}
