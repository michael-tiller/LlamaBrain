using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Context provided to the replay callback for generating a single turn.
  /// </summary>
  public sealed class ReplayContext
  {
    /// <summary>
    /// The original audit record being replayed.
    /// </summary>
    public AuditRecord OriginalRecord { get; set; } = new AuditRecord();

    /// <summary>
    /// The NPC ID for this interaction.
    /// </summary>
    public string NpcId { get; set; } = "";

    /// <summary>
    /// The player input to replay.
    /// </summary>
    public string PlayerInput { get; set; } = "";

    /// <summary>
    /// The seed to use for deterministic generation.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// The interaction count (turn number).
    /// </summary>
    public int InteractionCount { get; set; }
  }

  /// <summary>
  /// Delegate for generating a single replay turn.
  /// </summary>
  /// <param name="context">The replay context with input and seed.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The audit record produced during replay.</returns>
  public delegate Task<AuditRecord> ReplayGeneratorDelegate(
    ReplayContext context,
    CancellationToken cancellationToken);

  /// <summary>
  /// Options for configuring replay behavior.
  /// </summary>
  public sealed class ReplayOptions
  {
    /// <summary>
    /// Whether to stop on first drift detected.
    /// </summary>
    public bool StopOnFirstDrift { get; set; }

    /// <summary>
    /// Whether to stop on first failure.
    /// </summary>
    public bool StopOnFirstFailure { get; set; } = true;

    /// <summary>
    /// Maximum number of records to replay (0 = all).
    /// </summary>
    public int MaxRecords { get; set; }

    /// <summary>
    /// Whether to validate model fingerprint before replay.
    /// </summary>
    public bool ValidateModelFingerprint { get; set; } = true;

    /// <summary>
    /// Whether to require exact model match (vs compatible).
    /// </summary>
    public bool RequireExactModelMatch { get; set; }
  }

  /// <summary>
  /// Engine for replaying debug packages and detecting drift.
  /// Provides callbacks for integration with actual generation systems.
  /// </summary>
  /// <remarks>
  /// The ReplayEngine doesn't directly generate LLM outputs - instead,
  /// it accepts a delegate that handles the actual generation. This allows
  /// integration with BrainAgent or mocked generators for testing.
  /// </remarks>
  public sealed class ReplayEngine
  {
    private readonly DebugPackageImporter _importer;
    private readonly DriftDetector _driftDetector;

    /// <summary>
    /// Creates a new ReplayEngine with default dependencies.
    /// </summary>
    public ReplayEngine()
    {
      _importer = new DebugPackageImporter();
      _driftDetector = new DriftDetector();
    }

    /// <summary>
    /// Creates a new ReplayEngine with custom dependencies.
    /// </summary>
    public ReplayEngine(DebugPackageImporter importer, DriftDetector driftDetector)
    {
      _importer = importer ?? throw new ArgumentNullException(nameof(importer));
      _driftDetector = driftDetector ?? throw new ArgumentNullException(nameof(driftDetector));
    }

    /// <summary>
    /// Replays a debug package using the provided generator.
    /// </summary>
    /// <param name="package">The debug package to replay.</param>
    /// <param name="generator">Delegate that generates a single replay turn.</param>
    /// <param name="currentModelFingerprint">Current model fingerprint for validation.</param>
    /// <param name="options">Replay options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Replay result with per-record outcomes.</returns>
    public async Task<ReplayResult> ReplayAsync(
      DebugPackage package,
      ReplayGeneratorDelegate generator,
      ModelFingerprint? currentModelFingerprint = null,
      ReplayOptions? options = null,
      CancellationToken cancellationToken = default)
    {
      if (package == null)
        throw new ArgumentNullException(nameof(package));

      if (generator == null)
        throw new ArgumentNullException(nameof(generator));

      options ??= new ReplayOptions();

      var sw = Stopwatch.StartNew();
      var recordResults = new List<RecordReplayResult>();

      // Validate model fingerprint if requested
      ModelFingerprintValidationResult? modelValidation = null;
      if (options.ValidateModelFingerprint && currentModelFingerprint != null)
      {
        modelValidation = _importer.ValidateModelFingerprint(package, currentModelFingerprint);

        if (options.RequireExactModelMatch && !modelValidation.IsExactMatch)
        {
          return ReplayResult.Failed(package,
            $"Model fingerprint mismatch: {modelValidation.MismatchDescription}");
        }

        if (!modelValidation.IsCompatible)
        {
          return ReplayResult.Failed(package,
            $"Incompatible model: {modelValidation.MismatchDescription}");
        }
      }

      // Determine how many records to replay
      var recordCount = package.Records.Count;
      if (options.MaxRecords > 0 && options.MaxRecords < recordCount)
      {
        recordCount = options.MaxRecords;
      }

      // Replay each record
      for (int i = 0; i < recordCount; i++)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var originalRecord = package.Records[i];

        try
        {
          var context = new ReplayContext
          {
            OriginalRecord = originalRecord,
            NpcId = originalRecord.NpcId,
            PlayerInput = originalRecord.PlayerInput,
            Seed = originalRecord.Seed,
            InteractionCount = originalRecord.InteractionCount
          };

          var replayedRecord = await generator(context, cancellationToken);
          var comparisonResult = _driftDetector.Compare(originalRecord, replayedRecord);
          recordResults.Add(comparisonResult);

          // Check stop conditions
          if (options.StopOnFirstDrift && comparisonResult.DriftType != DriftType.None)
          {
            break;
          }
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (Exception ex)
        {
          var failedResult = RecordReplayResult.Failed(originalRecord, ex.Message);
          recordResults.Add(failedResult);

          if (options.StopOnFirstFailure)
          {
            break;
          }
        }
      }

      sw.Stop();

      var result = ReplayResult.Succeeded(package, recordResults);
      result.ReplayDurationMs = sw.ElapsedMilliseconds;
      result.ModelValidation = modelValidation;

      return result;
    }

    /// <summary>
    /// Replays a debug package synchronously.
    /// </summary>
    public ReplayResult Replay(
      DebugPackage package,
      Func<ReplayContext, AuditRecord> generator,
      ModelFingerprint? currentModelFingerprint = null,
      ReplayOptions? options = null)
    {
      if (generator == null)
        throw new ArgumentNullException(nameof(generator));

      // Wrap sync generator in async delegate
      Task<AuditRecord> asyncGenerator(ReplayContext ctx, CancellationToken _)
      {
        var result = generator(ctx);
        return Task.FromResult(result);
      }

      return ReplayAsync(package, asyncGenerator, currentModelFingerprint, options)
        .GetAwaiter()
        .GetResult();
    }

    /// <summary>
    /// Validates a debug package without replaying.
    /// Checks integrity, version, and model compatibility.
    /// </summary>
    /// <param name="json">JSON string of the debug package.</param>
    /// <param name="currentModelFingerprint">Current model fingerprint for validation.</param>
    /// <returns>Validation result.</returns>
    public ReplayValidationResult Validate(
      string json,
      ModelFingerprint? currentModelFingerprint = null)
    {
      var importResult = _importer.FromJson(json, validateIntegrity: true);

      var result = new ReplayValidationResult
      {
        ImportSucceeded = importResult.Success,
        ImportWarnings = importResult.Warnings,
        IntegrityValid = importResult.IntegrityValid,
        VersionSupported = importResult.VersionSupported,
        ErrorMessage = importResult.ErrorMessage
      };

      if (!importResult.Success || importResult.Package == null)
      {
        return result;
      }

      result.Package = importResult.Package;
      result.RecordCount = importResult.Package.Records.Count;
      result.NpcIds = new List<string>(importResult.Package.NpcIds);

      // Validate model if fingerprint provided
      if (currentModelFingerprint != null)
      {
        result.ModelValidation = _importer.ValidateModelFingerprint(
          importResult.Package, currentModelFingerprint);
      }

      result.CanReplay = result.ImportSucceeded &&
                         (result.IntegrityValid ?? true) &&
                         (result.ModelValidation?.IsCompatible ?? true);

      return result;
    }

    /// <summary>
    /// Gets a drift summary for replay results.
    /// </summary>
    public string GetDriftSummary(ReplayResult result)
    {
      return _driftDetector.CreateDriftSummary(result);
    }
  }

  /// <summary>
  /// Result of validating a debug package for replay.
  /// </summary>
  public sealed class ReplayValidationResult
  {
    /// <summary>
    /// Whether the JSON was successfully imported.
    /// </summary>
    public bool ImportSucceeded { get; set; }

    /// <summary>
    /// Import warnings (e.g., version mismatch).
    /// </summary>
    public List<string> ImportWarnings { get; set; } = new List<string>();

    /// <summary>
    /// Whether the integrity hash was valid.
    /// </summary>
    public bool? IntegrityValid { get; set; }

    /// <summary>
    /// Whether the package version is supported.
    /// </summary>
    public bool VersionSupported { get; set; } = true;

    /// <summary>
    /// Error message if import failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The imported package (if successful).
    /// </summary>
    public DebugPackage? Package { get; set; }

    /// <summary>
    /// Number of records in the package.
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// NPC IDs in the package.
    /// </summary>
    public List<string> NpcIds { get; set; } = new List<string>();

    /// <summary>
    /// Model fingerprint validation result.
    /// </summary>
    public ModelFingerprintValidationResult? ModelValidation { get; set; }

    /// <summary>
    /// Whether the package can be replayed.
    /// </summary>
    public bool CanReplay { get; set; }
  }
}
