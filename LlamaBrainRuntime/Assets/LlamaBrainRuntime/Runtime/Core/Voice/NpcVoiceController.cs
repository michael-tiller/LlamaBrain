using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Whisper;

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
      _voiceInput.OnListeningStarted.AddListener(() =>
      {
        _isListening = true;
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
        _isSpeaking = false;
        ResetSilenceTimer();
        OnSpeakingFinished?.Invoke();
      });
    }

    private async void Start()
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

    private void Update()
    {
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
    /// </summary>
    /// <param name="text">The text to speak.</param>
    /// <param name="ct">Cancellation token to cancel the speaking operation.</param>
    /// <returns>A task that completes when the speech finishes or is cancelled.</returns>
    public async UniTask SpeakAsync(string text, CancellationToken ct = default)
    {
      if (string.IsNullOrWhiteSpace(text))
        return;

      using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
          ct, _conversationCts?.Token ?? CancellationToken.None);

      await _voiceOutput.SpeakAsync(text, linkedCts.Token);
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

      try
      {
        // Notify that player input was recognized
        OnPlayerSpeechRecognized?.Invoke(playerInput);
        Debug.Log($"[NpcVoiceController] Player said: \"{playerInput}\"");

        // Get response from agent
        var response = await agent.SendPlayerInputAsync(playerInput);

        if (string.IsNullOrWhiteSpace(response))
        {
          Debug.LogWarning("[NpcVoiceController] Agent returned empty response");
          return "";
        }

        // Notify that response was generated
        OnNpcResponseGenerated?.Invoke(response);
        Debug.Log($"[NpcVoiceController] NPC response: \"{response}\"");

        // Speak the response
        await SpeakAsync(response, _conversationCts?.Token ?? CancellationToken.None);

        // Restart listening if always-on and still in conversation
        if (alwaysListening && _isInConversation)
        {
          _voiceInput.StartListening();
        }

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

    private void OnTranscriptionCompleteHandler(string transcription)
    {
      if (string.IsNullOrWhiteSpace(transcription))
        return;

      // Process the transcription
      ProcessInputAsync(transcription).Forget();
    }

    private void ResetSilenceTimer()
    {
      _lastActivityTime = Time.time;
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
