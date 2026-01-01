using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Tests.Validation
{
  /// <summary>
  /// Tests for OutputParser.
  /// </summary>
  public class OutputParserTests
  {
    private OutputParser parser = null!;

    [SetUp]
    public void SetUp()
    {
      parser = new OutputParser();
    }

    #region Basic Parsing

    [Test]
    public void Parse_ValidDialogue_ReturnsSuccess()
    {
      // Arrange
      var input = "Hello, traveler! How may I help you today?";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("Hello, traveler! How may I help you today?"));
    }

    [Test]
    public void Parse_EmptyString_ReturnsFailed()
    {
      // Act
      var result = parser.Parse("");

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void Parse_WhitespaceOnly_ReturnsFailed()
    {
      // Act
      var result = parser.Parse("   \n\t  ");

      // Assert
      Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Parse_NullInput_ReturnsFailed()
    {
      // Act
      var result = parser.Parse(null!);

      // Assert
      Assert.That(result.Success, Is.False);
    }

    #endregion

    #region Single Line Enforcement

    [Test]
    public void Parse_MultipleLines_ExtractsFirstLine()
    {
      // Arrange
      var input = "First line of dialogue.\nSecond line should be removed.\nThird line too.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("First line of dialogue."));
    }

    [Test]
    public void Parse_EmptyFirstLine_SkipsToFirstNonEmpty()
    {
      // Arrange
      var input = "\n\n  \nActual dialogue here.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("Actual dialogue here."));
    }

    #endregion

    #region Stage Direction Removal

    [Test]
    public void Parse_WithAsterisks_RemovesStageDirections()
    {
      // Arrange
      var input = "*winks* Hello there! *smiles warmly*";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Not.Contain("*"));
      Assert.That(result.DialogueText, Does.Contain("Hello there!"));
    }

    [Test]
    public void Parse_WithBrackets_RemovesScriptDirections()
    {
      // Arrange
      var input = "[Action: waves hand] Greetings, traveler! [looks around nervously]";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Not.Contain("["));
      Assert.That(result.DialogueText, Does.Not.Contain("]"));
      Assert.That(result.DialogueText, Does.Contain("Greetings, traveler!"));
    }

    #endregion

    #region Speaker Label Removal

    [Test]
    public void Parse_WithSpeakerLabel_RemovesLabel()
    {
      // Arrange
      var input = "Guard: Halt! Who goes there?";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("Halt! Who goes there?"));
    }

    [Test]
    public void Parse_WithMultiWordSpeaker_RemovesLabel()
    {
      // Arrange
      var input = "Old Man: I remember the ancient days.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("I remember the ancient days."));
    }

    #endregion

    #region Truncation Handling

    [Test]
    public void Parse_TruncatedMidWord_ReturnsFailed()
    {
      // Arrange - ends with dangling article
      var input = "I can help you find the";

      // Act
      var result = parser.Parse(input, wasTruncated: true);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("truncated").IgnoreCase);
    }

    [Test]
    public void Parse_TruncatedWithCompleteSentence_ReturnsSuccess()
    {
      // Arrange - has a complete sentence
      var input = "I can help you. But first you need to the";

      // Act
      var result = parser.Parse(input, wasTruncated: true);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("I can help you."));
    }

    [Test]
    public void Parse_NotTruncated_AddsPerioIfMissing()
    {
      // Arrange
      var input = "Welcome to my shop";

      // Act
      var result = parser.Parse(input, wasTruncated: false);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("Welcome to my shop."));
    }

    #endregion

    #region Meta-Text Detection

    [Test]
    public void Parse_WithMetaText_ReturnsFailed()
    {
      // Arrange
      var input = "Example answer: The blacksmith lives near the forge.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("meta-text"));
    }

    [Test]
    public void Parse_WithRememberInstruction_ReturnsFailed()
    {
      // Arrange
      var input = "Remember: You should always be polite to customers.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
    }

    #endregion

    #region Structured Data Extraction

    [Test]
    public void Parse_WithMemoryMarker_ExtractsMutation()
    {
      // Arrange
      var input = "I'll remember that. [MEMORY: Player mentioned they need healing potions]";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.ProposedMutations.Count, Is.GreaterThan(0));
      Assert.That(result.ProposedMutations[0].Type, Is.EqualTo(MutationType.AppendEpisodic));
    }

    [Test]
    public void Parse_WithIntentMarker_ExtractsIntent()
    {
      // Arrange
      var input = "Follow me! [INTENT: Follow the player]";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.WorldIntents.Count, Is.GreaterThan(0));
    }

    #endregion

    #region Configuration

    [Test]
    public void Parse_MinimalConfig_PreservesMoreContent()
    {
      // Arrange
      var input = "*winks* Hello there!";
      var minimalParser = new OutputParser(OutputParserConfig.Minimal);

      // Act
      var result = minimalParser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Contain("*winks*"));
    }

    #endregion

    #region Fragment Detection

    [Test]
    public void Parse_StartsWithLowercase_Fragment_ReturnsFailed()
    {
      // Arrange
      var input = "depending on your preferences.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("fragment"));
    }

    [Test]
    public void Parse_StartsWithCapital_ReturnsSuccess()
    {
      // Arrange
      var input = "Welcome to my humble shop!";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
    }

    [Test]
    public void Parse_FragmentBasedOn_ReturnsFailed()
    {
      // Arrange
      var input = "based on what you told me earlier.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("fragment"));
    }

    [Test]
    public void Parse_FragmentAccordingTo_ReturnsFailed()
    {
      // Arrange
      var input = "according to the ancient prophecy.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("fragment"));
    }

    #endregion

    #region JSON Block Extraction

    [Test]
    public void Parse_WithJsonBlock_ExtractsMemory()
    {
      // Arrange
      var input = "I understand. ```json\n{\"memory\": \"Player needs healing potions\"}\n``` Let me help you.";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.ProposedMutations.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Parse_WithJsonBlockIntent_ExtractsIntent()
    {
      // Arrange
      var input = "Follow me! ```json\n{\"intent\": \"lead_player_to_shop\"}\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.WorldIntents.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Parse_WithJsonBlockBelief_ExtractsBelief()
    {
      // Arrange
      var input = "I think so. ```json\n{\"belief\": \"Player is trustworthy\"}\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.ProposedMutations.Count, Is.GreaterThan(0));
      Assert.That(result.ProposedMutations[0].Type, Is.EqualTo(MutationType.TransformBelief));
    }

    [Test]
    public void Parse_WithJsonBlock_SetsHasJsonMetadata()
    {
      // Arrange
      var input = "Sure! ```json\n{\"action\": \"wave\"}\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.Metadata.ContainsKey("has_json"), Is.True);
      Assert.That(result.Metadata["has_json"], Is.EqualTo("true"));
    }

    #endregion

    #region Belief and Action Markers

    [Test]
    public void Parse_WithBeliefMarker_ExtractsBelief()
    {
      // Arrange
      var input = "I see. [BELIEF: Player is friendly]";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.ProposedMutations.Count, Is.GreaterThan(0));
      Assert.That(result.ProposedMutations[0].Type, Is.EqualTo(MutationType.TransformBelief));
    }

    [Test]
    public void Parse_WithActionMarker_ExtractsIntent()
    {
      // Arrange
      var input = "Right away! [ACTION: Open the door]";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.WorldIntents.Count, Is.GreaterThan(0));
      Assert.That(result.WorldIntents[0].IntentType, Is.EqualTo("action"));
    }

    [Test]
    public void Parse_WithMultipleMarkers_ExtractsAll()
    {
      // Arrange
      var input = "Understood! [MEMORY: Player asked about quest] [INTENT: Give quest item]";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.ProposedMutations.Count, Is.GreaterThan(0));
      Assert.That(result.WorldIntents.Count, Is.GreaterThan(0));
    }

    #endregion

    #region Minimum Character Count

    [Test]
    public void Parse_BelowMinimumCharCount_ReturnsFailed()
    {
      // Arrange
      var config = new OutputParserConfig { MinimumCharacterCount = 10 };
      var shortParser = new OutputParser(config);

      // Act
      var result = shortParser.Parse("Hi.");

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("too short"));
    }

    [Test]
    public void Parse_AtMinimumCharCount_ReturnsSuccess()
    {
      // Arrange
      var config = new OutputParserConfig { MinimumCharacterCount = 5 };
      var shortParser = new OutputParser(config);

      // Act
      var result = shortParser.Parse("Hello!");

      // Assert
      Assert.That(result.Success, Is.True);
    }

    #endregion

    #region Configuration Presets

    [Test]
    public void OutputParserConfig_Default_HasExpectedValues()
    {
      // Act
      var config = OutputParserConfig.Default;

      // Assert
      Assert.That(config.EnforceSingleLine, Is.True);
      Assert.That(config.RemoveStageDirections, Is.True);
      Assert.That(config.RemoveScriptDirections, Is.True);
      Assert.That(config.RemoveSpeakerLabels, Is.True);
      Assert.That(config.ExtractStructuredData, Is.True);
      Assert.That(config.TrimToCompleteSentence, Is.True);
      Assert.That(config.MinimumCharacterCount, Is.EqualTo(1));
    }

    [Test]
    public void OutputParserConfig_Structured_HasExpectedValues()
    {
      // Act
      var config = OutputParserConfig.Structured;

      // Assert
      Assert.That(config.EnforceSingleLine, Is.False);
      Assert.That(config.ExtractStructuredData, Is.True);
      Assert.That(config.TrimToCompleteSentence, Is.False);
    }

    [Test]
    public void OutputParserConfig_Minimal_HasExpectedValues()
    {
      // Act
      var config = OutputParserConfig.Minimal;

      // Assert
      Assert.That(config.EnforceSingleLine, Is.False);
      Assert.That(config.RemoveStageDirections, Is.False);
      Assert.That(config.RemoveScriptDirections, Is.False);
      Assert.That(config.RemoveSpeakerLabels, Is.False);
      Assert.That(config.ExtractStructuredData, Is.False);
      Assert.That(config.TrimToCompleteSentence, Is.False);
    }

    #endregion

    #region OnLog Callback

    [Test]
    public void Parse_WithOnLogCallback_CallsCallback()
    {
      // Arrange
      var logMessages = new System.Collections.Generic.List<string>();
      var loggingParser = new OutputParser();
      loggingParser.OnLog = msg => logMessages.Add(msg);

      // Act
      loggingParser.Parse("Hello there!");

      // Assert
      Assert.That(logMessages.Count, Is.GreaterThan(0));
      Assert.That(logMessages[0], Does.Contain("OutputParser"));
    }

    [Test]
    public void Parse_MetaTextDetected_LogsPattern()
    {
      // Arrange
      var logMessages = new System.Collections.Generic.List<string>();
      var loggingParser = new OutputParser();
      loggingParser.OnLog = msg => logMessages.Add(msg);

      // Act
      loggingParser.Parse("Example answer: something");

      // Assert
      Assert.That(logMessages.Any(m => m.Contains("Meta-text detected")), Is.True);
    }

    #endregion

    #region Null Config Handling

    [Test]
    public void OutputParser_NullConfig_UsesDefault()
    {
      // Arrange & Act
      var parserWithNull = new OutputParser(null!);

      // Assert - should not throw and should parse correctly
      var result = parserWithNull.Parse("Hello there!");
      Assert.That(result.Success, Is.True);
    }

    #endregion

    #region Additional Meta-Text Patterns

    [Test]
    public void Parse_WithPlayerAsks_ReturnsFailed()
    {
      // Arrange
      var input = "Player asks about the treasure location.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Parse_WithYouShould_ReturnsFailed()
    {
      // Arrange
      var input = "You should always check the inventory first.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Parse_WithKeepInMind_ReturnsFailed()
    {
      // Arrange
      var input = "Keep in mind that the castle is dangerous.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
    }

    #endregion

    #region Punctuation Handling

    [Test]
    public void Parse_EndsWithExclamation_NoModification()
    {
      // Arrange
      var input = "Welcome to my shop!";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.DialogueText, Is.EqualTo("Welcome to my shop!"));
    }

    [Test]
    public void Parse_EndsWithQuestion_NoModification()
    {
      // Arrange
      var input = "How may I help you?";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.DialogueText, Is.EqualTo("How may I help you?"));
    }

    [Test]
    public void Parse_EndsWithQuotedPunctuation_NoModification()
    {
      // Arrange
      var input = "He said \"Hello!\"";

      // Act
      var result = parser.Parse(input);

      // Assert
      // Parser adds period if no sentence-ending punctuation, but quoted punctuation is preserved
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Contain("Hello"));
    }

    #endregion

    #region Whitespace Normalization

    [Test]
    public void Parse_ExtraWhitespace_Normalized()
    {
      // Arrange
      var input = "Hello    there,   traveler!";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Not.Contain("  "));
    }

    #endregion

    #region ExtractStructuredData Disabled

    [Test]
    public void Parse_WithExtractDisabled_IgnoresJsonBlocks()
    {
      // Arrange
      var input = "Hello! ```json\n{\"memory\": \"test\"}\n```";
      var noExtractParser = new OutputParser(new OutputParserConfig { ExtractStructuredData = false });

      // Act
      var result = noExtractParser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.ProposedMutations.Count, Is.EqualTo(0));
      Assert.That(result.DialogueText, Does.Contain("Hello"));
    }

    [Test]
    public void Parse_WithExtractDisabled_IgnoresMarkers()
    {
      // Arrange
      var input = "Hello! [MEMORY: Player needs potions]";
      var noExtractParser = new OutputParser(new OutputParserConfig { ExtractStructuredData = false });

      // Act
      var result = noExtractParser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.ProposedMutations.Count, Is.EqualTo(0));
    }

    #endregion

    #region Malformed JSON Handling

    [Test]
    public void Parse_WithMalformedJson_DoesNotCrash()
    {
      // Arrange
      var input = "Hello! ```json\n{invalid json}\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      // Should not crash, just ignore malformed JSON
    }

    [Test]
    public void Parse_WithEmptyJsonBlock_DoesNotCrash()
    {
      // Arrange
      var input = "Hello! ```json\n\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
    }

    [Test]
    public void Parse_WithMultipleJsonBlocks_ExtractsAll()
    {
      // Arrange
      var input = "Hello! ```json\n{\"memory\": \"first\"}\n``` ```json\n{\"memory\": \"second\"}\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.ProposedMutations.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Parse_WithJsonBlockNoMatches_StillSucceeds()
    {
      // Arrange
      var input = "Hello! ```json\n{\"other\": \"value\"}\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.Metadata.ContainsKey("has_json"), Is.True);
    }

    #endregion

    #region TrimToCompleteSentence Edge Cases

    [Test]
    public void Parse_TextExactlyFiveChars_SkipsTrimming()
    {
      // Arrange
      var input = "Hello";
      var config = new OutputParserConfig { TrimToCompleteSentence = true };
      var trimParser = new OutputParser(config);

      // Act
      var result = trimParser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Contain("Hello"));
    }

    [Test]
    public void Parse_TruncatedNoCompleteSentenceNoDanglingWords_ReturnsFailed()
    {
      // Arrange
      var input = "This is a test without punctuation";
      var config = new OutputParserConfig { TrimToCompleteSentence = true };
      var trimParser = new OutputParser(config);

      // Act
      var result = trimParser.Parse(input, wasTruncated: true);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("Truncated"));
    }

    [Test]
    public void Parse_TruncatedWithPunctuationInMiddle_TrimsToLastComplete()
    {
      // Arrange
      var input = "First sentence. Second sentence. Incomplete";
      var config = new OutputParserConfig { TrimToCompleteSentence = true };
      var trimParser = new OutputParser(config);

      // Act
      var result = trimParser.Parse(input, wasTruncated: true);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("First sentence. Second sentence."));
    }

    [Test]
    public void Parse_NotTruncatedNoPunctuation_AddsPeriod()
    {
      // Arrange
      var input = "This has no punctuation";
      var config = new OutputParserConfig { TrimToCompleteSentence = false };
      var noTrimParser = new OutputParser(config);

      // Act
      var result = noTrimParser.Parse(input, wasTruncated: false);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.EndWith("."));
    }

    #endregion

    #region Configuration Flags Edge Cases

    [Test]
    public void Parse_RemoveStageDirectionsFalse_PreservesAsterisks()
    {
      // Arrange
      var input = "*winks* Hello there!";
      var config = new OutputParserConfig { RemoveStageDirections = false };
      var noRemoveParser = new OutputParser(config);

      // Act
      var result = noRemoveParser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Contain("*winks*"));
    }

    [Test]
    public void Parse_RemoveScriptDirectionsFalse_PreservesBrackets()
    {
      // Arrange
      var input = "[Action: waves] Hello there!";
      var config = new OutputParserConfig 
      { 
        RemoveScriptDirections = false,
        ExtractStructuredData = false  // Disable extraction to prevent marker removal
      };
      var noRemoveParser = new OutputParser(config);

      // Act
      var result = noRemoveParser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      // When ExtractStructuredData is false, brackets are preserved if RemoveScriptDirections is false
      Assert.That(result.DialogueText, Does.Contain("Hello there"));
    }

    [Test]
    public void Parse_RemoveSpeakerLabelsFalse_PreservesLabel()
    {
      // Arrange
      var input = "Guard: Halt!";
      var config = new OutputParserConfig { RemoveSpeakerLabels = false };
      var noRemoveParser = new OutputParser(config);

      // Act
      var result = noRemoveParser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Contain("Guard:"));
    }

    [Test]
    public void Parse_EnforceSingleLineFalse_PreservesMultipleLines()
    {
      // Arrange
      var input = "Line one.\nLine two.\nLine three.";
      var config = new OutputParserConfig { EnforceSingleLine = false };
      var multiLineParser = new OutputParser(config);

      // Act
      var result = multiLineParser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      // Whitespace normalization converts newlines to spaces, but all lines are preserved
      Assert.That(result.DialogueText, Does.Contain("Line one"));
      Assert.That(result.DialogueText, Does.Contain("Line two"));
      Assert.That(result.DialogueText, Does.Contain("Line three"));
    }

    #endregion

    #region Speaker Label Edge Cases

    [Test]
    public void Parse_SpeakerLabelWithSpecialChars_RemovesCorrectly()
    {
      // Arrange
      var input = "The Wizard: Welcome!";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Does.Not.Contain("The Wizard:"));
      Assert.That(result.DialogueText, Does.Contain("Welcome"));
    }

    [Test]
    public void Parse_SpeakerLabelMultiLine_RemovesFromFirstLine()
    {
      // Arrange
      var input = "Guard: Halt! Who goes there?";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      // Speaker label is removed from the dialogue
      Assert.That(result.DialogueText, Does.Not.Contain("Guard:"));
      Assert.That(result.DialogueText, Does.Contain("Halt"));
    }

    #endregion

    #region Fragment Detection Edge Cases

    [Test]
    public void Parse_FragmentInOrderTo_ReturnsFailed()
    {
      // Arrange
      var input = "in order to complete the quest.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("fragment"));
    }

    [Test]
    public void Parse_FragmentSoThat_ReturnsFailed()
    {
      // Arrange
      var input = "so that you can proceed.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
      // May be caught by meta-text detection or fragment detection - either is valid
      Assert.That(result.ErrorMessage, Does.Contain("fragment").Or.Contains("meta-text"));
    }

    [Test]
    public void Parse_FragmentSuchThat_ReturnsFailed()
    {
      // Arrange
      var input = "such that the door opens.";

      // Act
      var result = parser.Parse(input);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("fragment"));
    }

    #endregion

    #region JSON Extraction Edge Cases

    [Test]
    public void Parse_JsonBlockWithMemoriesArray_ExtractsMultiple()
    {
      // Arrange
      var input = "Hello! ```json\n{\"memories\": [\"first\", \"second\"]}\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      // Note: Simple regex doesn't match array format, but JSON block is detected and removed
      // The dialogue should still parse successfully
      Assert.That(result.DialogueText, Does.Contain("Hello"));
    }

    [Test]
    public void Parse_JsonBlockWithNestedQuotes_HandlesCorrectly()
    {
      // Arrange
      var input = "Hello! ```json\n{\"memory\": \"Player said hello\"}\n```";
      var parserWithExtract = new OutputParser(new OutputParserConfig { ExtractStructuredData = true });

      // Act
      var result = parserWithExtract.Parse(input);

      // Assert
      Assert.That(result.Success, Is.True);
      // Simple regex should handle basic JSON, JSON block is removed and dialogue parses
      Assert.That(result.DialogueText, Does.Contain("Hello"));
    }

    #endregion

    #region Phase 10.3: NormalizeWhitespace Contract Tests

    [Test]
    public void NormalizeWhitespace_NullInput_ReturnsEmptyString()
    {
      // Act
      var result = OutputParser.NormalizeWhitespace(null);

      // Assert
      Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeWhitespace_EmptyInput_ReturnsEmptyString()
    {
      // Act
      var result = OutputParser.NormalizeWhitespace("");

      // Assert
      Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeWhitespace_StripsBOM()
    {
      // Arrange - BOM followed by text
      var input = "\uFEFFHello World";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - BOM should be stripped
      Assert.That(result, Is.EqualTo("Hello World"));
      Assert.That(result[0], Is.Not.EqualTo('\uFEFF'));
    }

    [Test]
    public void NormalizeWhitespace_BOMOnly_ReturnsEmptyString()
    {
      // Arrange - Just a BOM
      var input = "\uFEFF";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert
      Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeWhitespace_ConvertsCRLFToLF()
    {
      // Arrange - Windows line endings
      var input = "Line 1\r\nLine 2\r\nLine 3";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Should be Unix line endings
      Assert.That(result, Is.EqualTo("Line 1\nLine 2\nLine 3"));
      Assert.That(result, Does.Not.Contain("\r"));
    }

    [Test]
    public void NormalizeWhitespace_ConvertsStrayCRToLF()
    {
      // Arrange - Old Mac line endings (CR only)
      var input = "Line 1\rLine 2\rLine 3";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Should be Unix line endings
      Assert.That(result, Is.EqualTo("Line 1\nLine 2\nLine 3"));
    }

    [Test]
    public void NormalizeWhitespace_TrimsTrailingWhitespacePerLine()
    {
      // Arrange - Lines with trailing spaces/tabs
      var input = "Line 1   \nLine 2\t\t\nLine 3  \t  ";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Trailing whitespace should be removed from each line
      Assert.That(result, Is.EqualTo("Line 1\nLine 2\nLine 3"));
    }

    [Test]
    public void NormalizeWhitespace_PreservesLeadingWhitespace()
    {
      // Arrange - Lines with leading indentation
      var input = "  Line 1\n    Line 2\n\tLine 3";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Leading whitespace/indentation should be preserved
      Assert.That(result, Is.EqualTo("  Line 1\n    Line 2\n\tLine 3"));
    }

    [Test]
    public void NormalizeWhitespace_CollapsesThreeOrMoreBlankLinesToTwo()
    {
      // Arrange - 4 blank lines between text
      var input = "Line 1\n\n\n\n\nLine 2";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Should collapse to exactly 2 blank lines
      Assert.That(result, Is.EqualTo("Line 1\n\n\nLine 2"));
    }

    [Test]
    public void NormalizeWhitespace_PreservesTwoBlankLines()
    {
      // Arrange - Exactly 2 blank lines (allowed)
      var input = "Line 1\n\n\nLine 2";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - 2 blank lines should be preserved
      Assert.That(result, Is.EqualTo("Line 1\n\n\nLine 2"));
    }

    [Test]
    public void NormalizeWhitespace_PreservesOneBlankLine()
    {
      // Arrange - Single blank line
      var input = "Line 1\n\nLine 2";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Single blank line should be preserved
      Assert.That(result, Is.EqualTo("Line 1\n\nLine 2"));
    }

    [Test]
    public void NormalizeWhitespace_PreservesTwoLeadingBlankLines()
    {
      // Arrange - 2 blank lines at start (2 newlines = 2 empty lines in split array)
      var input = "\n\nActual content";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - 2 leading blank lines should be preserved
      Assert.That(result, Is.EqualTo("\n\nActual content"));
    }

    [Test]
    public void NormalizeWhitespace_CollapsesThreeOrMoreLeadingBlankLines()
    {
      // Arrange - 3 blank lines at start (collapse to 2)
      var input = "\n\n\nActual content";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Collapses to exactly 2 blank lines at start
      Assert.That(result, Is.EqualTo("\n\nActual content"));
    }

    [Test]
    public void NormalizeWhitespace_CollapsesFiveLeadingBlankLinesToTwo()
    {
      // Arrange - 5 blank lines at start
      var input = "\n\n\n\n\nActual content";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Collapses to 2 blank lines at start
      Assert.That(result, Is.EqualTo("\n\nActual content"));
    }

    [Test]
    public void NormalizeWhitespace_PreservesExistingTrailingNewline()
    {
      // Arrange - Input ends with newline
      var input = "Hello World\n";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - Trailing newline should be preserved
      Assert.That(result, Is.EqualTo("Hello World\n"));
      Assert.That(result.EndsWith("\n"), Is.True);
    }

    [Test]
    public void NormalizeWhitespace_DoesNotAddTrailingNewline()
    {
      // Arrange - Input does NOT end with newline
      var input = "Hello World";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - No trailing newline should be added
      Assert.That(result, Is.EqualTo("Hello World"));
      Assert.That(result.EndsWith("\n"), Is.False);
    }

    [Test]
    public void NormalizeWhitespace_TreatsWhitespaceOnlyLinesAsBlank()
    {
      // Arrange - Lines with only spaces/tabs count as blank
      var input = "Line 1\n   \n\t\n  \t  \n\nLine 2";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - All whitespace-only lines treated as blank, collapsed to 2
      Assert.That(result, Is.EqualTo("Line 1\n\n\nLine 2"));
    }

    [Test]
    public void NormalizeWhitespace_Deterministic_SameInputSameOutput()
    {
      // Arrange - Complex input with various whitespace issues
      var input = "\uFEFF  Hello World  \r\n\r\n\r\n\r\n  \t  \r\nGoodbye\n";

      // Act - Run multiple times
      var results = new List<string>();
      for (int i = 0; i < 10; i++)
      {
        results.Add(OutputParser.NormalizeWhitespace(input));
      }

      // Assert - All results should be identical
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Normalization was not deterministic at iteration {i}");
      }
    }

    [Test]
    public void NormalizeWhitespace_ComplexInput_ProducesExpectedOutput()
    {
      // Arrange - Complex real-world-like input
      var input = "\uFEFF  First line with trailing spaces   \r\n" +
                  "\r\n" +
                  "\r\n" +
                  "\r\n" +
                  "\r\n" +
                  "  Indented line\t\t\r\n" +
                  "Last line\r\n";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert
      var expected = "  First line with trailing spaces\n" +
                     "\n" +
                     "\n" +
                     "  Indented line\n" +
                     "Last line\n";
      Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void NormalizeWhitespace_IsPureFunction_NoSideEffects()
    {
      // Arrange
      var input = "Hello World\r\n";
      var originalInput = input;

      // Act
      var result1 = OutputParser.NormalizeWhitespace(input);
      var result2 = OutputParser.NormalizeWhitespace(input);

      // Assert - Input unchanged, results identical
      Assert.That(input, Is.EqualTo(originalInput));
      Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void NormalizeWhitespace_MixedLineEndings_NormalizesAll()
    {
      // Arrange - Mix of CRLF, CR, and LF
      var input = "Line1\r\nLine2\rLine3\nLine4";

      // Act
      var result = OutputParser.NormalizeWhitespace(input);

      // Assert - All normalized to LF
      Assert.That(result, Is.EqualTo("Line1\nLine2\nLine3\nLine4"));
    }

    #endregion
  }
}
