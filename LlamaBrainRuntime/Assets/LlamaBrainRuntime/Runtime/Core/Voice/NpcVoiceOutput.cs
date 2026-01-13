using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Events;
using uPiper.Core;
using uPiper.Core.AudioGeneration;
using Debug = UnityEngine.Debug;
#if !UNITY_WEBGL
using uPiper.Core.Phonemizers;
using uPiper.Core.Phonemizers.Implementations;
#endif

namespace LlamaBrain.Runtime.Core.Voice
{
  /// <summary>
  /// Handles text-to-speech output using Piper TTS.
  /// Wraps the Piper audio generation pipeline for NPC voice output.
  /// </summary>
  [RequireComponent(typeof(AudioSource))]
  public class NpcVoiceOutput : MonoBehaviour
  {
    [Header("Configuration")]
    [SerializeField]
    [Tooltip("Voice configuration for this NPC.")]
    private NpcSpeechConfig speechConfig;

    [Header("Events")]
    [Tooltip("Fired when the NPC starts speaking.")]
    /// <summary>
    /// Event fired when the NPC starts speaking.
    /// </summary>
    public UnityEvent OnSpeakingStarted = new UnityEvent();

    [Tooltip("Fired when the NPC finishes speaking.")]
    /// <summary>
    /// Event fired when the NPC finishes speaking.
    /// </summary>
    public UnityEvent OnSpeakingFinished = new UnityEvent();

    [Tooltip("Fired when audio generation fails.")]
    /// <summary>
    /// Event fired when audio generation fails. The string parameter contains the error message.
    /// </summary>
    public UnityEvent<string> OnSpeakingFailed = new UnityEvent<string>();

    private AudioSource _audioSource;
    private AsyncAudioGenerator _generator;
    private PhonemeEncoder _encoder;
    private AudioClipBuilder _audioBuilder;
    private AudioCache _audioCache;
    private PiperVoiceConfig _currentVoiceConfig;
    private string _loadedModelName;
    private bool _isInitialized;
    private bool _isSpeaking;

    // Semaphore to prevent concurrent TTS generation which can cause deadlocks
    private static readonly System.Threading.SemaphoreSlim _ttsSemaphore = new(1, 1);

    // Timeout for audio playback polling to prevent infinite hangs
    private const float AUDIO_PLAYBACK_TIMEOUT_SECONDS = 60f;

    // Queue for streaming audio clips (sentence-by-sentence playback)
    private readonly ConcurrentQueue<AudioClip> _audioClipQueue = new();
    private volatile bool _isGeneratingMore;

    // Regex for sentence splitting - matches sentence-ending punctuation followed by space or end of string
    private static readonly Regex SentenceSplitRegex = new Regex(
        @"(?<=[.!?])\s+",
        RegexOptions.Compiled);

#if !UNITY_WEBGL
    private ITextPhonemizer _japanesePhonemizer;
    private uPiper.Core.Phonemizers.Backend.Flite.FliteLTSPhonemizer _englishPhonemizer;
#endif

    /// <summary>
    /// Whether the NPC is currently speaking.
    /// </summary>
    public bool IsSpeaking => _isSpeaking;

    /// <summary>
    /// Whether the TTS system is initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// The current speech configuration.
    /// </summary>
    public NpcSpeechConfig SpeechConfig
    {
      get => speechConfig;
      set => speechConfig = value;
    }

    private void Awake()
    {
      _audioSource = GetComponent<AudioSource>();
      _generator = new AsyncAudioGenerator();
      _audioBuilder = new AudioClipBuilder();
      // AudioCache initialized lazily in InitializeAsync to use config values
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
        Debug.LogError($"[NpcVoiceOutput] Initialization failed: {ex.Message}\n{ex.StackTrace}");
        OnSpeakingFailed?.Invoke($"TTS initialization failed: {ex.Message}");
      }
    }

    private void OnDestroy()
    {
      _generator?.Dispose();
      _audioCache?.Dispose();
#if !UNITY_WEBGL
      if (_japanesePhonemizer is TextPhonemizerAdapter adapter)
      {
        var field = adapter.GetType().GetField("_phonemizer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field?.GetValue(adapter) is IDisposable disposable)
        {
          disposable.Dispose();
        }
      }
      _englishPhonemizer?.Dispose();
#endif
    }

    /// <summary>
    /// Initialize the TTS system.
    /// </summary>
    /// <returns>A task that completes when initialization is finished.</returns>
    public async UniTask InitializeAsync()
    {
      if (_isInitialized)
        return;

#if !UNITY_WEBGL
      // Initialize Japanese phonemizer
      try
      {
        var openJTalk = new OpenJTalkPhonemizer();
        _japanesePhonemizer = new TextPhonemizerAdapter(openJTalk);
        Debug.Log("[NpcVoiceOutput] OpenJTalk phonemizer initialized");
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[NpcVoiceOutput] Failed to initialize OpenJTalk: {ex.Message}");
        _japanesePhonemizer = null;
      }

      // Initialize English phonemizer
      await InitializeEnglishPhonemizerAsync();
#endif

      // Initialize audio cache if caching is enabled
      if (speechConfig != null && speechConfig.EnableAudioCaching && _audioCache == null)
      {
        _audioCache = new AudioCache(speechConfig.AudioCacheMaxSizeMB);
        Debug.Log($"[NpcVoiceOutput] Audio cache initialized with {speechConfig.AudioCacheMaxSizeMB} MB limit");
      }

      _isInitialized = true;
      Debug.Log("[NpcVoiceOutput] TTS system initialized");

      // Pre-warm cache if enabled (runs in background, doesn't block)
      if (speechConfig != null && speechConfig.PreWarmCacheOnInit)
      {
        PreWarmCacheInBackgroundAsync().Forget();
      }
    }

    private async UniTaskVoid PreWarmCacheInBackgroundAsync()
    {
      try
      {
        // Small delay to allow other initialization to complete first
        await UniTask.Delay(100);
        await PreWarmCacheAsync();
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[NpcVoiceOutput] Cache pre-warming failed: {ex.Message}");
      }
    }

#if !UNITY_WEBGL
    private async Task InitializeEnglishPhonemizerAsync()
    {
      try
      {
        _englishPhonemizer = new uPiper.Core.Phonemizers.Backend.Flite.FliteLTSPhonemizer();
        var dictPath = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            "uPiper",
            "Phonemizers",
            "cmudict-0.7b.txt"
        );

        var options = new uPiper.Core.Phonemizers.Backend.PhonemizerBackendOptions
        {
          DataPath = dictPath
        };

        var initialized = await _englishPhonemizer.InitializeAsync(options);
        if (initialized)
        {
          Debug.Log("[NpcVoiceOutput] Flite LTS phonemizer initialized");
        }
        else
        {
          Debug.LogWarning("[NpcVoiceOutput] Failed to initialize Flite LTS phonemizer");
          _englishPhonemizer = null;
        }
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[NpcVoiceOutput] Failed to initialize English phonemizer: {ex.Message}");
        _englishPhonemizer = null;
      }
    }
#endif

    /// <summary>
    /// Speak the given text using TTS.
    /// </summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="ct">Cancellation token to cancel the speaking operation.</param>
    /// <returns>A task that completes when the speech finishes or is cancelled.</returns>
    public async UniTask SpeakAsync(string text, CancellationToken ct = default)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        Debug.LogWarning("[NpcVoiceOutput] Cannot speak empty text");
        return;
      }

      if (speechConfig == null)
      {
        Debug.LogError("[NpcVoiceOutput] No speech config assigned");
        OnSpeakingFailed?.Invoke("No speech config assigned");
        return;
      }

      // Validate and handle text length
      var processedText = ValidateAndProcessTextLength(text);
      if (processedText == null)
      {
        // Text was rejected due to length
        return;
      }
      text = processedText;

      if (!_isInitialized)
      {
        await InitializeAsync();
      }

      // Acquire semaphore to prevent concurrent TTS generation
      Debug.Log("[NpcVoiceOutput] Waiting for TTS semaphore...");
      var semaphoreAcquired = await _ttsSemaphore.WaitAsync(TimeSpan.FromSeconds(30), ct);
      if (!semaphoreAcquired)
      {
        Debug.LogWarning("[NpcVoiceOutput] TTS semaphore timeout - another TTS operation is taking too long");
        OnSpeakingFailed?.Invoke("TTS busy - please wait");
        return;
      }

      // Try block MUST start immediately after acquiring semaphore to ensure release
      try
      {
        _isSpeaking = true;
        OnSpeakingStarted?.Invoke();

        var generationStopwatch = Stopwatch.StartNew();

        var modelName = speechConfig.GetModelName();
        var language = speechConfig.GetLanguage();

        // Load model if needed
        await LoadModelIfNeededAsync(modelName);

        // Yield to allow UI to update before heavy inference work
        await UniTask.Yield();

        // Convert text to phonemes
        var phonemes = await PhonemizeTextAsync(text, language, ct);
        if (phonemes == null || phonemes.Length == 0)
        {
          throw new Exception("Failed to generate phonemes from text");
        }

        // Encode phonemes to IDs
        var phonemeIds = _encoder.Encode(phonemes);
        if (phonemeIds == null || phonemeIds.Length == 0)
        {
          throw new Exception("Failed to encode phonemes");
        }

        // Check audio cache first
        float[] audioData;
        string cacheKey = null;

        if (speechConfig.EnableAudioCaching && _audioCache != null)
        {
          cacheKey = AudioCache.ComputeCacheKey(
              text,
              modelName,
              speechConfig.LengthScale,
              speechConfig.NoiseScale,
              speechConfig.NoiseW);

          if (_audioCache.TryGet(cacheKey, out var cachedAudio, out _))
          {
            audioData = cachedAudio;
            Debug.Log($"[NpcVoiceOutput] Cache hit for: \"{text.Substring(0, Math.Min(30, text.Length))}...\"");
          }
          else
          {
            // Yield before inference to keep UI responsive
            await UniTask.Yield();

            // Generate audio
            audioData = await _generator.GenerateAudioAsync(
                phonemeIds,
                lengthScale: speechConfig.LengthScale,
                noiseScale: speechConfig.NoiseScale,
                noiseW: speechConfig.NoiseW
            );

            // Store in cache
            if (audioData != null && audioData.Length > 0)
            {
              _audioCache.Set(cacheKey, audioData, _currentVoiceConfig.SampleRate);
            }
          }
        }
        else
        {
          // Yield before inference to keep UI responsive
          await UniTask.Yield();

          // Generate audio without caching
          audioData = await _generator.GenerateAudioAsync(
              phonemeIds,
              lengthScale: speechConfig.LengthScale,
              noiseScale: speechConfig.NoiseScale,
              noiseW: speechConfig.NoiseW
          );
        }

        ct.ThrowIfCancellationRequested();

        if (audioData == null || audioData.Length == 0)
        {
          throw new InvalidOperationException("Audio generation returned empty data");
        }

        var generationTimeMs = generationStopwatch.ElapsedMilliseconds;
        Debug.Log($"[PERF] TTS generation completed in {generationTimeMs}ms");

        Debug.Log($"[NpcVoiceOutput] Processing audio: {audioData.Length} samples");

        // Process audio (normalize if needed)
        float[] processedAudio;
        var maxVal = audioData.Max(x => Math.Abs(x));
        Debug.Log($"[NpcVoiceOutput] Max amplitude: {maxVal}");

        if (speechConfig.NormalizeAudio && maxVal > 1.0f)
        {
          processedAudio = _audioBuilder.NormalizeAudio(audioData, 0.95f);
        }
        else if (maxVal < 0.01f)
        {
          // Audio too quiet, amplify
          if (maxVal <= float.Epsilon)
          {
            // Skip amplification if maxVal is zero or near-zero
            processedAudio = audioData.ToArray();
          }
          else
          {
            var amplificationFactor = 0.3f / maxVal;
            processedAudio = audioData.Select(x => x * amplificationFactor).ToArray();
          }
        }
        else
        {
          processedAudio = audioData;
        }

        // Note: Volume is now controlled via AudioSource.volume in PlayWithFadeAsync
        // to enable proper fade-in/fade-out. This also allows cached audio to be
        // reused at different volume levels.

        // Build AudioClip
        Debug.Log($"[NpcVoiceOutput] Building AudioClip, sampleRate={_currentVoiceConfig?.SampleRate ?? 0}");
        var audioClip = _audioBuilder.BuildAudioClip(
            processedAudio,
            _currentVoiceConfig.SampleRate,
            $"NpcSpeech_{DateTime.Now:HHmmss}"
        );
        Debug.Log($"[NpcVoiceOutput] AudioClip built: {audioClip?.length ?? 0}s");

        ct.ThrowIfCancellationRequested();

        // Apply spatial audio settings
        ApplySpatialSettings();

        // Play audio with fade support
        Debug.Log("[NpcVoiceOutput] Starting playback...");
        var playbackStopwatch = Stopwatch.StartNew();
        _audioSource.clip = audioClip;
        await PlayWithFadeAsync(ct);
        var playbackTimeMs = playbackStopwatch.ElapsedMilliseconds;

        Debug.Log($"[PERF] TTS playback completed in {playbackTimeMs}ms (clip length: {audioClip?.length ?? 0:F2}s)");
        Debug.Log($"[NpcVoiceOutput] Finished speaking: \"{text.Substring(0, Math.Min(50, text.Length))}...\"");
      }
      catch (OperationCanceledException)
      {
        Debug.Log("[NpcVoiceOutput] Speaking cancelled");
        throw;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceOutput] Failed to speak: {ex.Message}");
        OnSpeakingFailed?.Invoke(ex.Message);
      }
      finally
      {
        Debug.Log("[NpcVoiceOutput] Entering finally block");
        _isSpeaking = false;
        Debug.Log("[NpcVoiceOutput] Releasing semaphore...");
        _ttsSemaphore.Release();
        Debug.Log("[NpcVoiceOutput] TTS semaphore released, invoking OnSpeakingFinished...");
        OnSpeakingFinished?.Invoke();
        Debug.Log("[NpcVoiceOutput] OnSpeakingFinished invoked, exiting finally");
      }
    }

    /// <summary>
    /// Speak the given text using TTS with sentence-level streaming.
    /// Starts playback immediately after generating the first sentence,
    /// while remaining sentences are generated in the background.
    /// </summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="ct">Cancellation token to cancel the speaking operation.</param>
    /// <returns>A task that completes when the speech finishes or is cancelled.</returns>
    public async UniTask SpeakStreamingAsync(string text, CancellationToken ct = default)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        Debug.LogWarning("[NpcVoiceOutput] Cannot speak empty text");
        return;
      }

      if (speechConfig == null)
      {
        Debug.LogError("[NpcVoiceOutput] No speech config assigned");
        OnSpeakingFailed?.Invoke("No speech config assigned");
        return;
      }

      // Validate and handle text length
      var processedText = ValidateAndProcessTextLength(text);
      if (processedText == null)
      {
        return;
      }
      text = processedText;

      if (!_isInitialized)
      {
        await InitializeAsync();
      }

      // Split text into sentences
      var sentences = SplitIntoSentences(text);
      if (sentences.Length == 0)
      {
        Debug.LogWarning("[NpcVoiceOutput] No sentences to speak");
        return;
      }

      // For single sentence, use non-streaming path (simpler)
      if (sentences.Length == 1)
      {
        await SpeakAsync(text, ct);
        return;
      }

      Debug.Log($"[NpcVoiceOutput] Streaming {sentences.Length} sentences");

      // Acquire semaphore to prevent concurrent TTS generation
      Debug.Log("[NpcVoiceOutput] Waiting for TTS semaphore (streaming)...");
      var semaphoreAcquired = await _ttsSemaphore.WaitAsync(TimeSpan.FromSeconds(30), ct);
      if (!semaphoreAcquired)
      {
        Debug.LogWarning("[NpcVoiceOutput] TTS semaphore timeout - another TTS operation is taking too long");
        OnSpeakingFailed?.Invoke("TTS busy - please wait");
        return;
      }

      try
      {
        _isSpeaking = true;
        OnSpeakingStarted?.Invoke();

        var overallStopwatch = Stopwatch.StartNew();
        var modelName = speechConfig.GetModelName();
        var language = speechConfig.GetLanguage();

        // Load model if needed
        await LoadModelIfNeededAsync(modelName);

        // Clear any leftover clips from previous runs
        while (_audioClipQueue.TryDequeue(out _)) { }

        // Generate first sentence immediately
        var firstSentenceStopwatch = Stopwatch.StartNew();
        var firstClip = await GenerateSentenceAudioAsync(sentences[0], modelName, language, ct);
        var firstSentenceTimeMs = firstSentenceStopwatch.ElapsedMilliseconds;

        if (firstClip == null)
        {
          throw new InvalidOperationException("Failed to generate audio for first sentence");
        }

        Debug.Log($"[PERF] First sentence TTS completed in {firstSentenceTimeMs}ms");

        // Start background generation for remaining sentences
        _isGeneratingMore = sentences.Length > 1;
        var backgroundTask = GenerateRemainingSentencesAsync(sentences, 1, modelName, language, ct);

        // Start playback of first sentence immediately
        ApplySpatialSettings();
        _audioSource.clip = firstClip;
        _audioSource.volume = speechConfig.Volume;
        _audioSource.Play();

        Debug.Log($"[PERF] Time to first audio: {overallStopwatch.ElapsedMilliseconds}ms");

        // Play clips as they become available
        await PlayQueuedClipsAsync(ct);

        // Wait for background generation to complete
        await backgroundTask;

        var totalTimeMs = overallStopwatch.ElapsedMilliseconds;
        Debug.Log($"[PERF] Total streaming TTS completed in {totalTimeMs}ms");
        Debug.Log($"[NpcVoiceOutput] Finished speaking (streaming): \"{text.Substring(0, Math.Min(50, text.Length))}...\"");
      }
      catch (OperationCanceledException)
      {
        Debug.Log("[NpcVoiceOutput] Speaking cancelled (streaming)");
        throw;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceOutput] Failed to speak (streaming): {ex.Message}");
        OnSpeakingFailed?.Invoke(ex.Message);
      }
      finally
      {
        _isSpeaking = false;
        _isGeneratingMore = false;
        while (_audioClipQueue.TryDequeue(out _)) { } // Clear queue
        _ttsSemaphore.Release();
        OnSpeakingFinished?.Invoke();
      }
    }

    /// <summary>
    /// Splits text into sentences at sentence-ending punctuation.
    /// </summary>
    private static string[] SplitIntoSentences(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return Array.Empty<string>();

      // Split at sentence boundaries
      var sentences = SentenceSplitRegex.Split(text)
          .Select(s => s.Trim())
          .Where(s => !string.IsNullOrWhiteSpace(s))
          .ToArray();

      // If no split happened (no sentence-ending punctuation), return as single sentence
      if (sentences.Length == 0)
        return new[] { text.Trim() };

      return sentences;
    }

    /// <summary>
    /// Generates audio for a single sentence.
    /// </summary>
    private async UniTask<AudioClip> GenerateSentenceAudioAsync(
        string sentence,
        string modelName,
        string language,
        CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();

      // Yield to allow UI to update
      await UniTask.Yield();

      // Convert text to phonemes
      var phonemes = await PhonemizeTextAsync(sentence, language, ct);
      if (phonemes == null || phonemes.Length == 0)
      {
        Debug.LogWarning($"[NpcVoiceOutput] Failed to phonemize: \"{sentence}\"");
        return null;
      }

      // Encode phonemes to IDs
      var phonemeIds = _encoder.Encode(phonemes);
      if (phonemeIds == null || phonemeIds.Length == 0)
      {
        Debug.LogWarning($"[NpcVoiceOutput] Failed to encode phonemes for: \"{sentence}\"");
        return null;
      }

      // Check cache first
      float[] audioData;
      if (speechConfig.EnableAudioCaching && _audioCache != null)
      {
        var cacheKey = AudioCache.ComputeCacheKey(
            sentence,
            modelName,
            speechConfig.LengthScale,
            speechConfig.NoiseScale,
            speechConfig.NoiseW);

        if (_audioCache.TryGet(cacheKey, out var cachedAudio, out _))
        {
          audioData = cachedAudio;
          Debug.Log($"[NpcVoiceOutput] Cache hit for sentence: \"{sentence.Substring(0, Math.Min(30, sentence.Length))}...\"");
        }
        else
        {
          await UniTask.Yield();
          audioData = await _generator.GenerateAudioAsync(
              phonemeIds,
              lengthScale: speechConfig.LengthScale,
              noiseScale: speechConfig.NoiseScale,
              noiseW: speechConfig.NoiseW
          );

          if (audioData != null && audioData.Length > 0)
          {
            _audioCache.Set(cacheKey, audioData, _currentVoiceConfig.SampleRate);
          }
        }
      }
      else
      {
        await UniTask.Yield();
        audioData = await _generator.GenerateAudioAsync(
            phonemeIds,
            lengthScale: speechConfig.LengthScale,
            noiseScale: speechConfig.NoiseScale,
            noiseW: speechConfig.NoiseW
        );
      }

      ct.ThrowIfCancellationRequested();

      if (audioData == null || audioData.Length == 0)
      {
        Debug.LogWarning($"[NpcVoiceOutput] Empty audio generated for: \"{sentence}\"");
        return null;
      }

      // Process audio (normalize if needed)
      float[] processedAudio;
      var maxVal = audioData.Max(x => Math.Abs(x));

      if (speechConfig.NormalizeAudio && maxVal > 1.0f)
      {
        processedAudio = _audioBuilder.NormalizeAudio(audioData, 0.95f);
      }
      else if (maxVal < 0.01f && maxVal > float.Epsilon)
      {
        var amplificationFactor = 0.3f / maxVal;
        processedAudio = audioData.Select(x => x * amplificationFactor).ToArray();
      }
      else
      {
        processedAudio = audioData;
      }

      // Build AudioClip
      var audioClip = _audioBuilder.BuildAudioClip(
          processedAudio,
          _currentVoiceConfig.SampleRate,
          $"NpcSpeech_Sentence_{DateTime.Now:HHmmss}"
      );

      return audioClip;
    }

    /// <summary>
    /// Generates audio for remaining sentences in background and queues them.
    /// </summary>
    private async UniTask GenerateRemainingSentencesAsync(
        string[] sentences,
        int startIndex,
        string modelName,
        string language,
        CancellationToken ct)
    {
      try
      {
        for (int i = startIndex; i < sentences.Length; i++)
        {
          ct.ThrowIfCancellationRequested();

          var clip = await GenerateSentenceAudioAsync(sentences[i], modelName, language, ct);
          if (clip != null)
          {
            _audioClipQueue.Enqueue(clip);
            Debug.Log($"[NpcVoiceOutput] Queued sentence {i + 1}/{sentences.Length}");
          }
        }
      }
      finally
      {
        _isGeneratingMore = false;
      }
    }

    /// <summary>
    /// Plays the current clip and any queued clips seamlessly.
    /// </summary>
    private async UniTask PlayQueuedClipsAsync(CancellationToken ct)
    {
      var startTime = Time.realtimeSinceStartup;

      while (true)
      {
        ct.ThrowIfCancellationRequested();

        // Check timeout
        var elapsed = Time.realtimeSinceStartup - startTime;
        if (elapsed > AUDIO_PLAYBACK_TIMEOUT_SECONDS)
        {
          Debug.LogError($"[NpcVoiceOutput] Streaming playback timeout after {elapsed:F1}s");
          _audioSource.Stop();
          break;
        }

        // Wait for current clip to finish
        if (_audioSource.isPlaying)
        {
          await UniTask.Yield();
          continue;
        }

        // Current clip finished, try to get next
        if (_audioClipQueue.TryDequeue(out var clip))
        {
          _audioSource.clip = clip;
          _audioSource.Play();
          Debug.Log($"[NpcVoiceOutput] Playing next queued clip ({clip.length:F2}s)");
        }
        else if (_isGeneratingMore)
        {
          // Wait for more clips to be generated
          await UniTask.Yield();
        }
        else
        {
          // All done
          break;
        }
      }
    }

    /// <summary>
    /// Stop speaking immediately.
    /// </summary>
    public void Stop()
    {
      if (_audioSource != null && _audioSource.isPlaying)
      {
        _audioSource.Stop();
      }
      _isSpeaking = false;
    }

    /// <summary>
    /// Gets statistics about the audio cache.
    /// </summary>
    /// <returns>Cache statistics, or null if caching is disabled.</returns>
    public AudioCache.AudioCacheStatistics? GetCacheStatistics()
    {
      return _audioCache?.GetStatistics();
    }

    /// <summary>
    /// Clears the audio cache.
    /// </summary>
    public void ClearCache()
    {
      _audioCache?.Clear();
      Debug.Log("[NpcVoiceOutput] Audio cache cleared");
    }

    /// <summary>
    /// Pre-warms the audio cache with common phrases for faster response.
    /// Call this during scene load or idle time for best results.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A task that completes when pre-warming is done.</returns>
    public async UniTask PreWarmCacheAsync(CancellationToken ct = default)
    {
      if (!speechConfig.EnableAudioCaching || _audioCache == null)
      {
        Debug.Log("[NpcVoiceOutput] Cache pre-warming skipped (caching disabled)");
        return;
      }

      if (!_isInitialized)
      {
        await InitializeAsync();
      }

      var modelName = speechConfig.GetModelName();
      await LoadModelIfNeededAsync(modelName);

      // Common phrases to pre-warm (short, likely responses)
      var commonPhrases = new[]
      {
        // Greetings
        "Hello.",
        "Hi there.",
        "Welcome.",
        "Greetings.",

        // Acknowledgments
        "I understand.",
        "Of course.",
        "Certainly.",
        "Yes.",
        "No.",
        "Okay.",

        // Thinking indicators
        "Let me think.",
        "Hmm.",
        "Well.",
        "Interesting.",

        // Common responses
        "I don't know.",
        "I'm not sure.",
        "Could you repeat that?",
        "Tell me more.",
        "Go on.",
        "I see."
      };

      Debug.Log($"[NpcVoiceOutput] Pre-warming cache with {commonPhrases.Length} common phrases...");
      var stopwatch = Stopwatch.StartNew();
      var cachedCount = 0;
      var language = speechConfig.GetLanguage();

      foreach (var phrase in commonPhrases)
      {
        ct.ThrowIfCancellationRequested();

        // Check if already cached
        var cacheKey = AudioCache.ComputeCacheKey(
            phrase,
            modelName,
            speechConfig.LengthScale,
            speechConfig.NoiseScale,
            speechConfig.NoiseW);

        if (_audioCache.TryGet(cacheKey, out _, out _))
        {
          // Already in cache
          continue;
        }

        try
        {
          // Yield to keep UI responsive
          await UniTask.Yield();

          // Generate audio for this phrase
          var phonemes = await PhonemizeTextAsync(phrase, language, ct);
          if (phonemes == null || phonemes.Length == 0)
            continue;

          var phonemeIds = _encoder.Encode(phonemes);
          if (phonemeIds == null || phonemeIds.Length == 0)
            continue;

          var audioData = await _generator.GenerateAudioAsync(
              phonemeIds,
              lengthScale: speechConfig.LengthScale,
              noiseScale: speechConfig.NoiseScale,
              noiseW: speechConfig.NoiseW
          );

          if (audioData != null && audioData.Length > 0)
          {
            _audioCache.Set(cacheKey, audioData, _currentVoiceConfig.SampleRate);
            cachedCount++;
          }
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[NpcVoiceOutput] Failed to pre-warm phrase '{phrase}': {ex.Message}");
        }
      }

      Debug.Log($"[NpcVoiceOutput] Cache pre-warming complete: {cachedCount} phrases in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Pre-warms the audio cache with custom phrases.
    /// Use this for NPC-specific phrases that are likely to be used.
    /// </summary>
    /// <param name="phrases">Array of phrases to pre-warm.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A task that completes when pre-warming is done.</returns>
    public async UniTask PreWarmCacheAsync(string[] phrases, CancellationToken ct = default)
    {
      if (!speechConfig.EnableAudioCaching || _audioCache == null)
      {
        Debug.Log("[NpcVoiceOutput] Cache pre-warming skipped (caching disabled)");
        return;
      }

      if (phrases == null || phrases.Length == 0)
        return;

      if (!_isInitialized)
      {
        await InitializeAsync();
      }

      var modelName = speechConfig.GetModelName();
      await LoadModelIfNeededAsync(modelName);

      Debug.Log($"[NpcVoiceOutput] Pre-warming cache with {phrases.Length} custom phrases...");
      var stopwatch = Stopwatch.StartNew();
      var cachedCount = 0;
      var language = speechConfig.GetLanguage();

      foreach (var phrase in phrases)
      {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(phrase))
          continue;

        // Check if already cached
        var cacheKey = AudioCache.ComputeCacheKey(
            phrase,
            modelName,
            speechConfig.LengthScale,
            speechConfig.NoiseScale,
            speechConfig.NoiseW);

        if (_audioCache.TryGet(cacheKey, out _, out _))
        {
          continue;
        }

        try
        {
          await UniTask.Yield();

          var phonemes = await PhonemizeTextAsync(phrase, language, ct);
          if (phonemes == null || phonemes.Length == 0)
            continue;

          var phonemeIds = _encoder.Encode(phonemes);
          if (phonemeIds == null || phonemeIds.Length == 0)
            continue;

          var audioData = await _generator.GenerateAudioAsync(
              phonemeIds,
              lengthScale: speechConfig.LengthScale,
              noiseScale: speechConfig.NoiseScale,
              noiseW: speechConfig.NoiseW
          );

          if (audioData != null && audioData.Length > 0)
          {
            _audioCache.Set(cacheKey, audioData, _currentVoiceConfig.SampleRate);
            cachedCount++;
          }
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[NpcVoiceOutput] Failed to pre-warm phrase '{phrase}': {ex.Message}");
        }
      }

      Debug.Log($"[NpcVoiceOutput] Custom cache pre-warming complete: {cachedCount} phrases in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Validates text length and processes it according to TextLengthBehavior setting.
    /// </summary>
    /// <param name="text">The input text to validate.</param>
    /// <returns>The processed text, or null if the text was rejected.</returns>
    private string ValidateAndProcessTextLength(string text)
    {
      var maxLength = speechConfig.MaxTextLength;
      var behavior = speechConfig.TextLengthBehavior;

      // Allow behavior bypasses all checks
      if (behavior == TextLengthBehavior.Allow)
      {
        return text;
      }

      // Check if text exceeds limit
      if (text.Length <= maxLength)
      {
        return text;
      }

      // Handle based on behavior
      switch (behavior)
      {
        case TextLengthBehavior.Reject:
          Debug.LogWarning($"[NpcVoiceOutput] Text length ({text.Length}) exceeds maximum ({maxLength}). Rejecting.");
          OnSpeakingFailed?.Invoke($"Text too long: {text.Length} characters exceeds limit of {maxLength}");
          return null;

        case TextLengthBehavior.Truncate:
        default:
          var truncated = TruncateTextAtBoundary(text, maxLength);
          Debug.LogWarning($"[NpcVoiceOutput] Text truncated from {text.Length} to {truncated.Length} characters");
          return truncated;
      }
    }

    /// <summary>
    /// Truncates text at a sentence or word boundary before the specified limit.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The truncated text ending at a natural boundary.</returns>
    private static string TruncateTextAtBoundary(string text, int maxLength)
    {
      if (text.Length <= maxLength)
        return text;

      // Try to find a sentence boundary (. ! ?) before the limit
      var truncated = text.Substring(0, maxLength);

      // Look for the last sentence-ending punctuation
      var lastSentenceEnd = -1;
      for (int i = truncated.Length - 1; i >= 0; i--)
      {
        char c = truncated[i];
        if (c == '.' || c == '!' || c == '?')
        {
          // Make sure it's not in the middle of an abbreviation (check for space after)
          if (i == truncated.Length - 1 || char.IsWhiteSpace(truncated[i + 1]))
          {
            lastSentenceEnd = i + 1; // Include the punctuation
            break;
          }
        }
      }

      // If we found a sentence boundary in the last 30% of the text, use it
      if (lastSentenceEnd > maxLength * 0.7)
      {
        return truncated.Substring(0, lastSentenceEnd).TrimEnd();
      }

      // Otherwise, try to find a word boundary (space)
      var lastSpace = truncated.LastIndexOf(' ');
      if (lastSpace > maxLength * 0.7)
      {
        return truncated.Substring(0, lastSpace).TrimEnd() + "...";
      }

      // Last resort: hard truncate with ellipsis
      return truncated.Substring(0, maxLength - 3).TrimEnd() + "...";
    }

    /// <summary>
    /// Plays audio with optional fade-in and fade-out.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    private async UniTask PlayWithFadeAsync(CancellationToken ct)
    {
      var targetVolume = speechConfig.Volume;
      var fadeInDuration = speechConfig.FadeInDuration;
      var fadeOutDuration = speechConfig.FadeOutDuration;
      var clipLength = _audioSource.clip.length;

      // Calculate timeout based on clip length plus buffer
      var timeoutSeconds = Mathf.Max(clipLength + 5f, AUDIO_PLAYBACK_TIMEOUT_SECONDS);
      var startTime = Time.realtimeSinceStartup;

      // Start with zero volume if fading in
      if (fadeInDuration > 0)
      {
        _audioSource.volume = 0f;
      }
      else
      {
        _audioSource.volume = targetVolume;
      }

      _audioSource.Play();

      // Fade in
      if (fadeInDuration > 0)
      {
        var fadeInElapsed = 0f;
        while (fadeInElapsed < fadeInDuration)
        {
          ct.ThrowIfCancellationRequested();
          CheckPlaybackTimeout(startTime, timeoutSeconds);

          fadeInElapsed += Time.deltaTime;
          var t = Mathf.Clamp01(fadeInElapsed / fadeInDuration);
          _audioSource.volume = Mathf.Lerp(0f, targetVolume, t);
          await UniTask.Yield();
        }
        _audioSource.volume = targetVolume;
      }

      // Wait for playback, checking for fade-out point
      while (_audioSource.isPlaying)
      {
        ct.ThrowIfCancellationRequested();
        CheckPlaybackTimeout(startTime, timeoutSeconds);

        // Check if we should start fading out
        var timeRemaining = clipLength - _audioSource.time;
        if (fadeOutDuration > 0 && timeRemaining <= fadeOutDuration)
        {
          // Start fade out
          await FadeOutAsync(targetVolume, timeRemaining, ct);
          break;
        }

        await UniTask.Yield();
      }
    }

    /// <summary>
    /// Checks if playback has exceeded timeout and throws if so.
    /// </summary>
    private void CheckPlaybackTimeout(float startTime, float timeoutSeconds)
    {
      var elapsed = Time.realtimeSinceStartup - startTime;
      if (elapsed > timeoutSeconds)
      {
        Debug.LogError($"[NpcVoiceOutput] Audio playback timeout after {elapsed:F1}s (limit: {timeoutSeconds:F1}s)");
        _audioSource.Stop();
        throw new TimeoutException($"Audio playback exceeded {timeoutSeconds}s timeout");
      }
    }

    /// <summary>
    /// Fades out the audio over the specified duration.
    /// </summary>
    /// <param name="startVolume">Starting volume level.</param>
    /// <param name="duration">Fade duration in seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    private async UniTask FadeOutAsync(float startVolume, float duration, CancellationToken ct)
    {
      var elapsed = 0f;
      while (elapsed < duration && _audioSource.isPlaying)
      {
        ct.ThrowIfCancellationRequested();

        elapsed += Time.deltaTime;
        var t = Mathf.Clamp01(elapsed / duration);
        _audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
        await UniTask.Yield();
      }
      _audioSource.volume = 0f;
    }

    /// <summary>
    /// Applies spatial audio settings from the speech config to the AudioSource.
    /// </summary>
    private void ApplySpatialSettings()
    {
      if (speechConfig == null || _audioSource == null)
        return;

      _audioSource.spatialBlend = speechConfig.SpatialBlend;
      _audioSource.minDistance = speechConfig.MinDistance;
      _audioSource.maxDistance = speechConfig.MaxDistance;
      _audioSource.rolloffMode = speechConfig.RolloffMode;
    }

    private async UniTask LoadModelIfNeededAsync(string modelName)
    {
      if (_loadedModelName == modelName && _currentVoiceConfig != null)
        return;

      Debug.Log($"[NpcVoiceOutput] Loading model: {modelName}");

      // Load model asset (Unity strips .onnx extension)
      var modelAsset = Resources.Load<ModelAsset>($"uPiper/Models/{modelName}") ??
                       Resources.Load<ModelAsset>($"Models/{modelName}");

      if (modelAsset == null)
      {
        Debug.LogError($"[NpcVoiceOutput] Model asset not found. Searched:\n" +
                       $"  - Resources/uPiper/Models/{modelName}\n" +
                       $"  - Resources/Models/{modelName}");
        throw new Exception($"Model not found: {modelName}");
      }
      Debug.Log($"[NpcVoiceOutput] Model asset loaded: {modelAsset.name}");

      // Load config JSON - Unity strips the .json extension, so load as "{modelName}.onnx"
      string jsonText = null;

      // Try with .onnx (Unity imported the .onnx.json file, stripping .json)
      var jsonAsset = Resources.Load<TextAsset>($"uPiper/Models/{modelName}.onnx") ??
                      Resources.Load<TextAsset>($"Models/{modelName}.onnx");

      // Also try without .onnx in case naming is different
      if (jsonAsset == null)
      {
        jsonAsset = Resources.Load<TextAsset>($"uPiper/Models/{modelName}") ??
                    Resources.Load<TextAsset>($"Models/{modelName}");
      }

      if (jsonAsset != null)
      {
        jsonText = jsonAsset.text;
        Debug.Log($"[NpcVoiceOutput] Config loaded from Resources: {jsonAsset.name}");
      }
      else
      {
        // Try StreamingAssets as fallback
        var filePath = System.IO.Path.Combine(Application.streamingAssetsPath, $"{modelName}.onnx.json");
        Debug.Log($"[NpcVoiceOutput] Config not in Resources, trying StreamingAssets: {filePath}");
        if (System.IO.File.Exists(filePath))
        {
          jsonText = System.IO.File.ReadAllText(filePath);
          Debug.Log($"[NpcVoiceOutput] Config loaded from StreamingAssets");
        }
        else
        {
          // Also try uPiper subfolder in StreamingAssets
          filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "uPiper", "Models", $"{modelName}.onnx.json");
          if (System.IO.File.Exists(filePath))
          {
            jsonText = System.IO.File.ReadAllText(filePath);
            Debug.Log($"[NpcVoiceOutput] Config loaded from StreamingAssets/uPiper/Models");
          }
        }
      }

      if (jsonText == null)
      {
        Debug.LogError($"[NpcVoiceOutput] Config not found. Searched:\n" +
                       $"  - Resources/uPiper/Models/{modelName}.onnx\n" +
                       $"  - Resources/Models/{modelName}.onnx\n" +
                       $"  - StreamingAssets/{modelName}.onnx.json\n" +
                       $"  - StreamingAssets/uPiper/Models/{modelName}.onnx.json");
        throw new Exception($"Config not found for model: {modelName}");
      }

      // Parse config
      _currentVoiceConfig = ParseConfig(jsonText, modelName);
      _encoder = new PhonemeEncoder(_currentVoiceConfig);

      // Initialize generator - using our async-friendly wrapper
      await _generator.InitializeAsync(modelAsset, _currentVoiceConfig);
      _loadedModelName = modelName;

      Debug.Log($"[NpcVoiceOutput] Model loaded successfully: {modelName}");
    }

    private async UniTask<string[]> PhonemizeTextAsync(string text, string language, CancellationToken ct)
    {
#if !UNITY_WEBGL
      if (language == "ja" && _japanesePhonemizer != null)
      {
        var result = await _japanesePhonemizer.PhonemizeAsync(text, language);
        return OpenJTalkToPiperMapping.ConvertToPiperPhonemes(result.Phonemes);
      }
      else if (_englishPhonemizer != null)
      {
        var result = await _englishPhonemizer.PhonemizeAsync(text, "en");
        // Use uPiper's ArpabetToIPAConverter which has correct diphthong mappings
        return ArpabetToIPAConverter.ConvertAll(result.Phonemes);
      }
#endif

      // Fallback: simple word splitting
      return text.ToLower()
          .Replace(",", " _")
          .Replace(".", " _")
          .Replace("!", " _")
          .Replace("?", " _")
          .Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    // Removed: Using uPiper.Core.Phonemizers.ArpabetToIPAConverter instead
    // which has correct diphthong mappings (AW→aʊ, AY→aɪ, OW→oʊ, etc.)

    private PiperVoiceConfig ParseConfig(string json, string modelName)
    {
      var config = new PiperVoiceConfig
      {
        VoiceId = modelName,
        DisplayName = modelName,
        Language = "en",
        SampleRate = 22050,
        PhonemeIdMap = new Dictionary<string, int>()
      };

      try
      {
        var jsonObj = JObject.Parse(json);

        if (jsonObj["language"]?["code"] != null)
          config.Language = jsonObj["language"]["code"].ToString();

        if (jsonObj["audio"]?["sample_rate"] != null)
          config.SampleRate = jsonObj["audio"]["sample_rate"].ToObject<int>();

        if (jsonObj["inference"]?["noise_scale"] != null)
          config.NoiseScale = jsonObj["inference"]["noise_scale"].ToObject<float>();

        if (jsonObj["inference"]?["length_scale"] != null)
          config.LengthScale = jsonObj["inference"]["length_scale"].ToObject<float>();

        if (jsonObj["inference"]?["noise_w"] != null)
          config.NoiseW = jsonObj["inference"]["noise_w"].ToObject<float>();

        if (jsonObj["phoneme_id_map"] is JObject phonemeIdMap)
        {
          foreach (var kvp in phonemeIdMap)
          {
            if (kvp.Value is JArray idArray && idArray.Count > 0)
            {
              config.PhonemeIdMap[kvp.Key] = idArray[0].ToObject<int>();
            }
          }
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"[NpcVoiceOutput] Error parsing config: {ex.Message}");
      }

      return config;
    }
  }
}
