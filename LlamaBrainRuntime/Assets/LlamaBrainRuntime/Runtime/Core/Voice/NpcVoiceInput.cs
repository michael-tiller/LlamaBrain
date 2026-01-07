using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Whisper;
using Whisper.Utils;

namespace LlamaBrain.Runtime.Core.Voice
{
  /// <summary>
  /// Handles speech-to-text input using Whisper.
  /// Wraps the Whisper streaming transcription for player voice input.
  /// </summary>
  public class NpcVoiceInput : MonoBehaviour
  {
    [Header("Whisper Settings")]
    [SerializeField]
    [Tooltip("Reference to the WhisperManager in the scene.")]
    private WhisperManager whisperManager;

    [SerializeField]
    [Tooltip("Language for speech recognition.")]
    private string language = "en";

    [Header("Microphone Settings")]
    [SerializeField]
    [Tooltip("Reference to MicrophoneRecord component.")]
    private MicrophoneRecord microphoneRecord;

    [SerializeField]
    [Tooltip("Maximum recording length in seconds.")]
    private int maxRecordingLength = 30;

    [SerializeField]
    [Tooltip("Auto-stop recording after silence (VAD-based).")]
    private bool vadStop = true;

    [SerializeField]
    [Tooltip("Seconds of silence before auto-stop.")]
    private float vadStopTime = 2f;

    [Header("Events")]
    [Tooltip("Fired when a complete transcription is available.")]
    /// <summary>
    /// Event fired when a complete transcription is available. The string parameter contains the final transcribed text.
    /// </summary>
    public UnityEvent<string> OnTranscriptionComplete = new UnityEvent<string>();

    [Tooltip("Fired when partial transcription is available (streaming).")]
    /// <summary>
    /// Event fired when partial transcription is available during streaming. The string parameter contains the current partial transcription.
    /// </summary>
    public UnityEvent<string> OnPartialTranscription = new UnityEvent<string>();

    [Tooltip("Fired when listening starts.")]
    /// <summary>
    /// Event fired when listening for voice input starts.
    /// </summary>
    public UnityEvent OnListeningStarted = new UnityEvent();

    [Tooltip("Fired when listening stops.")]
    /// <summary>
    /// Event fired when listening for voice input stops.
    /// </summary>
    public UnityEvent OnListeningStopped = new UnityEvent();

    [Tooltip("Fired when voice activity is detected.")]
    /// <summary>
    /// Event fired when voice activity detection state changes. The bool parameter indicates whether speech is currently detected.
    /// </summary>
    public UnityEvent<bool> OnVoiceActivityChanged = new UnityEvent<bool>();

    private WhisperStream _whisperStream;
    private bool _isListening;
    private bool _isInitialized;
    private string _currentTranscription = "";
    private TaskCompletionSource<string> _listenCompletionSource;

    /// <summary>
    /// Whether currently listening for voice input.
    /// </summary>
    public bool IsListening => _isListening;

    /// <summary>
    /// Whether Whisper is initialized and ready.
    /// </summary>
    public bool IsInitialized => _isInitialized && whisperManager != null && whisperManager.IsLoaded;

    /// <summary>
    /// The language for speech recognition.
    /// </summary>
    public string Language
    {
      get => language;
      set => language = value;
    }

    private void Awake()
    {
      // Create MicrophoneRecord if not assigned
      if (microphoneRecord == null)
      {
        microphoneRecord = gameObject.AddComponent<MicrophoneRecord>();
        ConfigureMicrophone();
      }
    }

    private async void Start()
    {
      await InitializeAsync();
    }

    private void OnDestroy()
    {
      StopListening();
    }

    /// <summary>
    /// Initialize the STT system.
    /// </summary>
    /// <returns>A task that completes when initialization is finished.</returns>
    public async UniTask InitializeAsync()
    {
      if (_isInitialized)
        return;

      // Wait for WhisperManager to be ready
      if (whisperManager == null)
      {
        whisperManager = FindAnyObjectByType<WhisperManager>();
        if (whisperManager == null)
        {
          Debug.LogError("[NpcVoiceInput] No WhisperManager found in scene");
          return;
        }
      }

      // Wait for model to load
      while (!whisperManager.IsLoaded && whisperManager.IsLoading)
      {
        await UniTask.Yield();
      }

      if (!whisperManager.IsLoaded)
      {
        Debug.LogError("[NpcVoiceInput] WhisperManager failed to load model");
        return;
      }

      ConfigureMicrophone();
      _isInitialized = true;
      Debug.Log("[NpcVoiceInput] STT system initialized");
    }

    private void ConfigureMicrophone()
    {
      if (microphoneRecord == null)
        return;

      // Log available microphones
      var devices = Microphone.devices;
      Debug.Log($"[NpcVoiceInput] Available microphones ({devices.Length}):");
      for (int i = 0; i < devices.Length; i++)
      {
        Debug.Log($"  [{i}] {devices[i]}");
      }

      if (devices.Length == 0)
      {
        Debug.LogError("[NpcVoiceInput] NO MICROPHONES DETECTED! Check Windows audio settings.");
        return;
      }

      microphoneRecord.maxLengthSec = maxRecordingLength;
      microphoneRecord.loop = false;
      microphoneRecord.useVad = true;
      microphoneRecord.vadStop = vadStop;
      microphoneRecord.vadStopTime = vadStopTime;
      microphoneRecord.echo = false;

      // Subscribe to VAD changes
      microphoneRecord.OnVadChanged += OnVadChangedHandler;

      Debug.Log($"[NpcVoiceInput] Microphone configured: maxLength={maxRecordingLength}s, vadStop={vadStop}, vadStopTime={vadStopTime}s");
    }

    /// <summary>
    /// Start listening for voice input.
    /// </summary>
    public void StartListening()
    {
      Debug.Log($"[NpcVoiceInput] StartListening called. _isListening={_isListening}, IsInitialized={IsInitialized}");

      if (_isListening)
      {
        Debug.LogWarning("[NpcVoiceInput] Already listening");
        return;
      }

      if (!IsInitialized)
      {
        Debug.LogError("[NpcVoiceInput] Not initialized. Call InitializeAsync first.");
        return;
      }

      _isListening = true;
      _currentTranscription = "";
      OnListeningStarted?.Invoke();

      // Start microphone
      Debug.Log($"[NpcVoiceInput] Starting microphone recording...");
      microphoneRecord.StartRecord();
      Debug.Log($"[NpcVoiceInput] Microphone.IsRecording={microphoneRecord.IsRecording}");

      // Create and start whisper stream
      StartStreamAsync().Forget();
    }

    private async UniTaskVoid StartStreamAsync()
    {
      try
      {
        _whisperStream = await whisperManager.CreateStream(microphoneRecord);
        if (_whisperStream == null)
        {
          Debug.LogError("[NpcVoiceInput] Failed to create WhisperStream");
          StopListening();
          return;
        }

        // Subscribe to stream events
        _whisperStream.OnResultUpdated += OnStreamResultUpdated;
        _whisperStream.OnSegmentFinished += OnStreamSegmentFinished;
        _whisperStream.OnStreamFinished += OnStreamFinished;

        // Start the stream
        _whisperStream.StartStream();
        Debug.Log("[NpcVoiceInput] Started listening for voice input");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceInput] Failed to start stream: {ex.Message}");
        StopListening();
      }
    }

    /// <summary>
    /// Stop listening for voice input.
    /// </summary>
    public void StopListening()
    {
      if (!_isListening)
        return;

      _isListening = false;

      // Stop microphone
      if (microphoneRecord != null && microphoneRecord.IsRecording)
      {
        microphoneRecord.StopRecord();
      }

      // Stop whisper stream
      if (_whisperStream != null)
      {
        _whisperStream.OnResultUpdated -= OnStreamResultUpdated;
        _whisperStream.OnSegmentFinished -= OnStreamSegmentFinished;
        _whisperStream.OnStreamFinished -= OnStreamFinished;
        _whisperStream.StopStream();
        _whisperStream = null;
      }

      OnListeningStopped?.Invoke();
      Debug.Log("[NpcVoiceInput] Stopped listening");
    }

    /// <summary>
    /// Listen for voice input and return the transcribed text.
    /// Blocks until voice input is complete (VAD-based or manual stop).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transcribed text.</returns>
    public async UniTask<string> ListenForInputAsync(CancellationToken ct = default)
    {
      if (!IsInitialized)
      {
        await InitializeAsync();
      }

      _listenCompletionSource = new TaskCompletionSource<string>();

      // Register cancellation and capture the registration for disposal
      var cancellationRegistration = ct.Register(() =>
      {
        StopListening();
        _listenCompletionSource.TrySetCanceled();
      });

      StartListening();

      try
      {
        return await _listenCompletionSource.Task;
      }
      catch (TaskCanceledException)
      {
        return "";
      }
      finally
      {
        // Dispose the registration to unregister the callback and prevent memory leak
        cancellationRegistration.Dispose();
      }
    }

    private void OnStreamResultUpdated(string updatedResult)
    {
      _currentTranscription = updatedResult;
      OnPartialTranscription?.Invoke(updatedResult);
    }

    private void OnStreamSegmentFinished(WhisperResult segment)
    {
      // Segment finished, update transcription
      Debug.Log($"[NpcVoiceInput] Segment finished: {segment.Result}");
    }

    private void OnStreamFinished(string finalResult)
    {
      Debug.Log($"[NpcVoiceInput] Stream finished with result: {finalResult}");

      _currentTranscription = finalResult;
      _isListening = false;

      // Notify listeners
      OnTranscriptionComplete?.Invoke(finalResult);
      OnListeningStopped?.Invoke();

      // Complete the async listen task
      _listenCompletionSource?.TrySetResult(finalResult);
    }

    private void OnVadChangedHandler(bool isSpeechDetected)
    {
      Debug.Log($"[NpcVoiceInput] VAD changed: isSpeechDetected={isSpeechDetected}");
      OnVoiceActivityChanged?.Invoke(isSpeechDetected);
    }

    /// <summary>
    /// Get the current partial transcription.
    /// </summary>
    /// <returns>The current partial transcription text, or an empty string if no transcription is available.</returns>
    public string GetCurrentTranscription()
    {
      return _currentTranscription;
    }
  }
}
