using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Runtime.Core;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  /// <summary>
  /// Debug overlay that displays real-time memory state and mutation tracking.
  /// Toggle with F2 key (configured in RedRoomCanvas).
  /// </summary>
  public class MemoryMutationOverlay : MonoBehaviour
  {
    [Header("Panel References")]
    [SerializeField] private GameObject _overlayPanel;
    [SerializeField] private TextMeshProUGUI _memoryText; // Legacy: single combined text field
    
    [Header("Layout Containers (for 60/40 split)")]
    [SerializeField] private RectTransform _headerContainer; // Top bar container
    [SerializeField] private RectTransform _contentContainer; // Container for memory + mutation log side-by-side
    [SerializeField] private RectTransform _memoryContainer; // Left side (60%)
    [SerializeField] private RectTransform _mutationLogContainer; // Right side (40%)
    
    [Header("Memory Section Scroll Areas")]
    [SerializeField] private ScrollRect _canonicalFactsScrollRect;
    [SerializeField] private TextMeshProUGUI _canonicalFactsText;
    [SerializeField] private ScrollRect _worldStateScrollRect;
    [SerializeField] private TextMeshProUGUI _worldStateText;
    [SerializeField] private ScrollRect _episodicMemoriesScrollRect;
    [SerializeField] private TextMeshProUGUI _episodicMemoriesText;
    [SerializeField] private ScrollRect _beliefsScrollRect;
    [SerializeField] private TextMeshProUGUI _beliefsText;
    
    [Header("Other Panels")]
    [SerializeField] private TextMeshProUGUI _mutationLogText;
    [SerializeField] private TextMeshProUGUI _statsText;

    [Header("Section Toggles")]
    [SerializeField] private Toggle _showCanonicalFacts;
    [SerializeField] private Toggle _showWorldState;
    [SerializeField] private Toggle _showEpisodicMemories;
    [SerializeField] private Toggle _showBeliefs;
    [SerializeField] private Toggle _showMutationLog;

    [Header("Settings")]
    [SerializeField] private int _maxMutationLogEntries = 50;
    [SerializeField] private int _maxEpisodicDisplay = 10;
    [SerializeField] private float _refreshInterval = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color _canonicalColor = new Color(0.4f, 0.8f, 1f);    // Light blue (protected)
    [SerializeField] private Color _worldStateColor = new Color(0.4f, 1f, 0.6f);   // Light green
    [SerializeField] private Color _episodicColor = new Color(1f, 0.9f, 0.5f);     // Light yellow
    [SerializeField] private Color _beliefColor = new Color(1f, 0.6f, 0.8f);       // Light pink
    [SerializeField] private Color _approvedColor = new Color(0.3f, 1f, 0.3f);     // Green
    [SerializeField] private Color _rejectedColor = new Color(1f, 0.3f, 0.3f);     // Red
    [SerializeField] private Color _headerColor = new Color(0.8f, 0.8f, 0.8f);     // Light gray

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static MemoryMutationOverlay Instance { get; private set; }

    /// <summary>
    /// Whether the overlay is currently visible.
    /// </summary>
    public bool IsVisible => _overlayPanel != null && _overlayPanel.activeSelf;

    private LlamaBrainAgent _trackedAgent;
    private PersonaMemoryStore _memoryStore;
    private AuthoritativeMemorySystem _memorySystem;
    private List<MutationLogEntry> _mutationLog = new List<MutationLogEntry>();
    private float _lastRefreshTime;
    private StringBuilder _sb = new StringBuilder(4096);

    /// <summary>
    /// Represents a logged mutation event.
    /// </summary>
    private struct MutationLogEntry
    {
      public DateTime Timestamp;
      public string MutationType;
      public string Target;
      public bool Approved;
      public string Details;
      public string FailureReason;
    }

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;

      // Initialize toggles if not set
      SetupDefaultToggles();
    }

    private void Start()
    {
      // Start hidden
      if (_overlayPanel != null)
      {
        _overlayPanel.SetActive(false);
      }

      // Try to find an agent to track
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
    /// <param name="agent">The LlamaBrainAgent instance to track for memory mutations and state display.</param>
    public void SetTrackedAgent(LlamaBrainAgent agent)
    {
      _trackedAgent = agent;
      UpdateMemorySystemReference();

      if (agent != null)
      {
        // Hook into mutation events if available
        HookMutationEvents();
      }
    }

    /// <summary>
    /// Logs a mutation event to the overlay.
    /// </summary>
    /// <param name="mutationType">The type of mutation that occurred (e.g., "EpisodicMemory", "Belief", "WorldState").</param>
    /// <param name="target">The target of the mutation (e.g., memory ID, belief subject).</param>
    /// <param name="approved">Whether the mutation was approved or rejected.</param>
    /// <param name="details">Additional details about the mutation.</param>
    /// <param name="failureReason">Optional reason for failure if the mutation was rejected.</param>
    public void LogMutation(string mutationType, string target, bool approved, string details, string failureReason = null)
    {
      var entry = new MutationLogEntry
      {
        Timestamp = DateTime.Now,
        MutationType = mutationType,
        Target = target,
        Approved = approved,
        Details = details,
        FailureReason = failureReason
      };

      _mutationLog.Insert(0, entry); // Most recent first

      // Trim log if too long
      while (_mutationLog.Count > _maxMutationLogEntries)
      {
        _mutationLog.RemoveAt(_mutationLog.Count - 1);
      }

      // Refresh mutation log display immediately
      if (IsVisible)
      {
        RefreshMutationLog();
      }
    }

    private void FindAgentToTrack()
    {
      if (_trackedAgent != null && _trackedAgent.IsInitialized) return;

      // Find first initialized agent in scene
      var agents = FindObjectsOfType<LlamaBrainAgent>();
      foreach (var agent in agents)
      {
        if (agent.IsInitialized)
        {
          SetTrackedAgent(agent);
          return;
        }
      }
    }

    private void UpdateMemorySystemReference()
    {
      if (_trackedAgent == null || !_trackedAgent.IsInitialized)
      {
        _memorySystem = null;
        return;
      }

      // Get memory store through the agent's internal state
      // Use reflection to access the private memoryProvider field
      var profile = _trackedAgent.RuntimeProfile;
      if (profile == null)
      {
        _memorySystem = null;
        return;
      }

      // Access memoryProvider through reflection since it's private
      var agentType = typeof(LlamaBrainAgent);
      var memoryProviderField = agentType.GetField("memoryProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (memoryProviderField != null)
      {
        _memoryStore = memoryProviderField.GetValue(_trackedAgent) as PersonaMemoryStore;
        if (_memoryStore != null && profile != null)
        {
          _memorySystem = _memoryStore.GetOrCreateSystem(profile.PersonaId);
        }
        else
        {
          _memorySystem = null;
        }
      }
      else
      {
        _memorySystem = null;
      }
    }

    private void HookMutationEvents()
    {
      // Hook into the agent's mutation results
      // This will be called when mutations occur
      // For now, we poll LastMutationBatchResult
    }

    private void RefreshDisplay()
    {
      UpdateMemorySystemReference();
      RefreshMemoryDisplay();
      RefreshMutationLog();
      RefreshStats();
    }

    private void RefreshMemoryDisplay()
    {
      if (_memorySystem == null)
      {
        var noDataText = "<color=#888888>No memory system available.\nWaiting for agent initialization...</color>";
        
        // Update individual sections if available, otherwise fall back to legacy single text
        if (HasIndividualScrollAreas())
        {
          if (_canonicalFactsText != null) _canonicalFactsText.text = noDataText;
          if (_worldStateText != null) _worldStateText.text = noDataText;
          if (_episodicMemoriesText != null) _episodicMemoriesText.text = noDataText;
          if (_beliefsText != null) _beliefsText.text = noDataText;
        }
        else if (_memoryText != null)
        {
          _memoryText.text = noDataText;
        }
        return;
      }

      // Use individual scroll areas if available, otherwise use legacy single text field
      if (HasIndividualScrollAreas())
      {
        // Refresh each section individually
        if (_showCanonicalFacts == null || _showCanonicalFacts.isOn)
        {
          if (_canonicalFactsText != null)
          {
            RefreshCanonicalFacts();
          }
          if (_canonicalFactsScrollRect != null)
          {
            _canonicalFactsScrollRect.gameObject.SetActive(true);
          }
        }
        else
        {
          if (_canonicalFactsText != null) _canonicalFactsText.text = "";
          if (_canonicalFactsScrollRect != null)
          {
            _canonicalFactsScrollRect.gameObject.SetActive(false);
          }
        }

        if (_showWorldState == null || _showWorldState.isOn)
        {
          if (_worldStateText != null)
          {
            RefreshWorldState();
          }
          if (_worldStateScrollRect != null)
          {
            _worldStateScrollRect.gameObject.SetActive(true);
          }
        }
        else
        {
          if (_worldStateText != null) _worldStateText.text = "";
          if (_worldStateScrollRect != null)
          {
            _worldStateScrollRect.gameObject.SetActive(false);
          }
        }

        if (_showEpisodicMemories == null || _showEpisodicMemories.isOn)
        {
          if (_episodicMemoriesText != null)
          {
            RefreshEpisodicMemories();
          }
          if (_episodicMemoriesScrollRect != null)
          {
            _episodicMemoriesScrollRect.gameObject.SetActive(true);
          }
        }
        else
        {
          if (_episodicMemoriesText != null) _episodicMemoriesText.text = "";
          if (_episodicMemoriesScrollRect != null)
          {
            _episodicMemoriesScrollRect.gameObject.SetActive(false);
          }
        }

        if (_showBeliefs == null || _showBeliefs.isOn)
        {
          if (_beliefsText != null)
          {
            RefreshBeliefs();
          }
          if (_beliefsScrollRect != null)
          {
            _beliefsScrollRect.gameObject.SetActive(true);
          }
        }
        else
        {
          if (_beliefsText != null) _beliefsText.text = "";
          if (_beliefsScrollRect != null)
          {
            _beliefsScrollRect.gameObject.SetActive(false);
          }
        }
      }
      else
      {
        // Legacy mode: use single text field
        if (_memoryText == null) return;
        
        _sb.Clear();

        // Canonical Facts
        if (_showCanonicalFacts == null || _showCanonicalFacts.isOn)
        {
          AppendCanonicalFacts();
        }

        // World State
        if (_showWorldState == null || _showWorldState.isOn)
        {
          AppendWorldState();
        }

        // Episodic Memories
        if (_showEpisodicMemories == null || _showEpisodicMemories.isOn)
        {
          AppendEpisodicMemories();
        }

        // Beliefs
        if (_showBeliefs == null || _showBeliefs.isOn)
        {
          AppendBeliefs();
        }

        _memoryText.text = _sb.ToString();
      }
    }

    /// <summary>
    /// Checks if individual scroll areas are configured.
    /// </summary>
    private bool HasIndividualScrollAreas()
    {
      return _canonicalFactsText != null || _worldStateText != null || 
             _episodicMemoriesText != null || _beliefsText != null;
    }

    private void RefreshCanonicalFacts()
    {
      if (_canonicalFactsText == null) return;
      
      var facts = _memorySystem.GetCanonicalFacts().ToList();
      var colorHex = ColorUtility.ToHtmlStringRGB(_canonicalColor);
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      _sb.Clear();
      _sb.AppendLine($"<color=#{headerHex}><b>=== CANONICAL FACTS ({facts.Count}) ===</b></color> <color=#888888>[PROTECTED]</color>");

      if (facts.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  (none)</color>");
      }
      else
      {
        foreach (var fact in facts)
        {
          var domain = string.IsNullOrEmpty(fact.Domain) ? "" : $"[{fact.Domain}] ";
          _sb.AppendLine($"<color=#{colorHex}>  {domain}{fact.Fact}</color>");
        }
      }

      _canonicalFactsText.text = _sb.ToString();
      
      // Scroll to top when content changes
      if (_canonicalFactsScrollRect != null)
      {
        _canonicalFactsScrollRect.verticalNormalizedPosition = 1f;
      }
    }

    private void AppendCanonicalFacts()
    {
      var facts = _memorySystem.GetCanonicalFacts().ToList();
      var colorHex = ColorUtility.ToHtmlStringRGB(_canonicalColor);
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      _sb.AppendLine($"<color=#{headerHex}><b>=== CANONICAL FACTS ({facts.Count}) ===</b></color> <color=#888888>[PROTECTED]</color>");

      if (facts.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  (none)</color>");
      }
      else
      {
        foreach (var fact in facts)
        {
          var domain = string.IsNullOrEmpty(fact.Domain) ? "" : $"[{fact.Domain}] ";
          _sb.AppendLine($"<color=#{colorHex}>  {domain}{fact.Fact}</color>");
        }
      }
      _sb.AppendLine();
    }


    private void RefreshWorldState()
    {
      if (_worldStateText == null) return;
      
      var states = _memorySystem.GetAllWorldState().ToList();
      var colorHex = ColorUtility.ToHtmlStringRGB(_worldStateColor);
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      _sb.Clear();
      _sb.AppendLine($"<color=#{headerHex}><b>=== WORLD STATE ({states.Count}) ===</b></color>");

      if (states.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  (none)</color>");
      }
      else
      {
        foreach (var state in states)
        {
          _sb.AppendLine($"<color=#{colorHex}>  {state.Key} = {state.Value}</color>");
        }
      }

      _worldStateText.text = _sb.ToString();
      
      // Scroll to top when content changes
      if (_worldStateScrollRect != null)
      {
        _worldStateScrollRect.verticalNormalizedPosition = 1f;
      }
    }

    private void AppendWorldState()
    {
      var states = _memorySystem.GetAllWorldState().ToList();
      var colorHex = ColorUtility.ToHtmlStringRGB(_worldStateColor);
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      _sb.AppendLine($"<color=#{headerHex}><b>=== WORLD STATE ({states.Count}) ===</b></color>");

      if (states.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  (none)</color>");
      }
      else
      {
        foreach (var state in states)
        {
          _sb.AppendLine($"<color=#{colorHex}>  {state.Key} = {state.Value}</color>");
        }
      }
      _sb.AppendLine();
    }


    private void RefreshEpisodicMemories()
    {
      if (_episodicMemoriesText == null) return;
      
      var memories = _memorySystem.GetRecentMemories(_maxEpisodicDisplay).ToList();
      var allMemories = _memorySystem.GetActiveEpisodicMemories().ToList();
      var totalCount = allMemories.Count;
      var colorHex = ColorUtility.ToHtmlStringRGB(_episodicColor);
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      _sb.Clear();
      var displayInfo = memories.Count < totalCount
        ? $"{memories.Count}/{totalCount}, showing most recent"
        : $"{memories.Count}";

      _sb.AppendLine($"<color=#{headerHex}><b>=== EPISODIC MEMORIES ({displayInfo}) ===</b></color>");

      if (memories.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  (none)</color>");
      }
      else
      {
        foreach (var memory in memories)
        {
          var significance = memory.Significance.ToString("F2");
          var source = $"[{memory.Source}] ";
          var decayIndicator = memory.Significance < 0.3f ? " <color=#FF6666>(fading)</color>" : "";
          _sb.AppendLine($"<color=#{colorHex}>  {source}{memory.Content} <color=#888888>(sig:{significance}){decayIndicator}</color></color>");
        }
      }

      _episodicMemoriesText.text = _sb.ToString();
      
      // Scroll to top when content changes
      if (_episodicMemoriesScrollRect != null)
      {
        _episodicMemoriesScrollRect.verticalNormalizedPosition = 1f;
      }
    }

    private void AppendEpisodicMemories()
    {
      var memories = _memorySystem.GetRecentMemories(_maxEpisodicDisplay).ToList();
      var allMemories = _memorySystem.GetActiveEpisodicMemories().ToList();
      var totalCount = allMemories.Count;
      var colorHex = ColorUtility.ToHtmlStringRGB(_episodicColor);
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      var displayInfo = memories.Count < totalCount
        ? $"{memories.Count}/{totalCount}, showing most recent"
        : $"{memories.Count}";

      _sb.AppendLine($"<color=#{headerHex}><b>=== EPISODIC MEMORIES ({displayInfo}) ===</b></color>");

      if (memories.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  (none)</color>");
      }
      else
      {
        foreach (var memory in memories)
        {
          var significance = memory.Significance.ToString("F2");
          var source = $"[{memory.Source}] ";
          var decayIndicator = memory.Significance < 0.3f ? " <color=#FF6666>(fading)</color>" : "";
          _sb.AppendLine($"<color=#{colorHex}>  {source}{memory.Content} <color=#888888>(sig:{significance}){decayIndicator}</color></color>");
        }
      }
      _sb.AppendLine();
    }


    private void RefreshBeliefs()
    {
      if (_beliefsText == null) return;
      
      var beliefs = _memorySystem.GetAllBeliefs().ToList();
      var colorHex = ColorUtility.ToHtmlStringRGB(_beliefColor);
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      _sb.Clear();
      _sb.AppendLine($"<color=#{headerHex}><b>=== BELIEFS ({beliefs.Count}) ===</b></color>");

      if (beliefs.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  (none)</color>");
      }
      else
      {
        foreach (var belief in beliefs)
        {
          var confidence = belief.Confidence.ToString("F2");
          var about = !string.IsNullOrEmpty(belief.Subject) ? $"[About: {belief.Subject}] " : "";
          _sb.AppendLine($"<color=#{colorHex}>  {about}{belief.Content} <color=#888888>(conf:{confidence})</color></color>");
        }
      }

      _beliefsText.text = _sb.ToString();
      
      // Scroll to top when content changes
      if (_beliefsScrollRect != null)
      {
        _beliefsScrollRect.verticalNormalizedPosition = 1f;
      }
    }

    private void AppendBeliefs()
    {
      var beliefs = _memorySystem.GetAllBeliefs().ToList();
      var colorHex = ColorUtility.ToHtmlStringRGB(_beliefColor);
      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);

      _sb.AppendLine($"<color=#{headerHex}><b>=== BELIEFS ({beliefs.Count}) ===</b></color>");

      if (beliefs.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  (none)</color>");
      }
      else
      {
        foreach (var belief in beliefs)
        {
          var confidence = belief.Confidence.ToString("F2");
          var about = !string.IsNullOrEmpty(belief.Subject) ? $"[About: {belief.Subject}] " : "";
          _sb.AppendLine($"<color=#{colorHex}>  {about}{belief.Content} <color=#888888>(conf:{confidence})</color></color>");
        }
      }
      _sb.AppendLine();
    }


    private void RefreshMutationLog()
    {
      if (_mutationLogText == null) return;
      if (_showMutationLog != null && !_showMutationLog.isOn)
      {
        _mutationLogText.text = "";
        return;
      }

      // Also check for new mutations from the agent
      CheckForNewMutations();

      _sb.Clear();

      var headerHex = ColorUtility.ToHtmlStringRGB(_headerColor);
      _sb.AppendLine($"<color=#{headerHex}><b>=== MUTATION LOG ({_mutationLog.Count}) ===</b></color>");

      if (_mutationLog.Count == 0)
      {
        _sb.AppendLine("<color=#666666>  No mutations recorded yet.</color>");
      }
      else
      {
        var approvedHex = ColorUtility.ToHtmlStringRGB(_approvedColor);
        var rejectedHex = ColorUtility.ToHtmlStringRGB(_rejectedColor);

        foreach (var entry in _mutationLog.Take(20)) // Show last 20
        {
          var colorHex = entry.Approved ? approvedHex : rejectedHex;
          var status = entry.Approved ? "APPROVED" : "REJECTED";
          var time = entry.Timestamp.ToString("HH:mm:ss");
          var reason = !entry.Approved && !string.IsNullOrEmpty(entry.FailureReason)
            ? $"\n      Reason: {entry.FailureReason}"
            : "";

          _sb.AppendLine($"<color=#{colorHex}>[{time}] {status}: {entry.MutationType} -> {entry.Target}</color>");
          if (!string.IsNullOrEmpty(entry.Details))
          {
            _sb.AppendLine($"    <color=#AAAAAA>{entry.Details}</color>{reason}");
          }
        }
      }

      _mutationLogText.text = _sb.ToString();
    }

    private void CheckForNewMutations()
    {
      if (_trackedAgent == null) return;

      var lastResult = _trackedAgent.LastMutationBatchResult;
      if (lastResult == null) return;

      // Check if this is a new result (simple heuristic: check timestamp or count)
      // For now, we rely on external calls to LogMutation
    }

    private void RefreshStats()
    {
      if (_statsText == null) return;
      if (_trackedAgent == null)
      {
        _statsText.text = "Agent: None";
        return;
      }

      _sb.Clear();

      var agentName = _trackedAgent.RuntimeProfile?.Name ?? _trackedAgent.gameObject.name;
      _sb.AppendLine($"<b>Agent:</b> {agentName}");

      // Mutation stats
      var mutationStats = _trackedAgent.MutationStats;
      if (mutationStats != null)
      {
        _sb.AppendLine($"<b>Mutations:</b> {mutationStats.TotalAttempted} attempted | {mutationStats.TotalSucceeded} succeeded | {mutationStats.TotalFailed} failed");
        if (mutationStats.CanonicalMutationAttempts > 0)
        {
          _sb.AppendLine($"<color=#FF6666><b>Protected:</b> {mutationStats.CanonicalMutationAttempts} canonical fact mutations blocked</color>");
        }
      }

      // Fallback stats
      var fallbackStats = _trackedAgent.FallbackStats;
      if (fallbackStats != null && fallbackStats.TotalFallbacks > 0)
      {
        _sb.AppendLine($"<b>Fallbacks:</b> {fallbackStats.TotalFallbacks} total | {fallbackStats.EmergencyFallbacks} emergency");
      }

      // Last inference info
      var lastResult = _trackedAgent.LastInferenceResult;
      if (lastResult != null)
      {
        var status = lastResult.Success ? "<color=#66FF66>Success</color>" : "<color=#FF6666>Failed</color>";
        _sb.AppendLine($"<b>Last Inference:</b> {status} ({lastResult.AttemptCount} attempts, {lastResult.TotalElapsedMilliseconds}ms)");
      }

      _statsText.text = _sb.ToString();
    }

    private void SetupDefaultToggles()
    {
      // Set up toggle listeners if they exist
      if (_showCanonicalFacts != null)
      {
        _showCanonicalFacts.isOn = true;
        _showCanonicalFacts.onValueChanged.AddListener(_ => RefreshMemoryDisplay());
      }
      if (_showWorldState != null)
      {
        _showWorldState.isOn = true;
        _showWorldState.onValueChanged.AddListener(_ => RefreshMemoryDisplay());
      }
      if (_showEpisodicMemories != null)
      {
        _showEpisodicMemories.isOn = true;
        _showEpisodicMemories.onValueChanged.AddListener(_ => RefreshMemoryDisplay());
      }
      if (_showBeliefs != null)
      {
        _showBeliefs.isOn = true;
        _showBeliefs.onValueChanged.AddListener(_ => RefreshMemoryDisplay());
      }
      if (_showMutationLog != null)
      {
        _showMutationLog.isOn = true;
        _showMutationLog.onValueChanged.AddListener(_ => RefreshMutationLog());
      }
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
