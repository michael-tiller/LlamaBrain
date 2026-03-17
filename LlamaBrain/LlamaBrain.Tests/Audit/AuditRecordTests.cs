using System;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for AuditRecord DTO and serialization.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class AuditRecordTests
  {
    #region Default Value Tests

    [Test]
    public void Constructor_DefaultValues_AllPropertiesInitialized()
    {
      // Act
      var record = new AuditRecord();

      // Assert
      Assert.That(record.RecordId, Is.EqualTo(""));
      Assert.That(record.NpcId, Is.EqualTo(""));
      Assert.That(record.InteractionCount, Is.EqualTo(0));
      Assert.That(record.Seed, Is.EqualTo(0));
      Assert.That(record.SnapshotTimeUtcTicks, Is.EqualTo(0L));
      Assert.That(record.CapturedAtUtcTicks, Is.EqualTo(0L));
      Assert.That(record.PlayerInput, Is.EqualTo(""));
      Assert.That(record.TriggerReason, Is.EqualTo(0));
      Assert.That(record.TriggerId, Is.EqualTo(""));
      Assert.That(record.SceneName, Is.EqualTo(""));
      Assert.That(record.MemoryHashBefore, Is.EqualTo(""));
      Assert.That(record.PromptHash, Is.EqualTo(""));
      Assert.That(record.ConstraintsHash, Is.EqualTo(""));
      Assert.That(record.ConstraintsSerialized, Is.EqualTo(""));
      Assert.That(record.RawOutput, Is.EqualTo(""));
      Assert.That(record.OutputHash, Is.EqualTo(""));
      Assert.That(record.DialogueText, Is.EqualTo(""));
      Assert.That(record.ValidationPassed, Is.False);
      Assert.That(record.ViolationCount, Is.EqualTo(0));
      Assert.That(record.MutationsApplied, Is.EqualTo(0));
      Assert.That(record.FallbackUsed, Is.False);
      Assert.That(record.FallbackReason, Is.EqualTo(""));
      Assert.That(record.TtftMs, Is.EqualTo(0L));
      Assert.That(record.TotalTimeMs, Is.EqualTo(0L));
      Assert.That(record.PromptTokenCount, Is.EqualTo(0));
      Assert.That(record.GeneratedTokenCount, Is.EqualTo(0));
    }

    [Test]
    public void CurrentVersion_IsOne()
    {
      // Assert
      Assert.That(AuditRecord.CurrentVersion, Is.EqualTo(1));
    }

    #endregion

    #region Property Assignment Tests

    [Test]
    public void Properties_CanBeSetAndRetrieved()
    {
      // Arrange
      var record = new AuditRecord
      {
        RecordId = "rec-123",
        NpcId = "npc-456",
        InteractionCount = 42,
        Seed = 12345,
        SnapshotTimeUtcTicks = 638000000000000000L,
        CapturedAtUtcTicks = 638000000000000001L,
        PlayerInput = "Hello, NPC!",
        TriggerReason = 1,
        TriggerId = "trigger-789",
        SceneName = "TestScene",
        MemoryHashBefore = "abc123",
        PromptHash = "def456",
        ConstraintsHash = "ghi789",
        ConstraintsSerialized = "{}",
        RawOutput = "Hello, Player!",
        OutputHash = "jkl012",
        DialogueText = "Hello, Player!",
        ValidationPassed = true,
        ViolationCount = 0,
        MutationsApplied = 2,
        FallbackUsed = false,
        FallbackReason = "",
        TtftMs = 100,
        TotalTimeMs = 500,
        PromptTokenCount = 200,
        GeneratedTokenCount = 50
      };

      // Assert
      Assert.That(record.RecordId, Is.EqualTo("rec-123"));
      Assert.That(record.NpcId, Is.EqualTo("npc-456"));
      Assert.That(record.InteractionCount, Is.EqualTo(42));
      Assert.That(record.Seed, Is.EqualTo(12345));
      Assert.That(record.SnapshotTimeUtcTicks, Is.EqualTo(638000000000000000L));
      Assert.That(record.CapturedAtUtcTicks, Is.EqualTo(638000000000000001L));
      Assert.That(record.PlayerInput, Is.EqualTo("Hello, NPC!"));
      Assert.That(record.TriggerReason, Is.EqualTo(1));
      Assert.That(record.TriggerId, Is.EqualTo("trigger-789"));
      Assert.That(record.SceneName, Is.EqualTo("TestScene"));
      Assert.That(record.MemoryHashBefore, Is.EqualTo("abc123"));
      Assert.That(record.PromptHash, Is.EqualTo("def456"));
      Assert.That(record.ConstraintsHash, Is.EqualTo("ghi789"));
      Assert.That(record.ConstraintsSerialized, Is.EqualTo("{}"));
      Assert.That(record.RawOutput, Is.EqualTo("Hello, Player!"));
      Assert.That(record.OutputHash, Is.EqualTo("jkl012"));
      Assert.That(record.DialogueText, Is.EqualTo("Hello, Player!"));
      Assert.That(record.ValidationPassed, Is.True);
      Assert.That(record.ViolationCount, Is.EqualTo(0));
      Assert.That(record.MutationsApplied, Is.EqualTo(2));
      Assert.That(record.FallbackUsed, Is.False);
      Assert.That(record.FallbackReason, Is.EqualTo(""));
      Assert.That(record.TtftMs, Is.EqualTo(100));
      Assert.That(record.TotalTimeMs, Is.EqualTo(500));
      Assert.That(record.PromptTokenCount, Is.EqualTo(200));
      Assert.That(record.GeneratedTokenCount, Is.EqualTo(50));
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void Serialization_RoundTrip_PreservesAllProperties()
    {
      // Arrange
      var original = new AuditRecord
      {
        RecordId = "rec-test",
        NpcId = "npc-test",
        InteractionCount = 99,
        Seed = 54321,
        SnapshotTimeUtcTicks = 638000000000000000L,
        CapturedAtUtcTicks = 638000000000000001L,
        PlayerInput = "Test input with special chars: <>&\"'",
        TriggerReason = 2,
        TriggerId = "trigger-test",
        SceneName = "TestScene",
        MemoryHashBefore = "memhash",
        PromptHash = "prompthash",
        ConstraintsHash = "constrainthash",
        ConstraintsSerialized = "{\"key\": \"value\"}",
        RawOutput = "Test output",
        OutputHash = "outputhash",
        DialogueText = "Parsed dialogue",
        ValidationPassed = true,
        ViolationCount = 1,
        MutationsApplied = 3,
        FallbackUsed = true,
        FallbackReason = "Validation failed",
        TtftMs = 50,
        TotalTimeMs = 250,
        PromptTokenCount = 150,
        GeneratedTokenCount = 30
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var deserialized = JsonConvert.DeserializeObject<AuditRecord>(json);

      // Assert
      Assert.That(deserialized, Is.Not.Null);
      Assert.That(deserialized!.RecordId, Is.EqualTo(original.RecordId));
      Assert.That(deserialized.NpcId, Is.EqualTo(original.NpcId));
      Assert.That(deserialized.InteractionCount, Is.EqualTo(original.InteractionCount));
      Assert.That(deserialized.Seed, Is.EqualTo(original.Seed));
      Assert.That(deserialized.SnapshotTimeUtcTicks, Is.EqualTo(original.SnapshotTimeUtcTicks));
      Assert.That(deserialized.CapturedAtUtcTicks, Is.EqualTo(original.CapturedAtUtcTicks));
      Assert.That(deserialized.PlayerInput, Is.EqualTo(original.PlayerInput));
      Assert.That(deserialized.TriggerReason, Is.EqualTo(original.TriggerReason));
      Assert.That(deserialized.TriggerId, Is.EqualTo(original.TriggerId));
      Assert.That(deserialized.SceneName, Is.EqualTo(original.SceneName));
      Assert.That(deserialized.MemoryHashBefore, Is.EqualTo(original.MemoryHashBefore));
      Assert.That(deserialized.PromptHash, Is.EqualTo(original.PromptHash));
      Assert.That(deserialized.ConstraintsHash, Is.EqualTo(original.ConstraintsHash));
      Assert.That(deserialized.ConstraintsSerialized, Is.EqualTo(original.ConstraintsSerialized));
      Assert.That(deserialized.RawOutput, Is.EqualTo(original.RawOutput));
      Assert.That(deserialized.OutputHash, Is.EqualTo(original.OutputHash));
      Assert.That(deserialized.DialogueText, Is.EqualTo(original.DialogueText));
      Assert.That(deserialized.ValidationPassed, Is.EqualTo(original.ValidationPassed));
      Assert.That(deserialized.ViolationCount, Is.EqualTo(original.ViolationCount));
      Assert.That(deserialized.MutationsApplied, Is.EqualTo(original.MutationsApplied));
      Assert.That(deserialized.FallbackUsed, Is.EqualTo(original.FallbackUsed));
      Assert.That(deserialized.FallbackReason, Is.EqualTo(original.FallbackReason));
      Assert.That(deserialized.TtftMs, Is.EqualTo(original.TtftMs));
      Assert.That(deserialized.TotalTimeMs, Is.EqualTo(original.TotalTimeMs));
      Assert.That(deserialized.PromptTokenCount, Is.EqualTo(original.PromptTokenCount));
      Assert.That(deserialized.GeneratedTokenCount, Is.EqualTo(original.GeneratedTokenCount));
    }

    [Test]
    public void Serialization_DefaultRecord_ProducesValidJson()
    {
      // Arrange
      var record = new AuditRecord();

      // Act
      var json = JsonConvert.SerializeObject(record);

      // Assert
      Assert.That(json, Is.Not.Empty);
      Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<AuditRecord>(json));
    }

    [Test]
    public void Serialization_WithUnicodeContent_PreservesContent()
    {
      // Arrange
      var record = new AuditRecord
      {
        PlayerInput = "Hello, 世界! 🌍",
        RawOutput = "你好，世界！ 🎮",
        DialogueText = "Emoji test: 😀 🎉 🚀"
      };

      // Act
      var json = JsonConvert.SerializeObject(record);
      var deserialized = JsonConvert.DeserializeObject<AuditRecord>(json);

      // Assert
      Assert.That(deserialized!.PlayerInput, Is.EqualTo(record.PlayerInput));
      Assert.That(deserialized.RawOutput, Is.EqualTo(record.RawOutput));
      Assert.That(deserialized.DialogueText, Is.EqualTo(record.DialogueText));
    }

    #endregion

    #region Determinism Tests

    [Test]
    [Category("Determinism")]
    public void Serialization_SameRecord_ProducesSameJson()
    {
      // Arrange
      var record = new AuditRecord
      {
        RecordId = "determinism-test",
        NpcId = "npc-1",
        InteractionCount = 1,
        Seed = 42,
        PlayerInput = "Test"
      };

      // Act
      var json1 = JsonConvert.SerializeObject(record);
      var json2 = JsonConvert.SerializeObject(record);

      // Assert
      Assert.That(json1, Is.EqualTo(json2));
    }

    #endregion
  }
}
