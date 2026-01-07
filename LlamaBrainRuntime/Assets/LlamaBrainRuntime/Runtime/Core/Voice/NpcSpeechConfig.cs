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
        VoiceModelPreset.Custom => "en", // Default to English for custom
        _ => "en"
      };
    }
  }
}
