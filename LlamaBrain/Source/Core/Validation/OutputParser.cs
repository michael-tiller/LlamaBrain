using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LlamaBrain.Core.Validation
{
  /// <summary>
  /// Configuration for the output parser.
  /// </summary>
  [Serializable]
  public class OutputParserConfig
  {
    /// <summary>
    /// Whether to enforce single-line dialogue output.
    /// </summary>
    public bool EnforceSingleLine { get; set; } = true;

    /// <summary>
    /// Whether to remove stage directions (text in asterisks).
    /// </summary>
    public bool RemoveStageDirections { get; set; } = true;

    /// <summary>
    /// Whether to remove script directions (text in brackets).
    /// </summary>
    public bool RemoveScriptDirections { get; set; } = true;

    /// <summary>
    /// Whether to remove speaker labels (e.g., "NPC: ").
    /// </summary>
    public bool RemoveSpeakerLabels { get; set; } = true;

    /// <summary>
    /// Whether to attempt structured data extraction.
    /// </summary>
    public bool ExtractStructuredData { get; set; } = true;

    /// <summary>
    /// Whether to trim to the last complete sentence when truncated.
    /// </summary>
    public bool TrimToCompleteSentence { get; set; } = true;

    /// <summary>
    /// Minimum character count to consider a response valid.
    /// </summary>
    public int MinimumCharacterCount { get; set; } = 1;

    /// <summary>
    /// Patterns that indicate meta-text (should be rejected).
    /// </summary>
    public List<string> MetaTextPatterns { get; set; } = new List<string>
    {
      "example answer:", "for example:", "example:", "note:", "remember:",
      "important:", "hint:", "tip:", "answer:", "reply:", "response:",
      "player asks", "player says", "npc replies", "npc says", "character responds",
      "if you wish", "if you want", "don't forget", "keep in mind",
      "you should", "you can", "you may", "use punctuation",
      "indicate a question", "respectively", "for strong emotions"
    };

    /// <summary>
    /// Default configuration.
    /// </summary>
    public static OutputParserConfig Default => new OutputParserConfig();

    /// <summary>
    /// Configuration for structured output that may contain JSON.
    /// </summary>
    public static OutputParserConfig Structured => new OutputParserConfig
    {
      EnforceSingleLine = false,
      ExtractStructuredData = true,
      TrimToCompleteSentence = false
    };

    /// <summary>
    /// Minimal parsing configuration.
    /// </summary>
    public static OutputParserConfig Minimal => new OutputParserConfig
    {
      EnforceSingleLine = false,
      RemoveStageDirections = false,
      RemoveScriptDirections = false,
      RemoveSpeakerLabels = false,
      ExtractStructuredData = false,
      TrimToCompleteSentence = false
    };
  }

  /// <summary>
  /// Parses LLM output into structured format.
  /// Extracts dialogue text, proposed mutations, and world intents.
  /// </summary>
  public class OutputParser
  {
    private readonly OutputParserConfig config;

    /// <summary>
    /// Optional logging callback.
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// Creates a new output parser with default configuration.
    /// </summary>
    public OutputParser() : this(OutputParserConfig.Default) { }

    /// <summary>
    /// Creates a new output parser with specified configuration.
    /// </summary>
    /// <param name="config">The configuration to use for parsing. If null, uses default configuration.</param>
    public OutputParser(OutputParserConfig config)
    {
      this.config = config ?? OutputParserConfig.Default;
    }

    /// <summary>
    /// Parses raw LLM output into a structured ParsedOutput.
    /// </summary>
    /// <param name="rawOutput">The raw LLM output text</param>
    /// <param name="wasTruncated">Whether the output was truncated by token limit</param>
    /// <returns>Parsed output with dialogue, mutations, and intents</returns>
    public ParsedOutput Parse(string rawOutput, bool wasTruncated = false)
    {
      if (string.IsNullOrWhiteSpace(rawOutput))
      {
        return ParsedOutput.Failed("Response is empty or whitespace", rawOutput ?? "");
      }

      // Step 1: Check for meta-text BEFORE any cleaning
      // This catches responses like "Example answer: ..." before they get cleaned
      if (ContainsMetaText(rawOutput))
      {
        return ParsedOutput.Failed("Response contains meta-text/explanation instead of dialogue", rawOutput);
      }

      var result = ParsedOutput.Dialogue("", rawOutput);
      var text = rawOutput;

      // Step 2: Extract structured data if enabled
      if (config.ExtractStructuredData)
      {
        text = ExtractStructuredData(text, result);
      }

      // Step 3: Extract dialogue text (clean the response)
      var dialogueResult = ExtractDialogueText(text, wasTruncated);
      if (!dialogueResult.Success)
      {
        return ParsedOutput.Failed(dialogueResult.ErrorMessage ?? "Failed to extract dialogue", rawOutput);
      }

      result.DialogueText = dialogueResult.Text;

      // Step 4: Validate the final dialogue
      if (string.IsNullOrWhiteSpace(result.DialogueText))
      {
        return ParsedOutput.Failed("Dialogue text is empty after parsing", rawOutput);
      }

      if (result.DialogueText.Length < config.MinimumCharacterCount)
      {
        return ParsedOutput.Failed($"Dialogue too short ({result.DialogueText.Length} chars, minimum {config.MinimumCharacterCount})", rawOutput);
      }

      result.Success = true;
      OnLog?.Invoke($"[OutputParser] Parsed: {result}");

      return result;
    }

    /// <summary>
    /// Extracts structured data from the text (JSON blocks, special markers).
    /// Returns the remaining text after extraction.
    /// </summary>
    private string ExtractStructuredData(string text, ParsedOutput result)
    {
      // Look for JSON blocks (```json ... ```)
      var jsonBlockPattern = @"```json\s*([\s\S]*?)```";
      var jsonMatches = Regex.Matches(text, jsonBlockPattern, RegexOptions.IgnoreCase);
      foreach (Match match in jsonMatches)
      {
        var jsonContent = match.Groups[1].Value.Trim();
        TryParseStructuredJson(jsonContent, result);
        text = text.Replace(match.Value, "").Trim();
      }

      // Look for inline structured markers
      // Format: [MEMORY: content] or [INTENT: content] or [BELIEF: content]
      var markerPatterns = new Dictionary<string, Action<string, ParsedOutput>>
      {
        { @"\[MEMORY:\s*(.+?)\]", (content, r) => r.WithMutation(ProposedMutation.AppendEpisodic(content, content)) },
        { @"\[BELIEF:\s*(.+?)\]", (content, r) => r.WithMutation(ProposedMutation.TransformBelief("extracted", content, 0.8f, content)) },
        { @"\[INTENT:\s*(.+?)\]", (content, r) => r.WithIntent(WorldIntent.Create("extracted", content)) },
        { @"\[ACTION:\s*(.+?)\]", (content, r) => r.WithIntent(WorldIntent.Create("action", content)) }
      };

      foreach (var kvp in markerPatterns)
      {
        var matches = Regex.Matches(text, kvp.Key, RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
          var content = match.Groups[1].Value.Trim();
          kvp.Value(content, result);
          text = text.Replace(match.Value, "").Trim();
        }
      }

      return text;
    }

    /// <summary>
    /// Attempts to parse structured JSON content.
    /// </summary>
    private void TryParseStructuredJson(string json, ParsedOutput result)
    {
      // Simple JSON parsing for common patterns
      // Full JSON parsing would require a library - this handles simple cases

      // Look for "memory" or "memories" arrays
      var memoryPattern = @"""memory""\s*:\s*""([^""]+)""";
      var memoryMatches = Regex.Matches(json, memoryPattern);
      foreach (Match match in memoryMatches)
      {
        result.WithMutation(ProposedMutation.AppendEpisodic(match.Groups[1].Value, json));
      }

      // Look for "intent" or "action" fields
      var intentPattern = @"""(?:intent|action)""\s*:\s*""([^""]+)""";
      var intentMatches = Regex.Matches(json, intentPattern);
      foreach (Match match in intentMatches)
      {
        result.WithIntent(WorldIntent.Create("json_intent", match.Groups[1].Value));
      }

      // Look for "belief" fields
      var beliefPattern = @"""belief""\s*:\s*""([^""]+)""";
      var beliefMatches = Regex.Matches(json, beliefPattern);
      foreach (Match match in beliefMatches)
      {
        result.WithMutation(ProposedMutation.TransformBelief("json_belief", match.Groups[1].Value, 0.8f, json));
      }

      result.WithMetadata("has_json", "true");
    }

    /// <summary>
    /// Extracts and cleans dialogue text from the response.
    /// </summary>
    private DialogueExtractionResult ExtractDialogueText(string text, bool wasTruncated)
    {
      var original = text;

      // Step 1: Enforce single line if required
      if (config.EnforceSingleLine)
      {
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        string? firstNonEmpty = null;
        foreach (var line in lines)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrWhiteSpace(trimmed))
          {
            firstNonEmpty = trimmed;
            break;
          }
        }
        text = firstNonEmpty ?? "";
      }

      if (string.IsNullOrWhiteSpace(text))
      {
        return new DialogueExtractionResult { Success = false, ErrorMessage = "No dialogue text after single-line extraction" };
      }

      // Step 2: Remove stage directions *text*
      if (config.RemoveStageDirections && text.Contains("*"))
      {
        text = Regex.Replace(text, @"\*[^*]*\*", "").Trim();
      }

      // Step 3: Remove script directions [text]
      if (config.RemoveScriptDirections && (text.Contains("[") || text.Contains("]")))
      {
        text = Regex.Replace(text, @"\[.*?\]", "").Trim();
      }

      // Step 4: Remove speaker labels (Name: ) - handles multi-word names like "Old Man:"
      if (config.RemoveSpeakerLabels)
      {
        // Match names like "Guard:", "Old Man:", "The Wizard:", etc.
        text = Regex.Replace(text, @"^\s*[A-Z][A-Za-z\s]+:\s*", "", RegexOptions.Multiline).Trim();
      }

      // Step 5: Clean whitespace
      text = Regex.Replace(text, @"\s+", " ").Trim();

      if (string.IsNullOrWhiteSpace(text))
      {
        return new DialogueExtractionResult { Success = false, ErrorMessage = "Dialogue empty after removing directions" };
      }

      // Step 6: Trim to complete sentence if needed
      if (config.TrimToCompleteSentence && text.Length > 5)
      {
        var result = TrimToCompleteSentence(text, wasTruncated);
        if (!result.Success)
        {
          return result;
        }
        text = result.Text;
      }

      // Step 7: Check for fragment patterns (starts with lowercase)
      if (text.Length > 0 && char.IsLower(text[0]))
      {
        var fragmentPatterns = new[] { "depending on", "based on", "according to", "in order to", "so that", "such that" };
        foreach (var pattern in fragmentPatterns)
        {
          if (text.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
          {
            return new DialogueExtractionResult { Success = false, ErrorMessage = $"Response is a fragment (starts with '{pattern}')" };
          }
        }
      }

      // Step 8: Ensure sentence-ending punctuation
      if (!text.EndsWith(".") && !text.EndsWith("!") && !text.EndsWith("?") &&
          !text.EndsWith(".\"") && !text.EndsWith("!\"") && !text.EndsWith("?\""))
      {
        text = text.TrimEnd() + ".";
      }

      return new DialogueExtractionResult { Success = true, Text = text };
    }

    /// <summary>
    /// Trims text to the last complete sentence.
    /// </summary>
    private DialogueExtractionResult TrimToCompleteSentence(string text, bool wasTruncated)
    {
      var lastPunctuation = text.LastIndexOfAny(new[] { '.', '!', '?' });

      if (lastPunctuation > 0)
      {
        return new DialogueExtractionResult { Success = true, Text = text.Substring(0, lastPunctuation + 1).Trim() };
      }

      if (wasTruncated)
      {
        // Check for dangling words that indicate truncation
        var danglingWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
          "the", "a", "an", "to", "and", "or", "but", "for", "with", "of", "in", "on", "at", "by", "some", "kind"
        };

        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0)
        {
          var lastWord = words[words.Length - 1].TrimEnd('.', '!', '?', ',');
          if (danglingWords.Contains(lastWord))
          {
            return new DialogueExtractionResult
            {
              Success = false,
              ErrorMessage = $"Truncated output ends mid-sentence with '{lastWord}'"
            };
          }
        }

        return new DialogueExtractionResult
        {
          Success = false,
          ErrorMessage = "Truncated output with no complete sentence"
        };
      }

      // Not truncated but no punctuation - text is returned as-is
      return new DialogueExtractionResult { Success = true, Text = text };
    }

    /// <summary>
    /// Checks if the text contains meta-text patterns.
    /// </summary>
    private bool ContainsMetaText(string text)
    {
      var textLower = text.ToLowerInvariant();
      foreach (var pattern in config.MetaTextPatterns)
      {
        if (textLower.Contains(pattern))
        {
          OnLog?.Invoke($"[OutputParser] Meta-text detected: '{pattern}'");
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Internal result class for dialogue extraction.
    /// </summary>
    private class DialogueExtractionResult
    {
      public bool Success { get; set; }
      public string Text { get; set; } = "";
      public string? ErrorMessage { get; set; }
    }
  }
}
