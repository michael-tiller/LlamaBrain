using System.Collections.Generic;

/// <summary>
/// This class is used to store the persona
/// </summary>
namespace UnityBrain.Persona
{

  /// <summary>
  /// Dialogue history
  /// </summary>
  public sealed class DialogueHistory
  {
    /// <summary>
    /// The lines of the dialogue history
    /// </summary>
    private readonly List<string> _lines = new List<string>();

    /// <summary>
    /// Append a line to the dialogue history
    /// </summary>
    /// <param name="speaker">The speaker of the line</param>
    /// <param name="text">The text of the line</param>
    public void Append(string speaker, string text) => _lines.Add($"{speaker}: {text}");

    /// <summary>
    /// Get the dialogue history
    /// </summary>
    public IReadOnlyList<string> GetHistory() => _lines;

    /// <summary>
    /// Clear the dialogue history
    /// </summary>
    public void Clear() => _lines.Clear();
  }

}