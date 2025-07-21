using System.Collections.Generic;

namespace UnityBrain.Core
{
  /// <summary>
  /// Tracks the conversation history between participants, typically a player and an NPC.
  /// </summary>
  public class DialogueSession
  {
    /// <summary>
    /// The ID of the persona.
    /// </summary>
    public string PersonaId { get; }
    /// <summary>
    /// The dialogue history.
    /// </summary>
    private readonly List<string> _history = new List<string>();

    /// <summary>
    /// Initializes a new instance of the DialogueSession class.
    /// </summary>
    /// <param name="personaId">The ID of the persona.</param>
    public DialogueSession(string personaId)
    {
      PersonaId = personaId;
    }

    /// <summary>
    /// Appends a message to the dialogue history with a generic speaker.
    /// </summary>
    /// <param name="speaker">The speaker of the message.</param>
    /// <param name="text">The message text.</param>
    public void Append(string speaker, string text)
    {
      _history.Add($"{speaker}: {text}");
    }

    /// <summary>
    /// Appends a player message to the dialogue history.
    /// </summary>
    /// <param name="input">The player message.</param>
    public void AppendPlayer(string input)
    {
      Append("Player", input);
    }

    /// <summary>
    /// Appends an NPC message to the dialogue history.
    /// </summary>
    /// <param name="response">The NPC message.</param>
    public void AppendNpc(string response)
    {
      Append("NPC", response);
    }

    /// <summary>
    /// Gets the dialogue history.
    /// </summary>
    /// <returns>The dialogue history.</returns>
    public IReadOnlyList<string> GetHistory() => _history;

    /// <summary>
    /// Clears the dialogue history.
    /// </summary>
    public void Clear()
    {
      _history.Clear();
    }
  }
}