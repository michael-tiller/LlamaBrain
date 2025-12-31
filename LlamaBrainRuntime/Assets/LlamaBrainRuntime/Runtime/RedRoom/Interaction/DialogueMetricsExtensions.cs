#nullable enable
using LlamaBrain.Core;
using LlamaBrain.Core.Metrics;
using LlamaBrain.Runtime.Core;

namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  /// <summary>
  /// Extension methods to bridge Unity Runtime types to Core metrics interfaces.
  /// </summary>
  public static class DialogueMetricsExtensions
  {
    /// <summary>
    /// Creates a DialogueInteraction from Unity Runtime types.
    /// </summary>
    /// <param name="metrics">The completion metrics from the LLM response</param>
    /// <param name="trigger">The Unity trigger that initiated this interaction</param>
    /// <param name="npcName">The name of the NPC</param>
    /// <returns>A new DialogueInteraction with metrics populated</returns>
    public static LlamaBrain.Core.Metrics.DialogueInteraction FromMetrics(
      CompletionMetrics metrics,
      NpcDialogueTrigger trigger,
      string npcName)
    {
      // Use the Core factory method - NpcDialogueTrigger implements ITriggerInfo
      return LlamaBrain.Core.Metrics.DialogueInteraction.FromMetrics(metrics, trigger, npcName);
    }

    /// <summary>
    /// Populates architectural metrics from Unity LlamaBrainAgent state.
    /// </summary>
    /// <param name="interaction">The interaction to populate</param>
    /// <param name="agent">The Unity LlamaBrainAgent that performed the inference</param>
    public static void PopulateArchitecturalMetrics(
      this LlamaBrain.Core.Metrics.DialogueInteraction interaction,
      LlamaBrainAgent agent)
    {
      // LlamaBrainAgent implements IAgentMetrics directly
      interaction.PopulateArchitecturalMetrics(agent);
    }
  }
}

