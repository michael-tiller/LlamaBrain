using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for AuditCaptureContext struct.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class AuditCaptureContextTests
  {
    #region Default Value Tests

    [Test]
    public void DefaultContext_HasNullStringProperties()
    {
      // Arrange & Act
      var context = new AuditCaptureContext();

      // Assert - struct default values are null for reference types
      Assert.That(context.NpcId, Is.Null);
      Assert.That(context.PlayerInput, Is.Null);
      Assert.That(context.TriggerId, Is.Null);
      Assert.That(context.SceneName, Is.Null);
      Assert.That(context.MemoryHashBefore, Is.Null);
      Assert.That(context.PromptHash, Is.Null);
      Assert.That(context.ConstraintsHash, Is.Null);
      Assert.That(context.ConstraintsSerialized, Is.Null);
      Assert.That(context.RawOutput, Is.Null);
      Assert.That(context.DialogueText, Is.Null);
      Assert.That(context.FallbackReason, Is.Null);
    }

    [Test]
    public void DefaultContext_HasZeroNumericValues()
    {
      // Arrange & Act
      var context = new AuditCaptureContext();

      // Assert
      Assert.That(context.InteractionCount, Is.EqualTo(0));
      Assert.That(context.Seed, Is.EqualTo(0));
      Assert.That(context.SnapshotTimeUtcTicks, Is.EqualTo(0));
      Assert.That(context.TriggerReason, Is.EqualTo(0));
      Assert.That(context.ViolationCount, Is.EqualTo(0));
      Assert.That(context.MutationsApplied, Is.EqualTo(0));
      Assert.That(context.TtftMs, Is.EqualTo(0));
      Assert.That(context.TotalTimeMs, Is.EqualTo(0));
      Assert.That(context.PromptTokenCount, Is.EqualTo(0));
      Assert.That(context.GeneratedTokenCount, Is.EqualTo(0));
    }

    [Test]
    public void DefaultContext_HasFalseBooleanValues()
    {
      // Arrange & Act
      var context = new AuditCaptureContext();

      // Assert
      Assert.That(context.ValidationPassed, Is.False);
      Assert.That(context.FallbackUsed, Is.False);
    }

    #endregion

    #region Property Get/Set Tests

    [Test]
    public void NpcId_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.NpcId = "test-npc";

      // Assert
      Assert.That(context.NpcId, Is.EqualTo("test-npc"));
    }

    [Test]
    public void InteractionCount_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.InteractionCount = 42;

      // Assert
      Assert.That(context.InteractionCount, Is.EqualTo(42));
    }

    [Test]
    public void Seed_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.Seed = 12345;

      // Assert
      Assert.That(context.Seed, Is.EqualTo(12345));
    }

    [Test]
    public void SnapshotTimeUtcTicks_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.SnapshotTimeUtcTicks = 638000000000000000L;

      // Assert
      Assert.That(context.SnapshotTimeUtcTicks, Is.EqualTo(638000000000000000L));
    }

    [Test]
    public void PlayerInput_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.PlayerInput = "Hello, NPC!";

      // Assert
      Assert.That(context.PlayerInput, Is.EqualTo("Hello, NPC!"));
    }

    [Test]
    public void TriggerReason_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.TriggerReason = 2;

      // Assert
      Assert.That(context.TriggerReason, Is.EqualTo(2));
    }

    [Test]
    public void TriggerId_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.TriggerId = "trigger-zone-1";

      // Assert
      Assert.That(context.TriggerId, Is.EqualTo("trigger-zone-1"));
    }

    [Test]
    public void SceneName_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.SceneName = "MainScene";

      // Assert
      Assert.That(context.SceneName, Is.EqualTo("MainScene"));
    }

    [Test]
    public void StateHashes_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.MemoryHashBefore = "memoryhash123";
      context.PromptHash = "prompthash456";
      context.ConstraintsHash = "constraintshash789";

      // Assert
      Assert.That(context.MemoryHashBefore, Is.EqualTo("memoryhash123"));
      Assert.That(context.PromptHash, Is.EqualTo("prompthash456"));
      Assert.That(context.ConstraintsHash, Is.EqualTo("constraintshash789"));
    }

    [Test]
    public void ConstraintsSerialized_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.ConstraintsSerialized = "{\"rules\": []}";

      // Assert
      Assert.That(context.ConstraintsSerialized, Is.EqualTo("{\"rules\": []}"));
    }

    [Test]
    public void OutputProperties_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.RawOutput = "Raw LLM response";
      context.DialogueText = "Parsed dialogue";

      // Assert
      Assert.That(context.RawOutput, Is.EqualTo("Raw LLM response"));
      Assert.That(context.DialogueText, Is.EqualTo("Parsed dialogue"));
    }

    [Test]
    public void ValidationOutcome_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.ValidationPassed = true;
      context.ViolationCount = 3;
      context.MutationsApplied = 2;

      // Assert
      Assert.That(context.ValidationPassed, Is.True);
      Assert.That(context.ViolationCount, Is.EqualTo(3));
      Assert.That(context.MutationsApplied, Is.EqualTo(2));
    }

    [Test]
    public void FallbackProperties_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.FallbackUsed = true;
      context.FallbackReason = "Validation failed";

      // Assert
      Assert.That(context.FallbackUsed, Is.True);
      Assert.That(context.FallbackReason, Is.EqualTo("Validation failed"));
    }

    [Test]
    public void PerformanceMetrics_CanBeSetAndRetrieved()
    {
      // Arrange
      var context = new AuditCaptureContext();

      // Act
      context.TtftMs = 50;
      context.TotalTimeMs = 250;
      context.PromptTokenCount = 200;
      context.GeneratedTokenCount = 40;

      // Assert
      Assert.That(context.TtftMs, Is.EqualTo(50));
      Assert.That(context.TotalTimeMs, Is.EqualTo(250));
      Assert.That(context.PromptTokenCount, Is.EqualTo(200));
      Assert.That(context.GeneratedTokenCount, Is.EqualTo(40));
    }

    #endregion

    #region ToAuditRecord Tests

    [Test]
    public void ToAuditRecord_CreatesRecord()
    {
      // Arrange
      var context = CreateFullContext();

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record, Is.Not.Null);
      Assert.That(record, Is.TypeOf<AuditRecord>());
    }

    [Test]
    public void ToAuditRecord_MapsNpcId()
    {
      // Arrange
      var context = new AuditCaptureContext { NpcId = "npc-123" };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.NpcId, Is.EqualTo("npc-123"));
    }

    [Test]
    public void ToAuditRecord_MapsInteractionCount()
    {
      // Arrange
      var context = new AuditCaptureContext { InteractionCount = 42 };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.InteractionCount, Is.EqualTo(42));
    }

    [Test]
    public void ToAuditRecord_MapsSeed()
    {
      // Arrange
      var context = new AuditCaptureContext { Seed = 99999 };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.Seed, Is.EqualTo(99999));
    }

    [Test]
    public void ToAuditRecord_MapsSnapshotTimeUtcTicks()
    {
      // Arrange
      var context = new AuditCaptureContext { SnapshotTimeUtcTicks = 638000000000000000L };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.SnapshotTimeUtcTicks, Is.EqualTo(638000000000000000L));
    }

    [Test]
    public void ToAuditRecord_MapsPlayerInput()
    {
      // Arrange
      var context = new AuditCaptureContext { PlayerInput = "Hello!" };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.PlayerInput, Is.EqualTo("Hello!"));
    }

    [Test]
    public void ToAuditRecord_MapsTriggerInfo()
    {
      // Arrange
      var context = new AuditCaptureContext
      {
        TriggerReason = 1,
        TriggerId = "trigger-abc",
        SceneName = "TestScene"
      };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.TriggerReason, Is.EqualTo(1));
      Assert.That(record.TriggerId, Is.EqualTo("trigger-abc"));
      Assert.That(record.SceneName, Is.EqualTo("TestScene"));
    }

    [Test]
    public void ToAuditRecord_MapsStateHashes()
    {
      // Arrange
      var context = new AuditCaptureContext
      {
        MemoryHashBefore = "memhash",
        PromptHash = "prompthash",
        ConstraintsHash = "constrainthash"
      };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.MemoryHashBefore, Is.EqualTo("memhash"));
      Assert.That(record.PromptHash, Is.EqualTo("prompthash"));
      Assert.That(record.ConstraintsHash, Is.EqualTo("constrainthash"));
    }

    [Test]
    public void ToAuditRecord_MapsConstraintsSerialized()
    {
      // Arrange
      var context = new AuditCaptureContext { ConstraintsSerialized = "{\"test\": true}" };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.ConstraintsSerialized, Is.EqualTo("{\"test\": true}"));
    }

    [Test]
    public void ToAuditRecord_MapsOutput()
    {
      // Arrange
      var context = new AuditCaptureContext
      {
        RawOutput = "Raw response",
        DialogueText = "Parsed text"
      };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.RawOutput, Is.EqualTo("Raw response"));
      Assert.That(record.DialogueText, Is.EqualTo("Parsed text"));
    }

    [Test]
    public void ToAuditRecord_ComputesOutputHash()
    {
      // Arrange
      const string rawOutput = "Test output for hashing";
      var context = new AuditCaptureContext { RawOutput = rawOutput };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.OutputHash, Is.Not.Empty);
      Assert.That(record.OutputHash, Is.EqualTo(AuditHasher.ComputeSha256(rawOutput)));
    }

    [Test]
    public void ToAuditRecord_MapsValidationOutcome()
    {
      // Arrange
      var context = new AuditCaptureContext
      {
        ValidationPassed = true,
        ViolationCount = 2,
        MutationsApplied = 3
      };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.ValidationPassed, Is.True);
      Assert.That(record.ViolationCount, Is.EqualTo(2));
      Assert.That(record.MutationsApplied, Is.EqualTo(3));
    }

    [Test]
    public void ToAuditRecord_MapsPerformanceMetrics()
    {
      // Arrange
      var context = new AuditCaptureContext
      {
        TtftMs = 100,
        TotalTimeMs = 500,
        PromptTokenCount = 150,
        GeneratedTokenCount = 50
      };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.TtftMs, Is.EqualTo(100));
      Assert.That(record.TotalTimeMs, Is.EqualTo(500));
      Assert.That(record.PromptTokenCount, Is.EqualTo(150));
      Assert.That(record.GeneratedTokenCount, Is.EqualTo(50));
    }

    #endregion

    #region ToAuditRecord Fallback Tests

    [Test]
    public void ToAuditRecord_WhenFallbackUsed_SetsFallbackProperties()
    {
      // Arrange
      var context = new AuditCaptureContext
      {
        FallbackUsed = true,
        FallbackReason = "Timeout"
      };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.FallbackUsed, Is.True);
      Assert.That(record.FallbackReason, Is.EqualTo("Timeout"));
    }

    [Test]
    public void ToAuditRecord_WhenFallbackNotUsed_FallbackUsedIsFalse()
    {
      // Arrange
      var context = new AuditCaptureContext
      {
        FallbackUsed = false,
        FallbackReason = "Ignored reason"
      };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.FallbackUsed, Is.False);
    }

    [Test]
    public void ToAuditRecord_WhenFallbackUsedWithNullReason_HandlesGracefully()
    {
      // Arrange
      var context = new AuditCaptureContext
      {
        FallbackUsed = true,
        FallbackReason = null!
      };

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.FallbackUsed, Is.True);
      Assert.That(record.FallbackReason, Is.EqualTo(""));
    }

    #endregion

    #region ToAuditRecord Full Integration Tests

    [Test]
    public void ToAuditRecord_WithAllFieldsPopulated_MapsAllFields()
    {
      // Arrange
      var context = CreateFullContext();

      // Act
      var record = context.ToAuditRecord();

      // Assert - Identification
      Assert.That(record.NpcId, Is.EqualTo("npc-full"));
      Assert.That(record.InteractionCount, Is.EqualTo(100));

      // Assert - Determinism Keys
      Assert.That(record.Seed, Is.EqualTo(55555));
      Assert.That(record.SnapshotTimeUtcTicks, Is.EqualTo(638000000000000000L));

      // Assert - Input
      Assert.That(record.PlayerInput, Is.EqualTo("Full integration test"));
      Assert.That(record.TriggerReason, Is.EqualTo(2));
      Assert.That(record.TriggerId, Is.EqualTo("trigger-full"));
      Assert.That(record.SceneName, Is.EqualTo("FullScene"));

      // Assert - State Hashes
      Assert.That(record.MemoryHashBefore, Is.EqualTo("fullmemhash"));
      Assert.That(record.PromptHash, Is.EqualTo("fullprompthash"));
      Assert.That(record.ConstraintsHash, Is.EqualTo("fullconstrainthash"));
      Assert.That(record.ConstraintsSerialized, Is.EqualTo("{\"full\": true}"));

      // Assert - Output
      Assert.That(record.RawOutput, Is.EqualTo("Full raw output"));
      Assert.That(record.DialogueText, Is.EqualTo("Full dialogue text"));
      Assert.That(record.OutputHash, Is.EqualTo(AuditHasher.ComputeSha256("Full raw output")));

      // Assert - Outcome
      Assert.That(record.ValidationPassed, Is.True);
      Assert.That(record.ViolationCount, Is.EqualTo(0));
      Assert.That(record.MutationsApplied, Is.EqualTo(5));
      Assert.That(record.FallbackUsed, Is.False);

      // Assert - Performance Metrics
      Assert.That(record.TtftMs, Is.EqualTo(25));
      Assert.That(record.TotalTimeMs, Is.EqualTo(200));
      Assert.That(record.PromptTokenCount, Is.EqualTo(300));
      Assert.That(record.GeneratedTokenCount, Is.EqualTo(75));
    }

    [Test]
    public void ToAuditRecord_GeneratesRecordId()
    {
      // Arrange
      var context = CreateFullContext();

      // Act
      var record = context.ToAuditRecord();

      // Assert
      Assert.That(record.RecordId, Is.Not.Null);
      Assert.That(record.RecordId, Is.Not.Empty);
    }

    [Test]
    public void ToAuditRecord_MultipleCalls_GenerateUniqueRecordIds()
    {
      // Arrange
      var context = CreateFullContext();

      // Act
      var record1 = context.ToAuditRecord();
      var record2 = context.ToAuditRecord();

      // Assert
      Assert.That(record1.RecordId, Is.Not.EqualTo(record2.RecordId));
    }

    [Test]
    public void ToAuditRecord_SetsCapturedAtUtcTicks()
    {
      // Arrange
      var context = CreateFullContext();
      var beforeTicks = System.DateTimeOffset.UtcNow.UtcTicks;

      // Act
      var record = context.ToAuditRecord();

      var afterTicks = System.DateTimeOffset.UtcNow.UtcTicks;

      // Assert
      Assert.That(record.CapturedAtUtcTicks, Is.GreaterThanOrEqualTo(beforeTicks));
      Assert.That(record.CapturedAtUtcTicks, Is.LessThanOrEqualTo(afterTicks));
    }

    #endregion

    #region Null Handling Tests

    [Test]
    public void ToAuditRecord_WithNullStrings_HandlesGracefully()
    {
      // Arrange - default context has null strings
      var context = new AuditCaptureContext();

      // Act
      var record = context.ToAuditRecord();

      // Assert - builder should convert nulls to empty strings
      Assert.That(record.NpcId, Is.EqualTo(""));
      Assert.That(record.PlayerInput, Is.EqualTo(""));
      Assert.That(record.TriggerId, Is.EqualTo(""));
      Assert.That(record.SceneName, Is.EqualTo(""));
      Assert.That(record.MemoryHashBefore, Is.EqualTo(""));
      Assert.That(record.PromptHash, Is.EqualTo(""));
      Assert.That(record.ConstraintsHash, Is.EqualTo(""));
      Assert.That(record.ConstraintsSerialized, Is.EqualTo(""));
      Assert.That(record.RawOutput, Is.EqualTo(""));
      Assert.That(record.DialogueText, Is.EqualTo(""));
    }

    #endregion

    #region Helper Methods

    private static AuditCaptureContext CreateFullContext()
    {
      return new AuditCaptureContext
      {
        // Identification
        NpcId = "npc-full",
        InteractionCount = 100,

        // Determinism Keys
        Seed = 55555,
        SnapshotTimeUtcTicks = 638000000000000000L,

        // Input
        PlayerInput = "Full integration test",
        TriggerReason = 2,
        TriggerId = "trigger-full",
        SceneName = "FullScene",

        // State Hashes
        MemoryHashBefore = "fullmemhash",
        PromptHash = "fullprompthash",
        ConstraintsHash = "fullconstrainthash",
        ConstraintsSerialized = "{\"full\": true}",

        // Output
        RawOutput = "Full raw output",
        DialogueText = "Full dialogue text",

        // Outcome
        ValidationPassed = true,
        ViolationCount = 0,
        MutationsApplied = 5,
        FallbackUsed = false,
        FallbackReason = null!,

        // Performance Metrics
        TtftMs = 25,
        TotalTimeMs = 200,
        PromptTokenCount = 300,
        GeneratedTokenCount = 75
      };
    }

    #endregion
  }
}
