using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for ReplayResult and RecordReplayResult DTOs.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class ReplayResultTests
  {
    #region DriftType Tests

    [Test]
    public void DriftType_HasExpectedValues()
    {
      Assert.That((int)DriftType.None, Is.EqualTo(0));
      Assert.That((int)DriftType.Prompt, Is.EqualTo(1));
      Assert.That((int)DriftType.Memory, Is.EqualTo(2));
      Assert.That((int)DriftType.Output, Is.EqualTo(3));
      Assert.That((int)DriftType.Validation, Is.EqualTo(4));
      Assert.That((int)DriftType.Constraints, Is.EqualTo(5));
    }

    #endregion

    #region RecordReplayResult Tests

    [Test]
    public void RecordReplayResult_DefaultValues()
    {
      var result = new RecordReplayResult();

      Assert.That(result.OriginalRecord, Is.Not.Null);
      Assert.That(result.ReplayedRecord, Is.Null);
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Is.Null);
      Assert.That(result.DriftType, Is.EqualTo(DriftType.None));
      Assert.That(result.DriftDescription, Is.Null);
      Assert.That(result.OutputMatches, Is.False);
      Assert.That(result.PromptMatches, Is.False);
      Assert.That(result.MemoryMatches, Is.False);
      Assert.That(result.ValidationMatches, Is.False);
    }

    [Test]
    public void RecordReplayResult_Succeeded_CreatesSuccessResult()
    {
      var original = new AuditRecord { RecordId = "orig-1" };
      var replayed = new AuditRecord { RecordId = "replay-1" };

      var result = RecordReplayResult.Succeeded(original, replayed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.OriginalRecord, Is.SameAs(original));
      Assert.That(result.ReplayedRecord, Is.SameAs(replayed));
      Assert.That(result.DriftType, Is.EqualTo(DriftType.None));
      Assert.That(result.OutputMatches, Is.True);
      Assert.That(result.PromptMatches, Is.True);
      Assert.That(result.MemoryMatches, Is.True);
      Assert.That(result.ValidationMatches, Is.True);
    }

    [Test]
    public void RecordReplayResult_Failed_CreatesFailedResult()
    {
      var original = new AuditRecord { RecordId = "orig-1" };
      var errorMsg = "Failed to generate output";

      var result = RecordReplayResult.Failed(original, errorMsg);

      Assert.That(result.Success, Is.False);
      Assert.That(result.OriginalRecord, Is.SameAs(original));
      Assert.That(result.ReplayedRecord, Is.Null);
      Assert.That(result.ErrorMessage, Is.EqualTo(errorMsg));
    }

    #endregion

    #region ReplayResult Tests

    [Test]
    public void ReplayResult_DefaultValues()
    {
      var result = new ReplayResult();

      Assert.That(result.Package, Is.Null);
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Is.Null);
      Assert.That(result.RecordResults, Is.Empty);
      Assert.That(result.ExactMatches, Is.EqualTo(0));
      Assert.That(result.PromptDrifts, Is.EqualTo(0));
      Assert.That(result.OutputDrifts, Is.EqualTo(0));
      Assert.That(result.MemoryDrifts, Is.EqualTo(0));
      Assert.That(result.Failures, Is.EqualTo(0));
      Assert.That(result.ReplayDurationMs, Is.EqualTo(0));
      Assert.That(result.AllMatched, Is.True); // Empty list means all matched
      Assert.That(result.HasDrift, Is.False);
      Assert.That(result.ModelValidation, Is.Null);
    }

    [Test]
    public void ReplayResult_Succeeded_CreatesSuccessResult()
    {
      var package = new DebugPackage { PackageId = "pkg-1" };
      var recordResults = new List<RecordReplayResult>
      {
        RecordReplayResult.Succeeded(new AuditRecord(), new AuditRecord()),
        RecordReplayResult.Succeeded(new AuditRecord(), new AuditRecord())
      };

      var result = ReplayResult.Succeeded(package, recordResults);

      Assert.That(result.Success, Is.True);
      Assert.That(result.Package, Is.SameAs(package));
      Assert.That(result.RecordResults, Has.Count.EqualTo(2));
      Assert.That(result.ExactMatches, Is.EqualTo(2));
      Assert.That(result.AllMatched, Is.True);
      Assert.That(result.HasDrift, Is.False);
    }

    [Test]
    public void ReplayResult_Failed_CreatesFailedResult()
    {
      var package = new DebugPackage { PackageId = "pkg-1" };
      var errorMsg = "Model mismatch";

      var result = ReplayResult.Failed(package, errorMsg);

      Assert.That(result.Success, Is.False);
      Assert.That(result.Package, Is.SameAs(package));
      Assert.That(result.ErrorMessage, Is.EqualTo(errorMsg));
    }

    [Test]
    public void ReplayResult_UpdateStatistics_CountsCorrectly()
    {
      var result = new ReplayResult
      {
        Success = true,
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.None },
          new RecordReplayResult { Success = true, DriftType = DriftType.None },
          new RecordReplayResult { Success = true, DriftType = DriftType.Prompt },
          new RecordReplayResult { Success = true, DriftType = DriftType.Output },
          new RecordReplayResult { Success = true, DriftType = DriftType.Memory },
          new RecordReplayResult { Success = false, ErrorMessage = "Failed" }
        }
      };

      result.UpdateStatistics();

      Assert.That(result.ExactMatches, Is.EqualTo(2));
      Assert.That(result.PromptDrifts, Is.EqualTo(1));
      Assert.That(result.OutputDrifts, Is.EqualTo(1));
      Assert.That(result.MemoryDrifts, Is.EqualTo(1));
      Assert.That(result.Failures, Is.EqualTo(1));
      Assert.That(result.AllMatched, Is.False);
      Assert.That(result.HasDrift, Is.True);
    }

    [Test]
    public void ReplayResult_AllMatched_TrueWhenAllExactMatches()
    {
      var result = new ReplayResult
      {
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.None },
          new RecordReplayResult { Success = true, DriftType = DriftType.None }
        }
      };

      result.UpdateStatistics();

      Assert.That(result.AllMatched, Is.True);
    }

    [Test]
    public void ReplayResult_AllMatched_FalseWithDrift()
    {
      var result = new ReplayResult
      {
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.None },
          new RecordReplayResult { Success = true, DriftType = DriftType.Output }
        }
      };

      result.UpdateStatistics();

      Assert.That(result.AllMatched, Is.False);
    }

    [Test]
    public void ReplayResult_AllMatched_FalseWithFailures()
    {
      var result = new ReplayResult
      {
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.None },
          new RecordReplayResult { Success = false, ErrorMessage = "Error" }
        }
      };

      result.UpdateStatistics();

      Assert.That(result.AllMatched, Is.False);
    }

    [Test]
    public void ReplayResult_HasDrift_TrueWithPromptDrift()
    {
      var result = new ReplayResult
      {
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.Prompt }
        }
      };

      result.UpdateStatistics();

      Assert.That(result.HasDrift, Is.True);
    }

    [Test]
    public void ReplayResult_HasDrift_FalseWithOnlyExactMatches()
    {
      var result = new ReplayResult
      {
        RecordResults = new List<RecordReplayResult>
        {
          new RecordReplayResult { Success = true, DriftType = DriftType.None }
        }
      };

      result.UpdateStatistics();

      Assert.That(result.HasDrift, Is.False);
    }

    #endregion
  }
}
