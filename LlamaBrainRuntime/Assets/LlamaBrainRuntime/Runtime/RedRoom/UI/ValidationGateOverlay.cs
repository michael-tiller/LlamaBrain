using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;
using LlamaBrain.Runtime.Core;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  /// <summary>
  /// Debug overlay that displays validation gate results and active constraints.
  /// Toggle with F3 key (configured in RedRoomCanvas).
  /// Combines Validation Gate Overlay and Constraint Demonstration Overlay.
  /// </summary>
  public class ValidationGateOverlay : MonoBehaviour
  {
    [Header("Panel References")]
    [SerializeField] private GameObject _overlayPanel;

    [Header("Layout Containers")]
    [SerializeField] private RectTransform _headerContainer;
    [SerializeField] private RectTransform _contentContainer;
    [SerializeField] private RectTransform _constraintsContainer; // Left side
    [SerializeField] private RectTransform _validationContainer;  // Right side

    [Header("Constraint Section")]
    [SerializeField] private ScrollRect _constraintsScrollRect;
    [SerializeField] private TextMeshProUGUI _constraintsText;

    [Header("Validation Section")]
    [SerializeField] private TextMeshProUGUI _gateStatusText;
    [SerializeField] private ScrollRect _failuresScrollRect;
    [SerializeField] private TextMeshProUGUI _failuresText;

    [Header("Retry Section")]
    [SerializeField] private TextMeshProUGUI _retryStatusText;
    [SerializeField] private ScrollRect _retryHistoryScrollRect;
    [SerializeField] private TextMeshProUGUI _retryHistoryText;

    [Header("Stats Section")]
    [SerializeField] private TextMeshProUGUI _statsText;

    [Header("Settings")]
    [SerializeField] private float _refreshInterval = 0.3f;
    [SerializeField] private int _maxRetryHistoryDisplay = 5;

    [Header("Colors")]
    [SerializeField] private Color _passColor = new Color(0.3f, 1f, 0.3f);        // Green
    [SerializeField] private Color _failColor = new Color(1f, 0.3f, 0.3f);        // Red
    [SerializeField] private Color _prohibitionColor = new Color(1f, 0.5f, 0.5f); // Light red
    [SerializeField] private Color _requirementColor = new Color(0.5f, 0.7f, 1f); // Light blue
    [SerializeField] private Color _permissionColor = new Color(0.5f, 1f, 0.5f);  // Light green
    [SerializeField] private Color _criticalColor = new Color(1f, 0.2f, 0.2f);    // Bright red
    [SerializeField] private Color _hardColor = new Color(1f, 0.6f, 0.2f);        // Orange
    [SerializeField] private Color _softColor = new Color(1f, 1f, 0.5f);          // Yellow
    [SerializeField] private Color _headerColor = new Color(0.8f, 0.8f, 0.8f);    // Light gray
    [SerializeField] private Color _dimColor = new Color(0.5f, 0.5f, 0.5f);       // Dim gray

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ValidationGateOverlay Instance { get; private set; }

    /// <summary>
    /// Whether the overlay is currently visible.
    /// </summary>
    public bool IsVisible => _overlayPanel != null && _overlayPanel.activeSelf;

    private LlamaBrainAgent _trackedAgent;
    private float _lastRefreshTime;
    private StringBuilder _sb = new StringBuilder(4096);

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;
    }

    private void Start()
    {
      // Start hidden
      if (_overlayPanel != null)
      {
        _overlayPanel.SetActive(false);
      }

      FindAgentToTrack();
    }

    private void Update()
    {
      if (!IsVisible) return;

      // Refresh periodically
      if (Time.time - _lastRefreshTime >= _refreshInterval)
      {
        RefreshDisplay();
        _lastRefreshTime = Time.time;
      }
    }

    /// <summary>
    /// Toggles the overlay visibility.
    /// </summary>
    public void Toggle()
    {
      if (_overlayPanel == null) return;

      var newState = !_overlayPanel.activeSelf;
      _overlayPanel.SetActive(newState);

      if (newState)
      {
        FindAgentToTrack();
        RefreshDisplay();
      }
    }

    /// <summary>
    /// Shows the overlay.
    /// </summary>
    public void Show()
    {
      if (_overlayPanel == null) return;
      _overlayPanel.SetActive(true);
      FindAgentToTrack();
      RefreshDisplay();
    }

    /// <summary>
    /// Hides the overlay.
    /// </summary>
    public void Hide()
    {
      if (_overlayPanel == null) return;
      _overlayPanel.SetActive(false);
    }

    /// <summary>
    /// Sets the agent to track.
    /// </summary>
    /// <param name="agent">The LlamaBrainAgent to track for validation state.</param>
    public void SetTrackedAgent(LlamaBrainAgent agent)
    {
      _trackedAgent = agent;
    }

    private void FindAgentToTrack()
    {
      if (_trackedAgent != null && _trackedAgent.IsInitialized) return;

      // Find first initialized agent in scene
      var agents = FindObjectsByType<LlamaBrainAgent>(FindObjectsSortMode.None);
      foreach (var agent in agents)
      {
        if (agent.IsInitialized)
        {
          SetTrackedAgent(agent);
          return;
        }
      }
    }

    private void RefreshDisplay()
    {
      RefreshConstraints();
      RefreshGateStatus();
      RefreshFailures();
      RefreshRetryStatus();
      RefreshStats();
    }

    private void RefreshConstraints()
    {
      if (_constraintsText == null) return;

      _sb.Clear();
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      if (_trackedAgent == null || !_trackedAgent.IsInitialized)
      {
        _sb.AppendLine($"<color=#{headerHex}><b>=== ACTIVE CONSTRAINTS ===</b></color>");
        _sb.AppendLine("<color=#888888>No agent tracked.</color>");
        _constraintsText.text = _sb.ToString();
        return;
      }

      var constraints = _trackedAgent.LastConstraints;
      if (constraints == null || !constraints.HasConstraints)
      {
        _sb.AppendLine($"<color=#{headerHex}><b>=== ACTIVE CONSTRAINTS ===</b></color>");
        _sb.AppendLine("<color=#666666>  No active constraints.</color>");
        _constraintsText.text = _sb.ToString();
        return;
      }

      _sb.AppendLine($"<color=#{headerHex}><b>=== ACTIVE CONSTRAINTS ({constraints.Count}) ===</b></color>");
      _sb.AppendLine();

      // Prohibitions
      var prohibitions = constraints.Prohibitions.ToList();
      if (prohibitions.Count > 0)
      {
        var prohibHex = ColorUtility.ToHtmlStringRGB(_prohibitionColor);
        _sb.AppendLine($"<color=#{prohibHex}><b>PROHIBITIONS ({prohibitions.Count})</b></color>");
        foreach (var c in prohibitions)
        {
          AppendConstraint(c, _prohibitionColor);
        }
        _sb.AppendLine();
      }

      // Requirements
      var requirements = constraints.Requirements.ToList();
      if (requirements.Count > 0)
      {
        var reqHex = ColorUtility.ToHtmlStringRGB(_requirementColor);
        _sb.AppendLine($"<color=#{reqHex}><b>REQUIREMENTS ({requirements.Count})</b></color>");
        foreach (var c in requirements)
        {
          AppendConstraint(c, _requirementColor);
        }
        _sb.AppendLine();
      }

      // Permissions
      var permissions = constraints.Permissions.ToList();
      if (permissions.Count > 0)
      {
        var permHex = ColorUtility.ToHtmlStringRGB(_permissionColor);
        _sb.AppendLine($"<color=#{permHex}><b>PERMISSIONS ({permissions.Count})</b></color>");
        foreach (var c in permissions)
        {
          AppendConstraint(c, _permissionColor);
        }
      }

      _constraintsText.text = _sb.ToString();
    }

    private void AppendConstraint(Constraint constraint, Color baseColor)
    {
      var colorHex = ColorUtility.ToHtmlStringRGB(baseColor);
      var severityColor = GetSeverityColor(constraint.Severity);
      var severityHex = ColorUtility.ToHtmlStringRGB(severityColor);
      var dimHex = ColorUtility.ToHtmlStringRGB(_dimColor);

      var severityLabel = constraint.Severity.ToString().ToUpper();
      var description = constraint.Description ?? constraint.PromptInjection ?? "(no description)";
      var source = !string.IsNullOrEmpty(constraint.SourceRule) ? $" [from: {constraint.SourceRule}]" : "";

      _sb.AppendLine($"  <color=#{severityHex}>[{severityLabel}]</color> <color=#{colorHex}>{description}</color><color=#{dimHex}>{source}</color>");
    }

    private Color GetSeverityColor(ConstraintSeverity severity)
    {
      return severity switch
      {
        ConstraintSeverity.Critical => _criticalColor,
        ConstraintSeverity.Hard => _hardColor,
        ConstraintSeverity.Soft => _softColor,
        _ => _hardColor
      };
    }

    private void RefreshGateStatus()
    {
      if (_gateStatusText == null) return;

      _sb.Clear();
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      if (_trackedAgent == null || !_trackedAgent.IsInitialized)
      {
        _sb.AppendLine($"<color=#{headerHex}><b>=== VALIDATION GATE ===</b></color>");
        _sb.AppendLine("<color=#888888>No agent tracked.</color>");
        _gateStatusText.text = _sb.ToString();
        return;
      }

      var gateResult = _trackedAgent?.LastGateResult;
      var inferenceResult = _trackedAgent?.LastInferenceResult;

      _sb.AppendLine($"<color=#{headerHex}><b>=== VALIDATION GATE ===</b></color>");

      if (gateResult == null && inferenceResult == null)
      {
        _sb.AppendLine("<color=#666666>No validation results yet.</color>");
        _gateStatusText.text = _sb.ToString();
        return;
      }

      // Gate pass/fail status
      bool passed = gateResult?.Passed ?? (inferenceResult?.Success ?? false);
      var statusColor = passed ? _passColor : _failColor;
      var statusHex = ColorUtility.ToHtmlStringRGB(statusColor);
      var statusText = passed ? "PASSED" : "FAILED";

      _sb.AppendLine($"<color=#{statusHex}><b>Status: {statusText}</b></color>");

      if (gateResult != null)
      {
        var failureCount = gateResult.Failures?.Count ?? 0;
        var hasCritical = gateResult.HasCriticalFailure;

        if (failureCount > 0)
        {
          var criticalNote = hasCritical ? " <color=#FF3333>(CRITICAL - will use fallback)</color>" : "";
          _sb.AppendLine($"Failures: {failureCount}{criticalNote}");
        }

        // Show approved mutations if any
        var mutations = gateResult.ApprovedMutations?.Count ?? 0;
        if (mutations > 0)
        {
          _sb.AppendLine($"<color=#66FF66>Approved Mutations: {mutations}</color>");
        }
      }

      _gateStatusText.text = _sb.ToString();
    }

    private void RefreshFailures()
    {
      if (_failuresText == null) return;

      _sb.Clear();
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      if (_trackedAgent == null || !_trackedAgent.IsInitialized)
      {
        _sb.AppendLine($"<color=#{headerHex}><b>=== VALIDATION FAILURES ===</b></color>");
        _sb.AppendLine("<color=#666666>No agent tracked.</color>");
        _failuresText.text = _sb.ToString();
        return;
      }

      var gateResult = _trackedAgent?.LastGateResult;
      var failures = gateResult?.Failures ?? new List<ValidationFailure>();

      _sb.AppendLine($"<color=#{headerHex}><b>=== VALIDATION FAILURES ({failures.Count}) ===</b></color>");

      if (failures.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  No failures.</color>");
        _failuresText.text = _sb.ToString();
        return;
      }

      // Group by severity
      var critical = failures.Where(f => f.Severity == ConstraintSeverity.Critical).ToList();
      var hard = failures.Where(f => f.Severity == ConstraintSeverity.Hard).ToList();
      var soft = failures.Where(f => f.Severity == ConstraintSeverity.Soft).ToList();

      if (critical.Count > 0)
      {
        var critHex = ColorUtility.ToHtmlStringRGB(_criticalColor);
        _sb.AppendLine($"<color=#{critHex}><b>CRITICAL ({critical.Count})</b></color>");
        foreach (var f in critical)
        {
          AppendFailure(f);
        }
      }

      if (hard.Count > 0)
      {
        var hardHex = ColorUtility.ToHtmlStringRGB(_hardColor);
        _sb.AppendLine($"<color=#{hardHex}><b>HARD ({hard.Count})</b></color>");
        foreach (var f in hard)
        {
          AppendFailure(f);
        }
      }

      if (soft.Count > 0)
      {
        var softHex = ColorUtility.ToHtmlStringRGB(_softColor);
        _sb.AppendLine($"<color=#{softHex}><b>SOFT ({soft.Count})</b></color>");
        foreach (var f in soft)
        {
          AppendFailure(f);
        }
      }

      _failuresText.text = _sb.ToString();
    }

    private void AppendFailure(ValidationFailure failure)
    {
      var reasonColor = GetSeverityColor(failure.Severity);
      var reasonHex = ColorUtility.ToHtmlStringRGB(reasonColor);
      var dimHex = ColorUtility.ToHtmlStringRGB(_dimColor);

      _sb.AppendLine($"  <color=#{reasonHex}>[{failure.Reason}]</color> {failure.Description}");

      if (!string.IsNullOrEmpty(failure.ViolatingText))
      {
        // Truncate long violating text
        var text = failure.ViolatingText.Length > 50
          ? failure.ViolatingText.Substring(0, 47) + "..."
          : failure.ViolatingText;
        _sb.AppendLine($"    <color=#{dimHex}>Text: \"{text}\"</color>");
      }

      if (!string.IsNullOrEmpty(failure.ViolatedRule))
      {
        _sb.AppendLine($"    <color=#{dimHex}>Rule: {failure.ViolatedRule}</color>");
      }
    }

    private void RefreshRetryStatus()
    {
      if (_retryStatusText == null && _retryHistoryText == null) return;

      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      if (_trackedAgent == null || !_trackedAgent.IsInitialized)
      {
        if (_retryStatusText != null)
        {
          _retryStatusText.text = $"<color=#{headerHex}><b>=== RETRY STATUS ===</b></color>\n<color=#666666>No agent tracked.</color>";
        }
        if (_retryHistoryText != null)
        {
          _retryHistoryText.text = $"<color=#{headerHex}><b>=== RETRY HISTORY ===</b></color>\n<color=#666666>No agent tracked.</color>";
        }
        return;
      }

      var inferenceResult = _trackedAgent?.LastInferenceResult;
      var snapshot = _trackedAgent?.LastSnapshot;

      // Retry status text
      if (_retryStatusText != null)
      {
        _sb.Clear();
        _sb.AppendLine($"<color=#{headerHex}><b>=== RETRY STATUS ===</b></color>");

        if (inferenceResult == null)
        {
          _sb.AppendLine("<color=#666666>No inference yet.</color>");
        }
        else if (inferenceResult != null)
        {
          var attemptCount = inferenceResult.AttemptCount;
          var maxAttempts = snapshot?.MaxAttempts ?? 3;
          var currentAttempt = snapshot?.AttemptNumber ?? 0;

          // Progress bar
          var progressBar = GetProgressBar(currentAttempt + 1, maxAttempts);
          _sb.AppendLine($"Attempts: {progressBar} ({attemptCount}/{maxAttempts})");

          // Escalation status
          var escalation = GetEscalationStatus(currentAttempt);
          _sb.AppendLine($"Escalation: {escalation}");

          // Timing
          _sb.AppendLine($"Total Time: {inferenceResult.TotalElapsedMilliseconds}ms");

          // Final status
          var statusColor = inferenceResult.Success ? _passColor : _failColor;
          var statusHex = ColorUtility.ToHtmlStringRGB(statusColor);
          var finalStatus = inferenceResult.Success ? "SUCCESS" : "EXHAUSTED";
          _sb.AppendLine($"<color=#{statusHex}>Final: {finalStatus}</color>");
        }
        else
        {
          _sb.AppendLine("<color=#666666>No inference data available.</color>");
        }

        _retryStatusText.text = _sb.ToString();
      }

      // Retry history text
      if (_retryHistoryText != null && inferenceResult != null)
      {
        _sb.Clear();
        _sb.AppendLine($"<color=#{headerHex}><b>=== RETRY HISTORY ===</b></color>");

        var attempts = inferenceResult.AllAttempts;
        if (attempts == null || attempts.Count == 0)
        {
          _sb.AppendLine("<color=#666666>No attempts recorded.</color>");
        }
        else
        {
          var displayCount = Math.Min(attempts.Count, _maxRetryHistoryDisplay);
          for (int i = 0; i < displayCount; i++)
          {
            var attempt = attempts[i];
            AppendAttempt(i + 1, attempt);
          }

          if (attempts.Count > _maxRetryHistoryDisplay)
          {
            var dimHex = ColorUtility.ToHtmlStringRGB(_dimColor);
            _sb.AppendLine($"<color=#{dimHex}>  ... and {attempts.Count - _maxRetryHistoryDisplay} more</color>");
          }
        }

        _retryHistoryText.text = _sb.ToString();
      }
    }

    private void AppendAttempt(int attemptNum, InferenceResult attempt)
    {
      var statusColor = attempt.Success ? _passColor : _failColor;
      var statusHex = ColorUtility.ToHtmlStringRGB(statusColor);
      var dimHex = ColorUtility.ToHtmlStringRGB(_dimColor);

      var status = attempt.Success ? "OK" : attempt.Outcome.ToString();
      var time = attempt.ElapsedMilliseconds;
      var violations = attempt.Violations?.Count ?? 0;

      _sb.AppendLine($"  <color=#{statusHex}>#{attemptNum}</color> [{status}] {time}ms");

      if (violations > 0)
      {
        _sb.AppendLine($"    <color=#{dimHex}>Violations: {violations}</color>");
      }

      // Show truncated response
      if (!string.IsNullOrEmpty(attempt.Response))
      {
        var response = attempt.Response.Length > 40
          ? attempt.Response.Substring(0, 37) + "..."
          : attempt.Response;
        _sb.AppendLine($"    <color=#{dimHex}>\"{response}\"</color>");
      }
    }

    private string GetProgressBar(int current, int max)
    {
      var filled = Math.Min(current, max);
      var empty = max - filled;
      return "[" + new string('=', filled) + new string('-', empty) + "]";
    }

    private string GetEscalationStatus(int attemptNumber)
    {
      // Based on RetryPolicy escalation logic
      return attemptNumber switch
      {
        0 => "<color=#66FF66>None</color>",
        1 => "<color=#FFFF66>Add Prohibition</color>",
        2 => "<color=#FF9933>Harden</color>",
        _ => "<color=#FF3333>Full</color>"
      };
    }

    private void RefreshStats()
    {
      if (_statsText == null) return;

      _sb.Clear();

      if (_trackedAgent == null || !_trackedAgent.IsInitialized)
      {
        _statsText.text = "Agent: None";
        return;
      }

      var agentName = _trackedAgent.RuntimeProfile?.Name ?? _trackedAgent.gameObject.name;
      _sb.AppendLine($"<b>Agent:</b> {agentName}");

      // Constraint counts
      var constraints = _trackedAgent.LastConstraints;
      if (constraints != null && constraints.HasConstraints)
      {
        _sb.AppendLine($"<b>Constraints:</b> {constraints.Prohibitions.Count()} P / {constraints.Requirements.Count()} R / {constraints.Permissions.Count()} A");
      }

      // Last inference timing
      var lastResult = _trackedAgent.LastInferenceResult;
      if (lastResult != null)
      {
        var status = lastResult.Success ? "<color=#66FF66>Pass</color>" : "<color=#FF6666>Fail</color>";
        _sb.AppendLine($"<b>Last:</b> {status} in {lastResult.TotalElapsedMilliseconds}ms ({lastResult.AttemptCount} attempts)");
      }

      // Context info
      var snapshot = _trackedAgent.LastSnapshot;
      if (snapshot != null)
      {
        var trigger = snapshot.Context?.TriggerReason.ToString() ?? "Unknown";
        _sb.AppendLine($"<b>Trigger:</b> {trigger}");
      }

      _statsText.text = _sb.ToString();
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }
  }
}
