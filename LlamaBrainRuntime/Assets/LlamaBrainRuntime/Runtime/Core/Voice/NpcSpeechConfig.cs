using UnityEngine;

namespace LlamaBrain.Runtime.Core.Voice
{
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
    public enum VoiceModelPreset
    {
      EnglishLessacHigh = 0,
      EnglishLjspeechHigh = 1,
      JapaneseTestMedium = 2,
      Custom = 99
    }

    [Header("Voice Model Selection")]
    [Tooltip("Select a preset voice model or choose Custom to specify a path.")]
    public VoiceModelPreset VoicePreset = VoiceModelPreset.EnglishLessacHigh;

    [Tooltip("Path to custom model (only used when VoicePreset is Custom).")]
    public string CustomModelPath = "";

    [Header("Prosody Controls")]
    [Tooltip("Controls speech speed. Lower = slower, clearer speech. Higher = faster.")]
    [Range(0.5f, 2.0f)]
    public float LengthScale = 1.0f;

    [Tooltip("Controls voice variation/randomness. Lower = more consistent, higher = more varied.")]
    [Range(0.0f, 2.0f)]
    public float NoiseScale = 0.667f;

    [Tooltip("Additional noise parameter for voice variation.")]
    [Range(0.0f, 2.0f)]
    public float NoiseW = 0.8f;

    [Header("Audio Settings")]
    [Tooltip("Whether to normalize audio output to prevent clipping.")]
    public bool NormalizeAudio = true;

    [Tooltip("Output volume multiplier.")]
    [Range(0.0f, 2.0f)]
    public float Volume = 1.0f;

    /// <summary>
    /// Gets the model name for the current preset.
    /// </summary>
    public string GetModelName()
    {
      return VoicePreset switch
      {
        VoiceModelPreset.EnglishLessacHigh => "en_US-lessac-high",
        VoiceModelPreset.EnglishLjspeechHigh => "en_US-ljspeech-high",
        VoiceModelPreset.JapaneseTestMedium => "ja_JP-test-medium",
        VoiceModelPreset.Custom => CustomModelPath,
        _ => "en_US-lessac-high"
      };
    }

    /// <summary>
    /// Gets the language code for the current preset.
    /// </summary>
    public string GetLanguage()
    {
      return VoicePreset switch
      {
        VoiceModelPreset.EnglishLessacHigh => "en",
        VoiceModelPreset.EnglishLjspeechHigh => "en",
        VoiceModelPreset.JapaneseTestMedium => "ja",
        VoiceModelPreset.Custom => "en", // Default to English for custom
        _ => "en"
      };
    }
  }
}
