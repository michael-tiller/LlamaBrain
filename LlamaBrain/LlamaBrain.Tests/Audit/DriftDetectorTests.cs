using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for DriftDetector.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class DriftDetectorTests
  {
    private DriftDetector _detector = null!;

    [SetUp]
    public void SetUp()
    {
      _detector = new DriftDetector();
    }

    #region Compare Tests

    [Test]
    public void Compare_IdenticalRecords_ReturnsNoDrift()
    {
      var original = CreateTestRecord("hash1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("hash1", "prompt1", "output1", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.DriftType, Is.EqualTo(DriftType.None));
      Assert.That(result.MemoryMatches, Is.True);
      Assert.That(result.PromptMatches, Is.True);
      Assert.That(result.OutputMatches, Is.True);
      Assert.That(result.ValidationMatches, Is.True);
      Assert.That(result.DriftDescription, Is.Null);
    }

    [Test]
    public void Compare_MemoryDrift_DetectsMemoryDrift()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem2", "prompt1", "output1", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.DriftType, Is.EqualTo(DriftType.Memory));
      Assert.That(result.MemoryMatches, Is.False);
      Assert.That(result.PromptMatches, Is.True);
      Assert.That(result.DriftDescription, Does.Contain("Memory"));
    }

    [Test]
    public void Compare_PromptDrift_DetectsPromptDrift()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem1", "prompt2", "output1", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.DriftType, Is.EqualTo(DriftType.Prompt));
      Assert.That(result.MemoryMatches, Is.True);
      Assert.That(result.PromptMatches, Is.False);
      Assert.That(result.DriftDescription, Does.Contain("Prompt"));
    }

    [Test]
    public void Compare_OutputDrift_DetectsOutputDrift()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem1", "prompt1", "output2", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.DriftType, Is.EqualTo(DriftType.Output));
      Assert.That(result.MemoryMatches, Is.True);
      Assert.That(result.PromptMatches, Is.True);
      Assert.That(result.OutputMatches, Is.False);
      Assert.That(result.DriftDescription, Does.Contain("output"));
    }

    [Test]
    public void Compare_ValidationDrift_DetectsValidationDrift()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem1", "prompt1", "output1", false);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.DriftType, Is.EqualTo(DriftType.Validation));
      Assert.That(result.ValidationMatches, Is.False);
      Assert.That(result.DriftDescription, Does.Contain("Validation"));
    }

    [Test]
    public void Compare_MultipleDrifts_ReturnsFirstDrift()
    {
      // Memory, prompt, and output all differ
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem2", "prompt2", "output2", false);

      var result = _detector.Compare(original, replayed);

      // Memory drift should be detected first
      Assert.That(result.DriftType, Is.EqualTo(DriftType.Memory));
      Assert.That(result.MemoryMatches, Is.False);
      Assert.That(result.PromptMatches, Is.False);
      Assert.That(result.OutputMatches, Is.False);
      Assert.That(result.ValidationMatches, Is.False);
    }

    [Test]
    public void Compare_EmptyHashes_TreatedAsEqual()
    {
      var original = CreateTestRecord("", "", "", true);
      var replayed = CreateTestRecord("", "", "", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.DriftType, Is.EqualTo(DriftType.None));
      Assert.That(result.MemoryMatches, Is.True);
      Assert.That(result.PromptMatches, Is.True);
      Assert.That(result.OutputMatches, Is.True);
    }

    [Test]
    public void Compare_NullHashes_TreatedAsEqual()
    {
      var original = new AuditRecord
      {
        MemoryHashBefore = null!,
        PromptHash = null!,
        OutputHash = null!,
        ValidationPassed = true
      };
      var replayed = new AuditRecord
      {
        MemoryHashBefore = null!,
        PromptHash = null!,
        OutputHash = null!,
        ValidationPassed = true
      };

      var result = _detector.Compare(original, replayed);

      Assert.That(result.DriftType, Is.EqualTo(DriftType.None));
    }

    [Test]
    public void Compare_NullOriginal_ThrowsException()
    {
      var replayed = CreateTestRecord("mem1", "prompt1", "output1", true);

      Assert.Throws<ArgumentNullException>(() => _detector.Compare(null!, replayed));
    }

    [Test]
    public void Compare_NullReplayed_ThrowsException()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);

      Assert.Throws<ArgumentNullException>(() => _detector.Compare(original, null!));
    }

    [Test]
    public void Compare_StoresOriginalAndReplayedRecords()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem1", "prompt1", "output1", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.OriginalRecord, Is.SameAs(original));
      Assert.That(result.ReplayedRecord, Is.SameAs(replayed));
    }

    #endregion

    #region CompareConstraints Tests

    [Test]
    public void CompareConstraints_SameHashes_ReturnsTrue()
    {
      var result = _detector.CompareConstraints("hash123", "hash123");

      Assert.That(result, Is.True);
    }

    [Test]
    public void CompareConstraints_DifferentHashes_ReturnsFalse()
    {
      var result = _detector.CompareConstraints("hash123", "hash456");

      Assert.That(result, Is.False);
    }

    [Test]
    public void CompareConstraints_BothEmpty_ReturnsTrue()
    {
      var result = _detector.CompareConstraints("", "");

      Assert.That(result, Is.True);
    }

    [Test]
    public void CompareConstraints_BothNull_ReturnsTrue()
    {
      var result = _detector.CompareConstraints(null, null);

      Assert.That(result, Is.True);
    }

    [Test]
    public void CompareConstraints_NullAndEmpty_ReturnsTrue()
    {
      Assert.That(_detector.CompareConstraints(null, ""), Is.True);
      Assert.That(_detector.CompareConstraints("", null), Is.True);
    }

    #endregion

    #region CreateDriftSummary Tests

    [Test]
    public void CreateDriftSummary_AllMatched_ReturnsSuccessMessage()
    {
      var results = new ReplayResult
      {
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.None },
          new RecordReplayResult { Success = true, DriftType = DriftType.None }
        }
      };
      results.UpdateStatistics();

      var summary = _detector.CreateDriftSummary(results);

      Assert.That(summary, Does.Contain("2 records matched exactly"));
    }

    [Test]
    public void CreateDriftSummary_WithDrift_ShowsDriftCounts()
    {
      var results = new ReplayResult
      {
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.None },
          new RecordReplayResult { Success = true, DriftType = DriftType.Prompt },
          new RecordReplayResult { Success = true, DriftType = DriftType.Output }
        }
      };
      results.UpdateStatistics();

      var summary = _detector.CreateDriftSummary(results);

      Assert.That(summary, Does.Contain("Exact matches: 1"));
      Assert.That(summary, Does.Contain("Prompt drifts: 1"));
      Assert.That(summary, Does.Contain("Output drifts: 1"));
    }

    [Test]
    public void CreateDriftSummary_WithFailures_ShowsFailureCount()
    {
      var results = new ReplayResult
      {
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.None },
          new RecordReplayResult { Success = false, ErrorMessage = "Failed" }
        }
      };
      results.UpdateStatistics();

      var summary = _detector.CreateDriftSummary(results);

      Assert.That(summary, Does.Contain("Failures: 1"));
    }

    [Test]
    public void CreateDriftSummary_NullResults_ThrowsException()
    {
      Assert.Throws<ArgumentNullException>(() => _detector.CreateDriftSummary(null!));
    }

    #endregion

    #region Drift Description Tests

    [Test]
    public void DriftDescription_MemoryDrift_IncludesHashInfo()
    {
      var original = CreateTestRecord("abc12345", "prompt1", "output1", true);
      var replayed = CreateTestRecord("xyz67890", "prompt1", "output1", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.DriftDescription, Does.Contain("abc12345"));
      Assert.That(result.DriftDescription, Does.Contain("xyz67890"));
    }

    [Test]
    public void DriftDescription_PromptDrift_IncludesGuidance()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem1", "prompt2", "output1", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.DriftDescription, Does.Contain("template").Or.Contains("governance"));
    }

    [Test]
    public void DriftDescription_OutputDrift_IncludesGuidance()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem1", "prompt1", "output2", true);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.DriftDescription, Does.Contain("model").Or.Contains("non-determinism"));
    }

    [Test]
    public void DriftDescription_ValidationDrift_ShowsOutcome()
    {
      var original = CreateTestRecord("mem1", "prompt1", "output1", true);
      var replayed = CreateTestRecord("mem1", "prompt1", "output1", false);

      var result = _detector.Compare(original, replayed);

      Assert.That(result.DriftDescription, Does.Contain("passed"));
      Assert.That(result.DriftDescription, Does.Contain("failed"));
    }

    #endregion

    #region Helper Methods

    private static AuditRecord CreateTestRecord(
      string memoryHash,
      string promptHash,
      string outputHash,
      bool validationPassed)
    {
      return new AuditRecord
      {
        RecordId = Guid.NewGuid().ToString("N").Substring(0, 8),
        MemoryHashBefore = memoryHash,
        PromptHash = promptHash,
        OutputHash = outputHash,
        ValidationPassed = validationPassed
      };
    }

    #endregion
  }
}
