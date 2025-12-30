using System;
using System.Collections.Generic;
using LlamaBrain.Core;

namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  /// <summary>
  /// Represents a single dialogue interaction with full metrics
  /// </summary>
  [Serializable]
  public class DialogueInteraction
  {
    public string InteractionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    // Trigger Information
    public string TriggerId { get; set; } = string.Empty;
    public string TriggerName { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public int PromptCount { get; set; }
    
    // NPC Information
    public string NpcName { get; set; } = string.Empty;
    
    // Response Information
    public string ResponseText { get; set; } = string.Empty;
    public int ResponseLength { get; set; }
    
    // Performance Metrics
    public long TtftMs { get; set; }
    public long PrefillTimeMs { get; set; }
    public long DecodeTimeMs { get; set; }
    public long TotalTimeMs { get; set; }
    
    // Token Metrics
    public int PromptTokenCount { get; set; }
    public int GeneratedTokenCount { get; set; }
    public int CachedTokenCount { get; set; }
    public double TokensPerSecond { get; set; }
    
    // Quality Flags
    public bool WasTruncated { get; set; }
    
    /// <summary>
    /// Creates a DialogueInteraction from CompletionMetrics and trigger info
    /// </summary>
    public static DialogueInteraction FromMetrics(
      CompletionMetrics metrics,
      NpcDialogueTrigger trigger,
      string npcName)
    {
      return new DialogueInteraction
      {
        Timestamp = DateTime.Now,
        TriggerId = trigger.Id.ToString(),
        TriggerName = trigger.GetTriggerName(),
        PromptText = trigger.GetPromptText(),
        PromptCount = trigger.PromptCount,
        NpcName = npcName,
        ResponseText = metrics.Content ?? string.Empty,
        ResponseLength = metrics.Content?.Length ?? 0,
        TtftMs = metrics.TtftMs,
        PrefillTimeMs = metrics.PrefillTimeMs,
        DecodeTimeMs = metrics.DecodeTimeMs,
        TotalTimeMs = metrics.TotalTimeMs,
        PromptTokenCount = metrics.PromptTokenCount,
        GeneratedTokenCount = metrics.GeneratedTokenCount,
        CachedTokenCount = metrics.CachedTokenCount,
        TokensPerSecond = metrics.TokensPerSecond,
        WasTruncated = false // Will be set by caller if needed
      };
    }
  }
  
  /// <summary>
  /// Collection of all dialogue interactions for export and analysis
  /// </summary>
  [Serializable]
  public class DialogueMetricsCollection
  {
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime SessionStart { get; set; } = DateTime.Now;
    public DateTime? SessionEnd { get; set; }
    public List<DialogueInteraction> Interactions { get; set; } = new List<DialogueInteraction>();
    
    public int TotalInteractions => Interactions.Count;
    
    public void AddInteraction(DialogueInteraction interaction)
    {
      Interactions.Add(interaction);
    }
    
    public void EndSession()
    {
      SessionEnd = DateTime.Now;
    }
  }
}

