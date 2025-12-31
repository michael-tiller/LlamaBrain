using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Runtime.Core.Expectancy
{
  /// <summary>
  /// Component that holds NPC-specific expectancy rules.
  /// Attach this to NPCs to give them custom constraints.
  /// </summary>
  public class NpcExpectancyConfig : MonoBehaviour
  {
    [Header("NPC-Specific Rules")]
    [Tooltip("Rules that only apply to this NPC.")]
    [SerializeField] private List<ExpectancyRuleAsset> npcRules = new List<ExpectancyRuleAsset>();

    [Header("Settings")]
    [Tooltip("Override NPC ID used in context matching. If empty, uses GameObject name.")]
    [SerializeField] private string npcIdOverride;

    /// <summary>
    /// The NPC ID used for context matching.
    /// </summary>
    public string NpcId => string.IsNullOrEmpty(npcIdOverride) ? gameObject.name : npcIdOverride;

    /// <summary>
    /// The NPC-specific rules.
    /// </summary>
    public IReadOnlyList<ExpectancyRuleAsset> Rules => npcRules;

    /// <summary>
    /// Evaluates rules for this NPC with the given context.
    /// Combines global rules (from ExpectancyEngine) with NPC-specific rules.
    /// </summary>
    /// <param name="context">The interaction context to evaluate</param>
    /// <returns>A ConstraintSet containing all applicable constraints</returns>
    public ConstraintSet Evaluate(InteractionContext context)
    {
      return Evaluate(context, null);
    }

    /// <summary>
    /// Evaluates rules for this NPC with the given context, including additional rules (e.g., from triggers).
    /// Combines global rules (from ExpectancyEngine) with NPC-specific rules and additional rules.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="additionalRules">Additional rules to evaluate (e.g., trigger-specific rules).</param>
    /// <returns>A ConstraintSet containing all applicable constraints</returns>
    public ConstraintSet Evaluate(InteractionContext context, IEnumerable<ExpectancyRuleAsset> additionalRules)
    {
      // Ensure the context has this NPC's ID
      context.NpcId = NpcId;

      // Combine NPC rules with additional rules
      var allRules = new List<ExpectancyRuleAsset>(npcRules);
      if (additionalRules != null)
      {
        allRules.AddRange(additionalRules);
      }

      return ExpectancyEngine.EvaluateStatic(context, allRules);
    }

    /// <summary>
    /// Creates an interaction context for a zone trigger.
    /// Uses Unity's Time.time and current scene name.
    /// </summary>
    /// <param name="triggerId">The ID of the trigger zone</param>
    /// <param name="triggerPrompt">The prompt text from the trigger</param>
    /// <returns>A new InteractionContext configured for a zone trigger</returns>
    public InteractionContext CreateZoneTriggerContext(string triggerId, string triggerPrompt)
    {
      return InteractionContextFactory.FromZoneTrigger(NpcId, triggerId, triggerPrompt);
    }

    /// <summary>
    /// Creates an interaction context for player dialogue.
    /// Uses Unity's Time.time and current scene name.
    /// </summary>
    /// <param name="playerInput">The player's input text</param>
    /// <returns>A new InteractionContext configured for a player utterance</returns>
    public InteractionContext CreatePlayerUtteranceContext(string playerInput)
    {
      return InteractionContextFactory.FromPlayerUtterance(NpcId, playerInput);
    }
  }
}
