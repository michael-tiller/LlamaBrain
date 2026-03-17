using System;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for AuditRecorder implementation.
  /// Verifies per-NPC ring buffer behavior and record management.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class AuditRecorderTests
  {
    #region Constructor Tests

    [Test]
    public void Constructor_DefaultCapacity_Is50()
    {
      // Act
      var recorder = new AuditRecorder();

      // Assert
      Assert.That(recorder.DefaultCapacity, Is.EqualTo(50));
    }

    [Test]
    public void Constructor_CustomCapacity_SetsDefault()
    {
      // Act
      var recorder = new AuditRecorder(100);

      // Assert
      Assert.That(recorder.DefaultCapacity, Is.EqualTo(100));
    }

    [Test]
    public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRange()
    {
      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => new AuditRecorder(0));
      Assert.Throws<ArgumentOutOfRangeException>(() => new AuditRecorder(-1));
    }

    [Test]
    public void Constructor_InitiallyEmpty()
    {
      // Act
      var recorder = new AuditRecorder();

      // Assert
      Assert.That(recorder.TotalRecordCount, Is.EqualTo(0));
      Assert.That(recorder.TrackedNpcIds, Is.Empty);
    }

    #endregion

    #region Record Tests

    [Test]
    public void Record_SingleRecord_IncreasesCount()
    {
      // Arrange
      var recorder = new AuditRecorder();
      var record = CreateRecord("npc-1");

      // Act
      recorder.Record(record);

      // Assert
      Assert.That(recorder.TotalRecordCount, Is.EqualTo(1));
      Assert.That(recorder.GetRecordCount("npc-1"), Is.EqualTo(1));
    }

    [Test]
    public void Record_MultipleRecords_SameNpc_AddsToBuffer()
    {
      // Arrange
      var recorder = new AuditRecorder();

      // Act
      recorder.Record(CreateRecord("npc-1", "input-1"));
      recorder.Record(CreateRecord("npc-1", "input-2"));
      recorder.Record(CreateRecord("npc-1", "input-3"));

      // Assert
      Assert.That(recorder.TotalRecordCount, Is.EqualTo(3));
      Assert.That(recorder.GetRecordCount("npc-1"), Is.EqualTo(3));
    }

    [Test]
    public void Record_MultipleRecords_DifferentNpcs_CreatesSeparateBuffers()
    {
      // Arrange
      var recorder = new AuditRecorder();

      // Act
      recorder.Record(CreateRecord("npc-1", "input-1"));
      recorder.Record(CreateRecord("npc-2", "input-2"));
      recorder.Record(CreateRecord("npc-1", "input-3"));

      // Assert
      Assert.That(recorder.TotalRecordCount, Is.EqualTo(3));
      Assert.That(recorder.GetRecordCount("npc-1"), Is.EqualTo(2));
      Assert.That(recorder.GetRecordCount("npc-2"), Is.EqualTo(1));
      Assert.That(recorder.TrackedNpcIds, Has.Count.EqualTo(2));
    }

    [Test]
    public void Record_NullRecord_ThrowsArgumentNull()
    {
      // Arrange
      var recorder = new AuditRecorder();

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => recorder.Record(null!));
    }

    [Test]
    public void Record_BeyondCapacity_OverwritesOldest()
    {
      // Arrange
      var recorder = new AuditRecorder(3);

      // Act - Record 5 records with capacity 3
      for (int i = 1; i <= 5; i++)
        recorder.Record(CreateRecord("npc-1", $"input-{i}"));

      // Assert - Should only have last 3
      Assert.That(recorder.GetRecordCount("npc-1"), Is.EqualTo(3));
      var records = recorder.GetRecords("npc-1");
      Assert.That(records[0].PlayerInput, Is.EqualTo("input-3"));
      Assert.That(records[1].PlayerInput, Is.EqualTo("input-4"));
      Assert.That(records[2].PlayerInput, Is.EqualTo("input-5"));
    }

    #endregion

    #region GetRecords Tests

    [Test]
    public void GetRecords_EmptyNpc_ReturnsEmptyArray()
    {
      // Arrange
      var recorder = new AuditRecorder();

      // Act
      var records = recorder.GetRecords("npc-unknown");

      // Assert
      Assert.That(records, Is.Empty);
    }

    [Test]
    public void GetRecords_ReturnsInOrder_OldestToNewest()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "first"));
      recorder.Record(CreateRecord("npc-1", "second"));
      recorder.Record(CreateRecord("npc-1", "third"));

      // Act
      var records = recorder.GetRecords("npc-1");

      // Assert
      Assert.That(records.Length, Is.EqualTo(3));
      Assert.That(records[0].PlayerInput, Is.EqualTo("first"));
      Assert.That(records[1].PlayerInput, Is.EqualTo("second"));
      Assert.That(records[2].PlayerInput, Is.EqualTo("third"));
    }

    [Test]
    public void GetRecords_SetsBufferIndex()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "first"));
      recorder.Record(CreateRecord("npc-1", "second"));
      recorder.Record(CreateRecord("npc-1", "third"));

      // Act
      var records = recorder.GetRecords("npc-1");

      // Assert
      Assert.That(records[0].BufferIndex, Is.EqualTo(0));
      Assert.That(records[1].BufferIndex, Is.EqualTo(1));
      Assert.That(records[2].BufferIndex, Is.EqualTo(2));
    }

    #endregion

    #region GetAllRecords Tests

    [Test]
    public void GetAllRecords_Empty_ReturnsEmptyArray()
    {
      // Arrange
      var recorder = new AuditRecorder();

      // Act
      var records = recorder.GetAllRecords();

      // Assert
      Assert.That(records, Is.Empty);
    }

    [Test]
    public void GetAllRecords_MultipleNpcs_ReturnsAllInTimeOrder()
    {
      // Arrange
      var recorder = new AuditRecorder();
      var record1 = CreateRecord("npc-1", "first", 100);
      var record2 = CreateRecord("npc-2", "second", 200);
      var record3 = CreateRecord("npc-1", "third", 300);

      recorder.Record(record1);
      recorder.Record(record2);
      recorder.Record(record3);

      // Act
      var records = recorder.GetAllRecords();

      // Assert
      Assert.That(records.Length, Is.EqualTo(3));
      Assert.That(records[0].CapturedAtUtcTicks, Is.EqualTo(100));
      Assert.That(records[1].CapturedAtUtcTicks, Is.EqualTo(200));
      Assert.That(records[2].CapturedAtUtcTicks, Is.EqualTo(300));
    }

    #endregion

    #region GetLatestRecord Tests

    [Test]
    public void GetLatestRecord_EmptyNpc_ReturnsNull()
    {
      // Arrange
      var recorder = new AuditRecorder();

      // Act
      var latest = recorder.GetLatestRecord("npc-unknown");

      // Assert
      Assert.That(latest, Is.Null);
    }

    [Test]
    public void GetLatestRecord_ReturnsMostRecent()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "first"));
      recorder.Record(CreateRecord("npc-1", "second"));
      recorder.Record(CreateRecord("npc-1", "third"));

      // Act
      var latest = recorder.GetLatestRecord("npc-1");

      // Assert
      Assert.That(latest, Is.Not.Null);
      Assert.That(latest!.PlayerInput, Is.EqualTo("third"));
    }

    #endregion

    #region ClearRecords Tests

    [Test]
    public void ClearRecords_RemovesOnlySpecifiedNpc()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "input-1"));
      recorder.Record(CreateRecord("npc-2", "input-2"));

      // Act
      recorder.ClearRecords("npc-1");

      // Assert
      Assert.That(recorder.GetRecordCount("npc-1"), Is.EqualTo(0));
      Assert.That(recorder.GetRecordCount("npc-2"), Is.EqualTo(1));
      Assert.That(recorder.TotalRecordCount, Is.EqualTo(1));
    }

    [Test]
    public void ClearRecords_UnknownNpc_DoesNotThrow()
    {
      // Arrange
      var recorder = new AuditRecorder();

      // Act & Assert
      Assert.DoesNotThrow(() => recorder.ClearRecords("unknown-npc"));
    }

    #endregion

    #region ClearAll Tests

    [Test]
    public void ClearAll_RemovesAllRecords()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "input-1"));
      recorder.Record(CreateRecord("npc-2", "input-2"));
      recorder.Record(CreateRecord("npc-3", "input-3"));

      // Act
      recorder.ClearAll();

      // Assert
      Assert.That(recorder.TotalRecordCount, Is.EqualTo(0));
      Assert.That(recorder.TrackedNpcIds, Is.Empty);
    }

    #endregion

    #region Capacity Tests

    [Test]
    public void GetCapacity_UnknownNpc_ReturnsDefault()
    {
      // Arrange
      var recorder = new AuditRecorder(100);

      // Act
      var capacity = recorder.GetCapacity("unknown-npc");

      // Assert
      Assert.That(capacity, Is.EqualTo(100));
    }

    [Test]
    public void SetCapacity_CustomCapacity_IsUsed()
    {
      // Arrange
      var recorder = new AuditRecorder(50);

      // Act
      recorder.SetCapacity("npc-special", 10);
      for (int i = 1; i <= 15; i++)
        recorder.Record(CreateRecord("npc-special", $"input-{i}"));

      // Assert
      Assert.That(recorder.GetCapacity("npc-special"), Is.EqualTo(10));
      Assert.That(recorder.GetRecordCount("npc-special"), Is.EqualTo(10));
    }

    [Test]
    public void SetCapacity_InvalidCapacity_ThrowsArgumentOutOfRange()
    {
      // Arrange
      var recorder = new AuditRecorder();

      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => recorder.SetCapacity("npc-1", 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => recorder.SetCapacity("npc-1", -1));
    }

    [Test]
    public void SetCapacity_SmallerThanExisting_TruncatesRecords()
    {
      // Arrange
      var recorder = new AuditRecorder(50);
      for (int i = 1; i <= 10; i++)
        recorder.Record(CreateRecord("npc-1", $"input-{i}"));

      // Act
      recorder.SetCapacity("npc-1", 3);

      // Assert
      Assert.That(recorder.GetRecordCount("npc-1"), Is.EqualTo(3));
      var records = recorder.GetRecords("npc-1");
      // Should keep the most recent 3
      Assert.That(records[0].PlayerInput, Is.EqualTo("input-8"));
      Assert.That(records[1].PlayerInput, Is.EqualTo("input-9"));
      Assert.That(records[2].PlayerInput, Is.EqualTo("input-10"));
    }

    #endregion

    #region TrackedNpcIds Tests

    [Test]
    public void TrackedNpcIds_ReturnsAllNpcsWithRecords()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "input"));
      recorder.Record(CreateRecord("npc-2", "input"));
      recorder.Record(CreateRecord("npc-3", "input"));

      // Act
      var npcIds = recorder.TrackedNpcIds;

      // Assert
      Assert.That(npcIds, Has.Count.EqualTo(3));
      Assert.That(npcIds, Does.Contain("npc-1"));
      Assert.That(npcIds, Does.Contain("npc-2"));
      Assert.That(npcIds, Does.Contain("npc-3"));
    }

    [Test]
    public void TrackedNpcIds_ReturnsNewCopyEachTime()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "input"));

      // Act
      var ids1 = recorder.TrackedNpcIds;
      var ids2 = recorder.TrackedNpcIds;

      // Assert
      Assert.That(ids1, Is.Not.SameAs(ids2));
    }

    #endregion

    #region Integration Tests

    [Test]
    public void MultipleOperations_MaintainsConsistentState()
    {
      // Arrange
      var recorder = new AuditRecorder(5);

      // Act - Mix of operations
      for (int i = 0; i < 10; i++)
        recorder.Record(CreateRecord("npc-1", $"a-{i}"));

      for (int i = 0; i < 3; i++)
        recorder.Record(CreateRecord("npc-2", $"b-{i}"));

      recorder.ClearRecords("npc-2");
      recorder.SetCapacity("npc-3", 2);

      for (int i = 0; i < 5; i++)
        recorder.Record(CreateRecord("npc-3", $"c-{i}"));

      // Assert
      Assert.That(recorder.GetRecordCount("npc-1"), Is.EqualTo(5)); // Ring buffer full
      Assert.That(recorder.GetRecordCount("npc-2"), Is.EqualTo(0)); // Cleared
      Assert.That(recorder.GetRecordCount("npc-3"), Is.EqualTo(2)); // Custom capacity
      Assert.That(recorder.TotalRecordCount, Is.EqualTo(7));
    }

    #endregion

    #region Helper Methods

    private static AuditRecord CreateRecord(string npcId, string playerInput = "test", long? capturedAtUtcTicks = null)
    {
      return new AuditRecord
      {
        RecordId = Guid.NewGuid().ToString("N").Substring(0, 16),
        NpcId = npcId,
        PlayerInput = playerInput,
        CapturedAtUtcTicks = capturedAtUtcTicks ?? DateTimeOffset.UtcNow.UtcTicks
      };
    }

    #endregion
  }
}
