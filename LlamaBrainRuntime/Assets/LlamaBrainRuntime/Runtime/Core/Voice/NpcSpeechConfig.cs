using UnityEngine;

namespace LlamaBrain.Runtime.Core.Voice
{
  /// <summary>
  /// Defines how to handle text that exceeds the maximum length limit.
  /// </summary>
  public enum TextLengthBehavior
  {
    /// <summary>
    /// Truncate text at the last complete sentence or word before the limit.
    /// </summary>
    Truncate = 0,

    /// <summary>
    /// Reject the text entirely and fire OnSpeakingFailed event.
    /// </summary>
    Reject = 1,

    /// <summary>
    /// Allow text of any length (may cause memory issues with very long text).
    /// </summary>
    Allow = 2
  }

  /// <summary>
  /// ScriptableObject configuration for NPC voice settings.
  /// Allows per-NPC voice model selection and prosody control.
  /// </summary>
  [CreateAssetMenu(fileName = "New NPC Speech Config", menuName = "LlamaBrain/NPC Speech Config")]
  public sealed class NpcSpeechConfig : ScriptableObject
  {
    /// <summary>
    /// Preset voice models available for selection.
    /// </summary>
    /// <summary>
    /// Preset voice models available for selection.
    /// </summary>
    public enum VoiceModelPreset
    {
      /// <summary>
      /// English voice model using Lessac high quality preset.
      /// </summary>
      EnglishLessacHigh = 0,
      /// <summary>
      /// English voice model using LJSpeech high quality preset.
      /// </summary>
      EnglishLjspeechHigh = 1,
      /// <summary>
      /// Japanese voice model using test medium quality preset.
      /// </summary>
      JapaneseTestMedium = 2,
      EnglishAmyMedium = 3,
      EnglishCoriHigh = 4,
      EnglishLjspeechMedium = 5,
      /// <summary>
      /// Custom voice model specified by path.
      /// </summary>
      Custom = 99
    }

    [Header("Voice Model Selection")]
    [Tooltip("Select a preset voice model or choose Custom to specify a path.")]
    /// <summary>
    /// The voice model preset to use for speech synthesis.
    /// </summary>
    public VoiceModelPreset VoicePreset = VoiceModelPreset.EnglishLessacHigh;

    [Tooltip("Path to custom model (only used when VoicePreset is Custom).")]
    /// <summary>
    /// Path to a custom voice model file (only used when VoicePreset is Custom).
    /// </summary>
    public string CustomModelPath = "";

    [Header("Prosody Controls")]
    [Tooltip("Controls speech speed. Lower = slower, clearer speech. Higher = faster.")]
    [Range(0.5f, 2.0f)]
    /// <summary>
    /// Controls speech speed. Lower values produce slower, clearer speech. Higher values produce faster speech.
    /// </summary>
    public float LengthScale = 1.0f;

    [Tooltip("Controls voice variation/randomness. Lower = more consistent, higher = more varied.")]
    [Range(0.0f, 2.0f)]
    /// <summary>
    /// Controls voice variation and randomness. Lower values produce more consistent speech. Higher values produce more varied speech.
    /// </summary>
    public float NoiseScale = 0.667f;

    [Tooltip("Additional noise parameter for voice variation.")]
    [Range(0.0f, 2.0f)]
    /// <summary>
    /// Additional noise parameter for voice variation control.
    /// </summary>
    public float NoiseW = 0.8f;

    [Header("Audio Settings")]
    [Tooltip("Whether to normalize audio output to prevent clipping.")]
    /// <summary>
    /// Whether to normalize audio output to prevent clipping.
    /// </summary>
    public bool NormalizeAudio = true;

    [Tooltip("Output volume multiplier.")]
    [Range(0.0f, 2.0f)]
    /// <summary>
    /// Output volume multiplier for the generated speech.
    /// </summary>
    public float Volume = 1.0f;

    [Header("Text Length Limits")]
    [Tooltip("Maximum text length in characters. Text exceeding this limit will be truncated or rejected based on TextLengthBehavior.")]
    [Range(50, 10000)]
    /// <summary>
    /// Maximum text length in characters for TTS generation.
    /// Longer text requires more memory and processing time.
    /// Default: 1000 characters (~30 seconds of speech at normal speed).
    /// </summary>
    public int MaxTextLength = 1000;

    [Tooltip("How to handle text that exceeds MaxTextLength.")]
    /// <summary>
    /// Behavior when text exceeds MaxTextLength.
    /// </summary>
    public TextLengthBehavior TextLengthBehavior = TextLengthBehavior.Truncate;

    [Header("Audio Fading")]
    [Tooltip("Duration in seconds to fade in audio at the start of speech.")]
    [Range(0f, 2f)]
    /// <summary>
    /// Duration in seconds to fade in audio at the start of speech.
    /// Set to 0 for instant start.
    /// </summary>
    public float FadeInDuration = 0f;

    [Tooltip("Duration in seconds to fade out audio at the end of speech.")]
    [Range(0f, 2f)]
    /// <summary>
    /// Duration in seconds to fade out audio at the end of speech.
    /// Set to 0 for instant stop.
    /// </summary>
    public float FadeOutDuration = 0f;

    [Header("Spatial Audio")]
    [Tooltip("0 = 2D (no spatialization), 1 = fully 3D spatialized audio.")]
    [Range(0f, 1f)]
    /// <summary>
    /// Controls how much the audio is affected by 3D spatialization.
    /// 0 = 2D (heard equally everywhere), 1 = fully 3D (position-based).
    /// </summary>
    public float SpatialBlend = 0f;

    [Tooltip("Minimum distance before audio starts attenuating.")]
    [Min(0.1f)]
    /// <summary>
    /// Within this distance, audio plays at full volume.
    /// </summary>
    public float MinDistance = 1f;

    [Tooltip("Maximum distance where audio is still audible.")]
    [Min(1f)]
    /// <summary>
    /// Beyond this distance, audio is inaudible (or at minimum volume depending on rolloff).
    /// </summary>
    public float MaxDistance = 50f;

    [Tooltip("How audio volume decreases with distance.")]
    /// <summary>
    /// The curve used to attenuate audio over distance.
    /// </summary>
    public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;

    [Header("Audio Caching")]
    [Tooltip("Enable caching of generated audio to avoid redundant TTS inference for repeated phrases.")]
    /// <summary>
    /// Enable caching of generated audio to avoid redundant TTS inference.
    /// When enabled, audio generated for the same text with the same voice settings will be reused.
    /// </summary>
    public bool EnableAudioCaching = true;

    [Tooltip("Maximum cache size in megabytes. Older entries are evicted when the limit is reached.")]
    [Range(10, 500)]
    /// <summary>
    /// Maximum audio cache size in megabytes.
    /// At 22kHz mono, 50 MB holds approximately 10 minutes of audio.
    /// </summary>
    public int AudioCacheMaxSizeMB = 50;

    [Tooltip("Pre-warm the cache with common phrases on initialization. May cause a short delay at startup.")]
    /// <summary>
    /// When enabled, common phrases (greetings, acknowledgments, etc.) are pre-generated
    /// during initialization for faster first response. May cause a short startup delay.
    /// </summary>
    public bool PreWarmCacheOnInit = false;

    /// <summary>
    /// Gets the model name for the current preset.
    /// </summary>
    /// <returns>The model name string for the current voice preset.</returns>
    public string GetModelName()
    {
      return VoicePreset switch
      {
        VoiceModelPreset.EnglishLessacHigh => "en_US-lessac-high",
        VoiceModelPreset.EnglishLjspeechHigh => "en_US-ljspeech-high",
        VoiceModelPreset.JapaneseTestMedium => "ja_JP-test-medium",
        VoiceModelPreset.EnglishAmyMedium => "en_US-amy-medium",
        VoiceModelPreset.EnglishCoriHigh => "en_GB-cori-high",
        VoiceModelPreset.EnglishLjspeechMedium => "en_US-ljspeech-medium",
        VoiceModelPreset.Custom => string.IsNullOrWhiteSpace(CustomModelPath) ? "en_US-lessac-high" : CustomModelPath,
        _ => "en_US-lessac-high"
      };
    }

    /// <summary>
    /// Unity callback that validates the configuration in the editor.
    /// </summary>
    private void OnValidate()
    {
      if (VoicePreset == VoiceModelPreset.Custom && string.IsNullOrWhiteSpace(CustomModelPath))
      {
        Debug.LogWarning($"NpcSpeechConfig '{name}': VoicePreset is set to Custom but CustomModelPath is null or empty. Falling back to default model 'en_US-lessac-high' at runtime.", this);
      }
    }

    /// <summary>
    /// Gets the language code for the current preset.
    /// </summary>
    /// <returns>The ISO language code (e.g., "en", "ja") for the current voice preset.</returns>
    public string GetLanguage()
    {
      return VoicePreset switch
      {
        VoiceModelPreset.EnglishLessacHigh => "en",
        VoiceModelPreset.EnglishLjspeechHigh => "en",
        VoiceModelPreset.JapaneseTestMedium => "ja",
        VoiceModelPreset.EnglishAmyMedium => "en",
        VoiceModelPreset.EnglishCoriHigh => "en",
        VoiceModelPreset.EnglishLjspeechMedium => "en",
        VoiceModelPreset.Custom => "en", // Default to English for custom
        _ => "en"
      };
    }
  }
}
