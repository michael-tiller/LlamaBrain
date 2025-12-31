using System;
using System.Collections.Generic;
using System.Text;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// Configuration for prompt assembly.
  /// </summary>
  public class PromptAssemblerConfig
  {
    /// <summary>
    /// Maximum total tokens for the prompt (estimated).
    /// Default: 2048.
    /// </summary>
    public int MaxPromptTokens { get; set; } = 2048;

    /// <summary>
    /// Reserve tokens for the response.
    /// Default: 256.
    /// </summary>
    public int ReserveResponseTokens { get; set; } = 256;

    /// <summary>
    /// Average characters per token for estimation.
    /// Default: 4 (typical for English text with llama tokenizers).
    /// </summary>
    public float CharsPerToken { get; set; } = 4.0f;

    /// <summary>
    /// Whether to include retry feedback in the prompt.
    /// Default: true.
    /// </summary>
    public bool IncludeRetryFeedback { get; set; } = true;

    /// <summary>
    /// Whether to include constraint text in the prompt.
    /// Default: true.
    /// </summary>
    public bool IncludeConstraints { get; set; } = true;

    /// <summary>
    /// Format string for system prompt section.
    /// {0} = system prompt text.
    /// </summary>
    public string SystemPromptFormat { get; set; } = "System: {0}";

    /// <summary>
    /// Format string for context section header.
    /// </summary>
    public string ContextHeader { get; set; } = "\n[Context]";

    /// <summary>
    /// Format string for conversation section header.
    /// </summary>
    public string ConversationHeader { get; set; } = "\n[Conversation]";

    /// <summary>
    /// Format string for player input.
    /// {0} = player input text.
    /// </summary>
    public string PlayerInputFormat { get; set; } = "\nPlayer: {0}";

    /// <summary>
    /// Format string for NPC response prompt.
    /// {0} = NPC name.
    /// </summary>
    public string NpcPromptFormat { get; set; } = "\n{0}:";

    /// <summary>
    /// Default NPC name if not specified.
    /// </summary>
    public string DefaultNpcName { get; set; } = "NPC";

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static PromptAssemblerConfig Default => new PromptAssemblerConfig();

    /// <summary>
    /// Creates a configuration for smaller context windows.
    /// </summary>
    public static PromptAssemblerConfig SmallContext => new PromptAssemblerConfig
    {
      MaxPromptTokens = 1024,
      ReserveResponseTokens = 128,
      CharsPerToken = 4.0f
    };

    /// <summary>
    /// Creates a configuration for larger context windows.
    /// </summary>
    public static PromptAssemblerConfig LargeContext => new PromptAssemblerConfig
    {
      MaxPromptTokens = 4096,
      ReserveResponseTokens = 512,
      CharsPerToken = 4.0f
    };

    /// <summary>
    /// Gets the maximum characters for the prompt based on token settings.
    /// </summary>
    public int MaxPromptCharacters => (int)((MaxPromptTokens - ReserveResponseTokens) * CharsPerToken);
  }

  /// <summary>
  /// Result of prompt assembly.
  /// </summary>
  public class AssembledPrompt
  {
    /// <summary>
    /// The assembled prompt text.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Estimated token count.
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// Character count.
    /// </summary>
    public int CharacterCount { get; set; }

    /// <summary>
    /// Whether the prompt was truncated to fit limits.
    /// </summary>
    public bool WasTruncated { get; set; }

    /// <summary>
    /// Breakdown of character usage by section.
    /// </summary>
    public PromptSectionBreakdown Breakdown { get; set; } = new PromptSectionBreakdown();

    /// <summary>
    /// The working memory used to build this prompt.
    /// </summary>
    public EphemeralWorkingMemory? WorkingMemory { get; set; }

    /// <summary>
    /// Returns a string representation.
    /// </summary>
    /// <returns>A string representation of the assembled prompt</returns>
    public override string ToString()
    {
      var truncated = WasTruncated ? " (truncated)" : "";
      return $"AssembledPrompt[{CharacterCount} chars, ~{EstimatedTokens} tokens{truncated}]";
    }
  }

  /// <summary>
  /// Breakdown of prompt sections by character count.
  /// </summary>
  public class PromptSectionBreakdown
  {
    /// <summary>Characters used by system prompt.</summary>
    public int SystemPrompt { get; set; }

    /// <summary>Characters used by context (memories).</summary>
    public int Context { get; set; }

    /// <summary>Characters used by constraints.</summary>
    public int Constraints { get; set; }

    /// <summary>Characters used by retry feedback.</summary>
    public int RetryFeedback { get; set; }

    /// <summary>Characters used by dialogue history.</summary>
    public int DialogueHistory { get; set; }

    /// <summary>Characters used by player input.</summary>
    public int PlayerInput { get; set; }

    /// <summary>Characters used by formatting/headers.</summary>
    public int Formatting { get; set; }

    /// <summary>Total characters.</summary>
    public int Total => SystemPrompt + Context + Constraints + RetryFeedback +
                        DialogueHistory + PlayerInput + Formatting;
  }

  /// <summary>
  /// Assembles prompts from state snapshots and working memory.
  /// Enforces token limits and produces bounded, constrained prompts.
  /// </summary>
  public class PromptAssembler
  {
    private readonly PromptAssemblerConfig _config;

    /// <summary>
    /// Optional logging callback.
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// Creates a new prompt assembler with default configuration.
    /// </summary>
    public PromptAssembler() : this(PromptAssemblerConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new prompt assembler with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration to use</param>
    public PromptAssembler(PromptAssemblerConfig config)
    {
      _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Assembles a prompt from a state snapshot.
    /// Creates working memory internally and manages its lifecycle.
    /// </summary>
    /// <param name="snapshot">The state snapshot</param>
    /// <param name="npcName">Optional NPC name for the response prompt</param>
    /// <param name="retryFeedback">Optional retry feedback from previous attempt</param>
    /// <param name="workingMemoryConfig">Optional working memory configuration</param>
    /// <returns>The assembled prompt</returns>
    public AssembledPrompt AssembleFromSnapshot(
      StateSnapshot snapshot,
      string? npcName = null,
      string? retryFeedback = null,
      WorkingMemoryConfig? workingMemoryConfig = null)
    {
      if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

      // Create working memory with appropriate bounds
      var wmConfig = workingMemoryConfig ?? CreateWorkingMemoryConfig();
      var workingMemory = new EphemeralWorkingMemory(snapshot, wmConfig);

      try
      {
        return AssembleFromWorkingMemory(workingMemory, npcName, retryFeedback);
      }
      catch
      {
        workingMemory.Dispose();
        throw;
      }
    }

    /// <summary>
    /// Assembles a prompt from pre-built working memory.
    /// </summary>
    /// <param name="workingMemory">The working memory</param>
    /// <param name="npcName">Optional NPC name for the response prompt</param>
    /// <param name="retryFeedback">Optional retry feedback from previous attempt</param>
    /// <returns>The assembled prompt</returns>
    public AssembledPrompt AssembleFromWorkingMemory(
      EphemeralWorkingMemory workingMemory,
      string? npcName = null,
      string? retryFeedback = null)
    {
      if (workingMemory == null) throw new ArgumentNullException(nameof(workingMemory));

      var effectiveNpcName = npcName ?? _config.DefaultNpcName;
      var builder = new StringBuilder();
      var breakdown = new PromptSectionBreakdown();
      var wasTruncated = workingMemory.WasTruncated;

      // 1. System prompt
      if (!string.IsNullOrEmpty(workingMemory.SystemPrompt))
      {
        var systemSection = string.Format(_config.SystemPromptFormat, workingMemory.SystemPrompt);
        builder.Append(systemSection);
        breakdown.SystemPrompt = systemSection.Length;
      }

      // 2. Context (memories)
      var contextText = workingMemory.GetFormattedContext();
      if (!string.IsNullOrEmpty(contextText))
      {
        builder.Append(_config.ContextHeader);
        builder.Append("\n");
        builder.Append(contextText);
        breakdown.Context = contextText.Length;
        breakdown.Formatting += _config.ContextHeader.Length + 1;
      }

      // 3. Constraints
      if (_config.IncludeConstraints && workingMemory.Constraints.HasConstraints)
      {
        var constraintText = workingMemory.Constraints.ToPromptInjection();
        builder.Append(constraintText);
        breakdown.Constraints = constraintText.Length;
      }

      // 4. Retry feedback
      if (_config.IncludeRetryFeedback && !string.IsNullOrEmpty(retryFeedback))
      {
        builder.Append("\n");
        builder.Append(retryFeedback);
        breakdown.RetryFeedback = retryFeedback.Length + 1;
      }

      // 5. Dialogue history
      var dialogueText = workingMemory.GetFormattedDialogue();
      if (!string.IsNullOrEmpty(dialogueText))
      {
        builder.Append(_config.ConversationHeader);
        builder.Append("\n");
        builder.Append(dialogueText);
        breakdown.DialogueHistory = dialogueText.Length;
        breakdown.Formatting += _config.ConversationHeader.Length + 1;
      }

      // 6. Player input
      var playerSection = string.Format(_config.PlayerInputFormat, workingMemory.PlayerInput);
      builder.Append(playerSection);
      breakdown.PlayerInput = playerSection.Length;

      // 7. NPC response prompt
      var npcPrompt = string.Format(_config.NpcPromptFormat, effectiveNpcName);
      builder.Append(npcPrompt);
      breakdown.Formatting += npcPrompt.Length;

      var promptText = builder.ToString();
      var charCount = promptText.Length;
      var estimatedTokens = EstimateTokens(charCount);

      // Check if we exceeded limits and need to truncate
      if (charCount > _config.MaxPromptCharacters)
      {
        wasTruncated = true;
        OnLog?.Invoke($"[PromptAssembler] Prompt exceeded limit ({charCount} > {_config.MaxPromptCharacters}), truncating");
        // For now, we rely on working memory truncation
        // Future: implement more aggressive truncation here
      }

      var result = new AssembledPrompt
      {
        Text = promptText,
        CharacterCount = charCount,
        EstimatedTokens = estimatedTokens,
        WasTruncated = wasTruncated,
        Breakdown = breakdown,
        WorkingMemory = workingMemory
      };

      OnLog?.Invoke($"[PromptAssembler] Assembled: {result}");

      return result;
    }

    /// <summary>
    /// Creates a working memory config based on the prompt assembler config.
    /// </summary>
    private WorkingMemoryConfig CreateWorkingMemoryConfig()
    {
      // Calculate character budget for working memory
      // Reserve space for formatting and player input
      var formattingReserve = 200; // Headers, formatting, NPC prompt
      var maxContextChars = _config.MaxPromptCharacters - formattingReserve;

      return new WorkingMemoryConfig
      {
        MaxContextCharacters = Math.Max(500, maxContextChars),
        MaxDialogueExchanges = 5,
        MaxEpisodicMemories = 5,
        MaxBeliefs = 3,
        AlwaysIncludeCanonicalFacts = true,
        AlwaysIncludeWorldState = true
      };
    }

    /// <summary>
    /// Estimates token count from character count.
    /// </summary>
    /// <param name="characters">Number of characters</param>
    /// <returns>Estimated token count</returns>
    public int EstimateTokens(int characters)
    {
      return (int)Math.Ceiling(characters / _config.CharsPerToken);
    }

    /// <summary>
    /// Estimates character count from token count.
    /// </summary>
    /// <param name="tokens">Number of tokens</param>
    /// <returns>Estimated character count</returns>
    public int EstimateCharacters(int tokens)
    {
      return (int)(tokens * _config.CharsPerToken);
    }

    /// <summary>
    /// Creates a minimal prompt for testing or simple use cases.
    /// </summary>
    /// <param name="systemPrompt">The system prompt</param>
    /// <param name="playerInput">The player input</param>
    /// <param name="npcName">Optional NPC name</param>
    /// <returns>The assembled prompt</returns>
    public AssembledPrompt AssembleMinimal(
      string systemPrompt,
      string playerInput,
      string? npcName = null)
    {
      var effectiveNpcName = npcName ?? _config.DefaultNpcName;
      var builder = new StringBuilder();

      if (!string.IsNullOrEmpty(systemPrompt))
      {
        builder.Append(string.Format(_config.SystemPromptFormat, systemPrompt));
      }

      builder.Append(string.Format(_config.PlayerInputFormat, playerInput));
      builder.Append(string.Format(_config.NpcPromptFormat, effectiveNpcName));

      var text = builder.ToString();

      return new AssembledPrompt
      {
        Text = text,
        CharacterCount = text.Length,
        EstimatedTokens = EstimateTokens(text.Length),
        WasTruncated = false,
        Breakdown = new PromptSectionBreakdown
        {
          SystemPrompt = systemPrompt.Length,
          PlayerInput = playerInput.Length
        }
      };
    }
  }
}
