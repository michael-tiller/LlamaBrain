#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Runtime.RedRoom.Audit
{
  /// <summary>
  /// UI component for visualizing replay progress and drift detection.
  /// Displays progress bar, current step info, and drift warnings.
  /// </summary>
  /// <remarks>
  /// This component:
  /// - Shows replay progress with a progress bar
  /// - Displays per-turn status (match, drift, failure)
  /// - Highlights drift locations for debugging
  /// - Provides step-through controls
  /// - Shows summary statistics after replay completes
  /// </remarks>
  public class ReplayProgressUI : MonoBehaviour
  {
    [Header("Progress Display")]
    [Tooltip("Slider for progress display.")]
    [SerializeField] private Slider? progressSlider;

    [Tooltip("Text showing current/total progress.")]
    [SerializeField] private TextMeshProUGUI? progressText;

    [Tooltip("Text showing percentage.")]
    [SerializeField] private TextMeshProUGUI? percentageText;

    [Header("Status Display")]
    [Tooltip("Panel showing current record details.")]
    [SerializeField] private GameObject? currentRecordPanel;

    [Tooltip("Text showing current record details.")]
    [SerializeField] private TextMeshProUGUI? currentRecordText;

    [Tooltip("Text showing drift status.")]
    [SerializeField] private TextMeshProUGUI? driftStatusText;

    [Tooltip("Image for drift indicator (changes color based on drift status).")]
    [SerializeField] private Image? driftIndicator;

    [Header("Summary Display")]
    [Tooltip("Panel showing replay summary.")]
    [SerializeField] private GameObject? summaryPanel;

    [Tooltip("Text showing replay summary.")]
    [SerializeField] private TextMeshProUGUI? summaryText;

    [Header("Controls")]
    [Tooltip("Button to start replay.")]
    [SerializeField] private Button? startButton;

    [Tooltip("Button to step forward.")]
    [SerializeField] private Button? stepButton;

    [Tooltip("Button to cancel/reset.")]
    [SerializeField] private Button? cancelButton;

    [Tooltip("Button to import a package.")]
    [SerializeField] private Button? importButton;

    [Header("Turn Timeline")]
    [Tooltip("Container for turn indicator dots.")]
    [SerializeField] private RectTransform? timelineContainer;

    [Tooltip("Prefab for turn indicator dot.")]
    [SerializeField] private GameObject? turnIndicatorPrefab;

    [Tooltip("Maximum number of turn indicators to show.")]
    [SerializeField] private int maxTimelineIndicators = 50;

    [Header("Colors")]
    [SerializeField] private Color matchColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color driftColor = new Color(0.9f, 0.6f, 0.1f);
    [SerializeField] private Color failureColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color pendingColor = new Color(0.5f, 0.5f, 0.5f);

    private RedRoomReplayController? _replayController;
    private List<Image> _turnIndicators = new List<Image>();

    private void Start()
    {
      _replayController = RedRoomReplayController.Instance;

      if (_replayController != null)
      {
        _replayController.OnReplayProgress.AddListener(OnReplayProgress);
        _replayController.OnReplayCompleted.AddListener(OnReplayCompleted);
        _replayController.OnReplayError.AddListener(OnReplayError);
      }

      SetupButtons();
      ResetUI();
    }

    private void OnDestroy()
    {
      if (_replayController != null)
      {
        _replayController.OnReplayProgress.RemoveListener(OnReplayProgress);
        _replayController.OnReplayCompleted.RemoveListener(OnReplayCompleted);
        _replayController.OnReplayError.RemoveListener(OnReplayError);
      }
    }

    private void SetupButtons()
    {
      if (startButton != null)
      {
        startButton.onClick.AddListener(OnStartClicked);
      }

      if (stepButton != null)
      {
        stepButton.onClick.AddListener(OnStepClicked);
      }

      if (cancelButton != null)
      {
        cancelButton.onClick.AddListener(OnCancelClicked);
      }

      if (importButton != null)
      {
        importButton.onClick.AddListener(OnImportClicked);
      }
    }

    /// <summary>
    /// Resets the UI to initial state.
    /// </summary>
    public void ResetUI()
    {
      if (progressSlider != null)
      {
        progressSlider.value = 0;
      }

      if (progressText != null)
      {
        progressText.text = "0 / 0";
      }

      if (percentageText != null)
      {
        percentageText.text = "0%";
      }

      if (currentRecordPanel != null)
      {
        currentRecordPanel.SetActive(false);
      }

      if (summaryPanel != null)
      {
        summaryPanel.SetActive(false);
      }

      if (driftIndicator != null)
      {
        driftIndicator.color = pendingColor;
      }

      if (driftStatusText != null)
      {
        driftStatusText.text = "Ready";
      }

      ClearTimeline();

      UpdateButtonStates();
    }

    /// <summary>
    /// Initializes the timeline for a package with the given record count.
    /// </summary>
    /// <param name="recordCount">Number of records in the package.</param>
    public void InitializeTimeline(int recordCount)
    {
      ClearTimeline();

      if (timelineContainer == null || turnIndicatorPrefab == null)
        return;

      var count = Mathf.Min(recordCount, maxTimelineIndicators);

      for (int i = 0; i < count; i++)
      {
        var indicator = Instantiate(turnIndicatorPrefab, timelineContainer);
        var image = indicator.GetComponent<Image>();
        if (image != null)
        {
          image.color = pendingColor;
          _turnIndicators.Add(image);
        }
      }
    }

    private void ClearTimeline()
    {
      foreach (var indicator in _turnIndicators)
      {
        if (indicator != null)
        {
          Destroy(indicator.gameObject);
        }
      }
      _turnIndicators.Clear();
    }

    private void OnReplayProgress(ReplayProgressEventData data)
    {
      // Update progress bar
      if (progressSlider != null)
      {
        progressSlider.value = data.ProgressPercent / 100f;
      }

      if (progressText != null)
      {
        progressText.text = $"{data.CurrentIndex + 1} / {data.TotalRecords}";
      }

      if (percentageText != null)
      {
        percentageText.text = $"{data.ProgressPercent:F0}%";
      }

      // Update current record panel
      if (currentRecordPanel != null)
      {
        currentRecordPanel.SetActive(true);
      }

      if (currentRecordText != null && data.CurrentResult != null)
      {
        var record = data.CurrentResult.OriginalRecord;
        currentRecordText.text =
          $"Turn {data.CurrentIndex + 1}\n" +
          $"NPC: {record?.NpcId ?? "Unknown"}\n" +
          $"Input: {TruncateString(record?.PlayerInput ?? "", 50)}\n" +
          $"Output: {TruncateString(record?.DialogueText ?? "", 50)}";
      }

      // Update drift indicator
      UpdateDriftDisplay(data.DriftDetected, data.DriftType, data.CurrentResult);

      // Update timeline
      UpdateTimelineIndicator(data.CurrentIndex, data.DriftDetected, data.CurrentResult != null && !data.CurrentResult.Success);
    }

    private void UpdateDriftDisplay(bool driftDetected, DriftType driftType, RecordReplayResult? result)
    {
      if (driftIndicator != null)
      {
        if (result != null && !result.Success)
        {
          driftIndicator.color = failureColor;
        }
        else if (driftDetected)
        {
          driftIndicator.color = driftColor;
        }
        else
        {
          driftIndicator.color = matchColor;
        }
      }

      if (driftStatusText != null)
      {
        if (result != null && !result.Success)
        {
          driftStatusText.text = $"FAILURE: {result.ErrorMessage}";
        }
        else if (driftDetected)
        {
          driftStatusText.text = $"DRIFT DETECTED: {driftType}";
        }
        else
        {
          driftStatusText.text = "Match";
        }
      }
    }

    private void UpdateTimelineIndicator(int index, bool driftDetected, bool failed)
    {
      if (index < 0 || index >= _turnIndicators.Count)
        return;

      var indicator = _turnIndicators[index];
      if (indicator != null)
      {
        if (failed)
        {
          indicator.color = failureColor;
        }
        else if (driftDetected)
        {
          indicator.color = driftColor;
        }
        else
        {
          indicator.color = matchColor;
        }
      }
    }

    private void OnReplayCompleted(ReplayCompletedEventData data)
    {
      // Show summary panel
      if (summaryPanel != null)
      {
        summaryPanel.SetActive(true);
      }

      if (summaryText != null)
      {
        summaryText.text =
          $"Replay Complete\n\n" +
          $"Exact Matches: {data.ExactMatches}\n" +
          $"Output Drifts: {data.OutputDrifts}\n" +
          $"Failures: {data.Failures}\n" +
          $"Duration: {data.DurationMs}ms\n\n" +
          (data.AllMatched ? "All records matched!" : "Drift detected - see timeline for details");
      }

      // Update final progress
      if (progressSlider != null)
      {
        progressSlider.value = 1f;
      }

      if (driftIndicator != null)
      {
        driftIndicator.color = data.AllMatched ? matchColor : driftColor;
      }

      if (driftStatusText != null)
      {
        driftStatusText.text = data.AllMatched ? "All Matched" : $"Drift Count: {data.OutputDrifts}";
      }

      UpdateButtonStates();
    }

    private void OnReplayError(string error)
    {
      if (driftStatusText != null)
      {
        driftStatusText.text = $"ERROR: {error}";
      }

      if (driftIndicator != null)
      {
        driftIndicator.color = failureColor;
      }

      UpdateButtonStates();
    }

    private void OnStartClicked()
    {
      if (_replayController == null || !_replayController.IsPackageLoaded)
      {
        Debug.LogWarning("[ReplayProgressUI] No package loaded");
        return;
      }

      InitializeTimeline(_replayController.CurrentPackage?.TotalInteractions ?? 0);
      _ = _replayController.ReplayWithMockGeneratorAsync();
      UpdateButtonStates();
    }

    private void OnStepClicked()
    {
      if (_replayController == null || !_replayController.IsPackageLoaded)
      {
        Debug.LogWarning("[ReplayProgressUI] No package loaded");
        return;
      }

      // Initialize timeline on first step
      if (_replayController.CurrentStepIndex == 0)
      {
        InitializeTimeline(_replayController.CurrentPackage?.TotalInteractions ?? 0);
      }

      _replayController.ReplayStep(ctx =>
      {
        // Mock generator for step-through
        return new AuditRecord
        {
          RecordId = System.Guid.NewGuid().ToString("N").Substring(0, 16),
          NpcId = ctx.NpcId,
          InteractionCount = ctx.InteractionCount,
          Seed = ctx.Seed,
          PlayerInput = ctx.PlayerInput,
          MemoryHashBefore = ctx.OriginalRecord.MemoryHashBefore,
          PromptHash = ctx.OriginalRecord.PromptHash,
          OutputHash = ctx.OriginalRecord.OutputHash,
          ValidationPassed = ctx.OriginalRecord.ValidationPassed
        };
      });

      UpdateButtonStates();
    }

    private void OnCancelClicked()
    {
      if (_replayController != null)
      {
        if (_replayController.IsReplaying)
        {
          _replayController.CancelReplay();
        }
        else
        {
          _replayController.ResetStepPosition();
          ResetUI();
        }
      }

      UpdateButtonStates();
    }

    private void OnImportClicked()
    {
#if UNITY_EDITOR
      var path = UnityEditor.EditorUtility.OpenFilePanel(
        "Open Debug Package",
        Application.persistentDataPath,
        "json"
      );

      if (!string.IsNullOrEmpty(path) && _replayController != null)
      {
        ResetUI();
        var result = _replayController.ImportFromFile(path);
        if (result.Success && _replayController.CurrentPackage != null)
        {
          InitializeTimeline(_replayController.CurrentPackage.TotalInteractions);

          if (driftStatusText != null)
          {
            driftStatusText.text = $"Loaded {_replayController.CurrentPackage.TotalInteractions} records";
          }
        }
        UpdateButtonStates();
      }
#else
      Debug.LogWarning("[ReplayProgressUI] File import only available in editor. Use ImportFromFile() API at runtime.");
#endif
    }

    private void UpdateButtonStates()
    {
      var hasPackage = _replayController?.IsPackageLoaded ?? false;
      var isReplaying = _replayController?.IsReplaying ?? false;

      if (startButton != null)
      {
        startButton.interactable = hasPackage && !isReplaying;
      }

      if (stepButton != null)
      {
        stepButton.interactable = hasPackage && !isReplaying;
      }

      if (cancelButton != null)
      {
        cancelButton.interactable = hasPackage;
      }

      if (importButton != null)
      {
        importButton.interactable = !isReplaying;
      }
    }

    private static string TruncateString(string str, int maxLength)
    {
      if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
        return str;

      return str.Substring(0, maxLength - 3) + "...";
    }

    /// <summary>
    /// Shows the first drift location in the timeline.
    /// </summary>
    public void ScrollToFirstDrift()
    {
      for (int i = 0; i < _turnIndicators.Count; i++)
      {
        if (_turnIndicators[i] != null && _turnIndicators[i].color == driftColor)
        {
          // In a more complete implementation, scroll the timeline to this position
          Debug.Log($"[ReplayProgressUI] First drift at turn {i + 1}");
          break;
        }
      }
    }

    /// <summary>
    /// Gets the turn index where drift first occurred.
    /// </summary>
    /// <returns>Index of first drift turn, or -1 if no drift.</returns>
    public int GetFirstDriftIndex()
    {
      for (int i = 0; i < _turnIndicators.Count; i++)
      {
        if (_turnIndicators[i] != null && _turnIndicators[i].color == driftColor)
        {
          return i;
        }
      }
      return -1;
    }
  }
}
