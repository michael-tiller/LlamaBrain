using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Utilities;

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
    /// Whether to use native structured output parsing when available.
    /// When true and input is valid JSON, ParseStructured will be used.
    /// </summary>
    public bool UseStructuredOutput { get; set; } = false;

    /// <summary>
    /// Default configuration.
    /// </summary>
    public static OutputParserConfig Default => new OutputParserConfig();

    /// <summary>
    /// Configuration for structured output that may contain JSON.
    /// Uses regex-based extraction from prompt-injected JSON responses.
    /// </summary>
    public static OutputParserConfig Structured => new OutputParserConfig
    {
      EnforceSingleLine = false,
      ExtractStructuredData = true,
      TrimToCompleteSentence = false
    };

    /// <summary>
    /// Configuration for native structured output (llama.cpp json_schema mode).
    /// Expects pure JSON responses that can be directly deserialized.
    /// </summary>
    public static OutputParserConfig NativeStructured => new OutputParserConfig
    {
      EnforceSingleLine = false,
      ExtractStructuredData = false,
      TrimToCompleteSentence = false,
      RemoveStageDirections = false,
      RemoveScriptDirections = false,
      RemoveSpeakerLabels = false,
      UseStructuredOutput = true
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
    /// Unicode BOM character that may appear at start of text.
    /// </summary>
    private const char BOM = '\uFEFF';

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
    /// Normalizes whitespace in text for deterministic output.
    /// This is a pure function that applies consistent whitespace normalization rules.
    ///
    /// Rules applied (in order):
    /// 1. Strip BOM (\\uFEFF) if present at start
    /// 2. Convert CRLF to LF (Windows → Unix line endings)
    /// 3. Trim trailing whitespace from each line
    /// 4. Collapse 3+ consecutive blank lines to exactly 2
    /// 5. Preserve leading blank lines (policy decision: preserved, not stripped)
    /// 6. Preserve existing trailing newline (policy decision: if input ends with \\n, output ends with \\n)
    ///
    /// A "blank line" is defined as a line that is empty after trimming trailing whitespace.
    /// </summary>
    /// <param name="text">The text to normalize. May be null or empty.</param>
    /// <returns>Normalized text with consistent whitespace. Returns empty string for null/empty input.</returns>
    public static string NormalizeWhitespace(string? text)
    {
      if (string.IsNullOrEmpty(text))
      {
        return string.Empty;
      }

      // Step 1: Strip BOM if present at start
      if (text[0] == BOM)
      {
        text = text.Substring(1);
        if (string.IsNullOrEmpty(text))
        {
          return string.Empty;
        }
      }

      // Step 2: Convert CRLF to LF
      text = text.Replace("\r\n", "\n");
      // Also handle any stray CR characters
      text = text.Replace("\r", "\n");

      // Check if original text ends with newline (to preserve it)
      bool endsWithNewline = text.EndsWith("\n");

      // Step 3: Split into lines and process each
      var lines = text.Split('\n');
      var processedLines = new List<string>(lines.Length);

      foreach (var line in lines)
      {
        // Trim trailing whitespace from each line (but preserve leading whitespace/indentation)
        processedLines.Add(line.TrimEnd());
      }

      // Step 4: Collapse 3+ consecutive blank lines to exactly 2
      var result = new List<string>();
      int consecutiveBlankLines = 0;

      foreach (var line in processedLines)
      {
        bool isBlank = string.IsNullOrEmpty(line);

        if (isBlank)
        {
          consecutiveBlankLines++;
          // Only add if we haven't exceeded 2 consecutive blank lines
          if (consecutiveBlankLines <= 2)
          {
            result.Add(line);
          }
          // If > 2, we skip adding this blank line (collapsing)
        }
        else
        {
          consecutiveBlankLines = 0;
          result.Add(line);
        }
      }

      // Step 5 & 6: Join lines and handle trailing newline
      var normalized = string.Join("\n", result);

      // If original ended with newline and result doesn't, add it back
      // If original didn't end with newline, don't add one
      if (endsWithNewline && !normalized.EndsWith("\n"))
      {
        normalized += "\n";
      }
      else if (!endsWithNewline && normalized.EndsWith("\n"))
      {
        // This shouldn't happen with our logic, but ensure we don't add trailing newline
        normalized = normalized.TrimEnd('\n');
      }

      return normalized;
    }

    /// <summary>
    /// Parses raw LLM output into a structured ParsedOutput.
    /// Applies whitespace normalization at the appropriate stage:
    /// - If ExtractStructuredData=false: NormalizeWhitespace(raw) first
    /// - If ExtractStructuredData=true: Extract(raw) → NormalizeWhitespace(dialogue)
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

      // Step 1: Apply early normalization if not extracting structured data
      // This ensures deterministic whitespace handling for raw mode
      var text = rawOutput;
      if (!config.ExtractStructuredData)
      {
        text = NormalizeWhitespace(text);
      }

      // Step 2: Check for meta-text BEFORE any cleaning
      // This catches responses like "Example answer: ..." before they get cleaned
      if (ContainsMetaText(text))
      {
        return ParsedOutput.Failed("Response contains meta-text/explanation instead of dialogue", rawOutput);
      }

      var result = ParsedOutput.Dialogue("", rawOutput);

      // Step 3: Extract structured data if enabled
      if (config.ExtractStructuredData)
      {
        text = ExtractStructuredData(text, result);
        // Normalize after structured data extraction
        text = NormalizeWhitespace(text);
      }

      // Step 4: Extract dialogue text (clean the response)
      var dialogueResult = ExtractDialogueText(text, wasTruncated);
      if (!dialogueResult.Success)
      {
        return ParsedOutput.Failed(dialogueResult.ErrorMessage ?? "Failed to extract dialogue", rawOutput);
      }

      result.DialogueText = dialogueResult.Text;

      // Step 5: Validate the final dialogue
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
    /// Parses a structured JSON response from native structured output mode.
    /// This method expects valid JSON that conforms to the ParsedOutput schema.
    /// Use this when the LLM was called with json_schema constraint.
    /// </summary>
    /// <param name="jsonResponse">The JSON response from the LLM.</param>
    /// <returns>Parsed output with dialogue, mutations, and intents.</returns>
    public ParsedOutput ParseStructured(string jsonResponse)
    {
      if (string.IsNullOrWhiteSpace(jsonResponse))
      {
        return ParsedOutput.Failed("JSON response is empty or whitespace", jsonResponse ?? "");
      }

      // Validate that it's valid JSON
      if (!JsonUtils.IsValidJson(jsonResponse))
      {
        OnLog?.Invoke($"[OutputParser] Invalid JSON, falling back to regex parsing");
        // Fall back to regex-based parsing
        return Parse(jsonResponse);
      }

      try
      {
        // Deserialize to the StructuredDialogueResponse DTO
        var structured = JsonUtils.Deserialize<StructuredDialogueResponse>(jsonResponse);

        if (structured == null)
        {
          return ParsedOutput.Failed("Failed to deserialize JSON response", jsonResponse);
        }

        // Build ParsedOutput from the structured response
        var result = ParsedOutput.Dialogue(
          structured.DialogueText ?? "",
          jsonResponse
        );

        // Convert mutations
        if (structured.ProposedMutations != null)
        {
          foreach (var mutation in structured.ProposedMutations)
          {
            result.WithMutation(mutation.ToProposedMutation());
          }
        }

        // Convert intents
        if (structured.WorldIntents != null)
        {
          foreach (var intent in structured.WorldIntents)
          {
            result.WithIntent(intent.ToWorldIntent());
          }
        }

        // Validate the result
        if (string.IsNullOrWhiteSpace(result.DialogueText))
        {
          return ParsedOutput.Failed("Dialogue text is empty in structured response", jsonResponse);
        }

        if (result.DialogueText.Length < config.MinimumCharacterCount)
        {
          return ParsedOutput.Failed(
            $"Dialogue too short ({result.DialogueText.Length} chars, minimum {config.MinimumCharacterCount})",
            jsonResponse);
        }

        result.Success = true;
        result.WithMetadata("parse_mode", "structured");
        OnLog?.Invoke($"[OutputParser] ParseStructured: {result}");

        return result;
      }
      catch (Exception ex)
      {
        OnLog?.Invoke($"[OutputParser] Structured parsing failed: {ex.Message}, falling back to regex");
        // Fall back to regex-based parsing
        return Parse(jsonResponse);
      }
    }

    /// <summary>
    /// Parses output with automatic detection of structured vs free-form.
    /// If the response is valid JSON matching the expected schema, uses ParseStructured.
    /// Otherwise falls back to regex-based Parse.
    /// </summary>
    /// <param name="response">The response from the LLM.</param>
    /// <param name="isStructuredOutput">Hint that the response is from structured output mode.</param>
    /// <returns>Parsed output with dialogue, mutations, and intents.</returns>
    public ParsedOutput ParseAuto(string response, bool isStructuredOutput = false)
    {
      if (string.IsNullOrWhiteSpace(response))
      {
        return ParsedOutput.Failed("Response is empty or whitespace", response ?? "");
      }

      // If explicitly marked as structured output, try structured parsing first
      if (isStructuredOutput || config.UseStructuredOutput)
      {
        // Check if it looks like JSON (starts with { and is valid JSON)
        var trimmed = response.Trim();
        if (trimmed.StartsWith("{") && JsonUtils.IsValidJson(trimmed))
        {
          var result = ParseStructured(trimmed);
          if (result.Success)
          {
            return result;
          }
          // If structured parsing failed, fall through to regex parsing
          OnLog?.Invoke("[OutputParser] Structured parsing failed, using regex fallback");
        }
      }

      // Use regex-based parsing
      return Parse(response);
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
