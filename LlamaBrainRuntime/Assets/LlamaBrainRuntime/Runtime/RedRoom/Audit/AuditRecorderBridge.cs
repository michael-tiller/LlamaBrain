#nullable enable
using System;
using UnityEngine;
using LlamaBrain.Core.Audit;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Core;
using LlamaBrain.Core.Inference;
using Newtonsoft.Json;

namespace LlamaBrain.Runtime.RedRoom.Audit
{
  /// <summary>
  /// Unity bridge component that integrates the core AuditRecorder with LlamaBrainAgent.
  /// Records interaction state for debug package export and bug reproduction.
  /// </summary>
  /// <remarks>
  /// This component:
  /// - Maintains an IAuditRecorder instance for the scene
  /// - Provides methods to capture audit records from LlamaBrainAgent interactions
  /// - Supports debug package export for bug reproduction
  /// - Can be configured per-scene or as a singleton
  /// </remarks>
  public class AuditRecorderBridge : MonoBehaviour
  {
    /// <summary>
    /// Singleton instance for scene-wide access.
    /// </summary>
    public static AuditRecorderBridge? Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Maximum number of records to keep per NPC (ring buffer size).")]
    [SerializeField] private int bufferCapacity = 50;

    [Tooltip("Whether to record interactions automatically when hooked to an agent.")]
    [SerializeField] private bool autoRecord = true;

    [Header("Model Fingerprint")]
    [Tooltip("Current model file path for fingerprint generation.")]
    [SerializeField] private string modelFilePath = "";

    [Tooltip("Current model file size in bytes (set automatically if BrainServer is available).")]
    [SerializeField] private long modelFileSizeBytes;

    [Tooltip("Context length used by the model.")]
    [SerializeField] private int contextLength = 4096;

    [Header("Debug")]
    [Tooltip("Enable verbose logging of audit operations.")]
    [SerializeField] private bool verboseLogging = false;

    private IAuditRecorder? _recorder;
    private ModelFingerprint? _modelFingerprint;
    private string _gameVersion = "";
    private string _currentSceneName = "";

    /// <summary>
    /// Gets the underlying audit recorder.
    /// </summary>
    public IAuditRecorder Recorder => _recorder ?? throw new InvalidOperationException("Recorder not initialized");

    /// <summary>
    /// Gets whether recording is enabled.
    /// </summary>
    public bool IsRecording => autoRecord && _recorder != null;

    /// <summary>
    /// Gets the current model fingerprint.
    /// </summary>
    public ModelFingerprint? ModelFingerprint => _modelFingerprint;

    /// <summary>
    /// Gets the total number of records across all NPCs.
    /// </summary>
    public int TotalRecordCount => _recorder?.TotalRecordCount ?? 0;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Debug.LogWarning("[AuditRecorderBridge] Duplicate instance detected, destroying this one.");
        Destroy(gameObject);
        return;
      }

      try
      {
        InitializeRecorder();
        Instance = this;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[AuditRecorderBridge] Failed to initialize recorder: {ex.Message}\n{ex}");
        Instance = null;
        Destroy(gameObject);
        return;
      }
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }

    private void Start()
    {
      // Capture scene name
      _currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

      // Capture game version
      _gameVersion = Application.version;

      // Try to auto-configure model fingerprint from BrainServer
      TryConfigureFromBrainServer();
    }

    private void InitializeRecorder()
    {
      _recorder = new AuditRecorder(bufferCapacity);

      if (verboseLogging)
      {
        Debug.Log($"[AuditRecorderBridge] Initialized with buffer capacity: {bufferCapacity}");
      }
    }

    private void TryConfigureFromBrainServer()
    {
      try
      {
        var brainServer = FindAnyObjectByType<BrainServer>();
        if (brainServer != null)
        {
          var settings = brainServer.Settings;
          if (settings != null && !string.IsNullOrEmpty(settings.ModelPath))
          {
            modelFilePath = settings.ModelPath;
            contextLength = settings.ContextSize;

            // Try to get file size
            if (System.IO.File.Exists(modelFilePath))
            {
              var fileInfo = new System.IO.FileInfo(modelFilePath);
              modelFileSizeBytes = fileInfo.Length;
            }

            UpdateModelFingerprint();

            if (verboseLogging)
            {
              Debug.Log($"[AuditRecorderBridge] Auto-configured from BrainServer: {modelFilePath}");
            }
          }
        }
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[AuditRecorderBridge] Failed to auto-configure from BrainServer: {ex.Message}");
      }
    }

    /// <summary>
    /// Updates the model fingerprint from current settings.
    /// </summary>
    public void UpdateModelFingerprint()
    {
      _modelFingerprint = new ModelFingerprint
      {
        ModelFileName = System.IO.Path.GetFileName(modelFilePath),
        ModelFileSizeBytes = modelFileSizeBytes,
        ContextLength = contextLength
      };
      _modelFingerprint.ComputeFingerprintHash();

      if (verboseLogging)
      {
        Debug.Log($"[AuditRecorderBridge] Model fingerprint updated: {_modelFingerprint.FingerprintHash}");
      }
    }

    /// <summary>
    /// Records an interaction from a LlamaBrainAgent after inference completes.
    /// </summary>
    /// <param name="agent">The agent that performed the interaction.</param>
    /// <param name="playerInput">The player's input message.</param>
    /// <param name="result">The inference result from the agent.</param>
    public void RecordInteraction(
      LlamaBrainAgent agent,
      string playerInput,
      InferenceResultWithRetries result)
    {
      if (_recorder == null || !autoRecord)
        return;

      if (agent == null)
      {
        Debug.LogWarning("[AuditRecorderBridge] Cannot record: agent is null");
        return;
      }

      try
      {
        var record = BuildAuditRecord(agent, playerInput, result);
        _recorder.Record(record);

        if (verboseLogging)
        {
          Debug.Log($"[AuditRecorderBridge] Recorded interaction for {record.NpcId}, count: {_recorder.GetRecordCount(record.NpcId)}");
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"[AuditRecorderBridge] Failed to record interaction: {ex.Message}");
      }
    }

    /// <summary>
    /// Builds an audit record from agent state and inference result.
    /// </summary>
    private AuditRecord BuildAuditRecord(
      LlamaBrainAgent agent,
      string playerInput,
      InferenceResultWithRetries result)
    {
      var finalResult = result.FinalResult;
      var snapshot = agent.LastSnapshot;
      var gateResult = agent.LastGateResult;
      var assembledPrompt = agent.LastAssembledPrompt;
      var mutationResult = agent.LastMutationBatchResult;
      var metrics = agent.LastMetrics;

      // Get NPC ID from persona config
      var npcId = agent.PersonaConfig?.PersonaId
        ?? agent.RuntimeProfile?.PersonaId
        ?? agent.gameObject.name;

      // Build state hashes
      var memoryHash = snapshot != null
        ? AuditHasher.ComputeSha256(string.Join("|", snapshot.GetAllMemoryForPrompt()))
        : "";

      var promptHash = assembledPrompt != null
        ? AuditHasher.ComputeSha256(assembledPrompt.Text)
        : "";

      var constraintsHash = snapshot?.Constraints != null
        ? AuditHasher.ComputeSha256(snapshot.Constraints.ToPromptInjection())
        : "";

      var constraintsSerialized = snapshot?.Constraints != null
        ? JsonConvert.SerializeObject(snapshot.Constraints)
        : "";

      // Determine trigger info
      int triggerReason = 0;
      string triggerId = "";

      if (snapshot?.Context != null)
      {
        triggerReason = (int)snapshot.Context.TriggerReason;
        triggerId = snapshot.Context.TriggerId ?? "";
      }

      var builder = new AuditRecordBuilder()
        .WithNpcId(npcId)
        .WithInteractionCount(agent.InteractionCount)
        .WithSeed(snapshot?.Context?.InteractionCount ?? agent.InteractionCount)
        .WithSnapshotTimeUtcTicks(snapshot?.CaptureTimeUtcTicks ?? DateTimeOffset.UtcNow.UtcTicks)
        .WithPlayerInput(playerInput)
        .WithTriggerInfo(triggerReason, triggerId, _currentSceneName)
        .WithStateHashes(memoryHash, promptHash, constraintsHash)
        .WithConstraints(constraintsSerialized)
        .WithOutput(finalResult.Response, finalResult.Response)
        .WithValidationOutcome(
          gateResult?.Passed ?? true,
          gateResult?.Failures.Count ?? 0,
          mutationResult?.TotalSucceeded ?? 0
        );

      // Add metrics if available
      if (metrics != null)
      {
        builder.WithMetrics(
          (long)metrics.TtftMs,
          (long)metrics.TotalTimeMs,
          metrics.PromptTokenCount,
          metrics.GeneratedTokenCount
        );
      }

      // Add fallback info if used
      if (!result.Success && finalResult.Response != null)
      {
        builder.WithFallback($"All {result.AttemptCount} attempts failed");
      }

      return builder.Build();
    }

    /// <summary>
    /// Exports all recorded interactions as a debug package.
    /// </summary>
    /// <param name="notes">Optional notes to include in the package (e.g., bug description).</param>
    /// <returns>The exported debug package.</returns>
    public DebugPackage ExportDebugPackage(string? notes = null)
    {
      if (_recorder == null)
        throw new InvalidOperationException("Recorder not initialized");

      var exporter = new DebugPackageExporter();
      var options = new ExportOptions
      {
        CreatorNotes = notes ?? "",
        GameVersion = _gameVersion,
        SceneName = _currentSceneName
      };

      var package = exporter.Export(_recorder, _modelFingerprint ?? new ModelFingerprint(), options);

      if (verboseLogging)
      {
        Debug.Log($"[AuditRecorderBridge] Exported debug package with {package.TotalInteractions} interactions");
      }

      return package;
    }

    /// <summary>
    /// Exports the debug package as a JSON string.
    /// </summary>
    /// <param name="notes">Optional notes to include.</param>
    /// <returns>JSON string representation of the debug package.</returns>
    public string ExportDebugPackageJson(string? notes = null)
    {
      var package = ExportDebugPackage(notes);
      var exporter = new DebugPackageExporter();
      return exporter.ToJson(package);
    }

    /// <summary>
    /// Saves the debug package to a file.
    /// </summary>
    /// <param name="filePath">Path to save the JSON file.</param>
    /// <param name="notes">Optional notes to include.</param>
    public void SaveDebugPackage(string filePath, string? notes = null)
    {
      var dir = System.IO.Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(dir))
      {
        System.IO.Directory.CreateDirectory(dir);
      }

      var json = ExportDebugPackageJson(notes);
      System.IO.File.WriteAllText(filePath, json);

      Debug.Log($"[AuditRecorderBridge] Saved debug package to: {filePath}");
    }

    /// <summary>
    /// Clears all recorded interactions for a specific NPC.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    public void ClearRecords(string npcId)
    {
      _recorder?.ClearRecords(npcId);

      if (verboseLogging)
      {
        Debug.Log($"[AuditRecorderBridge] Cleared records for: {npcId}");
      }
    }

    /// <summary>
    /// Clears all recorded interactions.
    /// </summary>
    public void ClearAllRecords()
    {
      _recorder?.ClearAll();

      if (verboseLogging)
      {
        Debug.Log("[AuditRecorderBridge] Cleared all records");
      }
    }

    /// <summary>
    /// Gets the number of records for a specific NPC.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    /// <returns>Number of recorded interactions.</returns>
    public int GetRecordCount(string npcId)
    {
      return _recorder?.GetRecordCount(npcId) ?? 0;
    }

    /// <summary>
    /// Gets all tracked NPC IDs.
    /// </summary>
    /// <returns>List of NPC identifiers with recorded interactions.</returns>
    public System.Collections.Generic.IReadOnlyList<string> GetTrackedNpcIds()
    {
      return _recorder?.TrackedNpcIds ?? Array.Empty<string>();
    }

    /// <summary>
    /// Sets a custom buffer capacity for a specific NPC.
    /// </summary>
    /// <param name="npcId">The NPC identifier.</param>
    /// <param name="capacity">The new buffer capacity.</param>
    public void SetNpcCapacity(string npcId, int capacity)
    {
      _recorder?.SetCapacity(npcId, capacity);
    }

#if UNITY_EDITOR
    [ContextMenu("Export Debug Package (Editor)")]
    private void EditorExportDebugPackage()
    {
      var path = UnityEditor.EditorUtility.SaveFilePanel(
        "Save Debug Package",
        Application.persistentDataPath,
        $"debug_package_{DateTime.Now:yyyyMMdd_HHmmss}.json",
        "json"
      );

      if (!string.IsNullOrEmpty(path))
      {
        SaveDebugPackage(path, "Exported from Unity Editor");
      }
    }

    [ContextMenu("Log Recorder Status")]
    private void EditorLogStatus()
    {
      Debug.Log($"[AuditRecorderBridge] Status:\n" +
        $"  Total Records: {TotalRecordCount}\n" +
        $"  Tracked NPCs: {string.Join(", ", GetTrackedNpcIds())}\n" +
        $"  Buffer Capacity: {bufferCapacity}\n" +
        $"  Model Fingerprint: {_modelFingerprint?.FingerprintHash ?? "Not set"}");
    }
#endif
  }
}
