using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for ReplayEngine.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class ReplayEngineTests
  {
    private ReplayEngine _engine = null!;
    private DebugPackageExporter _exporter = null!;

    [SetUp]
    public void SetUp()
    {
      _engine = new ReplayEngine();
      _exporter = new DebugPackageExporter();
    }

    #region Constructor Tests

    [Test]
    public void Constructor_DefaultDependencies_CreatesEngine()
    {
      var engine = new ReplayEngine();

      Assert.That(engine, Is.Not.Null);
    }

    [Test]
    public void Constructor_CustomDependencies_UsesProvided()
    {
      var importer = new DebugPackageImporter();
      var detector = new DriftDetector();

      var engine = new ReplayEngine(importer, detector);

      Assert.That(engine, Is.Not.Null);
    }

    [Test]
    public void Constructor_NullImporter_ThrowsException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new ReplayEngine(null!, new DriftDetector()));
    }

    [Test]
    public void Constructor_NullDetector_ThrowsException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        new ReplayEngine(new DebugPackageImporter(), null!));
    }

    #endregion

    #region Replay Tests

    [Test]
    public void Replay_EmptyPackage_ReturnsSuccess()
    {
      var package = CreateTestPackage(0);

      var result = _engine.Replay(package, ctx => CreateMatchingRecord(ctx));

      Assert.That(result.Success, Is.True);
      Assert.That(result.RecordResults, Is.Empty);
      Assert.That(result.AllMatched, Is.True);
    }

    [Test]
    public void Replay_SingleRecordMatches_ReturnsNoDrift()
    {
      var package = CreateTestPackage(1);

      var result = _engine.Replay(package, ctx => CreateMatchingRecord(ctx));

      Assert.That(result.Success, Is.True);
      Assert.That(result.RecordResults, Has.Count.EqualTo(1));
      Assert.That(result.ExactMatches, Is.EqualTo(1));
      Assert.That(result.AllMatched, Is.True);
    }

    [Test]
    public void Replay_MultipleRecords_ReplaysAll()
    {
      var package = CreateTestPackage(5);

      var result = _engine.Replay(package, ctx => CreateMatchingRecord(ctx));

      Assert.That(result.Success, Is.True);
      Assert.That(result.RecordResults, Has.Count.EqualTo(5));
      Assert.That(result.ExactMatches, Is.EqualTo(5));
    }

    [Test]
    public void Replay_OutputDrift_DetectsDrift()
    {
      var package = CreateTestPackage(1);

      var result = _engine.Replay(package, ctx =>
      {
        var record = CreateMatchingRecord(ctx);
        record.OutputHash = "different-hash"; // Cause drift
        return record;
      });

      Assert.That(result.Success, Is.True);
      Assert.That(result.OutputDrifts, Is.EqualTo(1));
      Assert.That(result.HasDrift, Is.True);
    }

    [Test]
    public void Replay_PromptDrift_DetectsDrift()
    {
      var package = CreateTestPackage(1);

      var result = _engine.Replay(package, ctx =>
      {
        var record = CreateMatchingRecord(ctx);
        record.PromptHash = "different-prompt-hash";
        return record;
      });

      Assert.That(result.Success, Is.True);
      Assert.That(result.PromptDrifts, Is.EqualTo(1));
    }

    [Test]
    public void Replay_MemoryDrift_DetectsDrift()
    {
      var package = CreateTestPackage(1);

      var result = _engine.Replay(package, ctx =>
      {
        var record = CreateMatchingRecord(ctx);
        record.MemoryHashBefore = "different-memory-hash";
        return record;
      });

      Assert.That(result.Success, Is.True);
      Assert.That(result.MemoryDrifts, Is.EqualTo(1));
    }

    [Test]
    public void Replay_GeneratorThrows_RecordsFailure()
    {
      var package = CreateTestPackage(3);
      var callCount = 0;

      var result = _engine.Replay(package, ctx =>
      {
        callCount++;
        if (callCount == 2)
          throw new InvalidOperationException("Generator failed");
        return CreateMatchingRecord(ctx);
      }, options: new ReplayOptions { StopOnFirstFailure = true });

      Assert.That(result.Success, Is.True); // Overall replay succeeds
      Assert.That(result.RecordResults, Has.Count.EqualTo(2)); // Stopped at failure
      Assert.That(result.Failures, Is.EqualTo(1));
    }

    [Test]
    public void Replay_StopOnFirstFailureFalse_ContinuesAfterFailure()
    {
      var package = CreateTestPackage(3);
      var callCount = 0;

      var result = _engine.Replay(package, ctx =>
      {
        callCount++;
        if (callCount == 2)
          throw new InvalidOperationException("Generator failed");
        return CreateMatchingRecord(ctx);
      }, options: new ReplayOptions { StopOnFirstFailure = false });

      Assert.That(result.RecordResults, Has.Count.EqualTo(3));
      Assert.That(result.Failures, Is.EqualTo(1));
      Assert.That(result.ExactMatches, Is.EqualTo(2));
    }

    [Test]
    public void Replay_StopOnFirstDrift_StopsOnDrift()
    {
      var package = CreateTestPackage(5);
      var callCount = 0;

      var result = _engine.Replay(package, ctx =>
      {
        callCount++;
        var record = CreateMatchingRecord(ctx);
        if (callCount == 3)
          record.OutputHash = "different"; // Cause drift on 3rd record
        return record;
      }, options: new ReplayOptions { StopOnFirstDrift = true });

      Assert.That(result.RecordResults, Has.Count.EqualTo(3)); // Stopped at drift
      Assert.That(result.ExactMatches, Is.EqualTo(2));
      Assert.That(result.OutputDrifts, Is.EqualTo(1));
    }

    [Test]
    public void Replay_MaxRecords_LimitsReplay()
    {
      var package = CreateTestPackage(10);

      var result = _engine.Replay(package, ctx => CreateMatchingRecord(ctx),
        options: new ReplayOptions { MaxRecords = 3 });

      Assert.That(result.RecordResults, Has.Count.EqualTo(3));
    }

    [Test]
    public void Replay_NullPackage_ThrowsException()
    {
      Assert.Throws<ArgumentNullException>(() =>
        _engine.Replay(null!, ctx => CreateMatchingRecord(ctx)));
    }

    [Test]
    public void Replay_NullGenerator_ThrowsException()
    {
      var package = CreateTestPackage(1);

      Assert.Throws<ArgumentNullException>(() =>
        _engine.Replay(package, (Func<ReplayContext, AuditRecord>)null!));
    }

    [Test]
    public void Replay_MeasuresDuration()
    {
      var package = CreateTestPackage(1);

      var result = _engine.Replay(package, ctx =>
      {
        Thread.Sleep(10); // Small delay
        return CreateMatchingRecord(ctx);
      });

      Assert.That(result.ReplayDurationMs, Is.GreaterThan(0));
    }

    [Test]
    public void Replay_PassesCorrectContext()
    {
      var record = new AuditRecord
      {
        RecordId = "rec-1",
        NpcId = "npc-test",
        PlayerInput = "Hello NPC!",
        Seed = 12345,
        InteractionCount = 3,
        MemoryHashBefore = "mem",
        PromptHash = "prompt",
        OutputHash = "output",
        ValidationPassed = true
      };

      var package = new DebugPackage { Records = new List<AuditRecord> { record } };

      ReplayContext? capturedContext = null;
      _engine.Replay(package, ctx =>
      {
        capturedContext = ctx;
        return CreateMatchingRecord(ctx);
      });

      Assert.That(capturedContext, Is.Not.Null);
      Assert.That(capturedContext!.NpcId, Is.EqualTo("npc-test"));
      Assert.That(capturedContext.PlayerInput, Is.EqualTo("Hello NPC!"));
      Assert.That(capturedContext.Seed, Is.EqualTo(12345));
      Assert.That(capturedContext.InteractionCount, Is.EqualTo(3));
      Assert.That(capturedContext.OriginalRecord, Is.SameAs(record));
    }

    #endregion

    #region Async Replay Tests

    [Test]
    public async Task ReplayAsync_CompletesSuccessfully()
    {
      var package = CreateTestPackage(2);

      var result = await _engine.ReplayAsync(
        package,
        async (ctx, ct) =>
        {
          await Task.Delay(1, ct);
          return CreateMatchingRecord(ctx);
        });

      Assert.That(result.Success, Is.True);
      Assert.That(result.RecordResults, Has.Count.EqualTo(2));
    }

    [Test]
    public void ReplayAsync_Cancellation_ThrowsOperationCanceled()
    {
      var package = CreateTestPackage(10);
      var cts = new CancellationTokenSource();
      var callCount = 0;

      Assert.ThrowsAsync<OperationCanceledException>(async () =>
      {
        await _engine.ReplayAsync(
          package,
          async (ctx, ct) =>
          {
            callCount++;
            if (callCount == 3)
              cts.Cancel();
            ct.ThrowIfCancellationRequested();
            await Task.Delay(1, ct);
            return CreateMatchingRecord(ctx);
          },
          cancellationToken: cts.Token);
      });
    }

    #endregion

    #region Model Fingerprint Validation Tests

    [Test]
    public void Replay_ValidModelFingerprint_Succeeds()
    {
      var fingerprint = new ModelFingerprint
      {
        ModelFileName = "test.gguf",
        ModelFileSizeBytes = 1000
      };
      fingerprint.ComputeFingerprintHash();

      var package = CreateTestPackage(1);
      package.ModelFingerprint = fingerprint;

      var result = _engine.Replay(
        package,
        ctx => CreateMatchingRecord(ctx),
        currentModelFingerprint: fingerprint,
        options: new ReplayOptions { ValidateModelFingerprint = true });

      Assert.That(result.Success, Is.True);
      Assert.That(result.ModelValidation, Is.Not.Null);
      Assert.That(result.ModelValidation!.IsExactMatch, Is.True);
    }

    [Test]
    public void Replay_IncompatibleModel_FailsReplay()
    {
      var packageFingerprint = new ModelFingerprint
      {
        ModelFileName = "model-a.gguf",
        ModelFileSizeBytes = 1000
      };
      packageFingerprint.ComputeFingerprintHash();

      var currentFingerprint = new ModelFingerprint
      {
        ModelFileName = "model-b.gguf",
        ModelFileSizeBytes = 2000
      };
      currentFingerprint.ComputeFingerprintHash();

      var package = CreateTestPackage(1);
      package.ModelFingerprint = packageFingerprint;

      var result = _engine.Replay(
        package,
        ctx => CreateMatchingRecord(ctx),
        currentModelFingerprint: currentFingerprint,
        options: new ReplayOptions { ValidateModelFingerprint = true });

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("Incompatible"));
    }

    [Test]
    public void Replay_CompatibleModel_SucceedsWithoutExactMatch()
    {
      var packageFingerprint = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1000,
        ContextLength = 4096
      };
      packageFingerprint.ComputeFingerprintHash();

      var currentFingerprint = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1000,
        ContextLength = 8192 // Different config
      };
      currentFingerprint.ComputeFingerprintHash();

      var package = CreateTestPackage(1);
      package.ModelFingerprint = packageFingerprint;

      var result = _engine.Replay(
        package,
        ctx => CreateMatchingRecord(ctx),
        currentModelFingerprint: currentFingerprint,
        options: new ReplayOptions
        {
          ValidateModelFingerprint = true,
          RequireExactModelMatch = false
        });

      Assert.That(result.Success, Is.True);
      Assert.That(result.ModelValidation!.IsCompatible, Is.True);
      Assert.That(result.ModelValidation.IsExactMatch, Is.False);
    }

    [Test]
    public void Replay_RequireExactMatch_FailsOnCompatibleOnly()
    {
      var packageFingerprint = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1000,
        ContextLength = 4096
      };
      packageFingerprint.ComputeFingerprintHash();

      var currentFingerprint = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1000,
        ContextLength = 8192
      };
      currentFingerprint.ComputeFingerprintHash();

      var package = CreateTestPackage(1);
      package.ModelFingerprint = packageFingerprint;

      var result = _engine.Replay(
        package,
        ctx => CreateMatchingRecord(ctx),
        currentModelFingerprint: currentFingerprint,
        options: new ReplayOptions
        {
          ValidateModelFingerprint = true,
          RequireExactModelMatch = true
        });

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("mismatch"));
    }

    #endregion

    #region Validate Tests

    [Test]
    public void Validate_ValidPackage_ReturnsCanReplay()
    {
      var package = CreateTestPackage(3);
      package.ComputeIntegrityHash();
      var json = _exporter.ToJson(package);

      var result = _engine.Validate(json);

      Assert.That(result.ImportSucceeded, Is.True);
      Assert.That(result.IntegrityValid, Is.True);
      Assert.That(result.CanReplay, Is.True);
      Assert.That(result.RecordCount, Is.EqualTo(3));
    }

    [Test]
    public void Validate_InvalidJson_ReturnsFalse()
    {
      var result = _engine.Validate("not valid json");

      Assert.That(result.ImportSucceeded, Is.False);
      Assert.That(result.CanReplay, Is.False);
      Assert.That(result.ErrorMessage, Is.Not.Null);
    }

    [Test]
    public void Validate_TamperedPackage_ReturnsIntegrityFailed()
    {
      var package = CreateTestPackage(1);
      package.ComputeIntegrityHash();
      var json = _exporter.ToJson(package);

      // Tamper with the JSON
      json = json.Replace(package.PackageId, "tampered-id");

      var result = _engine.Validate(json);

      Assert.That(result.ImportSucceeded, Is.False);
      Assert.That(result.IntegrityValid, Is.False);
      Assert.That(result.CanReplay, Is.False);
    }

    [Test]
    public void Validate_WithModelFingerprint_ValidatesModel()
    {
      var fingerprint = new ModelFingerprint
      {
        ModelFileName = "test.gguf",
        ModelFileSizeBytes = 1000
      };
      fingerprint.ComputeFingerprintHash();

      var package = CreateTestPackage(1);
      package.ModelFingerprint = fingerprint;
      package.ComputeIntegrityHash();
      var json = _exporter.ToJson(package);

      var result = _engine.Validate(json, fingerprint);

      Assert.That(result.ModelValidation, Is.Not.Null);
      Assert.That(result.ModelValidation!.IsExactMatch, Is.True);
    }

    [Test]
    public void Validate_ReturnsNpcIds()
    {
      var package = CreateTestPackage(0);
      package.NpcIds = new List<string> { "npc-1", "npc-2" };
      package.ComputeIntegrityHash();
      var json = _exporter.ToJson(package);

      var result = _engine.Validate(json);

      Assert.That(result.NpcIds, Contains.Item("npc-1"));
      Assert.That(result.NpcIds, Contains.Item("npc-2"));
    }

    #endregion

    #region GetDriftSummary Tests

    [Test]
    public void GetDriftSummary_ReturnsFormattedSummary()
    {
      var package = CreateTestPackage(3);
      var replayResult = _engine.Replay(package, ctx =>
      {
        var record = CreateMatchingRecord(ctx);
        if (ctx.InteractionCount == 2)
          record.OutputHash = "different";
        return record;
      });

      var summary = _engine.GetDriftSummary(replayResult);

      Assert.That(summary, Is.Not.Empty);
      Assert.That(summary, Does.Contain("Exact matches"));
    }

    #endregion

    #region Helper Methods

    private DebugPackage CreateTestPackage(int recordCount)
    {
      var recorder = new AuditRecorder(recordCount + 1);

      for (int i = 0; i < recordCount; i++)
      {
        recorder.Record(new AuditRecordBuilder()
          .WithNpcId("npc-1")
          .WithInteractionCount(i + 1)
          .WithSeed(1000 + i)
          .WithPlayerInput($"Input {i}")
          .WithOutput($"Output {i}", $"Dialogue {i}")
          .WithStateHashes($"mem-{i}", $"prompt-{i}", $"constraints-{i}")
          .WithValidationOutcome(true, 0, 0)
          .Build());
      }

      return _exporter.Export(recorder);
    }

    private static AuditRecord CreateMatchingRecord(ReplayContext ctx)
    {
      // Create a record that matches the original
      return new AuditRecord
      {
        RecordId = Guid.NewGuid().ToString("N").Substring(0, 8),
        NpcId = ctx.NpcId,
        InteractionCount = ctx.InteractionCount,
        Seed = ctx.Seed,
        PlayerInput = ctx.PlayerInput,
        MemoryHashBefore = ctx.OriginalRecord.MemoryHashBefore,
        PromptHash = ctx.OriginalRecord.PromptHash,
        OutputHash = ctx.OriginalRecord.OutputHash,
        ValidationPassed = ctx.OriginalRecord.ValidationPassed
      };
    }

    #endregion
  }
}
