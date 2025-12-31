using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;
using LlamaBrain.Runtime.Core.Expectancy;

namespace LlamaBrain.Runtime.Core.Inference
{
  /// <summary>
  /// Unity-specific helper for building StateSnapshots.
  /// Integrates with Unity components like ExpectancyEngine, NpcExpectancyConfig, etc.
  /// </summary>
  public static class UnityStateSnapshotBuilder
  {
    /// <summary>
    /// Default maximum retry attempts.
    /// </summary>
    public const int DefaultMaxAttempts = 3;

    /// <summary>
    /// Creates a StateSnapshot from Unity components for NPC dialogue.
    /// </summary>
    /// <param name="npcConfig">The NPC's expectancy configuration</param>
    /// <param name="memorySystem">The authoritative memory system</param>
    /// <param name="playerInput">The player's input text</param>
    /// <param name="systemPrompt">The NPC's system prompt</param>
    /// <param name="dialogueHistory">Recent dialogue history</param>
    /// <param name="retrievalConfig">Optional context retrieval configuration</param>
    /// <returns>A new StateSnapshot ready for inference</returns>
    public static StateSnapshot BuildForNpcDialogue(
      NpcExpectancyConfig npcConfig,
      AuthoritativeMemorySystem memorySystem,
      string playerInput,
      string systemPrompt,
      IEnumerable<string> dialogueHistory = null,
      ContextRetrievalConfig retrievalConfig = null)
    {
      // Create context
      var context = npcConfig.CreatePlayerUtteranceContext(playerInput);

      // Evaluate constraints
      var constraints = npcConfig.Evaluate(context);

      // Retrieve memories
      var retrieval = new ContextRetrievalLayer(memorySystem, retrievalConfig);
      var retrieved = retrieval.RetrieveContext(playerInput);

      // Build snapshot
      var builder = new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(systemPrompt)
        .WithPlayerInput(playerInput)
        .WithMaxAttempts(DefaultMaxAttempts)
        .WithMetadata("npc_id", npcConfig.NpcId)
        .WithMetadata("game_time", Time.time.ToString("F2"));

      // Apply retrieved context
      retrieved.ApplyTo(builder);

      // Add dialogue history
      if (dialogueHistory != null)
      {
        builder.WithDialogueHistory(dialogueHistory);
      }

      return builder.Build();
    }

    /// <summary>
    /// Creates a StateSnapshot for a zone trigger interaction.
    /// </summary>
    /// <param name="npcConfig">The NPC's expectancy configuration</param>
    /// <param name="triggerId">The trigger zone ID</param>
    /// <param name="triggerPrompt">The trigger's prompt text</param>
    /// <param name="memorySystem">The authoritative memory system</param>
    /// <param name="systemPrompt">The NPC's system prompt</param>
    /// <param name="triggerRules">Additional trigger-specific rules</param>
    /// <param name="dialogueHistory">Recent dialogue history</param>
    /// <returns>A new StateSnapshot ready for inference</returns>
    public static StateSnapshot BuildForZoneTrigger(
      NpcExpectancyConfig npcConfig,
      string triggerId,
      string triggerPrompt,
      AuthoritativeMemorySystem memorySystem,
      string systemPrompt,
      IEnumerable<ExpectancyRuleAsset> triggerRules = null,
      IEnumerable<string> dialogueHistory = null)
    {
      // Create context for zone trigger
      var context = npcConfig.CreateZoneTriggerContext(triggerId, triggerPrompt);

      // Evaluate constraints including trigger rules
      var constraints = npcConfig.Evaluate(context, triggerRules);

      // Retrieve memories with trigger-related topics
      var retrieval = new ContextRetrievalLayer(memorySystem);
      var retrieved = retrieval.RetrieveContext(triggerPrompt, new[] { triggerId });

      // Build snapshot
      var builder = new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(systemPrompt)
        .WithPlayerInput(triggerPrompt)
        .WithMaxAttempts(DefaultMaxAttempts)
        .WithMetadata("npc_id", npcConfig.NpcId)
        .WithMetadata("trigger_id", triggerId)
        .WithMetadata("trigger_type", "zone")
        .WithMetadata("game_time", Time.time.ToString("F2"));

      // Apply retrieved context
      retrieved.ApplyTo(builder);

      // Add dialogue history
      if (dialogueHistory != null)
      {
        builder.WithDialogueHistory(dialogueHistory);
      }

      return builder.Build();
    }

    /// <summary>
    /// Creates a StateSnapshot with explicit context (for custom use cases).
    /// </summary>
    /// <param name="context">The interaction context</param>
    /// <param name="constraints">The constraint set</param>
    /// <param name="memorySystem">The authoritative memory system</param>
    /// <param name="systemPrompt">The NPC's system prompt</param>
    /// <param name="playerInput">The player's input text</param>
    /// <param name="maxAttempts">Maximum retry attempts</param>
    /// <returns>A new StateSnapshot ready for inference</returns>
    public static StateSnapshot BuildWithExplicitContext(
      InteractionContext context,
      ConstraintSet constraints,
      AuthoritativeMemorySystem memorySystem,
      string systemPrompt,
      string playerInput,
      int maxAttempts = DefaultMaxAttempts)
    {
      var retrieval = new ContextRetrievalLayer(memorySystem);
      var retrieved = retrieval.RetrieveContext(playerInput);

      var builder = new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(systemPrompt)
        .WithPlayerInput(playerInput)
        .WithMaxAttempts(maxAttempts)
        .WithMetadata("game_time", Time.time.ToString("F2"));

      retrieved.ApplyTo(builder);

      return builder.Build();
    }

    /// <summary>
    /// Creates a minimal StateSnapshot for testing or simple use cases.
    /// </summary>
    /// <param name="systemPrompt">The system prompt</param>
    /// <param name="playerInput">The player's input</param>
    /// <param name="constraints">Optional constraints</param>
    /// <returns>A minimal StateSnapshot</returns>
    public static StateSnapshot BuildMinimal(
      string systemPrompt,
      string playerInput,
      ConstraintSet constraints = null)
    {
      return new StateSnapshotBuilder()
        .WithSystemPrompt(systemPrompt)
        .WithPlayerInput(playerInput)
        .WithConstraints(constraints ?? new ConstraintSet())
        .WithMaxAttempts(DefaultMaxAttempts)
        .Build();
    }
  }
}
