using System;
using System.Collections.Generic;
using LlamaBrain.Core.FunctionCalling;

namespace LlamaBrain.Core.Validation
{
  /// <summary>
  /// The type of proposed memory mutation.
  /// </summary>
  public enum MutationType
  {
    /// <summary>Append a new episodic memory.</summary>
    AppendEpisodic,
    /// <summary>Update or create a belief.</summary>
    TransformBelief,
    /// <summary>Update a relationship score/state.</summary>
    TransformRelationship,
    /// <summary>Emit a world intent (NPC action desire).</summary>
    EmitWorldIntent
  }

  /// <summary>
  /// A proposed mutation to memory that must be validated before execution.
  /// </summary>
  [Serializable]
  public class ProposedMutation
  {
    /// <summary>
    /// The type of mutation.
    /// </summary>
    public MutationType Type { get; set; }

    /// <summary>
    /// The target of the mutation (e.g., belief ID, relationship target).
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The content/value of the mutation.
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// Optional confidence level (0-1) for belief mutations.
    /// </summary>
    public float Confidence { get; set; } = 1.0f;

    /// <summary>
    /// The source text that triggered this mutation proposal.
    /// </summary>
    public string? SourceText { get; set; }

    /// <summary>
    /// Creates an episodic memory append mutation.
    /// </summary>
    /// <param name="content">The content of the episodic memory to append</param>
    /// <param name="sourceText">Optional source text that triggered this mutation</param>
    /// <returns>A new ProposedMutation for appending an episodic memory</returns>
    public static ProposedMutation AppendEpisodic(string content, string? sourceText = null)
    {
      return new ProposedMutation
      {
        Type = MutationType.AppendEpisodic,
        Content = content,
        SourceText = sourceText
      };
    }

    /// <summary>
    /// Creates a belief transformation mutation.
    /// </summary>
    /// <param name="beliefId">The ID of the belief to transform</param>
    /// <param name="content">The new content for the belief</param>
    /// <param name="confidence">The confidence level (0-1) for this belief mutation</param>
    /// <param name="sourceText">Optional source text that triggered this mutation</param>
    /// <returns>A new ProposedMutation for transforming a belief</returns>
    public static ProposedMutation TransformBelief(string beliefId, string content, float confidence = 1.0f, string? sourceText = null)
    {
      return new ProposedMutation
      {
        Type = MutationType.TransformBelief,
        Target = beliefId,
        Content = content,
        Confidence = confidence,
        SourceText = sourceText
      };
    }

    /// <summary>
    /// Creates a relationship transformation mutation.
    /// </summary>
    /// <param name="target">The target of the relationship (e.g., player ID, NPC ID)</param>
    /// <param name="content">The content describing the relationship change</param>
    /// <param name="sourceText">Optional source text that triggered this mutation</param>
    /// <returns>A new ProposedMutation for transforming a relationship</returns>
    public static ProposedMutation TransformRelationship(string target, string content, string? sourceText = null)
    {
      return new ProposedMutation
      {
        Type = MutationType.TransformRelationship,
        Target = target,
        Content = content,
        SourceText = sourceText
      };
    }

    /// <summary>
    /// Creates a world intent emission.
    /// </summary>
    /// <param name="intentType">The type of world intent (e.g., "follow_player", "give_item")</param>
    /// <param name="content">The content describing the intent</param>
    /// <param name="sourceText">Optional source text that triggered this mutation</param>
    /// <returns>A new ProposedMutation for emitting a world intent</returns>
    public static ProposedMutation EmitWorldIntent(string intentType, string content, string? sourceText = null)
    {
      return new ProposedMutation
      {
        Type = MutationType.EmitWorldIntent,
        Target = intentType,
        Content = content,
        SourceText = sourceText
      };
    }

    /// <summary>
    /// Returns a string representation of this mutation.
    /// </summary>
    /// <returns>A string representation of the proposed mutation</returns>
    public override string ToString()
    {
      return $"[{Type}] {Target ?? ""}: {Content}";
    }
  }

  /// <summary>
  /// A world intent representing an NPC's desire to affect the game world.
  /// These are not executed directly but consumed by game systems.
  /// </summary>
  [Serializable]
  public class WorldIntent
  {
    /// <summary>
    /// The type of intent (e.g., "follow_player", "give_item", "start_quest").
    /// </summary>
    public string IntentType { get; set; } = "";

    /// <summary>
    /// The target of the intent (e.g., player ID, item ID, location).
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Additional parameters for the intent. Supports complex types (nested objects, arrays).
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// The priority of this intent (higher = more urgent).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// The source dialogue text that generated this intent.
    /// </summary>
    public string? SourceText { get; set; }

    /// <summary>
    /// Creates a new world intent.
    /// </summary>
    /// <param name="intentType">The type of intent (e.g., "follow_player", "give_item", "start_quest")</param>
    /// <param name="target">The target of the intent (e.g., player ID, item ID, location)</param>
    /// <param name="priority">The priority of this intent (higher = more urgent)</param>
    /// <returns>A new WorldIntent instance</returns>
    public static WorldIntent Create(string intentType, string? target = null, int priority = 0)
    {
      return new WorldIntent
      {
        IntentType = intentType,
        Target = target,
        Priority = priority
      };
    }

    /// <summary>
    /// Returns a string representation of this world intent.
    /// </summary>
    /// <returns>A string representation of the world intent</returns>
    public override string ToString()
    {
      return $"WorldIntent[{IntentType}] -> {Target ?? "(none)"} (priority: {Priority})";
    }
  }

  /// <summary>
  /// The result of parsing LLM output into structured components.
  /// Contains dialogue text, proposed mutations, and world intents.
  /// </summary>
  [Serializable]
  public class ParsedOutput
  {
    /// <summary>
    /// Whether parsing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The clean dialogue text to display to the player.
    /// </summary>
    public string DialogueText { get; set; } = "";

    /// <summary>
    /// Any parsing error message (if Success is false).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The raw, unprocessed LLM output.
    /// </summary>
    public string RawOutput { get; set; } = "";

    /// <summary>
    /// Proposed memory mutations extracted from the output.
    /// </summary>
    public List<ProposedMutation> ProposedMutations { get; set; } = new List<ProposedMutation>();

    /// <summary>
    /// World intents extracted from the output.
    /// </summary>
    public List<WorldIntent> WorldIntents { get; set; } = new List<WorldIntent>();

    /// <summary>
    /// Function calls extracted from the output.
    /// These are requests from the LLM to call functions (e.g., get_memories, get_constraints).
    /// </summary>
    public List<FunctionCall> FunctionCalls { get; set; } = new List<FunctionCall>();

    /// <summary>
    /// Whether the output contained any structured data (mutations, intents, or function calls).
    /// </summary>
    public bool HasStructuredData => ProposedMutations.Count > 0 || WorldIntents.Count > 0 || FunctionCalls.Count > 0;

    /// <summary>
    /// Metadata extracted during parsing.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Creates a successful parse result with dialogue text.
    /// </summary>
    /// <param name="dialogueText">The clean dialogue text extracted from the raw output</param>
    /// <param name="rawOutput">The raw, unprocessed LLM output</param>
    /// <returns>A successful ParsedOutput instance with dialogue text</returns>
    public static ParsedOutput Dialogue(string dialogueText, string rawOutput)
    {
      return new ParsedOutput
      {
        Success = true,
        DialogueText = dialogueText,
        RawOutput = rawOutput
      };
    }

    /// <summary>
    /// Creates a failed parse result.
    /// </summary>
    /// <param name="errorMessage">The error message describing why parsing failed</param>
    /// <param name="rawOutput">The raw, unprocessed LLM output</param>
    /// <returns>A failed ParsedOutput instance with the error message</returns>
    public static ParsedOutput Failed(string errorMessage, string rawOutput)
    {
      return new ParsedOutput
      {
        Success = false,
        ErrorMessage = errorMessage,
        RawOutput = rawOutput
      };
    }

    /// <summary>
    /// Adds a proposed mutation to this output.
    /// </summary>
    /// <param name="mutation">The proposed mutation to add</param>
    /// <returns>This ParsedOutput instance for method chaining</returns>
    public ParsedOutput WithMutation(ProposedMutation mutation)
    {
      ProposedMutations.Add(mutation);
      return this;
    }

    /// <summary>
    /// Adds a world intent to this output.
    /// </summary>
    /// <param name="intent">The world intent to add</param>
    /// <returns>This ParsedOutput instance for method chaining</returns>
    public ParsedOutput WithIntent(WorldIntent intent)
    {
      WorldIntents.Add(intent);
      return this;
    }

    /// <summary>
    /// Adds a function call to this output.
    /// </summary>
    /// <param name="functionCall">The function call to add</param>
    /// <returns>This ParsedOutput instance for method chaining</returns>
    public ParsedOutput WithFunctionCall(FunctionCall functionCall)
    {
      FunctionCalls.Add(functionCall);
      return this;
    }

    /// <summary>
    /// Adds metadata to this output.
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    /// <returns>This ParsedOutput instance for method chaining</returns>
    public ParsedOutput WithMetadata(string key, string value)
    {
      Metadata[key] = value;
      return this;
    }

    /// <summary>
    /// Creates a new ParsedOutput with mutations replaced by the given list.
    /// Used for schema validation filtering.
    /// </summary>
    /// <param name="mutations">The validated mutations to use</param>
    /// <returns>A new ParsedOutput with the given mutations</returns>
    public ParsedOutput WithMutationsReplaced(IEnumerable<ProposedMutation> mutations)
    {
      return new ParsedOutput
      {
        Success = Success,
        DialogueText = DialogueText,
        RawOutput = RawOutput,
        ErrorMessage = ErrorMessage,
        ProposedMutations = new List<ProposedMutation>(mutations),
        WorldIntents = new List<WorldIntent>(WorldIntents),
        Metadata = new Dictionary<string, string>(Metadata)
      };
    }

    /// <summary>
    /// Creates a new ParsedOutput with intents replaced by the given list.
    /// Used for schema validation filtering.
    /// </summary>
    /// <param name="intents">The validated intents to use</param>
    /// <returns>A new ParsedOutput with the given intents</returns>
    public ParsedOutput WithIntentsReplaced(IEnumerable<WorldIntent> intents)
    {
      return new ParsedOutput
      {
        Success = Success,
        DialogueText = DialogueText,
        RawOutput = RawOutput,
        ErrorMessage = ErrorMessage,
        ProposedMutations = new List<ProposedMutation>(ProposedMutations),
        WorldIntents = new List<WorldIntent>(intents),
        Metadata = new Dictionary<string, string>(Metadata)
      };
    }

    /// <summary>
    /// Returns a string representation of this parsed output.
    /// </summary>
    /// <returns>A string representation of the parsed output</returns>
    public override string ToString()
    {
      if (!Success)
      {
        return $"ParsedOutput[Failed] {ErrorMessage}";
      }
      var parts = new List<string> { $"ParsedOutput[OK]" };
      if (!string.IsNullOrEmpty(DialogueText))
      {
        var preview = DialogueText.Length > 30 ? DialogueText.Substring(0, 30) + "..." : DialogueText;
        parts.Add($"Dialogue: \"{preview}\"");
      }
      if (ProposedMutations.Count > 0)
      {
        parts.Add($"{ProposedMutations.Count} mutations");
      }
      if (WorldIntents.Count > 0)
      {
        parts.Add($"{WorldIntents.Count} intents");
      }
      if (FunctionCalls.Count > 0)
      {
        parts.Add($"{FunctionCalls.Count} function calls");
      }
      return string.Join(" | ", parts);
    }
  }
}
