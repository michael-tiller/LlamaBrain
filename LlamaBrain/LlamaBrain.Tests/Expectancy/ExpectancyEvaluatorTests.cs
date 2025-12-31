using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Expectancy
{
  /// <summary>
  /// Tests for the ExpectancyEvaluator class.
  /// </summary>
  public class ExpectancyEvaluatorTests
  {
    #region Test Rule Implementation

    /// <summary>
    /// Simple test rule implementation for unit testing.
    /// </summary>
    private class TestRule : IExpectancyRule
    {
      public string RuleId { get; set; } = "test-rule";
      public string RuleName { get; set; } = "Test Rule";
      public bool IsEnabled { get; set; } = true;
      public int Priority { get; set; } = 0;

      public System.Func<InteractionContext, bool>? EvaluateFunc { get; set; }
      public System.Action<InteractionContext, ConstraintSet>? GenerateFunc { get; set; }

      public bool Evaluate(InteractionContext context)
      {
        return EvaluateFunc?.Invoke(context) ?? true;
      }

      public void GenerateConstraints(InteractionContext context, ConstraintSet constraints)
      {
        GenerateFunc?.Invoke(context, constraints);
      }
    }

    #endregion

    #region Basic Evaluation Tests

    [Test]
    public void Evaluate_NoRules_ReturnsEmptyConstraintSet()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var context = new InteractionContext { NpcId = "test-npc" };

      // Act
      var result = evaluator.Evaluate(context);

      // Assert
      Assert.That(result.Count, Is.EqualTo(0));
      Assert.That(result.HasConstraints, Is.False);
    }

    [Test]
    public void Evaluate_WithMatchingRule_GeneratesConstraints()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var rule = new TestRule
      {
        RuleId = "always-match",
        EvaluateFunc = _ => true,
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Prohibition("p1", "No secrets", "Do not reveal secrets"));
        }
      };
      evaluator.RegisterRule(rule);

      var context = new InteractionContext { NpcId = "test-npc" };

      // Act
      var result = evaluator.Evaluate(context);

      // Assert
      Assert.That(result.Count, Is.EqualTo(1));
      Assert.That(result.Contains("p1"), Is.True);
    }

    [Test]
    public void Evaluate_WithNonMatchingRule_ReturnsEmptyConstraintSet()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var rule = new TestRule
      {
        RuleId = "never-match",
        EvaluateFunc = _ => false,
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Prohibition("p1", "No secrets", "Do not reveal secrets"));
        }
      };
      evaluator.RegisterRule(rule);

      var context = new InteractionContext { NpcId = "test-npc" };

      // Act
      var result = evaluator.Evaluate(context);

      // Assert
      Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void Evaluate_DisabledRule_IsSkipped()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var rule = new TestRule
      {
        RuleId = "disabled-rule",
        IsEnabled = false,
        EvaluateFunc = _ => true,
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Prohibition("p1", "No secrets", "Do not reveal secrets"));
        }
      };
      evaluator.RegisterRule(rule);

      var context = new InteractionContext { NpcId = "test-npc" };

      // Act
      var result = evaluator.Evaluate(context);

      // Assert
      Assert.That(result.Count, Is.EqualTo(0));
    }

    #endregion

    #region Rule Registration Tests

    [Test]
    public void RegisterRule_AddsRule()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var rule = new TestRule { RuleId = "test-rule" };

      // Act
      evaluator.RegisterRule(rule);

      // Assert
      Assert.That(evaluator.CodeBasedRules.Count, Is.EqualTo(1));
    }

    [Test]
    public void RegisterRule_IgnoresNull()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();

      // Act
      evaluator.RegisterRule(null!);

      // Assert
      Assert.That(evaluator.CodeBasedRules.Count, Is.EqualTo(0));
    }

    [Test]
    public void RegisterRule_IgnoresDuplicateId()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var rule1 = new TestRule { RuleId = "same-id", RuleName = "Rule 1" };
      var rule2 = new TestRule { RuleId = "same-id", RuleName = "Rule 2" };

      // Act
      evaluator.RegisterRule(rule1);
      evaluator.RegisterRule(rule2);

      // Assert
      Assert.That(evaluator.CodeBasedRules.Count, Is.EqualTo(1));
      Assert.That(evaluator.CodeBasedRules[0].RuleName, Is.EqualTo("Rule 1"));
    }

    [Test]
    public void UnregisterRule_RemovesRule()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var rule = new TestRule { RuleId = "test-rule" };
      evaluator.RegisterRule(rule);

      // Act
      evaluator.UnregisterRule("test-rule");

      // Assert
      Assert.That(evaluator.CodeBasedRules.Count, Is.EqualTo(0));
    }

    [Test]
    public void ClearRules_RemovesAllRules()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      evaluator.RegisterRule(new TestRule { RuleId = "rule1" });
      evaluator.RegisterRule(new TestRule { RuleId = "rule2" });

      // Act
      evaluator.ClearRules();

      // Assert
      Assert.That(evaluator.CodeBasedRules.Count, Is.EqualTo(0));
    }

    #endregion

    #region Priority Tests

    [Test]
    public void Evaluate_RulesAreEvaluatedInPriorityOrder()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var executionOrder = new List<string>();

      var lowPriorityRule = new TestRule
      {
        RuleId = "low-priority",
        Priority = 1,
        EvaluateFunc = _ =>
        {
          executionOrder.Add("low");
          return true;
        },
        GenerateFunc = (_, __) => { }
      };

      var highPriorityRule = new TestRule
      {
        RuleId = "high-priority",
        Priority = 10,
        EvaluateFunc = _ =>
        {
          executionOrder.Add("high");
          return true;
        },
        GenerateFunc = (_, __) => { }
      };

      var mediumPriorityRule = new TestRule
      {
        RuleId = "medium-priority",
        Priority = 5,
        EvaluateFunc = _ =>
        {
          executionOrder.Add("medium");
          return true;
        },
        GenerateFunc = (_, __) => { }
      };

      // Register in random order
      evaluator.RegisterRule(lowPriorityRule);
      evaluator.RegisterRule(highPriorityRule);
      evaluator.RegisterRule(mediumPriorityRule);

      var context = new InteractionContext();

      // Act
      evaluator.Evaluate(context);

      // Assert - should be executed high -> medium -> low
      Assert.That(executionOrder, Is.EqualTo(new[] { "high", "medium", "low" }));
    }

    #endregion

    #region Additional Rules Tests

    [Test]
    public void Evaluate_WithAdditionalRules_EvaluatesBoth()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var registeredRule = new TestRule
      {
        RuleId = "registered-rule",
        EvaluateFunc = _ => true,
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Prohibition("p1", "Registered", "From registered rule"));
        }
      };
      evaluator.RegisterRule(registeredRule);

      var additionalRule = new TestRule
      {
        RuleId = "additional-rule",
        EvaluateFunc = _ => true,
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Requirement("r1", "Additional", "From additional rule"));
        }
      };

      var context = new InteractionContext();

      // Act
      var result = evaluator.Evaluate(context, new[] { additionalRule });

      // Assert
      Assert.That(result.Count, Is.EqualTo(2));
      Assert.That(result.Contains("p1"), Is.True);
      Assert.That(result.Contains("r1"), Is.True);
    }

    [Test]
    public void Evaluate_WithNullAdditionalRules_DoesNotThrow()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var context = new InteractionContext();

      // Act & Assert
      Assert.DoesNotThrow(() => evaluator.Evaluate(context, null));
    }

    #endregion

    #region Context-Based Evaluation Tests

    [Test]
    public void Evaluate_RuleCanAccessContextNpcId()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      string? capturedNpcId = null;

      var rule = new TestRule
      {
        RuleId = "npc-check",
        EvaluateFunc = ctx =>
        {
          capturedNpcId = ctx.NpcId;
          return ctx.NpcId == "special-npc";
        },
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Prohibition("special", "Special NPC rule", "Special constraint"));
        }
      };
      evaluator.RegisterRule(rule);

      var context = new InteractionContext { NpcId = "special-npc" };

      // Act
      var result = evaluator.Evaluate(context);

      // Assert
      Assert.That(capturedNpcId, Is.EqualTo("special-npc"));
      Assert.That(result.Contains("special"), Is.True);
    }

    [Test]
    public void Evaluate_RuleCanAccessContextTriggerReason()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();

      var rule = new TestRule
      {
        RuleId = "zone-only",
        EvaluateFunc = ctx => ctx.TriggerReason == TriggerReason.ZoneTrigger,
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Prohibition("zone", "Zone rule", "Zone constraint"));
        }
      };
      evaluator.RegisterRule(rule);

      var zoneContext = new InteractionContext { TriggerReason = TriggerReason.ZoneTrigger };
      var playerContext = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      var zoneResult = evaluator.Evaluate(zoneContext);
      var playerResult = evaluator.Evaluate(playerContext);

      // Assert
      Assert.That(zoneResult.Contains("zone"), Is.True);
      Assert.That(playerResult.Contains("zone"), Is.False);
    }

    [Test]
    public void Evaluate_RuleCanAccessContextTags()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();

      var rule = new TestRule
      {
        RuleId = "tag-check",
        EvaluateFunc = ctx => ctx.Tags != null && System.Array.Exists(ctx.Tags, t => t == "quest-giver"),
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Requirement("quest", "Quest giver rule", "Must offer quests"));
        }
      };
      evaluator.RegisterRule(rule);

      var taggedContext = new InteractionContext { Tags = new[] { "quest-giver", "merchant" } };
      var untaggedContext = new InteractionContext { Tags = new[] { "guard" } };

      // Act
      var taggedResult = evaluator.Evaluate(taggedContext);
      var untaggedResult = evaluator.Evaluate(untaggedContext);

      // Assert
      Assert.That(taggedResult.Contains("quest"), Is.True);
      Assert.That(untaggedResult.Contains("quest"), Is.False);
    }

    #endregion

    #region Logging Tests

    [Test]
    public void Evaluate_WithLoggingCallback_LogsMessages()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var logMessages = new List<string>();
      evaluator.OnLog = msg => logMessages.Add(msg);

      var rule = new TestRule
      {
        RuleId = "test-rule",
        RuleName = "Test Rule",
        EvaluateFunc = _ => true,
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Prohibition("p1", "Test", "Test injection"));
        }
      };
      evaluator.RegisterRule(rule);

      var context = new InteractionContext { NpcId = "test-npc" };

      // Act
      evaluator.Evaluate(context);

      // Assert
      Assert.That(logMessages.Count, Is.GreaterThan(0));
      Assert.That(logMessages, Has.Some.Contains("Test Rule"));
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public void Evaluate_RuleThrowsException_ContinuesWithOtherRules()
    {
      // Arrange
      var evaluator = new ExpectancyEvaluator();
      var logMessages = new List<string>();
      evaluator.OnLog = msg => logMessages.Add(msg);

      var badRule = new TestRule
      {
        RuleId = "bad-rule",
        EvaluateFunc = _ => throw new System.Exception("Test exception"),
        GenerateFunc = (_, __) => { }
      };

      var goodRule = new TestRule
      {
        RuleId = "good-rule",
        EvaluateFunc = _ => true,
        GenerateFunc = (_, constraints) =>
        {
          constraints.Add(Constraint.Prohibition("good", "Good rule", "Good constraint"));
        }
      };

      evaluator.RegisterRule(badRule);
      evaluator.RegisterRule(goodRule);

      var context = new InteractionContext();

      // Act
      var result = evaluator.Evaluate(context);

      // Assert
      Assert.That(result.Contains("good"), Is.True);
      Assert.That(logMessages, Has.Some.Contains("Error"));
    }

    #endregion
  }
}
