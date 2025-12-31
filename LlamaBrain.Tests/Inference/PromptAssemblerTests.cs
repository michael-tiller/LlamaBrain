using System;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  [TestFixture]
  public class PromptAssemblerTests
  {
    private PromptAssembler _assembler;

    [SetUp]
    public void SetUp()
    {
      _assembler = new PromptAssembler();
    }

    private StateSnapshot CreateTestSnapshot(
      string systemPrompt = "You are a helpful NPC.",
      string playerInput = "Hello there!",
      int constraintCount = 0)
    {
      var builder = new StateSnapshotBuilder()
        .WithSystemPrompt(systemPrompt)
        .WithPlayerInput(playerInput)
        .WithDialogueHistory(new[] { "Player: Hi", "NPC: Hello!" })
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .WithWorldState(new[] { "door_castle: open" })
        .WithEpisodicMemories(new[] { "Player arrived at noon" })
        .WithBeliefs(new[] { "Player seems friendly" });

      if (constraintCount > 0)
      {
        var constraints = new ConstraintSet();
        for (int i = 0; i < constraintCount; i++)
        {
          constraints.Add(Constraint.Prohibit($"Prohibition {i}", "test"));
        }
        builder.WithConstraints(constraints);
      }

      return builder.Build();
    }

    [Test]
    public void Constructor_AcceptsDefaultConfig()
    {
      var assembler = new PromptAssembler();
      Assert.That(assembler, Is.Not.Null);
    }

    [Test]
    public void Constructor_AcceptsCustomConfig()
    {
      var config = new PromptAssemblerConfig
      {
        MaxPromptTokens = 1024,
        DefaultNpcName = "Wizard"
      };

      var assembler = new PromptAssembler(config);
      Assert.That(assembler, Is.Not.Null);
    }

    [Test]
    public void Constructor_ThrowsOnNullConfig()
    {
      Assert.Throws<ArgumentNullException>(() => new PromptAssembler(null));
    }

    [Test]
    public void AssembleFromSnapshot_CreatesPrompt()
    {
      var snapshot = CreateTestSnapshot();
      var result = _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result, Is.Not.Null);
      Assert.That(result.Text, Is.Not.Empty);
      Assert.That(result.CharacterCount, Is.GreaterThan(0));
      Assert.That(result.EstimatedTokens, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_ThrowsOnNullSnapshot()
    {
      Assert.Throws<ArgumentNullException>(() => _assembler.AssembleFromSnapshot(null));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesSystemPrompt()
    {
      var snapshot = CreateTestSnapshot(systemPrompt: "You are a wise wizard.");
      var result = _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result.Text, Does.Contain("You are a wise wizard"));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesPlayerInput()
    {
      var snapshot = CreateTestSnapshot(playerInput: "What is your name?");
      var result = _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result.Text, Does.Contain("What is your name?"));
      Assert.That(result.Text, Does.Contain("Player:"));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesNpcPrompt()
    {
      var snapshot = CreateTestSnapshot();
      var result = _assembler.AssembleFromSnapshot(snapshot, npcName: "Gandalf");

      Assert.That(result.Text, Does.Contain("Gandalf:"));
    }

    [Test]
    public void AssembleFromSnapshot_UsesDefaultNpcName()
    {
      var snapshot = CreateTestSnapshot();
      var result = _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result.Text, Does.Contain("NPC:"));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesContext()
    {
      var snapshot = CreateTestSnapshot();
      var result = _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result.Text, Does.Contain("[Context]"));
      Assert.That(result.Text, Does.Contain("[Fact]"));
      Assert.That(result.Text, Does.Contain("The king is Arthur"));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesDialogueHistory()
    {
      var snapshot = CreateTestSnapshot();
      var result = _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result.Text, Does.Contain("[Conversation]"));
      Assert.That(result.Text, Does.Contain("Player: Hi"));
      Assert.That(result.Text, Does.Contain("NPC: Hello!"));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesConstraints_WhenConfigured()
    {
      var snapshot = CreateTestSnapshot(constraintCount: 2);
      var config = new PromptAssemblerConfig { IncludeConstraints = true };
      var assembler = new PromptAssembler(config);

      var result = assembler.AssembleFromSnapshot(snapshot);

      // Constraints should be present (depends on ToPromptInjection format)
      Assert.That(result.Breakdown.Constraints, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_ExcludesConstraints_WhenNotConfigured()
    {
      var snapshot = CreateTestSnapshot(constraintCount: 2);
      var config = new PromptAssemblerConfig { IncludeConstraints = false };
      var assembler = new PromptAssembler(config);

      var result = assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result.Breakdown.Constraints, Is.EqualTo(0));
    }

    [Test]
    public void AssembleFromSnapshot_IncludesRetryFeedback()
    {
      var snapshot = CreateTestSnapshot();
      var result = _assembler.AssembleFromSnapshot(
        snapshot,
        retryFeedback: "[RETRY ATTEMPT 2] Previous response violated constraints."
      );

      Assert.That(result.Text, Does.Contain("RETRY ATTEMPT 2"));
      Assert.That(result.Breakdown.RetryFeedback, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleFromSnapshot_CreatesWorkingMemory()
    {
      var snapshot = CreateTestSnapshot();
      var result = _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result.WorkingMemory, Is.Not.Null);
    }

    [Test]
    public void AssembleFromSnapshot_CalculatesBreakdown()
    {
      var snapshot = CreateTestSnapshot();
      var result = _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(result.Breakdown.SystemPrompt, Is.GreaterThan(0));
      Assert.That(result.Breakdown.PlayerInput, Is.GreaterThan(0));
      Assert.That(result.Breakdown.Total, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleMinimal_CreatesSimplePrompt()
    {
      var result = _assembler.AssembleMinimal(
        systemPrompt: "You are a guard.",
        playerInput: "Who goes there?",
        npcName: "Guard"
      );

      Assert.That(result.Text, Does.Contain("You are a guard"));
      Assert.That(result.Text, Does.Contain("Who goes there?"));
      Assert.That(result.Text, Does.Contain("Guard:"));
      Assert.That(result.WasTruncated, Is.False);
    }

    [Test]
    public void EstimateTokens_ReturnsReasonableEstimate()
    {
      // 400 characters at 4 chars/token = 100 tokens
      var tokens = _assembler.EstimateTokens(400);
      Assert.That(tokens, Is.EqualTo(100));
    }

    [Test]
    public void EstimateCharacters_ReturnsReasonableEstimate()
    {
      // 100 tokens at 4 chars/token = 400 characters
      var chars = _assembler.EstimateCharacters(100);
      Assert.That(chars, Is.EqualTo(400));
    }

    [Test]
    public void OnLog_CallbackInvoked()
    {
      var logMessages = new System.Collections.Generic.List<string>();
      _assembler.OnLog = msg => logMessages.Add(msg);

      var snapshot = CreateTestSnapshot();
      _assembler.AssembleFromSnapshot(snapshot);

      Assert.That(logMessages.Count, Is.GreaterThan(0));
    }
  }

  [TestFixture]
  public class PromptAssemblerConfigTests
  {
    [Test]
    public void Default_HasReasonableValues()
    {
      var config = PromptAssemblerConfig.Default;

      Assert.That(config.MaxPromptTokens, Is.EqualTo(2048));
      Assert.That(config.ReserveResponseTokens, Is.EqualTo(256));
      Assert.That(config.CharsPerToken, Is.EqualTo(4.0f));
      Assert.That(config.IncludeConstraints, Is.True);
      Assert.That(config.IncludeRetryFeedback, Is.True);
      Assert.That(config.DefaultNpcName, Is.EqualTo("NPC"));
    }

    [Test]
    public void SmallContext_HasSmallerValues()
    {
      var config = PromptAssemblerConfig.SmallContext;

      Assert.That(config.MaxPromptTokens, Is.LessThan(PromptAssemblerConfig.Default.MaxPromptTokens));
    }

    [Test]
    public void LargeContext_HasLargerValues()
    {
      var config = PromptAssemblerConfig.LargeContext;

      Assert.That(config.MaxPromptTokens, Is.GreaterThan(PromptAssemblerConfig.Default.MaxPromptTokens));
    }

    [Test]
    public void MaxPromptCharacters_CalculatedFromTokens()
    {
      var config = new PromptAssemblerConfig
      {
        MaxPromptTokens = 1000,
        ReserveResponseTokens = 200,
        CharsPerToken = 4.0f
      };

      // (1000 - 200) * 4 = 3200
      Assert.That(config.MaxPromptCharacters, Is.EqualTo(3200));
    }
  }

  [TestFixture]
  public class AssembledPromptTests
  {
    [Test]
    public void ToString_IncludesRelevantInfo()
    {
      var prompt = new AssembledPrompt
      {
        Text = "Test prompt",
        CharacterCount = 100,
        EstimatedTokens = 25,
        WasTruncated = false
      };

      var str = prompt.ToString();

      Assert.That(str, Does.Contain("100 chars"));
      Assert.That(str, Does.Contain("25 tokens"));
    }

    [Test]
    public void ToString_IndicatesTruncation()
    {
      var prompt = new AssembledPrompt
      {
        Text = "Test",
        CharacterCount = 100,
        EstimatedTokens = 25,
        WasTruncated = true
      };

      var str = prompt.ToString();

      Assert.That(str, Does.Contain("truncated"));
    }
  }

  [TestFixture]
  public class PromptSectionBreakdownTests
  {
    [Test]
    public void Total_SumsAllSections()
    {
      var breakdown = new PromptSectionBreakdown
      {
        SystemPrompt = 100,
        Context = 200,
        Constraints = 50,
        RetryFeedback = 30,
        DialogueHistory = 150,
        PlayerInput = 40,
        Formatting = 20
      };

      Assert.That(breakdown.Total, Is.EqualTo(590));
    }
  }
}
