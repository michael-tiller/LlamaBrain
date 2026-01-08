using System;
using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for AuditRecordBuilder fluent builder pattern.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class AuditRecordBuilderTests
  {
    #region Basic Build Tests

    [Test]
    public void Build_WithMinimalData_CreatesRecord()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithNpcId("npc-1")
        .WithPlayerInput("Hello")
        .Build();

      // Assert
      Assert.That(record, Is.Not.Null);
      Assert.That(record.NpcId, Is.EqualTo("npc-1"));
      Assert.That(record.PlayerInput, Is.EqualTo("Hello"));
    }

    [Test]
    public void Build_GeneratesRecordId()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithNpcId("npc-1")
        .Build();

      // Assert
      Assert.That(record.RecordId, Is.Not.Empty);
      Assert.That(record.RecordId.Length, Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void Build_SetsCapturedAtUtcTicks()
    {
      // Arrange
      var beforeTicks = DateTimeOffset.UtcNow.UtcTicks;

      // Act
      var record = new AuditRecordBuilder()
        .WithNpcId("npc-1")
        .Build();

      var afterTicks = DateTimeOffset.UtcNow.UtcTicks;

      // Assert
      Assert.That(record.CapturedAtUtcTicks, Is.GreaterThanOrEqualTo(beforeTicks));
      Assert.That(record.CapturedAtUtcTicks, Is.LessThanOrEqualTo(afterTicks));
    }

    #endregion

    #region Fluent API Tests

    [Test]
    public void WithNpcId_SetsProperty()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithNpcId("test-npc")
        .Build();

      // Assert
      Assert.That(record.NpcId, Is.EqualTo("test-npc"));
    }

    [Test]
    public void WithInteractionCount_SetsProperty()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithInteractionCount(42)
        .Build();

      // Assert
      Assert.That(record.InteractionCount, Is.EqualTo(42));
    }

    [Test]
    public void WithSeed_SetsProperty()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithSeed(12345)
        .Build();

      // Assert
      Assert.That(record.Seed, Is.EqualTo(12345));
    }

    [Test]
    public void WithSnapshotTimeUtcTicks_SetsProperty()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithSnapshotTimeUtcTicks(638000000000000000L)
        .Build();

      // Assert
      Assert.That(record.SnapshotTimeUtcTicks, Is.EqualTo(638000000000000000L));
    }

    [Test]
    public void WithPlayerInput_SetsProperty()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithPlayerInput("Hello, NPC!")
        .Build();

      // Assert
      Assert.That(record.PlayerInput, Is.EqualTo("Hello, NPC!"));
    }

    [Test]
    public void WithTriggerInfo_SetsProperties()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithTriggerInfo(1, "trigger-123", "TestScene")
        .Build();

      // Assert
      Assert.That(record.TriggerReason, Is.EqualTo(1));
      Assert.That(record.TriggerId, Is.EqualTo("trigger-123"));
      Assert.That(record.SceneName, Is.EqualTo("TestScene"));
    }

    [Test]
    public void WithStateHashes_SetsProperties()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithStateHashes("memhash", "prompthash", "constrainthash")
        .Build();

      // Assert
      Assert.That(record.MemoryHashBefore, Is.EqualTo("memhash"));
      Assert.That(record.PromptHash, Is.EqualTo("prompthash"));
      Assert.That(record.ConstraintsHash, Is.EqualTo("constrainthash"));
    }

    [Test]
    public void WithConstraints_SetsProperty()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithConstraints("{\"rules\": []}")
        .Build();

      // Assert
      Assert.That(record.ConstraintsSerialized, Is.EqualTo("{\"rules\": []}"));
    }

    [Test]
    public void WithOutput_SetsPropertiesAndComputesHash()
    {
      // Arrange
      const string rawOutput = "Test output";

      // Act
      var record = new AuditRecordBuilder()
        .WithOutput(rawOutput, "Parsed dialogue")
        .Build();

      // Assert
      Assert.That(record.RawOutput, Is.EqualTo(rawOutput));
      Assert.That(record.DialogueText, Is.EqualTo("Parsed dialogue"));
      Assert.That(record.OutputHash, Is.Not.Empty);
      Assert.That(record.OutputHash, Is.EqualTo(AuditHasher.ComputeSha256(rawOutput)));
    }

    [Test]
    public void WithValidationOutcome_SetsProperties()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithValidationOutcome(true, 0, 2)
        .Build();

      // Assert
      Assert.That(record.ValidationPassed, Is.True);
      Assert.That(record.ViolationCount, Is.EqualTo(0));
      Assert.That(record.MutationsApplied, Is.EqualTo(2));
    }

    [Test]
    public void WithFallback_SetsProperties()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithFallback("Validation failed")
        .Build();

      // Assert
      Assert.That(record.FallbackUsed, Is.True);
      Assert.That(record.FallbackReason, Is.EqualTo("Validation failed"));
    }

    [Test]
    public void WithMetrics_SetsProperties()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithMetrics(50, 250, 200, 40)
        .Build();

      // Assert
      Assert.That(record.TtftMs, Is.EqualTo(50));
      Assert.That(record.TotalTimeMs, Is.EqualTo(250));
      Assert.That(record.PromptTokenCount, Is.EqualTo(200));
      Assert.That(record.GeneratedTokenCount, Is.EqualTo(40));
    }

    #endregion

    #region Chaining Tests

    [Test]
    public void Methods_ReturnBuilder_ForChaining()
    {
      // Arrange
      var builder = new AuditRecordBuilder();

      // Act & Assert - All methods return the same builder instance
      Assert.That(builder.WithNpcId("test"), Is.SameAs(builder));
      Assert.That(builder.WithInteractionCount(1), Is.SameAs(builder));
      Assert.That(builder.WithSeed(42), Is.SameAs(builder));
      Assert.That(builder.WithSnapshotTimeUtcTicks(100), Is.SameAs(builder));
      Assert.That(builder.WithPlayerInput("test"), Is.SameAs(builder));
      Assert.That(builder.WithTriggerInfo(0, "", ""), Is.SameAs(builder));
      Assert.That(builder.WithStateHashes("", "", ""), Is.SameAs(builder));
      Assert.That(builder.WithConstraints(""), Is.SameAs(builder));
      Assert.That(builder.WithOutput("", ""), Is.SameAs(builder));
      Assert.That(builder.WithValidationOutcome(true, 0, 0), Is.SameAs(builder));
      Assert.That(builder.WithFallback(""), Is.SameAs(builder));
      Assert.That(builder.WithMetrics(0, 0, 0, 0), Is.SameAs(builder));
    }

    [Test]
    public void FullChain_BuildsCompleteRecord()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithNpcId("npc-full")
        .WithInteractionCount(100)
        .WithSeed(99999)
        .WithSnapshotTimeUtcTicks(638000000000000000L)
        .WithPlayerInput("Full chain test")
        .WithTriggerInfo(2, "trigger-full", "FullScene")
        .WithStateHashes("mem", "prompt", "constraint")
        .WithConstraints("{\"full\": true}")
        .WithOutput("Full output", "Full dialogue")
        .WithValidationOutcome(true, 0, 5)
        .WithMetrics(10, 100, 300, 60)
        .Build();

      // Assert
      Assert.That(record.NpcId, Is.EqualTo("npc-full"));
      Assert.That(record.InteractionCount, Is.EqualTo(100));
      Assert.That(record.Seed, Is.EqualTo(99999));
      Assert.That(record.SnapshotTimeUtcTicks, Is.EqualTo(638000000000000000L));
      Assert.That(record.PlayerInput, Is.EqualTo("Full chain test"));
      Assert.That(record.TriggerReason, Is.EqualTo(2));
      Assert.That(record.TriggerId, Is.EqualTo("trigger-full"));
      Assert.That(record.SceneName, Is.EqualTo("FullScene"));
      Assert.That(record.MemoryHashBefore, Is.EqualTo("mem"));
      Assert.That(record.PromptHash, Is.EqualTo("prompt"));
      Assert.That(record.ConstraintsHash, Is.EqualTo("constraint"));
      Assert.That(record.ConstraintsSerialized, Is.EqualTo("{\"full\": true}"));
      Assert.That(record.RawOutput, Is.EqualTo("Full output"));
      Assert.That(record.DialogueText, Is.EqualTo("Full dialogue"));
      Assert.That(record.ValidationPassed, Is.True);
      Assert.That(record.MutationsApplied, Is.EqualTo(5));
      Assert.That(record.TtftMs, Is.EqualTo(10));
      Assert.That(record.TotalTimeMs, Is.EqualTo(100));
      Assert.That(record.PromptTokenCount, Is.EqualTo(300));
      Assert.That(record.GeneratedTokenCount, Is.EqualTo(60));
    }

    #endregion

    #region Multiple Builds Tests

    [Test]
    public void Build_MultipleTimes_CreatesIndependentRecords()
    {
      // Arrange
      var builder = new AuditRecordBuilder()
        .WithNpcId("npc-1")
        .WithPlayerInput("First");

      // Act
      var record1 = builder.Build();
      builder.WithPlayerInput("Second");
      var record2 = builder.Build();

      // Assert
      Assert.That(record1.PlayerInput, Is.EqualTo("First"));
      Assert.That(record2.PlayerInput, Is.EqualTo("Second"));
      Assert.That(record1.RecordId, Is.Not.EqualTo(record2.RecordId));
    }

    [Test]
    public void Build_GeneratesUniqueRecordIds()
    {
      // Arrange
      var builder = new AuditRecordBuilder().WithNpcId("npc-1");

      // Act
      var ids = new string[100];
      for (int i = 0; i < 100; i++)
        ids[i] = builder.Build().RecordId;

      // Assert - All IDs should be unique
      Assert.That(ids, Is.Unique);
    }

    #endregion

    #region Null Input Tests

    [Test]
    public void WithNpcId_NullInput_UsesEmptyString()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithNpcId(null!)
        .Build();

      // Assert
      Assert.That(record.NpcId, Is.EqualTo(""));
    }

    [Test]
    public void WithPlayerInput_NullInput_UsesEmptyString()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithPlayerInput(null!)
        .Build();

      // Assert
      Assert.That(record.PlayerInput, Is.EqualTo(""));
    }

    [Test]
    public void WithOutput_NullRawOutput_UsesEmptyString()
    {
      // Act
      var record = new AuditRecordBuilder()
        .WithOutput(null!, null!)
        .Build();

      // Assert
      Assert.That(record.RawOutput, Is.EqualTo(""));
      Assert.That(record.DialogueText, Is.EqualTo(""));
      Assert.That(record.OutputHash, Is.Not.Empty); // Hash of empty string
    }

    #endregion

    #region Reset Tests

    [Test]
    public void Reset_ClearsAllValues()
    {
      // Arrange
      var builder = new AuditRecordBuilder()
        .WithNpcId("npc-1")
        .WithInteractionCount(42)
        .WithPlayerInput("Test");

      // Act
      builder.Reset();
      var record = builder.Build();

      // Assert
      Assert.That(record.NpcId, Is.EqualTo(""));
      Assert.That(record.InteractionCount, Is.EqualTo(0));
      Assert.That(record.PlayerInput, Is.EqualTo(""));
    }

    [Test]
    public void Reset_ReturnsBuilder_ForChaining()
    {
      // Arrange
      var builder = new AuditRecordBuilder();

      // Act & Assert
      Assert.That(builder.Reset(), Is.SameAs(builder));
    }

    #endregion
  }
}
