using System;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for RingBuffer&lt;T&gt; generic circular buffer implementation.
  /// Verifies O(1) append/access guarantees and correct wrap-around behavior.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class RingBufferTests
  {
    #region Constructor Tests

    [Test]
    public void Constructor_WithValidCapacity_CreatesEmptyBuffer()
    {
      // Act
      var buffer = new RingBuffer<int>(10);

      // Assert
      Assert.That(buffer.Count, Is.EqualTo(0));
      Assert.That(buffer.Capacity, Is.EqualTo(10));
      Assert.That(buffer.IsEmpty, Is.True);
      Assert.That(buffer.IsFull, Is.False);
    }

    [Test]
    public void Constructor_WithZeroCapacity_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
    }

    [Test]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(-1));
    }

    #endregion

    #region Append Tests

    [Test]
    public void Append_SingleItem_IncreasesCount()
    {
      // Arrange
      var buffer = new RingBuffer<string>(5);

      // Act
      buffer.Append("first");

      // Assert
      Assert.That(buffer.Count, Is.EqualTo(1));
      Assert.That(buffer.IsEmpty, Is.False);
    }

    [Test]
    public void Append_MultipleItems_IncreasesCountCorrectly()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);

      // Act
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);

      // Assert
      Assert.That(buffer.Count, Is.EqualTo(3));
    }

    [Test]
    public void Append_FillToCapacity_BufferIsFull()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);

      // Act
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);

      // Assert
      Assert.That(buffer.Count, Is.EqualTo(3));
      Assert.That(buffer.IsFull, Is.True);
    }

    [Test]
    public void Append_BeyondCapacity_OverwritesOldest()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);

      // Act
      buffer.Append(4);

      // Assert
      Assert.That(buffer.Count, Is.EqualTo(3));
      Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 2, 3, 4 }));
    }

    [Test]
    public void Append_MultipleWraps_MaintainsOrder()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);

      // Act - Fill and wrap multiple times
      for (int i = 1; i <= 10; i++)
      {
        buffer.Append(i);
      }

      // Assert - Should contain last 3 items: 8, 9, 10
      Assert.That(buffer.Count, Is.EqualTo(3));
      Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 8, 9, 10 }));
    }

    #endregion

    #region Access Tests (Indexer)

    [Test]
    public void Indexer_ValidIndex_ReturnsCorrectItem()
    {
      // Arrange
      var buffer = new RingBuffer<string>(5);
      buffer.Append("first");
      buffer.Append("second");
      buffer.Append("third");

      // Act & Assert
      Assert.That(buffer[0], Is.EqualTo("first"));
      Assert.That(buffer[1], Is.EqualTo("second"));
      Assert.That(buffer[2], Is.EqualTo("third"));
    }

    [Test]
    public void Indexer_AfterWrap_ReturnsCorrectItem()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);
      buffer.Append(4); // Wraps, overwrites 1

      // Act & Assert
      Assert.That(buffer[0], Is.EqualTo(2)); // Oldest remaining
      Assert.That(buffer[1], Is.EqualTo(3));
      Assert.That(buffer[2], Is.EqualTo(4)); // Newest
    }

    [Test]
    public void Indexer_NegativeIndex_ThrowsArgumentOutOfRange()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);

      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = buffer[-1]; });
    }

    [Test]
    public void Indexer_IndexEqualToCount_ThrowsArgumentOutOfRange()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);
      buffer.Append(2);

      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = buffer[2]; });
    }

    [Test]
    public void Indexer_EmptyBuffer_ThrowsArgumentOutOfRange()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);

      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = buffer[0]; });
    }

    #endregion

    #region Newest/Oldest Tests

    [Test]
    public void Newest_AfterAppend_ReturnsLastAppended()
    {
      // Arrange
      var buffer = new RingBuffer<string>(5);
      buffer.Append("first");
      buffer.Append("second");
      buffer.Append("third");

      // Act & Assert
      Assert.That(buffer.Newest, Is.EqualTo("third"));
    }

    [Test]
    public void Oldest_AfterAppend_ReturnsFirstInBuffer()
    {
      // Arrange
      var buffer = new RingBuffer<string>(5);
      buffer.Append("first");
      buffer.Append("second");
      buffer.Append("third");

      // Act & Assert
      Assert.That(buffer.Oldest, Is.EqualTo("first"));
    }

    [Test]
    public void Oldest_AfterWrap_ReturnsOldestRemaining()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);
      buffer.Append(4); // Wraps

      // Act & Assert
      Assert.That(buffer.Oldest, Is.EqualTo(2));
    }

    [Test]
    public void Newest_EmptyBuffer_ThrowsInvalidOperation()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);

      // Act & Assert
      Assert.Throws<InvalidOperationException>(() => { var _ = buffer.Newest; });
    }

    [Test]
    public void Oldest_EmptyBuffer_ThrowsInvalidOperation()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);

      // Act & Assert
      Assert.Throws<InvalidOperationException>(() => { var _ = buffer.Oldest; });
    }

    #endregion

    #region Clear Tests

    [Test]
    public void Clear_WithItems_ResetsToEmpty()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);

      // Act
      buffer.Clear();

      // Assert
      Assert.That(buffer.Count, Is.EqualTo(0));
      Assert.That(buffer.IsEmpty, Is.True);
      Assert.That(buffer.Capacity, Is.EqualTo(5)); // Capacity unchanged
    }

    [Test]
    public void Clear_EmptyBuffer_RemainsEmpty()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);

      // Act
      buffer.Clear();

      // Assert
      Assert.That(buffer.Count, Is.EqualTo(0));
      Assert.That(buffer.IsEmpty, Is.True);
    }

    [Test]
    public void Clear_CanAppendAfterClear()
    {
      // Arrange
      var buffer = new RingBuffer<string>(3);
      buffer.Append("old1");
      buffer.Append("old2");
      buffer.Clear();

      // Act
      buffer.Append("new1");

      // Assert
      Assert.That(buffer.Count, Is.EqualTo(1));
      Assert.That(buffer[0], Is.EqualTo("new1"));
    }

    #endregion

    #region ToArray Tests

    [Test]
    public void ToArray_EmptyBuffer_ReturnsEmptyArray()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);

      // Act
      var array = buffer.ToArray();

      // Assert
      Assert.That(array, Is.Empty);
    }

    [Test]
    public void ToArray_PartiallyFilled_ReturnsItemsInOrder()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);

      // Act
      var array = buffer.ToArray();

      // Assert
      Assert.That(array, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ToArray_AfterWrap_ReturnsItemsInCorrectOrder()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);
      buffer.Append(4);
      buffer.Append(5);

      // Act
      var array = buffer.ToArray();

      // Assert
      Assert.That(array, Is.EqualTo(new[] { 3, 4, 5 }));
    }

    [Test]
    public void ToArray_ReturnsNewArrayEachTime()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);

      // Act
      var array1 = buffer.ToArray();
      var array2 = buffer.ToArray();

      // Assert
      Assert.That(array1, Is.Not.SameAs(array2));
    }

    #endregion

    #region Enumeration Tests

    [Test]
    public void Enumeration_EmptyBuffer_YieldsNoItems()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);

      // Act
      var items = buffer.ToList();

      // Assert
      Assert.That(items, Is.Empty);
    }

    [Test]
    public void Enumeration_WithItems_YieldsInOrder()
    {
      // Arrange
      var buffer = new RingBuffer<string>(5);
      buffer.Append("a");
      buffer.Append("b");
      buffer.Append("c");

      // Act
      var items = buffer.ToList();

      // Assert
      Assert.That(items, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void Enumeration_AfterWrap_YieldsInCorrectOrder()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);
      for (int i = 1; i <= 7; i++)
        buffer.Append(i);

      // Act
      var items = buffer.ToList();

      // Assert
      Assert.That(items, Is.EqualTo(new[] { 5, 6, 7 }));
    }

    #endregion

    #region TryGetAt Tests

    [Test]
    public void TryGetAt_ValidIndex_ReturnsTrue()
    {
      // Arrange
      var buffer = new RingBuffer<string>(5);
      buffer.Append("test");

      // Act
      var success = buffer.TryGetAt(0, out var item);

      // Assert
      Assert.That(success, Is.True);
      Assert.That(item, Is.EqualTo("test"));
    }

    [Test]
    public void TryGetAt_InvalidIndex_ReturnsFalse()
    {
      // Arrange
      var buffer = new RingBuffer<string>(5);
      buffer.Append("test");

      // Act
      var success = buffer.TryGetAt(1, out var item);

      // Assert
      Assert.That(success, Is.False);
      Assert.That(item, Is.EqualTo(default(string)));
    }

    [Test]
    public void TryGetAt_EmptyBuffer_ReturnsFalse()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);

      // Act
      var success = buffer.TryGetAt(0, out var item);

      // Assert
      Assert.That(success, Is.False);
      Assert.That(item, Is.EqualTo(default(int)));
    }

    #endregion

    #region GetRange Tests

    [Test]
    public void GetRange_ValidRange_ReturnsCorrectItems()
    {
      // Arrange
      var buffer = new RingBuffer<int>(10);
      for (int i = 1; i <= 5; i++)
        buffer.Append(i);

      // Act
      var range = buffer.GetRange(1, 3);

      // Assert
      Assert.That(range, Is.EqualTo(new[] { 2, 3, 4 }));
    }

    [Test]
    public void GetRange_AfterWrap_ReturnsCorrectItems()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);
      for (int i = 1; i <= 5; i++)
        buffer.Append(i);
      // Buffer contains: 3, 4, 5

      // Act
      var range = buffer.GetRange(0, 2);

      // Assert
      Assert.That(range, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void GetRange_ZeroCount_ReturnsEmptyArray()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);

      // Act
      var range = buffer.GetRange(0, 0);

      // Assert
      Assert.That(range, Is.Empty);
    }

    [Test]
    public void GetRange_InvalidStart_ThrowsArgumentOutOfRange()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);

      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetRange(5, 1));
    }

    [Test]
    public void GetRange_CountExceedsAvailable_ThrowsArgumentOutOfRange()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);
      buffer.Append(2);

      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetRange(0, 5));
    }

    #endregion

    #region TotalAppended Tests

    [Test]
    public void TotalAppended_InitiallyZero()
    {
      // Arrange & Act
      var buffer = new RingBuffer<int>(5);

      // Assert
      Assert.That(buffer.TotalAppended, Is.EqualTo(0));
    }

    [Test]
    public void TotalAppended_IncreasesWithEachAppend()
    {
      // Arrange
      var buffer = new RingBuffer<int>(3);

      // Act
      buffer.Append(1);
      buffer.Append(2);
      buffer.Append(3);
      buffer.Append(4);
      buffer.Append(5);

      // Assert - Should track all appends, not just current count
      Assert.That(buffer.TotalAppended, Is.EqualTo(5));
      Assert.That(buffer.Count, Is.EqualTo(3)); // Only 3 fit
    }

    [Test]
    public void TotalAppended_ResetByClear()
    {
      // Arrange
      var buffer = new RingBuffer<int>(5);
      buffer.Append(1);
      buffer.Append(2);

      // Act
      buffer.Clear();

      // Assert
      Assert.That(buffer.TotalAppended, Is.EqualTo(0));
    }

    #endregion

    #region Reference Type Tests

    [Test]
    public void Append_ReferenceType_StoresReference()
    {
      // Arrange
      var buffer = new RingBuffer<TestItem>(5);
      var item = new TestItem { Id = 1, Name = "test" };

      // Act
      buffer.Append(item);

      // Assert
      Assert.That(buffer[0], Is.SameAs(item));
    }

    [Test]
    public void Append_NullReference_AllowsNull()
    {
      // Arrange
      var buffer = new RingBuffer<string?>(5);

      // Act
      buffer.Append(null);
      buffer.Append("test");
      buffer.Append(null);

      // Assert
      Assert.That(buffer[0], Is.Null);
      Assert.That(buffer[1], Is.EqualTo("test"));
      Assert.That(buffer[2], Is.Null);
    }

    #endregion

    #region Performance Tests (O(1) guarantees)

    [Test]
    [Category("Performance")]
    public void Append_IsO1_ConsistentTime()
    {
      // This test verifies O(1) append by checking that time doesn't grow with buffer size
      var smallBuffer = new RingBuffer<int>(10);
      var largeBuffer = new RingBuffer<int>(10000);

      // Warm up
      for (int i = 0; i < 1000; i++)
      {
        smallBuffer.Append(i % 10);
        largeBuffer.Append(i % 10000);
      }
      smallBuffer.Clear();
      largeBuffer.Clear();

      // Time many appends
      const int iterations = 10000;

      var sw1 = System.Diagnostics.Stopwatch.StartNew();
      for (int i = 0; i < iterations; i++)
        smallBuffer.Append(i);
      sw1.Stop();

      var sw2 = System.Diagnostics.Stopwatch.StartNew();
      for (int i = 0; i < iterations; i++)
        largeBuffer.Append(i);
      sw2.Stop();

      // Both should complete in similar time (within 3x factor to account for variance)
      Assert.That(sw2.ElapsedTicks, Is.LessThan(sw1.ElapsedTicks * 3 + 1000),
        $"Large buffer append time ({sw2.ElapsedTicks}) should be similar to small ({sw1.ElapsedTicks})");
    }

    [Test]
    [Category("Performance")]
    public void Access_IsO1_ConsistentTime()
    {
      // Fill a large buffer
      var buffer = new RingBuffer<int>(10000);
      for (int i = 0; i < 10000; i++)
        buffer.Append(i);

      // Accessing first, middle, and last should be similar time
      const int iterations = 100000;
      int sum = 0;

      var sw1 = System.Diagnostics.Stopwatch.StartNew();
      for (int i = 0; i < iterations; i++)
        sum += buffer[0];
      sw1.Stop();

      var sw2 = System.Diagnostics.Stopwatch.StartNew();
      for (int i = 0; i < iterations; i++)
        sum += buffer[5000];
      sw2.Stop();

      var sw3 = System.Diagnostics.Stopwatch.StartNew();
      for (int i = 0; i < iterations; i++)
        sum += buffer[9999];
      sw3.Stop();

      // All should be similar (within 2x factor)
      Assert.That(sw2.ElapsedTicks, Is.LessThan(sw1.ElapsedTicks * 2 + 1000));
      Assert.That(sw3.ElapsedTicks, Is.LessThan(sw1.ElapsedTicks * 2 + 1000));

      // Prevent optimization
      Assert.That(sum, Is.Not.EqualTo(0));
    }

    #endregion

    #region Helper Classes

    private class TestItem
    {
      public int Id { get; set; }
      public string Name { get; set; } = "";
    }

    #endregion
  }
}
