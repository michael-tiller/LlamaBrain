using UnityEngine;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Runtime.Core.Expectancy
{
  /// <summary>
  /// Unity-specific helper for creating InteractionContext with game engine time.
  /// The actual InteractionContext class is in LlamaBrain.Core.Expectancy.
  /// </summary>
  public static class InteractionContextFactory
  {
    /// <summary>
    /// Creates a context for a zone-triggered interaction with Unity's Time.time.
    /// </summary>
    public static InteractionContext FromZoneTrigger(string npcId, string triggerId, string triggerPrompt)
    {
      var context = InteractionContext.FromZoneTrigger(npcId, triggerId, triggerPrompt, Time.time);
      context.SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
      return context;
    }

    /// <summary>
    /// Creates a context for a player-initiated interaction with Unity's Time.time.
    /// </summary>
    public static InteractionContext FromPlayerUtterance(string npcId, string playerInput)
    {
      var context = InteractionContext.FromPlayerUtterance(npcId, playerInput, Time.time);
      context.SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
      return context;
    }
  }
}
