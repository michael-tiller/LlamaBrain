using System;
using System.Collections.Generic;
using System.Linq;
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
  /// Uses VAD-gated batch transcription for accurate results without processing silence.
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

    [Header("VAD Settings")]
    [SerializeField]
    [Tooltip("Seconds of leading audio to include before VAD detected speech (captures speech onset).")]
    [Range(0.5f, 3f)]
    private float leadingAudioSeconds = 1.0f;

    [SerializeField]
    [Tooltip("Seconds of trailing audio to include after VAD says speech ended.")]
    [Range(0.1f, 1f)]
    private float trailingAudioSeconds = 0.3f;

    [Header("Noise Filtering")]
    [SerializeField]
    [Tooltip("Minimum RMS volume to trigger transcription (prevents transcribing ambient noise).")]
    [Range(0f, 0.1f)]
    private float minVolumeThreshold = 0.01f;

    [SerializeField]
    [Tooltip("Minimum transcription length (characters) to consider valid. Short noise artifacts filtered.")]
    [Range(1, 20)]
    private int minTranscriptionLength = 3;

    [Header("Events")]
    [Tooltip("Fired when a complete transcription is available.")]
    public UnityEvent<string> OnTranscriptionComplete = new UnityEvent<string>();

    [Tooltip("Fired when partial transcription is available (not used in batch mode).")]
    public UnityEvent<string> OnPartialTranscription = new UnityEvent<string>();

    [Tooltip("Fired when listening starts.")]
    public UnityEvent OnListeningStarted = new UnityEvent();

    [Tooltip("Fired when listening stops.")]
    public UnityEvent OnListeningStopped = new UnityEvent();

    [Tooltip("Fired when voice activity is detected.")]
    public UnityEvent<bool> OnVoiceActivityChanged = new UnityEvent<bool>();

    [Tooltip("Fired when a valid speech segment is transcribed.")]
    public UnityEvent<string> OnEarlyTranscription = new UnityEvent<string>();

    [Tooltip("Fired when an error occurs (mic unavailable, transcription failed, etc).")]
    public UnityEvent<string> OnError = new UnityEvent<string>();

    // State
    private bool _isListening;
    private bool _isInitialized;
    private bool _isSpeechDetected;
    private bool _speechEndedDebounced;
    private float _speechEndedTime;
    private const float SpeechEndDebounceSeconds = 0.15f; // Short debounce to catch speech end
    private TaskCompletionSource<string> _listenCompletionSource;
    private bool _isTranscribing; // Prevent concurrent transcriptions
    private bool _hasPendingSpeechSegment; // True when we've captured speech start and waiting for end

    // Audio buffering for batch transcription
    private List<float> _audioBuffer;
    private int _speechStartSampleIndex;
    private int _speechEndedSampleIndex; // Where VAD said speech ended - stop buffering here
    private int _audioFrequency;
    private int _audioChannels;
    private float _maxVolumeDuringSpeech;
    private int _lastAudibleSampleIndex; // Last sample where we detected actual audio above threshold

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
      if (microphoneRecord == null)
      {
        microphoneRecord = gameObject.AddComponent<MicrophoneRecord>();
        ConfigureMicrophone();
      }
    }

    private void Start()
    {
      InitializeWithErrorHandlingAsync().Forget();
    }

    private async UniTaskVoid InitializeWithErrorHandlingAsync()
    {
      try
      {
        await InitializeAsync();
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceInput] Initialization failed: {ex.Message}\n{ex.StackTrace}");
      }
    }

    private void OnDestroy()
    {
      StopListening();

      if (microphoneRecord != null)
      {
        microphoneRecord.OnVadChanged -= OnVadChangedHandler;
        microphoneRecord.OnChunkReady -= OnAudioChunkReady;
      }
    }

    private void Update()
    {
      // Check if speech ended debounce has elapsed
      if (_isListening && !_isSpeechDetected && !_speechEndedDebounced && _speechEndedTime > 0)
      {
        if (Time.time - _speechEndedTime >= SpeechEndDebounceSeconds)
        {
          _speechEndedDebounced = true;
          Debug.Log($"[NpcVoiceInput] Speech end confirmed, triggering batch transcription");

          // Transcribe the captured speech
          TranscribeSpeechSegmentAsync().Forget();
        }
      }
    }

    /// <summary>
    /// Handler for audio chunks - buffers audio only during active speech.
    /// </summary>
    private void OnAudioChunkReady(AudioChunk chunk)
    {
      if (chunk.Data == null || chunk.Data.Length == 0)
        return;

      // Store audio format (always, for initialization)
      _audioFrequency = chunk.Frequency;
      _audioChannels = chunk.Channels;

      // Always buffer audio (we'll trim to speechEndedSampleIndex when transcribing)
      // Buffer audio - convert stereo to mono if needed
      _audioBuffer ??= new List<float>();
      if (chunk.Channels == 2)
      {
        // Stereo: detect which channel(s) have signal and convert to mono
        // Works for: true stereo mics, mono-on-left (Focusrite), mono-on-right, duplicated mono
        float leftRms = 0f, rightRms = 0f;
        var samplePairs = chunk.Data.Length / 2;

        for (var i = 0; i < chunk.Data.Length - 1; i += 2)
        {
          leftRms += chunk.Data[i] * chunk.Data[i];
          rightRms += chunk.Data[i + 1] * chunk.Data[i + 1];
        }

        leftRms = Mathf.Sqrt(leftRms / samplePairs);
        rightRms = Mathf.Sqrt(rightRms / samplePairs);

        // Determine which channel(s) to use based on signal presence
        const float silenceThreshold = 0.001f;
        var hasLeft = leftRms > silenceThreshold;
        var hasRight = rightRms > silenceThreshold;

        for (var i = 0; i < chunk.Data.Length - 1; i += 2)
        {
          float sample;
          if (hasLeft && hasRight)
          {
            // Both channels have signal - average them (true stereo or duplicated)
            sample = (chunk.Data[i] + chunk.Data[i + 1]) * 0.5f;
          }
          else if (hasLeft)
          {
            // Only left channel - use it directly
            sample = chunk.Data[i];
          }
          else if (hasRight)
          {
            // Only right channel - use it directly
            sample = chunk.Data[i + 1];
          }
          else
          {
            // Silence - still need to buffer for timing
            sample = 0f;
          }
          _audioBuffer.Add(sample);
        }
      }
      else
      {
        // Mono: use as-is
        _audioBuffer.AddRange(chunk.Data);
      }

      // Track volume and last audible sample position
      if (_isSpeechDetected || _hasPendingSpeechSegment)
      {
        var sum = 0f;
        for (var i = 0; i < chunk.Data.Length; i++)
        {
          sum += chunk.Data[i] * chunk.Data[i];
        }
        var rms = Mathf.Sqrt(sum / chunk.Data.Length);
        _maxVolumeDuringSpeech = Mathf.Max(_maxVolumeDuringSpeech, rms);

        // Track last position where we heard actual audio (not silence)
        // This gives us a more accurate "speech end" than VAD alone
        const float audibleThreshold = 0.005f;
        if (rms > audibleThreshold)
        {
          _lastAudibleSampleIndex = _audioBuffer.Count;
        }
      }

      // Limit buffer size to prevent memory issues (keep ~30s max)
      var maxSamples = _audioFrequency * maxRecordingLength;
      if (_audioBuffer.Count > maxSamples)
      {
        var excess = _audioBuffer.Count - maxSamples;
        _audioBuffer.RemoveRange(0, excess);
        // Adjust speech start index
        _speechStartSampleIndex = Mathf.Max(0, _speechStartSampleIndex - excess);
      }
    }

    /// <summary>
    /// Transcribe the speech segment captured between VAD start and end.
    /// </summary>
    private async UniTaskVoid TranscribeSpeechSegmentAsync()
    {
      if (_isTranscribing)
      {
        Debug.Log("[NpcVoiceInput] Already transcribing, skipping");
        return;
      }

      if (_audioBuffer == null || _audioBuffer.Count == 0)
      {
        Debug.Log("[NpcVoiceInput] No audio to transcribe");
        return;
      }

      // Check volume threshold
      if (_maxVolumeDuringSpeech < minVolumeThreshold)
      {
        Debug.Log($"[NpcVoiceInput] Volume too low ({_maxVolumeDuringSpeech:F4} < {minVolumeThreshold}), skipping transcription");
        ResetSpeechState();
        return;
      }

      _isTranscribing = true;

      try
      {
        // Use the more accurate of: VAD speech-end or last-audible-sample
        // VAD can be slow to detect end, so we also track where we last heard actual audio
        var trailingSamples = (int)(_audioFrequency * trailingAudioSeconds);

        // Determine effective speech end: prefer last audible sample if available
        int effectiveSpeechEnd;
        if (_lastAudibleSampleIndex > 0)
        {
          // Use last audible sample - more accurate than VAD alone
          effectiveSpeechEnd = _lastAudibleSampleIndex;
        }
        else if (_speechEndedSampleIndex > 0)
        {
          // Fall back to VAD
          effectiveSpeechEnd = _speechEndedSampleIndex;
        }
        else
        {
          // Last resort: use full buffer
          effectiveSpeechEnd = _audioBuffer.Count;
        }

        var segmentEnd = Mathf.Min(effectiveSpeechEnd + trailingSamples, _audioBuffer.Count);
        var segmentLength = segmentEnd;

        if (segmentLength <= 0)
        {
          Debug.Log("[NpcVoiceInput] Speech segment empty");
          return;
        }

        var segmentDuration = segmentLength / (float)_audioFrequency;
        // Extract only the speech portion (not silence accumulated during debounce)
        var speechSegment = new float[segmentLength];
        _audioBuffer.CopyTo(0, speechSegment, 0, segmentLength);
        Debug.Log($"[NpcVoiceInput] Transcribing {segmentLength} mono samples at {_audioFrequency}Hz ({segmentDuration:F2}s), src={_audioChannels}ch, maxVol={_maxVolumeDuringSpeech:F4}, vadEnd={_speechEndedSampleIndex}, audibleEnd={_lastAudibleSampleIndex}");

        // Force single segment output to prevent Whisper from looping/repeating
        whisperManager.singleSegment = true;
        // Initial prompt helps reduce repetition hallucinations
        whisperManager.initialPrompt = "Hello.";

        // Batch transcribe
        var result = await whisperManager.GetTextAsync(speechSegment, _audioFrequency, 1);

        if (result == null)
        {
          Debug.LogWarning("[NpcVoiceInput] Whisper returned null result");
          OnError?.Invoke("Speech recognition failed. Please try again.");
          return;
        }

        var text = result.Result?.Trim();
        Debug.Log($"[NpcVoiceInput] Whisper raw result: \"{text}\"");

        // Filter artifacts and validate
        if (string.IsNullOrWhiteSpace(text) || IsWhisperArtifact(text))
        {
          Debug.Log($"[NpcVoiceInput] Filtered artifact: \"{text}\"");
          return;
        }

        // Clean up: remove artifacts, then remove repetitions
        var cleanedText = StripWhisperArtifacts(text);
        cleanedText = RemoveRepetitions(cleanedText);
        Debug.Log($"[NpcVoiceInput] After cleaning/dedup: \"{cleanedText}\"");

        if (string.IsNullOrWhiteSpace(cleanedText) || cleanedText.Length < minTranscriptionLength)
        {
          Debug.Log($"[NpcVoiceInput] Text too short after cleaning: \"{cleanedText}\"");
          return;
        }

        if (IsLowQualityTranscription(cleanedText))
        {
          Debug.Log($"[NpcVoiceInput] Low quality transcription filtered: \"{cleanedText}\"");
          return;
        }

        Debug.Log($"[NpcVoiceInput] Valid transcription: \"{cleanedText}\"");
        OnEarlyTranscription?.Invoke(cleanedText);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceInput] Transcription failed: {ex.Message}");
        OnError?.Invoke("Speech recognition encountered an error. Please try again.");
      }
      finally
      {
        _isTranscribing = false;
        ResetSpeechState();
      }
    }

    private void ResetSpeechState()
    {
      _speechStartSampleIndex = _audioBuffer?.Count ?? 0;
      _speechEndedSampleIndex = 0;
      _lastAudibleSampleIndex = 0;
      _maxVolumeDuringSpeech = 0f;
      _hasPendingSpeechSegment = false;
    }

    public async UniTask InitializeAsync()
    {
      if (_isInitialized)
        return;

      if (whisperManager == null)
      {
        whisperManager = FindAnyObjectByType<WhisperManager>();
        if (whisperManager == null)
        {
          Debug.LogError("[NpcVoiceInput] No WhisperManager found in scene");
          return;
        }
      }

      while (!whisperManager.IsLoaded && whisperManager.IsLoading)
      {
        await UniTask.Yield();
      }

      if (!whisperManager.IsLoaded)
      {
        Debug.LogError("[NpcVoiceInput] WhisperManager failed to load model");
        OnError?.Invoke("Speech recognition failed to initialize. Check that the Whisper model file exists.");
        return;
      }

      ConfigureMicrophone();
      _isInitialized = true;
      Debug.Log("[NpcVoiceInput] STT system initialized (batch mode)");
    }

    private void ConfigureMicrophone()
    {
      if (microphoneRecord == null)
        return;

      var devices = Microphone.devices;
      Debug.Log($"[NpcVoiceInput] Available microphones ({devices.Length}):");
      for (int i = 0; i < devices.Length; i++)
      {
        Debug.Log($"  [{i}] {devices[i]}");
      }

      if (devices.Length == 0)
      {
        Debug.LogError("[NpcVoiceInput] NO MICROPHONES DETECTED!");
        OnError?.Invoke("No microphone detected. Please connect a microphone and restart.");
        return;
      }

      microphoneRecord.maxLengthSec = maxRecordingLength;
      microphoneRecord.loop = true;
      microphoneRecord.vadStop = false; // Don't auto-stop, we handle VAD ourselves
      microphoneRecord.echo = false;

      microphoneRecord.useVad = true;
      microphoneRecord.vadContextSec = 5f;
      microphoneRecord.vadUpdateRateSec = 0.3f;

      microphoneRecord.OnVadChanged += OnVadChangedHandler;
      microphoneRecord.OnChunkReady += OnAudioChunkReady;

      Debug.Log($"[NpcVoiceInput] Microphone configured for batch transcription");
    }

    public void StartListening()
    {
      Debug.Log($"[NpcVoiceInput] StartListening called");

      if (_isListening)
      {
        Debug.LogWarning("[NpcVoiceInput] Already listening");
        return;
      }

      if (!IsInitialized)
      {
        Debug.LogError("[NpcVoiceInput] Not initialized");
        return;
      }

      _isListening = true;
      _isSpeechDetected = false;
      _speechEndedDebounced = false;
      _speechEndedTime = 0f;
      _audioBuffer?.Clear();
      _audioBuffer ??= new List<float>();
      _speechStartSampleIndex = 0;
      _speechEndedSampleIndex = 0;
      _lastAudibleSampleIndex = 0;
      _maxVolumeDuringSpeech = 0f;
      _hasPendingSpeechSegment = false;

      OnListeningStarted?.Invoke();

      try
      {
        microphoneRecord.StartRecord();
        Debug.Log("[NpcVoiceInput] Started listening (batch mode)");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceInput] Failed to start recording: {ex.Message}");
        OnError?.Invoke("Could not access microphone. Check permissions and try again.");
        _isListening = false;
      }
    }

    public void StopListening()
    {
      if (!_isListening)
        return;

      _isListening = false;

      if (microphoneRecord != null && microphoneRecord.IsRecording)
      {
        microphoneRecord.StopRecord();
      }

      OnListeningStopped?.Invoke();
      Debug.Log("[NpcVoiceInput] Stopped listening");
    }

    public async UniTask<string> ListenForInputAsync(CancellationToken ct = default)
    {
      if (!IsInitialized)
      {
        await InitializeAsync();
      }

      _listenCompletionSource = new TaskCompletionSource<string>();

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
        cancellationRegistration.Dispose();
      }
    }

    private void OnVadChangedHandler(bool isSpeechDetected)
    {
      if (_isSpeechDetected == isSpeechDetected)
        return;

      _isSpeechDetected = isSpeechDetected;
      Debug.Log($"[NpcVoiceInput] VAD: {(isSpeechDetected ? "speech started" : "speech ended")} (pending={_hasPendingSpeechSegment})");
      OnVoiceActivityChanged?.Invoke(isSpeechDetected);

      if (isSpeechDetected)
      {
        // Speech started - resume buffering and mark position if no pending segment
        _speechEndedDebounced = false;
        _speechEndedTime = 0f;
        // Reset speech end marker so we capture the real end after this speech
        _speechEndedSampleIndex = 0;

        if (!_hasPendingSpeechSegment)
        {
          // Clear buffer and start fresh - this ensures we only capture actual speech
          // Keep some leading audio for context
          var leadingSamples = (int)(_audioFrequency * leadingAudioSeconds);
          if (_audioBuffer != null && _audioBuffer.Count > leadingSamples)
          {
            // Keep only the leading audio
            var keepFrom = _audioBuffer.Count - leadingSamples;
            _audioBuffer.RemoveRange(0, keepFrom);
          }
          _speechStartSampleIndex = 0; // Start of buffer is now start of speech context
          _lastAudibleSampleIndex = 0;
          _maxVolumeDuringSpeech = 0f;
          _hasPendingSpeechSegment = true;
          Debug.Log($"[NpcVoiceInput] NEW speech segment - buffer cleared, keeping {_audioBuffer?.Count ?? 0} leading samples");
        }
        else
        {
          Debug.Log($"[NpcVoiceInput] VAD flicker - continuing existing segment");
        }
      }
      else
      {
        // Speech ended - mark sample index and start debounce timer
        _speechEndedSampleIndex = _audioBuffer?.Count ?? 0;
        _speechEndedTime = Time.time;
        _speechEndedDebounced = false;
        Debug.Log($"[NpcVoiceInput] Speech ended at sample {_speechEndedSampleIndex}");
      }
    }

    public string GetCurrentTranscription()
    {
      return ""; // Not applicable in batch mode
    }

    #region Filtering

    /// <summary>
    /// Removes Whisper hallucinations including:
    /// - Exact repetitions: "testing testing testing" → "testing"
    /// - Fragment repetitions: "Bing bing bingo go go" → "bingo" (fragments of the same word)
    /// Runs in a loop until stable to catch nested/cascading repetitions.
    /// </summary>
    private static string RemoveRepetitions(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return text;

      // Run dedup in a loop until stable (catches cascading repetitions like "one two one two two two")
      var current = text;
      for (var iteration = 0; iteration < 5; iteration++)
      {
        var deduped = RemoveRepetitionsSinglePass(current);
        if (deduped == current)
          break;
        current = deduped;
      }
      return current;
    }

    private static string RemoveRepetitionsSinglePass(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return text;

      var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (words.Length == 0)
        return text;

      // Normalize: strip punctuation, lowercase
      string Normalize(string w) => w.Trim().TrimEnd('.', ',', '!', '?', ';', ':').ToLowerInvariant();

      var normalized = words.Select(Normalize).ToArray();

      // First pass: detect if this looks like fragmented repetition of a single word
      // e.g., "Bing bing bingo go go" - all fragments of "bingo"
      if (words.Length >= 3)
      {
        // Find the longest word - it's likely the real one
        var longestIdx = 0;
        var longestLen = normalized[0].Length;
        for (var i = 1; i < normalized.Length; i++)
        {
          if (normalized[i].Length > longestLen)
          {
            longestLen = normalized[i].Length;
            longestIdx = i;
          }
        }

        var longest = normalized[longestIdx];
        if (longest.Length >= 3)
        {
          // Check if other words are fragments of the longest word
          var fragmentCount = 0;
          foreach (var word in normalized)
          {
            if (word == longest)
              continue;
            // Is this word a substring of longest, or is longest a substring of this?
            if (longest.Contains(word) || word.Contains(longest))
              fragmentCount++;
            // Also check if they share a significant prefix/suffix
            else if (word.Length >= 2 && longest.Length >= 2)
            {
              var minLen = Math.Min(word.Length, longest.Length);
              var sharedPrefix = 0;
              var sharedSuffix = 0;
              for (var i = 0; i < minLen; i++)
              {
                if (word[i] == longest[i]) sharedPrefix++;
                else break;
              }
              for (var i = 0; i < minLen; i++)
              {
                if (word[word.Length - 1 - i] == longest[longest.Length - 1 - i]) sharedSuffix++;
                else break;
              }
              if (sharedPrefix >= 2 || sharedSuffix >= 2)
                fragmentCount++;
            }
          }

          // If most words are fragments of the longest, just return the longest
          if (fragmentCount >= (words.Length - 1) / 2)
          {
            Debug.Log($"[NpcVoiceInput] Detected fragment hallucination, extracting: \"{words[longestIdx]}\"");
            return words[longestIdx];
          }
        }
      }

      // Second pass: remove exact repetitions
      var result = new List<string>();
      var idx = 0;

      while (idx < words.Length)
      {
        var foundRepeat = false;

        for (var patternLen = 1; patternLen <= 4 && patternLen <= (words.Length - idx) / 2; patternLen++)
        {
          var repeatCount = 1;
          var j = idx + patternLen;

          while (j + patternLen <= words.Length)
          {
            var matches = true;
            for (var k = 0; k < patternLen; k++)
            {
              if (normalized[idx + k] != normalized[j + k])
              {
                matches = false;
                break;
              }
            }

            if (matches)
            {
              repeatCount++;
              j += patternLen;
            }
            else
            {
              break;
            }
          }

          if (repeatCount >= 2)
          {
            for (var k = 0; k < patternLen; k++)
            {
              result.Add(words[idx + k]);
            }
            idx = j;
            foundRepeat = true;
            break;
          }
        }

        if (!foundRepeat)
        {
          result.Add(words[idx]);
          idx++;
        }
      }

      return string.Join(" ", result);
    }

    private static string StripWhisperArtifacts(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return "";

      var cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", "");
      cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\([^)]*\)", "");
      cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();

      return cleaned;
    }

    private static bool IsLowQualityTranscription(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return true;

      var trimmed = text.Trim();

      if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[\s\p{P}]+$"))
        return true;

      if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"(.)\1{4,}"))
        return true;

      var lowerTrimmed = trimmed.ToLowerInvariant();
      if (lowerTrimmed.Contains("thank you for watching") ||
          lowerTrimmed.Contains("thanks for watching") ||
          lowerTrimmed.Contains("please subscribe") ||
          lowerTrimmed.Contains("like and subscribe") ||
          lowerTrimmed.Contains("see you next time") ||
          lowerTrimmed.Contains("goodbye") && trimmed.Length < 15)
        return true;

      return false;
    }

    private static bool IsWhisperArtifact(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return true;

      var trimmed = text.Trim();
      var lower = trimmed.ToLowerInvariant();

      if (trimmed.Equals("[BLANK_AUDIO]", StringComparison.OrdinalIgnoreCase)
          || trimmed.Equals("[MUSIC]", StringComparison.OrdinalIgnoreCase)
          || trimmed.Equals("[APPLAUSE]", StringComparison.OrdinalIgnoreCase)
          || trimmed.Equals("[LAUGHTER]", StringComparison.OrdinalIgnoreCase)
          || trimmed.Equals("[SILENCE]", StringComparison.OrdinalIgnoreCase)
          || trimmed.Equals("[NOISE]", StringComparison.OrdinalIgnoreCase)
          || trimmed.Equals("[INAUDIBLE]", StringComparison.OrdinalIgnoreCase))
        return true;

      if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\[.*\]$"))
        return true;

      if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\(.*\)$"))
      {
        if (lower.Contains("music") || lower.Contains("inaudible") ||
            lower.Contains("silence") || lower.Contains("noise") ||
            lower.Contains("applause") || lower.Contains("laughter") ||
            lower.Contains("cough") || lower.Contains("sigh") ||
            lower.Contains("breathing") || lower.Contains("footsteps") ||
            lower.Contains("typing") || lower.Contains("keyboard") ||
            lower.Contains("clicking"))
          return true;
      }

      if ((trimmed.StartsWith("(") && trimmed.EndsWith(")")) ||
          (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
        return true;

      if (lower == "you" || lower == "the" || lower == "a" || lower == "i" ||
          lower == "." || lower == "," || lower == "?" || lower == "!")
        return true;

      return false;
    }

    #endregion
  }
}
