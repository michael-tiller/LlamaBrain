using System.Collections.Generic;
using System.Text;

namespace UnityBrain.Core
{
  /// <summary>
  /// Utility for assembling LLM prompts with persona and session context.
  /// </summary>
  public static class PromptComposer
  {
    /// <summary>
    /// Compose a prompt for an LLM with persona and session context.
    /// </summary>
    /// <param name="personaName">The name of the persona.</param>
    /// <param name="description">The description of the persona.</param>
    /// <param name="memory">The memory of the persona.</param>
    /// <param name="dialogueHistory">The dialogue history.</param>
    public static string Compose(
        string personaName,
        string description,
        IReadOnlyList<string> memory,
        IReadOnlyList<string> dialogueHistory,
        string playerName,
        string playerInput)
    {
      var sb = new StringBuilder();
      sb.AppendLine($"NPC: {personaName}");
      sb.AppendLine($"Description: {description}");
      if (memory != null && memory.Count > 0)
      {
        sb.AppendLine("NPC Memory:");
        foreach (var m in memory) sb.AppendLine($"- {m}");
      }
      if (dialogueHistory != null && dialogueHistory.Count > 0)
      {
        sb.AppendLine("Dialogue so far:");
        foreach (var line in dialogueHistory) sb.AppendLine(line);
      }
      sb.AppendLine($"Player: {playerName}");
      sb.AppendLine($"Player says: {playerInput}");
      sb.AppendLine("NPC responds:");
      return sb.ToString();
    }
  }
}