using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Events;
using uPiper.Core;
using uPiper.Core.AudioGeneration;
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
    private InferenceAudioGenerator _generator;
    private PhonemeEncoder _encoder;
    private AudioClipBuilder _audioBuilder;
    private PiperVoiceConfig _currentVoiceConfig;
    private string _loadedModelName;
    private bool _isInitialized;
    private bool _isSpeaking;

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
      _generator = new InferenceAudioGenerator();
      _audioBuilder = new AudioClipBuilder();
    }

    private async void Start()
    {
      await InitializeAsync();
    }

    private void OnDestroy()
    {
      _generator?.Dispose();
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

      _isInitialized = true;
      Debug.Log("[NpcVoiceOutput] TTS system initialized");
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

      if (!_isInitialized)
      {
        await InitializeAsync();
      }

      _isSpeaking = true;
      OnSpeakingStarted?.Invoke();

      try
      {
        var modelName = speechConfig.GetModelName();
        var language = speechConfig.GetLanguage();

        // Load model if needed
        await LoadModelIfNeededAsync(modelName);

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

        // Generate audio
        var audioData = await _generator.GenerateAudioAsync(
            phonemeIds,
            lengthScale: speechConfig.LengthScale,
            noiseScale: speechConfig.NoiseScale,
            noiseW: speechConfig.NoiseW
        );

        ct.ThrowIfCancellationRequested();

        // Process audio (normalize if needed)
        float[] processedAudio;
        var maxVal = audioData.Max(x => Math.Abs(x));

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

        // Apply volume
        if (Math.Abs(speechConfig.Volume - 1.0f) > 0.01f)
        {
          processedAudio = processedAudio.Select(x => x * speechConfig.Volume).ToArray();
        }

        // Build AudioClip
        var audioClip = _audioBuilder.BuildAudioClip(
            processedAudio,
            _currentVoiceConfig.SampleRate,
            $"NpcSpeech_{DateTime.Now:HHmmss}"
        );

        ct.ThrowIfCancellationRequested();

        // Play audio
        _audioSource.clip = audioClip;
        _audioSource.Play();

        // Wait for playback to complete
        while (_audioSource.isPlaying)
        {
          if (ct.IsCancellationRequested)
          {
            _audioSource.Stop();
            throw new OperationCanceledException(ct);
          }
          await UniTask.Yield();
        }

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
        _isSpeaking = false;
        OnSpeakingFinished?.Invoke();
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

      // Initialize generator
      var piperConfig = new PiperConfig
      {
        Backend = InferenceBackend.CPU,
        AllowFallbackToCPU = true
      };

      await _generator.InitializeAsync(modelAsset, _currentVoiceConfig, piperConfig);
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
        return ConvertArpabetToIPA(result.Phonemes);
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

    private static readonly Dictionary<string, string> ArpabetToIPA = new()
    {
      ["AA"] = "\u0251", ["AE"] = "\u00e6", ["AH"] = "\u028c", ["AO"] = "\u0254",
      ["AW"] = "a", ["AY"] = "a", ["EH"] = "\u025b", ["ER"] = "\u025a",
      ["EY"] = "e", ["IH"] = "\u026a", ["IY"] = "i", ["OW"] = "o",
      ["OY"] = "\u0254", ["UH"] = "\u028a", ["UW"] = "u",
      ["B"] = "b", ["CH"] = "t\u0283", ["D"] = "d", ["DH"] = "\u00f0",
      ["F"] = "f", ["G"] = "\u0261", ["HH"] = "h", ["JH"] = "d\u0292",
      ["K"] = "k", ["L"] = "l", ["M"] = "m", ["N"] = "n", ["NG"] = "\u014b",
      ["P"] = "p", ["R"] = "\u0279", ["S"] = "s", ["SH"] = "\u0283",
      ["T"] = "t", ["TH"] = "\u03b8", ["V"] = "v", ["W"] = "w",
      ["Y"] = "j", ["Z"] = "z", ["ZH"] = "\u0292"
    };

    private string[] ConvertArpabetToIPA(string[] arpabetPhonemes)
    {
      var result = new string[arpabetPhonemes.Length];
      for (int i = 0; i < arpabetPhonemes.Length; i++)
      {
        var basePhoneme = arpabetPhonemes[i].TrimEnd('0', '1', '2');
        result[i] = ArpabetToIPA.TryGetValue(basePhoneme.ToUpper(), out var ipa)
            ? ipa : arpabetPhonemes[i].ToLower();
      }
      return result;
    }

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
