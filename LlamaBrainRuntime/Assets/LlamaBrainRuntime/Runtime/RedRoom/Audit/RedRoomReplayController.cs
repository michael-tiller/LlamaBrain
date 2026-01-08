#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using LlamaBrain.Core.Audit;
using LlamaBrain.Core;
using LlamaBrain.Runtime.Core;

namespace LlamaBrain.Runtime.RedRoom.Audit
{
  /// <summary>
  /// Event data for replay progress updates.
  /// </summary>
  [Serializable]
  public class ReplayProgressEventData
  {
    /// <summary>Current record index (0-based).</summary>
    public int CurrentIndex;

    /// <summary>Total number of records to replay.</summary>
    public int TotalRecords;

    /// <summary>Progress as a percentage (0-100).</summary>
    public float ProgressPercent;

    /// <summary>Result of the current record replay.</summary>
    public RecordReplayResult? CurrentResult;

    /// <summary>Whether drift was detected in the current record.</summary>
    public bool DriftDetected;

    /// <summary>Type of drift if detected.</summary>
    public DriftType DriftType;
  }

  /// <summary>
  /// Event data for replay completion.
  /// </summary>
  [Serializable]
  public class ReplayCompletedEventData
  {
    /// <summary>The full replay result.</summary>
    public ReplayResult Result = null!;

    /// <summary>Whether all records matched exactly.</summary>
    public bool AllMatched;

    /// <summary>Number of exact matches.</summary>
    public int ExactMatches;

    /// <summary>Number of output drifts detected.</summary>
    public int OutputDrifts;

    /// <summary>Number of failures.</summary>
    public int Failures;

    /// <summary>Total replay duration in milliseconds.</summary>
    public long DurationMs;

    /// <summary>Summary string for display.</summary>
    public string Summary = "";
  }

  /// <summary>
  /// Unity events for replay callbacks.
  /// </summary>
  [Serializable]
  public class ReplayProgressEvent : UnityEvent<ReplayProgressEventData> { }

  [Serializable]
  public class ReplayCompletedEvent : UnityEvent<ReplayCompletedEventData> { }

  [Serializable]
  public class ReplayErrorEvent : UnityEvent<string> { }

  /// <summary>
  /// Controller for importing and replaying debug packages in the RedRoom scene.
  /// Provides step-through debugging and drift visualization capabilities.
  /// </summary>
  /// <remarks>
  /// This component handles:
  /// - Debug package import from JSON files
  /// - Model fingerprint validation before replay
  /// - Full or step-by-step replay execution
  /// - Drift detection and visualization callbacks
  /// - Integration with LlamaBrainAgent for actual generation
  /// </remarks>
  public class RedRoomReplayController : MonoBehaviour
  {
    /// <summary>
    /// Singleton instance for easy access.
    /// </summary>
    public static RedRoomReplayController? Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Whether to stop replay on first drift detection.")]
    [SerializeField] private bool stopOnFirstDrift = false;

    [Tooltip("Whether to stop replay on first failure.")]
    [SerializeField] private bool stopOnFirstFailure = true;

    [Tooltip("Whether to validate model fingerprint before replay.")]
    [SerializeField] private bool validateModelFingerprint = true;

    [Tooltip("Whether to require exact model match (vs compatible).")]
    [SerializeField] private bool requireExactModelMatch = false;

    [Header("Events")]
    [Tooltip("Called when a record is replayed.")]
    public ReplayProgressEvent OnReplayProgress = new ReplayProgressEvent();

    [Tooltip("Called when replay completes.")]
    public ReplayCompletedEvent OnReplayCompleted = new ReplayCompletedEvent();

    [Tooltip("Called when an error occurs during replay.")]
    public ReplayErrorEvent OnReplayError = new ReplayErrorEvent();

    [Header("Debug")]
    [Tooltip("Enable verbose logging.")]
    [SerializeField] private bool verboseLogging = false;

    private ReplayEngine? _replayEngine;
    private DebugPackageImporter? _importer;
    private DebugPackage? _currentPackage;
    private ReplayResult? _lastReplayResult;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isReplaying;
    private int _currentStepIndex;

    /// <summary>
    /// Gets whether a replay is currently in progress.
    /// </summary>
    public bool IsReplaying => _isReplaying;

    /// <summary>
    /// Gets the currently loaded debug package.
    /// </summary>
    public DebugPackage? CurrentPackage => _currentPackage;

    /// <summary>
    /// Gets the last replay result.
    /// </summary>
    public ReplayResult? LastReplayResult => _lastReplayResult;

    /// <summary>
    /// Gets whether a package is loaded and ready for replay.
    /// </summary>
    public bool IsPackageLoaded => _currentPackage != null;

    /// <summary>
    /// Gets the current step index in step-through mode.
    /// </summary>
    public int CurrentStepIndex => _currentStepIndex;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Debug.LogWarning("[RedRoomReplayController] Duplicate instance detected, destroying this one.");
        Destroy(gameObject);
        return;
      }

      Instance = this;
      InitializeReplayEngine();
    }

    private void OnDestroy()
    {
      _cancellationTokenSource?.Cancel();
      _cancellationTokenSource?.Dispose();

      if (Instance == this)
      {
        Instance = null;
      }
    }

    private void InitializeReplayEngine()
    {
      _importer = new DebugPackageImporter();
      _replayEngine = new ReplayEngine();

      if (verboseLogging)
      {
        Debug.Log("[RedRoomReplayController] Initialized replay engine");
      }
    }

    /// <summary>
    /// Imports a debug package from a JSON string.
    /// </summary>
    /// <param name="json">The JSON content of the debug package.</param>
    /// <returns>Import result with validation information.</returns>
    public ImportResult ImportFromJson(string json)
    {
      if (_importer == null)
        throw new InvalidOperationException("Importer not initialized");

      var result = _importer.FromJson(json, validateIntegrity: true);

      if (result.Success && result.Package != null)
      {
        _currentPackage = result.Package;
        _currentStepIndex = 0;

        Debug.Log($"[RedRoomReplayController] Imported package: {result.Package.PackageId} with {result.Package.TotalInteractions} records");

        if (result.Warnings.Count > 0)
        {
          foreach (var warning in result.Warnings)
          {
            Debug.LogWarning($"[RedRoomReplayController] Import warning: {warning}");
          }
        }
      }
      else
      {
        Debug.LogError($"[RedRoomReplayController] Import failed: {result.ErrorMessage}");
        OnReplayError?.Invoke(result.ErrorMessage ?? "Unknown import error");
      }

      return result;
    }

    /// <summary>
    /// Imports a debug package from a file path.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <returns>Import result with validation information.</returns>
    public ImportResult ImportFromFile(string filePath)
    {
      try
      {
        if (!System.IO.File.Exists(filePath))
        {
          var error = $"File not found: {filePath}";
          OnReplayError?.Invoke(error);
          return ImportResult.Failed(error);
        }

        var json = System.IO.File.ReadAllText(filePath);
        return ImportFromJson(json);
      }
      catch (Exception ex)
      {
        var error = $"Failed to read file: {ex.Message}";
        OnReplayError?.Invoke(error);
        return ImportResult.Failed(error);
      }
    }

    /// <summary>
    /// Validates the loaded package against the current model fingerprint.
    /// </summary>
    /// <param name="currentFingerprint">The fingerprint of the currently loaded model.</param>
    /// <returns>Validation result with compatibility information.</returns>
    public ModelFingerprintValidationResult? ValidateModelFingerprint(ModelFingerprint currentFingerprint)
    {
      if (_currentPackage == null || _importer == null)
        return null;

      var result = _importer.ValidateModelFingerprint(_currentPackage, currentFingerprint);

      if (!result.IsCompatible)
      {
        Debug.LogWarning($"[RedRoomReplayController] Model fingerprint mismatch: {result.MismatchDescription}");
      }
      else if (!result.IsExactMatch)
      {
        Debug.Log($"[RedRoomReplayController] Model compatible but not exact match: {result.MismatchDescription}");
      }
      else
      {
        Debug.Log("[RedRoomReplayController] Model fingerprint matches exactly");
      }

      return result;
    }

    /// <summary>
    /// Gets the current model fingerprint from AuditRecorderBridge.
    /// </summary>
    /// <returns>The current model fingerprint, or null if not available.</returns>
    public ModelFingerprint? GetCurrentModelFingerprint()
    {
      var bridge = AuditRecorderBridge.Instance;
      return bridge?.ModelFingerprint;
    }

    /// <summary>
    /// Replays the loaded package using a mock generator for testing.
    /// Uses the exact recorded outputs instead of generating new ones.
    /// </summary>
    /// <returns>Replay result.</returns>
    public async UniTask<ReplayResult?> ReplayWithMockGeneratorAsync()
    {
      if (_currentPackage == null)
      {
        OnReplayError?.Invoke("No package loaded");
        return null;
      }

      return await ReplayAsync(ctx =>
      {
        // Mock generator: return a record matching the original
        return new AuditRecord
        {
          RecordId = Guid.NewGuid().ToString("N").Substring(0, 16),
          NpcId = ctx.NpcId,
          InteractionCount = ctx.InteractionCount,
          Seed = ctx.Seed,
          PlayerInput = ctx.PlayerInput,
          MemoryHashBefore = ctx.OriginalRecord.MemoryHashBefore,
          PromptHash = ctx.OriginalRecord.PromptHash,
          OutputHash = ctx.OriginalRecord.OutputHash,
          RawOutput = ctx.OriginalRecord.RawOutput,
          DialogueText = ctx.OriginalRecord.DialogueText,
          ValidationPassed = ctx.OriginalRecord.ValidationPassed
        };
      });
    }

    /// <summary>
    /// Replays the loaded package using a custom generator function.
    /// </summary>
    /// <param name="generator">Function that generates a replay record from context.</param>
    /// <returns>Replay result.</returns>
    public async UniTask<ReplayResult?> ReplayAsync(Func<ReplayContext, AuditRecord> generator)
    {
      if (_currentPackage == null || _replayEngine == null)
      {
        OnReplayError?.Invoke("No package loaded or engine not initialized");
        return null;
      }

      if (_isReplaying)
      {
        OnReplayError?.Invoke("Replay already in progress");
        return null;
      }

      _isReplaying = true;
      _cancellationTokenSource?.Dispose();
      _cancellationTokenSource = new CancellationTokenSource();

      try
      {
        var options = new ReplayOptions
        {
          StopOnFirstDrift = stopOnFirstDrift,
          StopOnFirstFailure = stopOnFirstFailure,
          ValidateModelFingerprint = validateModelFingerprint,
          RequireExactModelMatch = requireExactModelMatch
        };

        var currentFingerprint = GetCurrentModelFingerprint();

        // Wrap the sync generator to track progress
        int index = 0;
        Func<ReplayContext, AuditRecord> wrappedGenerator = ctx =>
        {
          var result = generator(ctx);

          // Invoke progress event on main thread
          var progressData = new ReplayProgressEventData
          {
            CurrentIndex = index,
            TotalRecords = _currentPackage.Records.Count,
            ProgressPercent = (float)(index + 1) / _currentPackage.Records.Count * 100f,
            DriftDetected = result.OutputHash != ctx.OriginalRecord.OutputHash,
            DriftType = result.OutputHash != ctx.OriginalRecord.OutputHash ? DriftType.Output : DriftType.None
          };

          // Note: We're in a sync context, so we queue to main thread
          UnityMainThreadDispatcher.Enqueue(() => OnReplayProgress?.Invoke(progressData));

          index++;
          return result;
        };

        var replayResult = _replayEngine.Replay(
          _currentPackage,
          wrappedGenerator,
          currentFingerprint,
          options);

        _lastReplayResult = replayResult;

        // Build completion data
        var completedData = new ReplayCompletedEventData
        {
          Result = replayResult,
          AllMatched = replayResult.AllMatched,
          ExactMatches = replayResult.ExactMatches,
          OutputDrifts = replayResult.OutputDrifts,
          Failures = replayResult.Failures,
          DurationMs = replayResult.ReplayDurationMs,
          Summary = _replayEngine.GetDriftSummary(replayResult)
        };

        OnReplayCompleted?.Invoke(completedData);

        if (verboseLogging)
        {
          Debug.Log($"[RedRoomReplayController] Replay completed:\n{completedData.Summary}");
        }

        return replayResult;
      }
      catch (OperationCanceledException)
      {
        Debug.Log("[RedRoomReplayController] Replay cancelled");
        return null;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[RedRoomReplayController] Replay failed: {ex.Message}");
        OnReplayError?.Invoke(ex.Message);
        return null;
      }
      finally
      {
        _isReplaying = false;
      }
    }

    /// <summary>
    /// Replays a single step for step-through debugging.
    /// </summary>
    /// <param name="generator">Function that generates a replay record from context.</param>
    /// <returns>The result of replaying the single record.</returns>
    public RecordReplayResult? ReplayStep(Func<ReplayContext, AuditRecord> generator)
    {
      if (_currentPackage == null || _replayEngine == null)
      {
        OnReplayError?.Invoke("No package loaded");
        return null;
      }

      if (_currentStepIndex >= _currentPackage.Records.Count)
      {
        Debug.Log("[RedRoomReplayController] No more steps to replay");
        return null;
      }

      var originalRecord = _currentPackage.Records[_currentStepIndex];

      var context = new ReplayContext
      {
        OriginalRecord = originalRecord,
        NpcId = originalRecord.NpcId,
        PlayerInput = originalRecord.PlayerInput,
        Seed = originalRecord.Seed,
        InteractionCount = originalRecord.InteractionCount
      };

      var replayedRecord = generator(context);
      var detector = new DriftDetector();
      var result = detector.Compare(originalRecord, replayedRecord);

      // Send progress event
      var progressData = new ReplayProgressEventData
      {
        CurrentIndex = _currentStepIndex,
        TotalRecords = _currentPackage.Records.Count,
        ProgressPercent = (float)(_currentStepIndex + 1) / _currentPackage.Records.Count * 100f,
        CurrentResult = result,
        DriftDetected = result.DriftType != DriftType.None,
        DriftType = result.DriftType
      };

      OnReplayProgress?.Invoke(progressData);

      _currentStepIndex++;

      if (verboseLogging)
      {
        Debug.Log($"[RedRoomReplayController] Step {_currentStepIndex}/{_currentPackage.Records.Count}: " +
          $"Drift={result.DriftType}, Match={result.IsExactMatch}");
      }

      return result;
    }

    /// <summary>
    /// Resets the step-through position to the beginning.
    /// </summary>
    public void ResetStepPosition()
    {
      _currentStepIndex = 0;
      Debug.Log("[RedRoomReplayController] Reset step position to beginning");
    }

    /// <summary>
    /// Cancels an in-progress replay.
    /// </summary>
    public void CancelReplay()
    {
      _cancellationTokenSource?.Cancel();
      Debug.Log("[RedRoomReplayController] Replay cancellation requested");
    }

    /// <summary>
    /// Clears the currently loaded package.
    /// </summary>
    public void ClearPackage()
    {
      _currentPackage = null;
      _lastReplayResult = null;
      _currentStepIndex = 0;
      Debug.Log("[RedRoomReplayController] Cleared loaded package");
    }

    /// <summary>
    /// Gets a summary of the loaded package.
    /// </summary>
    /// <returns>Package summary string.</returns>
    public string GetPackageSummary()
    {
      if (_currentPackage == null)
        return "No package loaded";

      return $"Package: {_currentPackage.PackageId}\n" +
        $"Records: {_currentPackage.TotalInteractions}\n" +
        $"NPCs: {string.Join(", ", _currentPackage.NpcIds)}\n" +
        $"Game Version: {_currentPackage.GameVersion}\n" +
        $"Scene: {_currentPackage.SceneName}\n" +
        $"Created: {new DateTime(_currentPackage.CreatedAtUtcTicks, DateTimeKind.Utc):u}\n" +
        $"Validation Failures: {_currentPackage.ValidationFailures}\n" +
        $"Fallbacks Used: {_currentPackage.FallbacksUsed}\n" +
        $"Notes: {_currentPackage.CreatorNotes}";
    }

    /// <summary>
    /// Gets a summary of the last replay result.
    /// </summary>
    /// <returns>Replay summary string.</returns>
    public string GetReplaySummary()
    {
      if (_lastReplayResult == null)
        return "No replay results available";

      return _replayEngine?.GetDriftSummary(_lastReplayResult) ?? "No summary available";
    }

#if UNITY_EDITOR
    [ContextMenu("Import Debug Package (Editor)")]
    private void EditorImportPackage()
    {
      var path = UnityEditor.EditorUtility.OpenFilePanel(
        "Open Debug Package",
        Application.persistentDataPath,
        "json"
      );

      if (!string.IsNullOrEmpty(path))
      {
        var result = ImportFromFile(path);
        if (result.Success)
        {
          Debug.Log(GetPackageSummary());
        }
      }
    }

    [ContextMenu("Replay With Mock Generator (Editor)")]
    private async void EditorReplayMock()
    {
      if (_currentPackage == null)
      {
        Debug.LogWarning("No package loaded. Import a package first.");
        return;
      }

      await ReplayWithMockGeneratorAsync();
    }

    [ContextMenu("Log Package Summary")]
    private void EditorLogPackageSummary()
    {
      Debug.Log(GetPackageSummary());
    }

    [ContextMenu("Log Replay Summary")]
    private void EditorLogReplaySummary()
    {
      Debug.Log(GetReplaySummary());
    }
#endif
  }

  /// <summary>
  /// Simple utility to dispatch actions to the Unity main thread.
  /// </summary>
  public static class UnityMainThreadDispatcher
  {
    private static readonly Queue<Action> _queue = new Queue<Action>();
    private static readonly object _lock = new object();
    private static bool _initialized;

    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// </summary>
    public static void Enqueue(Action action)
    {
      if (action == null) return;

      lock (_lock)
      {
        _queue.Enqueue(action);
      }
    }

    /// <summary>
    /// Processes queued actions. Call this from Update() on a MonoBehaviour.
    /// </summary>
    public static void ProcessQueue()
    {
      while (true)
      {
        Action? work = null;
        lock (_lock)
        {
          if (_queue.Count == 0)
            break;
          work = _queue.Dequeue();
        }

        if (work != null)
        {
          try
          {
            work();
          }
          catch (Exception ex)
          {
            Debug.LogError($"[UnityMainThreadDispatcher] Error executing queued action: {ex.Message}");
          }
        }
      }
    }

    /// <summary>
    /// Creates the dispatcher MonoBehaviour if not already initialized.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
      if (_initialized) return;

      var go = new GameObject("UnityMainThreadDispatcher");
      go.AddComponent<MainThreadDispatcherBehaviour>();
      UnityEngine.Object.DontDestroyOnLoad(go);
      _initialized = true;
    }

    private class MainThreadDispatcherBehaviour : MonoBehaviour
    {
      private void Update()
      {
        ProcessQueue();
      }
    }
  }
}
