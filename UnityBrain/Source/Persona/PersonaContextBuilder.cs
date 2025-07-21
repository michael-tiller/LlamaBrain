using System.Collections.Generic;
using System.Text;

namespace UnityBrain.Persona
{

  /// <summary>
  /// Builder for the context of a persona
  /// </summary>
  public static class PersonaContextBuilder
  {
    /// <summary>
    /// Build the context of a persona
    /// </summary>
    /// <param name="systemPrompt">The system prompt</param>
    /// <param name="personaName">The name of the persona</param>
    /// <param name="description">The description of the persona</param>
    /// <param name="memory">The memory of the persona</param>
    /// <param name="dialogueHistory">The dialogue history of the persona</param>
    /// <param name="playerName">The name of the player</param>
    /// <param name="playerInput">The input of the player</param>
    /// <returns>The context of the persona</returns>
    public static string BuildContext(
      string systemPrompt,
      string personaName,
      string description,
      IReadOnlyList<string> memory,
      IReadOnlyList<string> dialogueHistory,
      string playerName,
      string playerInput)
    {
      var sb = new StringBuilder();
      sb.AppendLine(systemPrompt);
      sb.AppendLine($"You are {personaName}. {description}");
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

      sb.AppendLine($"{playerName}: {playerInput}");
      sb.AppendLine($"{personaName}:");
      return sb.ToString();
    }
  }
}