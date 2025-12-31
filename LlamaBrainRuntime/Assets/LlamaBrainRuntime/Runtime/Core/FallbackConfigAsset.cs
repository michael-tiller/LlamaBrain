#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// Unity ScriptableObject for configuring fallback responses.
  /// Create via: Assets > Create > LlamaBrain > Fallback Config
  /// </summary>
  [CreateAssetMenu(fileName = "FallbackConfig", menuName = "LlamaBrain/Fallback Config")]
  public class FallbackConfigAsset : ScriptableObject
  {
    [Header("Generic Fallbacks")]
    [Tooltip("Generic safe responses that work in any context.")]
    [SerializeField] private List<string> genericFallbacks = new List<string>
    {
      "I'm not sure how to respond to that.",
      "Let me think about that.",
      "That's interesting.",
      "I see.",
      "Hmm, I'm not certain about that."
    };

    [Header("Player Utterance Fallbacks")]
    [Tooltip("Fallback responses for player utterances.")]
    [SerializeField] private List<string> playerUtteranceFallbacks = new List<string>
    {
      "I'm sorry, I didn't quite catch that.",
      "Could you repeat that?",
      "I'm not sure I understand.",
      "What did you say?"
    };

    [Header("Zone Trigger Fallbacks")]
    [Tooltip("Fallback responses for zone triggers.")]
    [SerializeField] private List<string> zoneTriggerFallbacks = new List<string>
    {
      "Hello there.",
      "Greetings, traveler.",
      "How can I help you?",
      "What brings you here?"
    };

    [Header("Time Trigger Fallbacks")]
    [Tooltip("Fallback responses for time-based triggers.")]
    [SerializeField] private List<string> timeTriggerFallbacks = new List<string>
    {
      "Another day passes...",
      "Time moves on.",
      "The world keeps turning."
    };

    [Header("Quest Trigger Fallbacks")]
    [Tooltip("Fallback responses for quest triggers.")]
    [SerializeField] private List<string> questTriggerFallbacks = new List<string>
    {
      "I have a task for you.",
      "There's something I need to tell you.",
      "I have important news."
    };

    [Header("NPC Interaction Fallbacks")]
    [Tooltip("Fallback responses for NPC interactions.")]
    [SerializeField] private List<string> npcInteractionFallbacks = new List<string>
    {
      "Hello, friend.",
      "Good to see you.",
      "How are you doing?"
    };

    [Header("World Event Fallbacks")]
    [Tooltip("Fallback responses for world events.")]
    [SerializeField] private List<string> worldEventFallbacks = new List<string>
    {
      "Something has changed in the world.",
      "The world shifts around us.",
      "Can you feel it too?"
    };

    [Header("Custom Trigger Fallbacks")]
    [Tooltip("Fallback responses for custom triggers.")]
    [SerializeField] private List<string> customTriggerFallbacks = new List<string>
    {
      "I'm here if you need me.",
      "What can I do for you?",
      "Is there something you need?"
    };

    [Header("Emergency Fallbacks")]
    [Tooltip("Emergency fallbacks used when all other lists are empty.")]
    [SerializeField] private List<string> emergencyFallbacks = new List<string>
    {
      "I'm having trouble responding right now.",
      "Something seems off, but I'm still here.",
      "I apologize, but I can't form a proper response."
    };

    /// <summary>
    /// Converts this ScriptableObject to a FallbackConfig for use by AuthorControlledFallback.
    /// </summary>
    /// <returns>A FallbackConfig instance with the values from this asset</returns>
    public AuthorControlledFallback.FallbackConfig ToFallbackConfig()
    {
      return new AuthorControlledFallback.FallbackConfig
      {
        GenericFallbacks = new List<string>(genericFallbacks),
        PlayerUtteranceFallbacks = new List<string>(playerUtteranceFallbacks),
        ZoneTriggerFallbacks = new List<string>(zoneTriggerFallbacks),
        TimeTriggerFallbacks = new List<string>(timeTriggerFallbacks),
        QuestTriggerFallbacks = new List<string>(questTriggerFallbacks),
        NpcInteractionFallbacks = new List<string>(npcInteractionFallbacks),
        WorldEventFallbacks = new List<string>(worldEventFallbacks),
        CustomTriggerFallbacks = new List<string>(customTriggerFallbacks),
        EmergencyFallbacks = new List<string>(emergencyFallbacks)
      };
    }

    /// <summary>
    /// Generic safe responses that work in any context.
    /// </summary>
    public IReadOnlyList<string> GenericFallbacks => genericFallbacks;
    
    /// <summary>
    /// Fallback responses for player utterances.
    /// </summary>
    public IReadOnlyList<string> PlayerUtteranceFallbacks => playerUtteranceFallbacks;
    
    /// <summary>
    /// Fallback responses for zone triggers.
    /// </summary>
    public IReadOnlyList<string> ZoneTriggerFallbacks => zoneTriggerFallbacks;
    
    /// <summary>
    /// Fallback responses for time-based triggers.
    /// </summary>
    public IReadOnlyList<string> TimeTriggerFallbacks => timeTriggerFallbacks;
    
    /// <summary>
    /// Fallback responses for quest triggers.
    /// </summary>
    public IReadOnlyList<string> QuestTriggerFallbacks => questTriggerFallbacks;
    
    /// <summary>
    /// Fallback responses for NPC interactions.
    /// </summary>
    public IReadOnlyList<string> NpcInteractionFallbacks => npcInteractionFallbacks;
    
    /// <summary>
    /// Fallback responses for world events.
    /// </summary>
    public IReadOnlyList<string> WorldEventFallbacks => worldEventFallbacks;
    
    /// <summary>
    /// Fallback responses for custom triggers.
    /// </summary>
    public IReadOnlyList<string> CustomTriggerFallbacks => customTriggerFallbacks;
    
    /// <summary>
    /// Emergency fallbacks used when all other lists are empty.
    /// </summary>
    public IReadOnlyList<string> EmergencyFallbacks => emergencyFallbacks;
  }
}

