using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Whisper;
using Debug = UnityEngine.Debug;

namespace LlamaBrain.Runtime.Core.Voice
{
  /// <summary>
  /// Main orchestrator for voice-based NPC interactions.
  /// Ties together: Whisper STT -> LlamaBrainAgent -> Piper TTS
  /// Handles the full conversation loop with silence timeout.
  /// </summary>
  [RequireComponent(typeof(AudioSource))]
  public class NpcVoiceController : MonoBehaviour
  {
    [Header("Agent Reference")]
    [SerializeField]
    [Tooltip("The LlamaBrainAgent to use for processing player input.")]
    private LlamaBrainAgent agent;

    [Header("Voice Configuration")]
    [SerializeField]
    [Tooltip("Voice configuration for this NPC's speech output.")]
    private NpcSpeechConfig speechConfig;

    [Header("Whisper Settings (STT)")]
    [SerializeField]
    [Tooltip("Reference to WhisperManager in the scene.")]
    private WhisperManager whisperManager;

#pragma warning disable CS0414 // whisperLanguage is reserved for future speech recognition configuration
    [SerializeField]
    [Tooltip("Language for speech recognition (reserved for future use).")]
    private string whisperLanguage = "en";
#pragma warning restore CS0414

    [Header("Input Mode")]
    [SerializeField]
    [Tooltip("When true, automatically listens for voice input (VAD-based).")]
    private bool alwaysListening = true;

    [SerializeField]
    [Tooltip("When true, text input can also be used as fallback.")]
    private bool enableTextFallback = true;

    [SerializeField]
    [Tooltip("When true, pauses listening while NPC speaks (prevents feedback but disables interruption).")]
    private bool pauseListeningDuringSpeech = true;

    [SerializeField]
    [Tooltip("When true, player can interrupt NPC by speaking.")]
    private bool allowInterruption = false;

    [SerializeField]
    [Tooltip("Minimum words required to trigger interruption (helps filter echo).")]
    [Range(1, 5)]
    private int minInterruptionWords = 2;

    [SerializeField]
    [Tooltip("Seconds to wait after TTS finishes before resuming listening (prevents mic picking up NPC speech).")]
    [Range(0f, 2f)]
    private float postSpeechDelay = 0.5f;

    [Header("Timing")]
    [SerializeField]
    [Tooltip("Seconds of silence before automatically ending conversation.")]
    private float silenceTimeoutSeconds = 6f;

    [Header("Events")]
    [Tooltip("Fired when player speech is recognized.")]
    /// <summary>
    /// Event fired when player speech is recognized. The string parameter contains the transcribed text.
    /// </summary>
    public UnityEvent<string> OnPlayerSpeechRecognized = new UnityEvent<string>();

    [Tooltip("Fired when NPC response is generated.")]
    /// <summary>
    /// Event fired when NPC response is generated. The string parameter contains the response text.
    /// </summary>
    public UnityEvent<string> OnNpcResponseGenerated = new UnityEvent<string>();

    [Tooltip("Fired when NPC starts listening for input.")]
    /// <summary>
    /// Event fired when the NPC starts listening for voice input.
    /// </summary>
    public UnityEvent OnListeningStarted = new UnityEvent();

    [Tooltip("Fired when NPC stops listening.")]
    /// <summary>
    /// Event fired when the NPC stops listening for voice input.
    /// </summary>
    public UnityEvent OnListeningStopped = new UnityEvent();

    [Tooltip("Fired when NPC starts speaking.")]
    /// <summary>
    /// Event fired when the NPC starts speaking.
    /// </summary>
    public UnityEvent OnSpeakingStarted = new UnityEvent();

    [Tooltip("Fired when NPC finishes speaking.")]
    /// <summary>
    /// Event fired when the NPC finishes speaking.
    /// </summary>
    public UnityEvent OnSpeakingFinished = new UnityEvent();

    [Tooltip("Fired when silence timeout is reached.")]
    /// <summary>
    /// Event fired when the silence timeout is reached and the conversation ends automatically.
    /// </summary>
    public UnityEvent OnSilenceTimeout = new UnityEvent();

    private NpcVoiceInput _voiceInput;
    private NpcVoiceOutput _voiceOutput;
    private bool _isInConversation;
    private bool _isListening;
    private bool _isSpeaking;
    private bool _isProcessing;
    private float _lastActivityTime;
    private CancellationTokenSource _conversationCts;
    private Stopwatch _sttStopwatch = new Stopwatch();
    private bool _earlyTranscriptionProcessed; // Track if we already routed early transcription to LLM
    private CancellationTokenSource _interruptionCts; // For cancelling TTS on player interruption
    private bool _wasInterrupted; // Track if last speech was interrupted
    private string _lastNpcResponse = ""; // Track what the NPC last said (for echo detection)
    private float _lastNpcSpeechTime; // When NPC last spoke (for echo detection timeout)
    private const float EchoDetectionWindowSeconds = 5f; // How long to check for echo after NPC speaks
    private const float EchoSimilarityThreshold = 0.3f; // 30% word overlap = probably echo

    /// <summary>
    /// Whether the controller is currently in a conversation.
    /// </summary>
    public bool IsInConversation => _isInConversation;

    /// <summary>
    /// Whether currently listening for voice input.
    /// </summary>
    public bool IsListening => _isListening;

    /// <summary>
    /// Whether the NPC is currently speaking.
    /// </summary>
    public bool IsSpeaking => _isSpeaking;

    /// <summary>
    /// Whether a request is being processed.
    /// </summary>
    public bool IsProcessing => _isProcessing;

    /// <summary>
    /// Whether text input is enabled as fallback.
    /// </summary>
    public bool EnableTextFallback => enableTextFallback;

    /// <summary>
    /// Whether player can interrupt NPC speech.
    /// </summary>
    public bool AllowInterruption
    {
      get => allowInterruption;
      set => allowInterruption = value;
    }

    /// <summary>
    /// The LlamaBrainAgent being used.
    /// </summary>
    public LlamaBrainAgent Agent
    {
      get => agent;
      set => agent = value;
    }

    /// <summary>
    /// The speech configuration.
    /// </summary>
    public NpcSpeechConfig SpeechConfig
    {
      get => speechConfig;
      set
      {
        speechConfig = value;
        if (_voiceOutput != null)
          _voiceOutput.SpeechConfig = value;
      }
    }

    private void Awake()
    {
      // Create voice input/output components
      _voiceInput = gameObject.AddComponent<NpcVoiceInput>();
      _voiceOutput = gameObject.AddComponent<NpcVoiceOutput>();

      if (speechConfig != null)
        _voiceOutput.SpeechConfig = speechConfig;

      // Wire up events
      _voiceInput.OnTranscriptionComplete.AddListener(OnTranscriptionCompleteHandler);
      _voiceInput.OnEarlyTranscription.AddListener(OnEarlyTranscriptionHandler);
      _voiceInput.OnListeningStarted.AddListener(() =>
      {
        _isListening = true;
        _earlyTranscriptionProcessed = false; // Reset early processing flag
        _sttStopwatch.Restart();
        ResetSilenceTimer();
        OnListeningStarted?.Invoke();
      });
      _voiceInput.OnListeningStopped.AddListener(() =>
      {
        _isListening = false;
        OnListeningStopped?.Invoke();
      });

      _voiceOutput.OnSpeakingStarted.AddListener(() =>
      {
        _isSpeaking = true;
        OnSpeakingStarted?.Invoke();
      });
      _voiceOutput.OnSpeakingFinished.AddListener(() =>
      {
        Debug.Log("[NpcVoiceController] OnSpeakingFinished received from VoiceOutput");
        _isSpeaking = false;
        ResetSilenceTimer();
        Debug.Log("[NpcVoiceController] Invoking OnSpeakingFinished event...");
        OnSpeakingFinished?.Invoke();
        Debug.Log("[NpcVoiceController] OnSpeakingFinished complete");
      });
    }

    private void Start()
    {
      InitializeWithErrorHandlingAsync().Forget();
    }

    private async UniTaskVoid InitializeWithErrorHandlingAsync()
    {
      try
      {
        // Set whisper manager reference
        if (whisperManager == null)
        {
          whisperManager = FindAnyObjectByType<WhisperManager>();
        }

        // Initialize components
        await _voiceInput.InitializeAsync();
        await _voiceOutput.InitializeAsync();

        Debug.Log("[NpcVoiceController] Voice controller initialized");

        // Auto-start listening if always-on mode is enabled
        if (alwaysListening && agent != null)
        {
          Debug.Log("[NpcVoiceController] Auto-starting conversation (always listening mode)...");
          StartConversation();
        }
        else if (alwaysListening && agent == null)
        {
          Debug.LogWarning("[NpcVoiceController] alwaysListening=true but no agent assigned. Cannot auto-start.");
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceController] Initialization failed: {ex.Message}\n{ex.StackTrace}");
      }
    }

    [Header("Debug")]
    [Tooltip("Log heartbeat every N frames to diagnose freezes. 0 = disabled.")]
    [SerializeField] private int heartbeatEveryNFrames = 0;
    private int _heartbeatCounter = 0;

    private void Update()
    {
      // Heartbeat debug logging
      if (heartbeatEveryNFrames > 0)
      {
        _heartbeatCounter++;
        if (_heartbeatCounter >= heartbeatEveryNFrames)
        {
          //Debug.Log($"[NpcVoiceController] Heartbeat frame={Time.frameCount} time={Time.time:F2} state: conv={_isInConversation} speak={_isSpeaking} listen={_isListening} proc={_isProcessing}");
          _heartbeatCounter = 0;
        }
      }

      // Check silence timeout when in conversation
      if (_isInConversation && !_isSpeaking && !_isListening && !_isProcessing)
      {
        if (Time.time - _lastActivityTime > silenceTimeoutSeconds)
        {
          Debug.Log($"[NpcVoiceController] Silence timeout reached ({silenceTimeoutSeconds}s)");
          OnSilenceTimeout?.Invoke();
          EndConversation();
        }
      }
    }

    private void OnDestroy()
    {
      EndConversation();
    }

    /// <summary>
    /// Start a voice conversation with this NPC.
    /// The controller will listen for voice input and respond.
    /// </summary>
    public void StartConversation()
    {
      Debug.Log($"[NpcVoiceController] StartConversation called. _isInConversation={_isInConversation}, agent={(agent != null ? agent.name : "NULL")}, alwaysListening={alwaysListening}");

      if (_isInConversation)
      {
        Debug.LogWarning("[NpcVoiceController] Already in conversation");
        return;
      }

      if (agent == null)
      {
        Debug.LogError("[NpcVoiceController] No agent assigned! Set the Agent field on NpcVoiceController in the Inspector.");
        return;
      }

      _isInConversation = true;
      _conversationCts = new CancellationTokenSource();
      ResetSilenceTimer();

      Debug.Log("[NpcVoiceController] Conversation started successfully");

      // Start listening if always-on
      if (alwaysListening)
      {
        Debug.Log("[NpcVoiceController] alwaysListening=true, calling _voiceInput.StartListening()...");
        _voiceInput.StartListening();
      }
      else
      {
        Debug.Log("[NpcVoiceController] alwaysListening=false, NOT starting voice input automatically");
      }
    }

    /// <summary>
    /// End the current conversation.
    /// </summary>
    public void EndConversation()
    {
      if (!_isInConversation)
        return;

      _isInConversation = false;

      // Cancel ongoing operations
      _conversationCts?.Cancel();
      _conversationCts?.Dispose();
      _conversationCts = null;

      // Stop voice I/O
      _voiceInput.StopListening();
      _voiceOutput.Stop();

      Debug.Log("[NpcVoiceController] Conversation ended");
    }

    /// <summary>
    /// Process text input (for text fallback mode).
    /// </summary>
    /// <param name="text">Player input text.</param>
    /// <returns>A task that completes with the NPC's response text, or an empty string if cancelled or no input provided.</returns>
    public async UniTask<string> ProcessTextInputAsync(string text)
    {
      if (!_isInConversation)
      {
        Debug.LogWarning("[NpcVoiceController] Not in conversation. Call StartConversation first.");
        return "";
      }

      if (string.IsNullOrWhiteSpace(text))
        return "";

      ResetSilenceTimer();
      return await ProcessInputAsync(text);
    }

    /// <summary>
    /// Listen for voice input and return the transcribed text.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the listening operation.</param>
    /// <returns>A task that completes with the transcribed text, or an empty string if cancelled or no input detected.</returns>
    public async UniTask<string> ListenAsync(CancellationToken ct = default)
    {
      if (!_isInConversation)
      {
        Debug.LogWarning("[NpcVoiceController] Not in conversation");
        return "";
      }

      using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
          ct, _conversationCts?.Token ?? CancellationToken.None);

      return await _voiceInput.ListenForInputAsync(linkedCts.Token);
    }

    /// <summary>
    /// Speak the given text using TTS.
    /// Uses sentence-level streaming for faster time-to-first-audio.
    /// </summary>
    /// <param name="text">The text to speak.</param>
    /// <param name="ct">Cancellation token to cancel the speaking operation.</param>
    /// <returns>A task that completes when the speech finishes or is cancelled.</returns>
    public async UniTask SpeakAsync(string text, CancellationToken ct = default)
    {
      if (string.IsNullOrWhiteSpace(text))
        return;

      _wasInterrupted = false;

      // Determine listening behavior during speech
      var shouldPauseListening = pauseListeningDuringSpeech && !allowInterruption;
      var shouldRestartListening = alwaysListening && _isInConversation;

      if (_isListening && shouldPauseListening)
      {
        Debug.Log("[NpcVoiceController] Pausing listening during speech...");
        _voiceInput.StopListening();
      }
      else if (allowInterruption && !_isListening && alwaysListening)
      {
        // Start listening for potential interruption
        Debug.Log("[NpcVoiceController] Keeping mic open for potential interruption...");
        _voiceInput.StartListening();
      }

      // Create interruption cancellation token
      _interruptionCts?.Dispose();
      _interruptionCts = new CancellationTokenSource();

      try
      {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ct,
            _conversationCts?.Token ?? CancellationToken.None,
            _interruptionCts.Token);

        // Use streaming TTS for faster time-to-first-audio
        // This generates and plays the first sentence immediately,
        // while remaining sentences are generated in the background
        await _voiceOutput.SpeakStreamingAsync(text, linkedCts.Token);

        // Wait after TTS ends before resuming listening (only if not interrupted)
        // This prevents the microphone from picking up residual NPC speech
        if (!_wasInterrupted && postSpeechDelay > 0 && shouldRestartListening && shouldPauseListening)
        {
          Debug.Log($"[NpcVoiceController] Waiting {postSpeechDelay}s before resuming listening...");
          await UniTask.Delay(TimeSpan.FromSeconds(postSpeechDelay), cancellationToken: linkedCts.Token);
        }
      }
      catch (OperationCanceledException) when (_wasInterrupted)
      {
        Debug.Log("[NpcVoiceController] Speech interrupted by player");
        // Don't rethrow - interruption is expected behavior
      }
      finally
      {
        _interruptionCts?.Dispose();
        _interruptionCts = null;

        // Resume listening after speaking if in always-listening mode (and we paused it)
        if (shouldRestartListening && _isInConversation && shouldPauseListening && !_wasInterrupted)
        {
          Debug.Log("[NpcVoiceController] Resuming listening after speech...");
          _voiceInput.StartListening();
        }
      }
    }

    /// <summary>
    /// Interrupt the current NPC speech (for player interruption).
    /// </summary>
    public void InterruptSpeech()
    {
      if (!_isSpeaking)
        return;

      Debug.Log("[NpcVoiceController] Interrupting NPC speech...");
      _wasInterrupted = true;
      _interruptionCts?.Cancel();
      _voiceOutput.Stop();
    }

    /// <summary>
    /// Stop any ongoing speech.
    /// </summary>
    public void StopSpeaking()
    {
      _voiceOutput.Stop();
    }

    /// <summary>
    /// Process a full voice interaction: listen -> agent -> speak.
    /// </summary>
    /// <returns>A task that completes with the NPC's response text, or an empty string if cancelled or no input detected.</returns>
    public async UniTask<string> ProcessVoiceInputAsync()
    {
      if (!_isInConversation)
      {
        Debug.LogWarning("[NpcVoiceController] Not in conversation");
        return "";
      }

      try
      {
        // Listen for input
        var playerInput = await ListenAsync(_conversationCts?.Token ?? CancellationToken.None);

        if (string.IsNullOrWhiteSpace(playerInput))
          return "";

        // Process and speak response
        return await ProcessInputAsync(playerInput);
      }
      catch (OperationCanceledException)
      {
        return "";
      }
    }

    private async UniTask<string> ProcessInputAsync(string playerInput)
    {
      if (agent == null || !agent.IsInitialized)
      {
        Debug.LogError("[NpcVoiceController] Agent not initialized");
        return "";
      }

      _isProcessing = true;
      ResetSilenceTimer();

      var totalStopwatch = Stopwatch.StartNew();
      var stageStopwatch = new Stopwatch();

      try
      {
        // Notify that player input was recognized
        OnPlayerSpeechRecognized?.Invoke(playerInput);
        Debug.Log($"[NpcVoiceController] Player said: \"{playerInput}\"");

        // === LLM Stage ===
        stageStopwatch.Restart();
        var response = await agent.SendPlayerInputAsync(playerInput);
        var llmTimeMs = stageStopwatch.ElapsedMilliseconds;

        if (string.IsNullOrWhiteSpace(response))
        {
          Debug.LogWarning("[NpcVoiceController] Agent returned empty response");
          return "";
        }

        // Notify that response was generated
        OnNpcResponseGenerated?.Invoke(response);
        Debug.Log($"[NpcVoiceController] NPC response: \"{response}\"");
        Debug.Log($"[PERF] LLM completed in {llmTimeMs}ms");

        // Store NPC response for echo detection before speaking
        _lastNpcResponse = response;
        _lastNpcSpeechTime = Time.time;

        // === TTS Stage ===
        stageStopwatch.Restart();
        await SpeakAsync(response, _conversationCts?.Token ?? CancellationToken.None);
        var ttsTimeMs = stageStopwatch.ElapsedMilliseconds;

        totalStopwatch.Stop();
        Debug.Log($"[PERF] TTS completed in {ttsTimeMs}ms");
        Debug.Log($"[PERF] Total pipeline (LLM+TTS): {totalStopwatch.ElapsedMilliseconds}ms");

        // Note: Listening is restarted in SpeakAsync's finally block with postSpeechDelay
        // to prevent microphone from picking up residual NPC speech

        return response;
      }
      catch (OperationCanceledException)
      {
        return "";
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceController] Error processing input: {ex.Message}");
        return "";
      }
      finally
      {
        _isProcessing = false;
        ResetSilenceTimer();
      }
    }

    private void OnEarlyTranscriptionHandler(string transcription)
    {
      // Early transcription fires when a valid segment is detected before stream formally ends
      // This allows us to start LLM processing immediately without waiting for silence timeout

      if (string.IsNullOrWhiteSpace(transcription))
        return;
      // Handle interruption: player spoke while NPC was speaking
      if (_isSpeaking && allowInterruption)
      {
        // Filter potential echo by requiring minimum word count
        var wordCount = CountWords(transcription);
        if (wordCount < minInterruptionWords)
        {
          Debug.Log($"[NpcVoiceController] Ignoring potential echo ({wordCount} words < {minInterruptionWords} required): \"{transcription}\"");
          return;
        }

        // Check if this is echo of what NPC just said (Whisper misheard NPC's speech)
        if (IsLikelyEcho(transcription))
        {
          Debug.Log($"[NpcVoiceController] Ignoring echo of NPC speech: \"{transcription}\"");
          return;
        }

        Debug.Log($"[NpcVoiceController] Player interruption detected ({wordCount} words): \"{transcription}\"");

        // Stop the NPC immediately
        InterruptSpeech();

        // Small delay to let audio stop before processing new input
        HandleInterruptionAsync(transcription).Forget();
        return;
      }

      // Also check for echo in non-interruption case (mic picked up residual NPC speech)
      if (IsLikelyEcho(transcription))
      {
        Debug.Log($"[NpcVoiceController] Ignoring residual echo: \"{transcription}\"");
        return;
      }

      // Skip if already processing or speaking (and interruption not allowed)
      if (_earlyTranscriptionProcessed || _isProcessing || _isSpeaking)
      {
        Debug.Log($"[NpcVoiceController] Ignoring early transcription (already processed or busy)");
        return;
      }

      var sttTimeMs = _sttStopwatch.ElapsedMilliseconds;
      Debug.Log($"[NpcVoiceController] Early transcription received: \"{transcription}\"");
      Debug.Log($"[PERF] STT (early) completed in {sttTimeMs}ms");

      _earlyTranscriptionProcessed = true;

      // Stop listening immediately to prevent further Whisper processing
      // This avoids the ~3s delay from final [BLANK_AUDIO] inference
      _voiceInput.StopListening();

      // Route to LLM immediately
      Debug.Log($"[NpcVoiceController] Routing early transcription to LlamaBrain: \"{transcription}\"");
      ProcessInputAsync(transcription).Forget();
    }

    private async UniTaskVoid HandleInterruptionAsync(string transcription)
    {
      // Brief delay to ensure TTS has stopped
      await UniTask.Delay(100);

      // Reset state for new input
      _earlyTranscriptionProcessed = true;
      _isProcessing = false; // Clear any stale processing state

      // Stop listening to prevent picking up residual audio
      _voiceInput.StopListening();

      // Route the interruption to LLM
      Debug.Log($"[NpcVoiceController] Processing interruption: \"{transcription}\"");
      await ProcessInputAsync(transcription);
    }

    private void OnTranscriptionCompleteHandler(string transcription)
    {
      var sttTimeMs = _sttStopwatch.ElapsedMilliseconds;
      _sttStopwatch.Stop();

      Debug.Log($"[NpcVoiceController] OnTranscriptionCompleteHandler received: \"{transcription}\" (length={transcription?.Length ?? 0})");

      // Skip if we already processed early transcription
      if (_earlyTranscriptionProcessed)
      {
        Debug.Log("[NpcVoiceController] Skipping - already processed via early transcription");
        return;
      }

      if (string.IsNullOrWhiteSpace(transcription))
      {
        // Stream finished with empty result (mic stopped without speech)
        // Restart listening if we're still in always-listening mode
        if (alwaysListening && _isInConversation && !_isSpeaking && !_isProcessing)
        {
          Debug.Log("[NpcVoiceController] Empty transcription, restarting listening...");
          ResetSilenceTimer();
          _voiceInput.StartListening();
        }
        return;
      }

      // Log STT performance
      Debug.Log($"[PERF] STT completed in {sttTimeMs}ms");

      // Process the transcription - route to LlamaBrain
      Debug.Log($"[NpcVoiceController] Routing transcription to LlamaBrain: \"{transcription}\"");
      ProcessInputAsync(transcription).Forget();
    }

    private void ResetSilenceTimer()
    {
      _lastActivityTime = Time.time;
    }

    /// <summary>
    /// Counts words in a string (simple whitespace split).
    /// </summary>
    private static int CountWords(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return 0;

      return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Checks if transcription is likely echo of what NPC just said.
    /// Uses word overlap to detect when Whisper transcribed NPC's own speech.
    /// </summary>
    private bool IsLikelyEcho(string transcription)
    {
      // No echo detection if NPC hasn't spoken recently
      if (string.IsNullOrWhiteSpace(_lastNpcResponse))
        return false;

      // Timeout: don't check for echo after detection window expires
      if (Time.time - _lastNpcSpeechTime > EchoDetectionWindowSeconds)
      {
        _lastNpcResponse = ""; // Clear stale response
        return false;
      }

      // Normalize both strings for comparison
      var transcriptionWords = NormalizeForComparison(transcription);
      var npcWords = NormalizeForComparison(_lastNpcResponse);

      if (transcriptionWords.Length == 0 || npcWords.Length == 0)
        return false;

      // Count matching words
      var matchCount = 0;
      var npcWordSet = new System.Collections.Generic.HashSet<string>(npcWords);
      foreach (var word in transcriptionWords)
      {
        if (npcWordSet.Contains(word))
          matchCount++;
      }

      // Calculate overlap ratio (based on transcription length)
      var overlapRatio = (float)matchCount / transcriptionWords.Length;

      if (overlapRatio >= EchoSimilarityThreshold)
      {
        Debug.Log($"[NpcVoiceController] Echo detected: {overlapRatio:P0} word overlap ({matchCount}/{transcriptionWords.Length} words)");
        return true;
      }

      return false;
    }

    /// <summary>
    /// Normalizes text for echo comparison: lowercase, remove punctuation, split into words.
    /// </summary>
    private static string[] NormalizeForComparison(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return Array.Empty<string>();

      // Lowercase and remove common punctuation
      var normalized = text.ToLowerInvariant();
      normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^\w\s]", " ");

      return normalized.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Get time remaining before silence timeout.
    /// </summary>
    /// <returns>The time remaining in seconds before silence timeout, or 0 if not in conversation.</returns>
    public float GetSilenceTimeRemaining()
    {
      if (!_isInConversation)
        return 0;

      var elapsed = Time.time - _lastActivityTime;
      return Mathf.Max(0, silenceTimeoutSeconds - elapsed);
    }
  }
}
