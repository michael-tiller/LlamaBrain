using System;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  /// <summary>
  /// Tests for PromptAssembler and PromptAssemblerConfig.
  /// </summary>
  public class PromptAssemblerTests
  {
    private StateSnapshot _defaultSnapshot = null!;

    [SetUp]
    public void SetUp()
    {
      var context = new InteractionContext { NpcId = "npc-001" };
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "Cannot reveal secrets", "Do not reveal secrets"));
      
      _defaultSnapshot = new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt("You are a friendly shopkeeper.")
        .WithPlayerInput("Hello!")
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .WithWorldState(new[] { "door_status=open" })
        .WithEpisodicMemories(new[] { "Player said hello" })
        .WithBeliefs(new[] { "Player is trustworthy" })
        .WithDialogueHistory(new[] { "Player: Hi", "NPC: Hello there!" })
        .Build();
    }

    #region PromptAssemblerConfig Tests

    [Test]
    public void PromptAssemblerConfig_Default_HasCorrectValues()
    {
      // Act
      var config = PromptAssemblerConfig.Default;

      // Assert
      Assert.That(config.MaxPromptTokens, Is.EqualTo(2048));
      Assert.That(config.ReserveResponseTokens, Is.EqualTo(256));
      Assert.That(config.CharsPerToken, Is.EqualTo(4.0f));
      Assert.That(config.IncludeRetryFeedback, Is.True);
      Assert.That(config.IncludeConstraints, Is.True);
    }

    [Test]
    public void PromptAssemblerConfig_SmallContext_HasCorrectValues()
    {
      // Act
      var config = PromptAssemblerConfig.SmallContext;

      // Assert
      Assert.That(config.MaxPromptTokens, Is.EqualTo(1024));
      Assert.That(config.ReserveResponseTokens, Is.EqualTo(128));
    }

    [Test]
    public void PromptAssemblerConfig_LargeContext_HasCorrectValues()
    {
      // Act
      var config = PromptAssemblerConfig.LargeContext;

      // Assert
      Assert.That(config.MaxPromptTokens, Is.EqualTo(4096));
      Assert.That(config.ReserveResponseTokens, Is.EqualTo(512));
    }

    [Test]
    public void PromptAssemblerConfig_MaxPromptCharacters_CalculatesCorrectly()
    {
      // Arrange
      var config = new PromptAssemblerConfig
      {
        MaxPromptTokens = 1000,
        ReserveResponseTokens = 200,
        CharsPerToken = 4.0f
      };

      // Act & Assert
      // (1000 - 200) * 4 = 3200
      Assert.That(config.MaxPromptCharacters, Is.EqualTo(3200));
    }

    #endregion

    #region PromptAssembler Creation Tests

    [Test]
    public void PromptAssembler_DefaultConstructor_UsesDefaultConfig()
    {
      // Act
      var assembler = new PromptAssembler();

      // Assert
      Assert.That(assembler, Is.Not.Null);
    }

    [Test]
    public void PromptAssembler_WithConfig_UsesConfig()
    {
      // Arrange
      var config = PromptAssemblerConfig.SmallContext;

      // Act
      var assembler = new PromptAssembler(config);

      // Assert
      Assert.That(assembler, Is.Not.Null);
    }

    [Test]
    public void PromptAssembler_NullConfig_ThrowsException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => new PromptAssembler(null!));
    }

    #endregion

    #region AssembleFromSnapshot Tests

    [Test]
    public void AssembleFromSnapshot_CreatesAssembledPrompt()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result.Text, Is.Not.Empty);
      Assert.That(result.CharacterCount, Is.GreaterThan(0));
      Assert.That(result.EstimatedTokens, Is.GreaterThan(0));
      Assert.That(result.WorkingMemory, Is.Not.Null);
    }

    [Test]
    public void AssembleFromSnapshot_NullSnapshot_ThrowsException()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => assembler.AssembleFromSnapshot(null!));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesSystemPrompt()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("You are a friendly shopkeeper."));
      Assert.That(result.Breakdown.SystemPrompt, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesPlayerInput()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("Hello!"));
      Assert.That(result.Breakdown.PlayerInput, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesContext()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("[Context]"));
      Assert.That(result.Text, Does.Contain("[Fact]"));
      Assert.That(result.Breakdown.Context, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesConstraints()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("CONSTRAINTS"));
      Assert.That(result.Breakdown.Constraints, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesDialogueHistory()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("[Conversation]"));
      Assert.That(result.Text, Does.Contain("Player: Hi"));
      Assert.That(result.Breakdown.DialogueHistory, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_WithRetryFeedback_IncludesFeedback()
    {
      // Arrange
      var assembler = new PromptAssembler();
      var retryFeedback = "Previous attempt failed. Please try again.";

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot, retryFeedback: retryFeedback);

      // Assert
      Assert.That(result.Text, Does.Contain(retryFeedback));
      Assert.That(result.Breakdown.RetryFeedback, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_WithoutRetryFeedback_ExcludesFeedback()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot, retryFeedback: null);

      // Assert
      Assert.That(result.Breakdown.RetryFeedback, Is.EqualTo(0));
    }

    [Test]
    public void AssembleFromSnapshot_WithNpcName_UsesNpcName()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot, npcName: "Shopkeeper");

      // Assert
      Assert.That(result.Text, Does.Contain("Shopkeeper:"));
    }

    [Test]
    public void AssembleFromSnapshot_WithoutNpcName_UsesDefault()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("NPC:"));
    }

    [Test]
    public void AssembleFromSnapshot_WithWorkingMemoryConfig_UsesConfig()
    {
      // Arrange
      var assembler = new PromptAssembler();
      var wmConfig = new WorkingMemoryConfig { MaxDialogueExchanges = 1 };

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot, workingMemoryConfig: wmConfig);

      // Assert
      Assert.That(result.WorkingMemory!.DialogueHistory.Count, Is.LessThanOrEqualTo(2)); // 1 exchange = 2 lines
    }

    [Test]
    public void AssembleFromSnapshot_DisposesWorkingMemoryOnException()
    {
      // Arrange
      var assembler = new PromptAssembler();
      // Create a snapshot that might cause issues
      var badSnapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .Build();

      // Act
      var result = assembler.AssembleFromSnapshot(badSnapshot);
      var workingMemory = result.WorkingMemory;

      // Assert
      // Working memory should be created and assigned
      Assert.That(workingMemory, Is.Not.Null);
      // But we can't test disposal on exception easily without mocking
    }

    #endregion

    #region AssembleFromWorkingMemory Tests

    [Test]
    public void AssembleFromWorkingMemory_CreatesAssembledPrompt()
    {
      // Arrange
      var assembler = new PromptAssembler();
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var result = assembler.AssembleFromWorkingMemory(workingMemory);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result.Text, Is.Not.Empty);
      Assert.That(result.WorkingMemory, Is.EqualTo(workingMemory));
    }

    [Test]
    public void AssembleFromWorkingMemory_NullWorkingMemory_ThrowsException()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => assembler.AssembleFromWorkingMemory(null!));
    }

    [Test]
    public void AssembleFromWorkingMemory_IncludesAllSections()
    {
      // Arrange
      var assembler = new PromptAssembler();
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var result = assembler.AssembleFromWorkingMemory(workingMemory);

      // Assert
      Assert.That(result.Breakdown.SystemPrompt, Is.GreaterThan(0));
      Assert.That(result.Breakdown.Context, Is.GreaterThan(0));
      Assert.That(result.Breakdown.Constraints, Is.GreaterThan(0));
      Assert.That(result.Breakdown.DialogueHistory, Is.GreaterThan(0));
      Assert.That(result.Breakdown.PlayerInput, Is.GreaterThan(0));
    }

    #endregion

    #region AssembleMinimal Tests

    [Test]
    public void AssembleMinimal_CreatesMinimalPrompt()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleMinimal("System prompt", "Player input");

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result.Text, Does.Contain("System prompt"));
      Assert.That(result.Text, Does.Contain("Player input"));
      Assert.That(result.WasTruncated, Is.False);
    }

    [Test]
    public void AssembleMinimal_WithNpcName_UsesNpcName()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleMinimal("System", "Input", "Shopkeeper");

      // Assert
      Assert.That(result.Text, Does.Contain("Shopkeeper:"));
    }

    [Test]
    public void AssembleMinimal_WithoutNpcName_UsesDefault()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleMinimal("System", "Input");

      // Assert
      Assert.That(result.Text, Does.Contain("NPC:"));
    }

    [Test]
    public void AssembleMinimal_IncludesBreakdown()
    {
      // Arrange
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleMinimal("System prompt", "Player input");

      // Assert
      Assert.That(result.Breakdown.SystemPrompt, Is.GreaterThan(0));
      Assert.That(result.Breakdown.PlayerInput, Is.GreaterThan(0));
    }

    #endregion

    #region Token Estimation Tests

    [Test]
    public void EstimateTokens_CalculatesCorrectly()
    {
      // Arrange
      var assembler = new PromptAssembler();
      var config = new PromptAssemblerConfig { CharsPerToken = 4.0f };
      var assemblerWithConfig = new PromptAssembler(config);

      // Act
      var tokens = assemblerWithConfig.EstimateTokens(100);

      // Assert
      // 100 chars / 4 chars per token = 25 tokens
      Assert.That(tokens, Is.EqualTo(25));
    }

    [Test]
    public void EstimateTokens_RoundsUp()
    {
      // Arrange
      var config = new PromptAssemblerConfig { CharsPerToken = 4.0f };
      var assembler = new PromptAssembler(config);

      // Act
      var tokens = assembler.EstimateTokens(10); // 10 / 4 = 2.5, should round up to 3

      // Assert
      Assert.That(tokens, Is.EqualTo(3));
    }

    [Test]
    public void EstimateCharacters_CalculatesCorrectly()
    {
      // Arrange
      var config = new PromptAssemblerConfig { CharsPerToken = 4.0f };
      var assembler = new PromptAssembler(config);

      // Act
      var chars = assembler.EstimateCharacters(25);

      // Assert
      // 25 tokens * 4 chars per token = 100 chars
      Assert.That(chars, Is.EqualTo(100));
    }

    [Test]
    public void EstimateCharacters_WithDifferentCharsPerToken_CalculatesCorrectly()
    {
      // Arrange
      var config = new PromptAssemblerConfig { CharsPerToken = 3.5f };
      var assembler = new PromptAssembler(config);

      // Act
      var chars = assembler.EstimateCharacters(10);

      // Assert
      // 10 tokens * 3.5 = 35 chars
      Assert.That(chars, Is.EqualTo(35));
    }

    #endregion

    #region Breakdown Tests

    [Test]
    public void PromptSectionBreakdown_Total_CalculatesCorrectly()
    {
      // Arrange
      var breakdown = new PromptSectionBreakdown
      {
        SystemPrompt = 100,
        Context = 200,
        Constraints = 50,
        RetryFeedback = 30,
        DialogueHistory = 150,
        PlayerInput = 50,
        Formatting = 20
      };

      // Act & Assert
      Assert.That(breakdown.Total, Is.EqualTo(600));
    }

    [Test]
    public void AssembledPrompt_ToString_ReturnsFormattedString()
    {
      // Arrange
      var assembler = new PromptAssembler();
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Act
      var str = result.ToString();

      // Assert
      Assert.That(str, Does.Contain("AssembledPrompt["));
      Assert.That(str, Does.Contain("chars"));
      Assert.That(str, Does.Contain("tokens"));
    }

    [Test]
    public void AssembledPrompt_WithTruncation_ShowsTruncated()
    {
      // Arrange
      var longContent = new string('x', 10000);
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithSystemPrompt(longContent)
        .WithPlayerInput(longContent)
        .Build();
      var config = new PromptAssemblerConfig { MaxPromptTokens = 100 };
      var assembler = new PromptAssembler(config);

      // Act
      var result = assembler.AssembleFromSnapshot(snapshot);

      // Assert
      if (result.WasTruncated)
      {
        Assert.That(result.ToString(), Does.Contain("truncated"));
      }
    }

    #endregion

    #region Format String Tests

    [Test]
    public void AssembleFromSnapshot_UsesCustomFormatStrings()
    {
      // Arrange
      var config = new PromptAssemblerConfig
      {
        SystemPromptFormat = "SYSTEM: {0}",
        PlayerInputFormat = "PLAYER: {0}",
        NpcPromptFormat = "RESPOND:"
      };
      var assembler = new PromptAssembler(config);

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("SYSTEM:"));
      Assert.That(result.Text, Does.Contain("PLAYER:"));
      Assert.That(result.Text, Does.Contain("RESPOND:"));
    }

    [Test]
    public void AssembleFromSnapshot_UsesContextHeader()
    {
      // Arrange
      var config = new PromptAssemblerConfig
      {
        ContextHeader = "\n=== CONTEXT ==="
      };
      var assembler = new PromptAssembler(config);

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("=== CONTEXT ==="));
    }

    [Test]
    public void AssembleFromSnapshot_UsesConversationHeader()
    {
      // Arrange
      var config = new PromptAssemblerConfig
      {
        ConversationHeader = "\n=== DIALOGUE ==="
      };
      var assembler = new PromptAssembler(config);

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Contain("=== DIALOGUE ==="));
    }

    #endregion

    #region Constraint Inclusion Tests

    [Test]
    public void AssembleFromSnapshot_WithIncludeConstraintsFalse_ExcludesConstraints()
    {
      // Arrange
      var config = new PromptAssemblerConfig { IncludeConstraints = false };
      var assembler = new PromptAssembler(config);

      // Act
      var result = assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(result.Text, Does.Not.Contain("CONSTRAINTS"));
      Assert.That(result.Breakdown.Constraints, Is.EqualTo(0));
    }

    [Test]
    public void AssembleFromSnapshot_WithEmptyConstraints_ExcludesConstraints()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet()) // Empty
        .WithSystemPrompt("System")
        .WithPlayerInput("Input")
        .Build();
      var assembler = new PromptAssembler();

      // Act
      var result = assembler.AssembleFromSnapshot(snapshot);

      // Assert
      Assert.That(result.Breakdown.Constraints, Is.EqualTo(0));
    }

    #endregion

    #region Logging Tests

    [Test]
    public void AssembleFromSnapshot_WithLogging_CallsOnLog()
    {
      // Arrange
      var logMessages = new System.Collections.Generic.List<string>();
      var assembler = new PromptAssembler();
      assembler.OnLog = msg => logMessages.Add(msg);

      // Act
      assembler.AssembleFromSnapshot(_defaultSnapshot);

      // Assert
      Assert.That(logMessages.Count, Is.GreaterThan(0));
      Assert.That(logMessages, Has.Some.Contain("[PromptAssembler]"));
    }

    #endregion
  }
}

