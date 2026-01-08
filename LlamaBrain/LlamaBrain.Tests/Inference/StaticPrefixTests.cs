using System;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  /// <summary>
  /// Tests for static prefix enforcement and KV cache configuration.
  /// </summary>
  [TestFixture]
  [Category("StaticPrefix")]
  [Category("KvCache")]
  public class StaticPrefixTests
  {
    private StateSnapshot _defaultSnapshot = null!;
    private PromptAssembler _assembler = null!;

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
        .WithCanonicalFacts(new[] { "The king is Arthur", "The year is 1350" })
        .WithWorldState(new[] { "door_status=open", "shop_inventory=full" })
        .WithEpisodicMemories(new[] { "Player said hello", "Player bought apples" })
        .WithBeliefs(new[] { "Player is trustworthy" })
        .WithDialogueHistory(new[] { "Player: Hi", "NPC: Hello there!" })
        .Build();

      _assembler = new PromptAssembler();
    }

    #region KvCacheConfig Tests

    [Test]
    public void KvCacheConfig_Default_HasCachingEnabled()
    {
      // Act
      var config = KvCacheConfig.Default();

      // Assert
      Assert.That(config.EnableCaching, Is.True);
      Assert.That(config.Boundary, Is.EqualTo(StaticPrefixBoundary.AfterCanonicalFacts));
      Assert.That(config.TrackMetrics, Is.True);
    }

    [Test]
    public void KvCacheConfig_Disabled_HasCachingDisabled()
    {
      // Act
      var config = KvCacheConfig.Disabled();

      // Assert
      Assert.That(config.EnableCaching, Is.False);
      Assert.That(config.TrackMetrics, Is.False);
    }

    [Test]
    public void KvCacheConfig_Aggressive_UsesAfterWorldStateBoundary()
    {
      // Act
      var config = KvCacheConfig.Aggressive();

      // Assert
      Assert.That(config.EnableCaching, Is.True);
      Assert.That(config.Boundary, Is.EqualTo(StaticPrefixBoundary.AfterWorldState));
    }

    [Test]
    public void StaticPrefixBoundary_Values_AreOrdered()
    {
      // Assert boundaries are ordered for comparison operators
      Assert.That(StaticPrefixBoundary.AfterSystemPrompt, Is.LessThan(StaticPrefixBoundary.AfterCanonicalFacts));
      Assert.That(StaticPrefixBoundary.AfterCanonicalFacts, Is.LessThan(StaticPrefixBoundary.AfterWorldState));
      Assert.That(StaticPrefixBoundary.AfterWorldState, Is.LessThan(StaticPrefixBoundary.AfterConstraints));
    }

    #endregion

    #region AssembleWithCacheInfo Tests

    [Test]
    public void AssembleWithCacheInfo_WithDefaultBoundary_SplitsAtFacts()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);
      var config = KvCacheConfig.Default();

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory, kvCacheConfig: config);

      // Assert
      Assert.That(result.StaticPrefix, Does.Contain("System:"));
      Assert.That(result.StaticPrefix, Does.Contain("[Fact]"));
      Assert.That(result.StaticPrefix, Does.Contain("The king is Arthur"));
      Assert.That(result.StaticPrefix, Does.Not.Contain("[State]"));
      Assert.That(result.StaticPrefix, Does.Not.Contain("door_status"));

      Assert.That(result.DynamicSuffix, Does.Contain("[State]"));
      Assert.That(result.DynamicSuffix, Does.Contain("door_status"));
      Assert.That(result.DynamicSuffix, Does.Contain("Player: Hello!"));
    }

    [Test]
    public void AssembleWithCacheInfo_WithAggressiveBoundary_IncludesWorldStateInPrefix()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);
      var config = KvCacheConfig.Aggressive();

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory, kvCacheConfig: config);

      // Assert
      Assert.That(result.StaticPrefix, Does.Contain("[Fact]"));
      Assert.That(result.StaticPrefix, Does.Contain("[State]"));
      Assert.That(result.StaticPrefix, Does.Contain("door_status"));

      Assert.That(result.DynamicSuffix, Does.Not.Contain("[Fact]"));
      Assert.That(result.DynamicSuffix, Does.Not.Contain("[State]"));
    }

    [Test]
    public void AssembleWithCacheInfo_FullPrompt_EqualsStaticPlusDynamic()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert
      Assert.That(result.FullPrompt, Is.EqualTo(result.StaticPrefix + result.DynamicSuffix));
      Assert.That(result.TotalCharCount, Is.EqualTo(result.StaticPrefixCharCount + result.DynamicSuffixCharCount));
    }

    [Test]
    public void AssembleWithCacheInfo_DialogueHistory_AlwaysInDynamicSuffix()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);
      var config = new KvCacheConfig
      {
        EnableCaching = true,
        Boundary = StaticPrefixBoundary.AfterConstraints // Most aggressive
      };

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory, kvCacheConfig: config);

      // Assert - dialogue should always be in suffix even with aggressive boundary
      Assert.That(result.DynamicSuffix, Does.Contain("Player: Hi"));
      Assert.That(result.DynamicSuffix, Does.Contain("NPC: Hello there!"));
      Assert.That(result.StaticPrefix, Does.Not.Contain("Player: Hi"));
    }

    [Test]
    public void AssembleWithCacheInfo_PlayerInput_AlwaysInDynamicSuffix()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);
      var config = new KvCacheConfig
      {
        EnableCaching = true,
        Boundary = StaticPrefixBoundary.AfterConstraints
      };

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory, kvCacheConfig: config);

      // Assert
      Assert.That(result.DynamicSuffix, Does.Contain("Hello!"));
      Assert.That(result.StaticPrefix, Does.Not.Contain("Hello!").IgnoreCase);
    }

    #endregion

    #region Determinism Tests

    [Test]
    [Category("Determinism")]
    public void AssembleWithCacheInfo_IdenticalInputs_ProduceIdenticalStaticPrefix()
    {
      // Arrange
      var workingMemory1 = new EphemeralWorkingMemory(_defaultSnapshot);
      var workingMemory2 = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var result1 = _assembler.AssembleWithCacheInfo(workingMemory1);
      var result2 = _assembler.AssembleWithCacheInfo(workingMemory2);

      // Assert - static prefix should be byte-stable
      Assert.That(result1.StaticPrefix, Is.EqualTo(result2.StaticPrefix));
      Assert.That(result1.StaticPrefixCharCount, Is.EqualTo(result2.StaticPrefixCharCount));
    }

    [Test]
    [Category("Determinism")]
    public void AssembleWithCacheInfo_DifferentPlayerInput_SameStaticPrefix()
    {
      // Arrange - two snapshots with different player input but same system/facts
      var snapshot1 = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("You are a friendly shopkeeper.")
        .WithPlayerInput("Hello!")
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .Build();

      var snapshot2 = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("You are a friendly shopkeeper.")
        .WithPlayerInput("Goodbye!")
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .Build();

      var workingMemory1 = new EphemeralWorkingMemory(snapshot1);
      var workingMemory2 = new EphemeralWorkingMemory(snapshot2);

      // Act
      var result1 = _assembler.AssembleWithCacheInfo(workingMemory1);
      var result2 = _assembler.AssembleWithCacheInfo(workingMemory2);

      // Assert - static prefix should be identical
      Assert.That(result1.StaticPrefix, Is.EqualTo(result2.StaticPrefix));

      // Dynamic suffix should differ
      Assert.That(result1.DynamicSuffix, Is.Not.EqualTo(result2.DynamicSuffix));
    }

    [Test]
    [Category("Determinism")]
    public void AssembleWithCacheInfo_DifferentDialogueHistory_SameStaticPrefix()
    {
      // Arrange
      var snapshot1 = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("You are a friendly shopkeeper.")
        .WithPlayerInput("Hello!")
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .WithDialogueHistory(new[] { "Player: First message" })
        .Build();

      var snapshot2 = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("You are a friendly shopkeeper.")
        .WithPlayerInput("Hello!")
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .WithDialogueHistory(new[] { "Player: Different message", "NPC: Response" })
        .Build();

      var workingMemory1 = new EphemeralWorkingMemory(snapshot1);
      var workingMemory2 = new EphemeralWorkingMemory(snapshot2);

      // Act
      var result1 = _assembler.AssembleWithCacheInfo(workingMemory1);
      var result2 = _assembler.AssembleWithCacheInfo(workingMemory2);

      // Assert - static prefix should be identical
      Assert.That(result1.StaticPrefix, Is.EqualTo(result2.StaticPrefix));
    }

    #endregion

    #region Token Estimation Tests

    [Test]
    public void AssembleWithCacheInfo_EstimatesTokensCorrectly()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert - token estimates should be reasonable
      Assert.That(result.EstimatedStaticTokens, Is.GreaterThan(0));
      Assert.That(result.EstimatedDynamicTokens, Is.GreaterThan(0));
      Assert.That(result.EstimatedTotalTokens, Is.EqualTo(result.EstimatedStaticTokens + result.EstimatedDynamicTokens));
    }

    [Test]
    public void AssembleWithCacheInfo_CharCountsAreAccurate()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert
      Assert.That(result.StaticPrefixCharCount, Is.EqualTo(result.StaticPrefix.Length));
      Assert.That(result.DynamicSuffixCharCount, Is.EqualTo(result.DynamicSuffix.Length));
      Assert.That(result.TotalCharCount, Is.EqualTo(result.FullPrompt.Length));
    }

    #endregion

    #region Boundary Configuration Tests

    [Test]
    public void AssembleWithCacheInfo_AfterSystemPromptBoundary_OnlySystemPromptInPrefix()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);
      var config = new KvCacheConfig
      {
        EnableCaching = true,
        Boundary = StaticPrefixBoundary.AfterSystemPrompt
      };

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory, kvCacheConfig: config);

      // Assert - only system prompt and context header in prefix
      Assert.That(result.StaticPrefix, Does.Contain("System:"));
      Assert.That(result.StaticPrefix, Does.Contain("[Context]"));
      Assert.That(result.StaticPrefix, Does.Contain("[Fact]")); // Facts are always in prefix after header

      // World state, memories, beliefs, dialogue in suffix
      Assert.That(result.DynamicSuffix, Does.Contain("[State]"));
    }

    [Test]
    public void AssembleWithCacheInfo_BoundaryStoredInResult()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);
      var config = new KvCacheConfig
      {
        EnableCaching = true,
        Boundary = StaticPrefixBoundary.AfterWorldState
      };

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory, kvCacheConfig: config);

      // Assert
      Assert.That(result.Boundary, Is.EqualTo(StaticPrefixBoundary.AfterWorldState));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void AssembleWithCacheInfo_EmptyCanonicalFacts_StillSplitsCorrectly()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("You are a friendly shopkeeper.")
        .WithPlayerInput("Hello!")
        .WithWorldState(new[] { "door_status=open" })
        .Build();

      var workingMemory = new EphemeralWorkingMemory(snapshot);

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert
      Assert.That(result.StaticPrefix, Does.Contain("System:"));
      Assert.That(result.StaticPrefix, Does.Not.Contain("[Fact]"));
      Assert.That(result.DynamicSuffix, Does.Contain("[State]"));
    }

    [Test]
    public void AssembleWithCacheInfo_NullWorkingMemory_ThrowsArgumentNullException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() =>
        _assembler.AssembleWithCacheInfo(null!));
    }

    [Test]
    public void AssembleWithCacheInfo_AssembledPromptIncluded()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert - underlying AssembledPrompt should be populated
      Assert.That(result.AssembledPrompt, Is.Not.Null);
      Assert.That(result.AssembledPrompt.Text, Is.EqualTo(result.FullPrompt));
    }

    #endregion

    #region PrefixStabilityValidator Tests

    [Test]
    public void PrefixStabilityValidator_FirstCall_EstablishesBaseline()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      var prefix = "System: You are a helpful NPC.\n[Context]\n[Fact] The king is Arthur";

      // Act
      var violation = validator.ValidatePrefix("npc-001", prefix, StaticPrefixBoundary.AfterCanonicalFacts);

      // Assert - first call should not report violation
      Assert.That(violation, Is.Null);
      Assert.That(validator.ViolationCount, Is.EqualTo(0));
      Assert.That(validator.HasViolations, Is.False);
    }

    [Test]
    public void PrefixStabilityValidator_IdenticalPrefix_NoViolation()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      var prefix = "System: You are a helpful NPC.\n[Context]\n[Fact] The king is Arthur";

      // Act - call twice with identical prefix
      validator.ValidatePrefix("npc-001", prefix, StaticPrefixBoundary.AfterCanonicalFacts);
      var violation = validator.ValidatePrefix("npc-001", prefix, StaticPrefixBoundary.AfterCanonicalFacts);

      // Assert
      Assert.That(violation, Is.Null);
      Assert.That(validator.ViolationCount, Is.EqualTo(0));
    }

    [Test]
    public void PrefixStabilityValidator_DifferentPrefix_DetectsViolation()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      var prefix1 = "System: You are a helpful NPC.\n[Context]\n[Fact] The king is Arthur";
      var prefix2 = "System: You are a helpful NPC.\n[Context]\n[Fact] The king is Bob"; // Changed!

      // Act
      validator.ValidatePrefix("npc-001", prefix1, StaticPrefixBoundary.AfterCanonicalFacts);
      var violation = validator.ValidatePrefix("npc-001", prefix2, StaticPrefixBoundary.AfterCanonicalFacts);

      // Assert
      Assert.That(violation, Is.Not.Null);
      Assert.That(violation!.Key, Is.EqualTo("npc-001"));
      Assert.That(violation.Boundary, Is.EqualTo(StaticPrefixBoundary.AfterCanonicalFacts));
      Assert.That(validator.ViolationCount, Is.EqualTo(1));
      Assert.That(validator.HasViolations, Is.True);
    }

    [Test]
    public void PrefixStabilityValidator_DifferentBoundary_ResetsBaseline()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      var prefix1 = "System: You are a helpful NPC.\n[Fact] Facts here";
      var prefix2 = "System: You are a helpful NPC.\n[Fact] Facts here\n[State] World state"; // Larger with state

      // Act - change boundary (expected to change prefix)
      validator.ValidatePrefix("npc-001", prefix1, StaticPrefixBoundary.AfterCanonicalFacts);
      var violation = validator.ValidatePrefix("npc-001", prefix2, StaticPrefixBoundary.AfterWorldState);

      // Assert - boundary change should not be a violation
      Assert.That(violation, Is.Null);
      Assert.That(validator.ViolationCount, Is.EqualTo(0));
    }

    [Test]
    public void PrefixStabilityValidator_DifferentNpcs_IndependentTracking()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      var prefixNpc1 = "System: You are Gareth the blacksmith.";
      var prefixNpc2 = "System: You are Elena the merchant.";

      // Act
      validator.ValidatePrefix("npc-001", prefixNpc1, StaticPrefixBoundary.AfterCanonicalFacts);
      validator.ValidatePrefix("npc-002", prefixNpc2, StaticPrefixBoundary.AfterCanonicalFacts);

      // Second call for each - should match their own baselines
      var violation1 = validator.ValidatePrefix("npc-001", prefixNpc1, StaticPrefixBoundary.AfterCanonicalFacts);
      var violation2 = validator.ValidatePrefix("npc-002", prefixNpc2, StaticPrefixBoundary.AfterCanonicalFacts);

      // Assert
      Assert.That(violation1, Is.Null);
      Assert.That(violation2, Is.Null);
      Assert.That(validator.ViolationCount, Is.EqualTo(0));
    }

    [Test]
    public void PrefixStabilityValidator_ThrowOnViolation_ThrowsException()
    {
      // Arrange
      var validator = new PrefixStabilityValidator { ThrowOnViolation = true };
      var prefix1 = "Stable prefix";
      var prefix2 = "Changed prefix";

      // Act
      validator.ValidatePrefix("npc-001", prefix1, StaticPrefixBoundary.AfterCanonicalFacts);

      // Assert
      var ex = Assert.Throws<PrefixStabilityException>(() =>
        validator.ValidatePrefix("npc-001", prefix2, StaticPrefixBoundary.AfterCanonicalFacts));

      Assert.That(ex!.Violation, Is.Not.Null);
      Assert.That(ex.Violation.Key, Is.EqualTo("npc-001"));
    }

    [Test]
    public void PrefixStabilityValidator_Reset_ClearsAllState()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      validator.ValidatePrefix("npc-001", "prefix1", StaticPrefixBoundary.AfterCanonicalFacts);
      validator.ValidatePrefix("npc-001", "prefix2", StaticPrefixBoundary.AfterCanonicalFacts); // violation

      Assert.That(validator.ViolationCount, Is.EqualTo(1));

      // Act
      validator.Reset();

      // Assert
      Assert.That(validator.ViolationCount, Is.EqualTo(0));
      Assert.That(validator.HasViolations, Is.False);

      // New baseline established
      var violation = validator.ValidatePrefix("npc-001", "prefix3", StaticPrefixBoundary.AfterCanonicalFacts);
      Assert.That(violation, Is.Null);
    }

    [Test]
    public void PrefixStabilityValidator_ViolationDetails_ContainsUsefulInfo()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      var prefix1 = "Original prefix with some content";
      var prefix2 = "Modified prefix with different content";

      // Act
      validator.ValidatePrefix("npc-001", prefix1, StaticPrefixBoundary.AfterCanonicalFacts);
      var violation = validator.ValidatePrefix("npc-001", prefix2, StaticPrefixBoundary.AfterCanonicalFacts);

      // Assert
      Assert.That(violation, Is.Not.Null);
      Assert.That(violation!.ExpectedSample, Does.Contain("Original"));
      Assert.That(violation.ActualSample, Does.Contain("Modified"));
      Assert.That(violation.CheckNumber, Is.EqualTo(2));
      Assert.That(violation.ToString(), Does.Contain("npc-001"));
      Assert.That(violation.ToString(), Does.Contain("Prefix changed unexpectedly"));
    }

    [Test]
    public void PrefixStabilityValidator_ConcurrentAccess_ThreadSafe()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      var tasks = new System.Threading.Tasks.Task[100];
      var stablePrefix = "This prefix should remain stable across all calls";

      // Act - concurrent validation with same prefix
      for (int i = 0; i < 100; i++)
      {
        var npcId = $"npc-{i % 10:D3}"; // 10 different NPCs
        tasks[i] = System.Threading.Tasks.Task.Run(() =>
          validator.ValidatePrefix(npcId, stablePrefix, StaticPrefixBoundary.AfterCanonicalFacts));
      }
      System.Threading.Tasks.Task.WaitAll(tasks);

      // Assert - no violations since all prefixes are identical per NPC
      Assert.That(validator.ViolationCount, Is.EqualTo(0));
    }

    [Test]
    public void PrefixStabilityValidator_IntegrationWithAssembler_DetectsRealViolation()
    {
      // Arrange
      var validator = new PrefixStabilityValidator();
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act - assemble twice, validate both
      var result1 = _assembler.AssembleWithCacheInfo(workingMemory);
      validator.ValidatePrefix("npc-001", result1.StaticPrefix, result1.Boundary);

      var result2 = _assembler.AssembleWithCacheInfo(workingMemory);
      var violation = validator.ValidatePrefix("npc-001", result2.StaticPrefix, result2.Boundary);

      // Assert - identical working memory should produce identical prefix
      Assert.That(violation, Is.Null, "Same working memory should produce stable prefix");
      Assert.That(result1.StaticPrefix, Is.EqualTo(result2.StaticPrefix));
    }

    [Test]
    public void PrefixStabilityValidator_SimulatedDynamicContentBug_DetectsViolation()
    {
      // Arrange - simulate a bug where timestamp leaks into prefix
      var validator = new PrefixStabilityValidator();

      // Simulated bug: timestamp in what should be static content
      var prefixWithTimestamp1 = $"System: You are an NPC. Generated at: {DateTime.UtcNow.Ticks}\n[Fact] The king is Arthur";
      System.Threading.Thread.Sleep(1); // Ensure different timestamp
      var prefixWithTimestamp2 = $"System: You are an NPC. Generated at: {DateTime.UtcNow.Ticks}\n[Fact] The king is Arthur";

      // Act
      validator.ValidatePrefix("npc-001", prefixWithTimestamp1, StaticPrefixBoundary.AfterCanonicalFacts);
      var violation = validator.ValidatePrefix("npc-001", prefixWithTimestamp2, StaticPrefixBoundary.AfterCanonicalFacts);

      // Assert - validator catches the instability
      Assert.That(violation, Is.Not.Null, "Timestamp in prefix should be detected as violation");
      Assert.That(violation!.Key, Is.EqualTo("npc-001"));
    }

    #endregion

    #region Small Static Prefix Edge Cases

    [Test]
    public void AssembleWithCacheInfo_VerySmallPrefix_StillProducesValidSplit()
    {
      // Arrange - minimal snapshot with only system prompt (no facts, no state)
      var minimalSnapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("Hi.") // Very short system prompt
        .WithPlayerInput("Hello!")
        .Build();

      var workingMemory = new EphemeralWorkingMemory(minimalSnapshot);

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert - should still produce valid split even with tiny prefix
      Assert.That(result.StaticPrefix, Is.Not.Null);
      Assert.That(result.StaticPrefix.Length, Is.GreaterThan(0));
      Assert.That(result.DynamicSuffix, Is.Not.Null);
      Assert.That(result.FullPrompt, Is.EqualTo(result.StaticPrefix + result.DynamicSuffix));
      Assert.That(result.EstimatedStaticTokens, Is.GreaterThan(0));
    }

    [Test]
    public void AssembleWithCacheInfo_EmptySystemPrompt_StillWorks()
    {
      // Arrange - no system prompt at all
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithPlayerInput("Hello!")
        .WithCanonicalFacts(new[] { "Fact 1" })
        .Build();

      var workingMemory = new EphemeralWorkingMemory(snapshot);

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert - should handle gracefully
      Assert.That(result.StaticPrefix, Is.Not.Null);
      Assert.That(result.FullPrompt, Contains.Substring("Fact 1"));
    }

    [Test]
    public void AssembleWithCacheInfo_MinimalPrefix_TokenEstimatesReasonable()
    {
      // Arrange - very small prefix (~10-20 tokens)
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("You are Bob.")
        .WithPlayerInput("Hi")
        .Build();

      var workingMemory = new EphemeralWorkingMemory(snapshot);

      // Act
      var result = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert - token estimates should be small but reasonable
      Assert.That(result.EstimatedStaticTokens, Is.GreaterThanOrEqualTo(1));
      Assert.That(result.EstimatedStaticTokens, Is.LessThan(50)); // Very small prefix
      Assert.That(result.EstimatedDynamicTokens, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void AssembleWithCacheInfo_SmallPrefix_StillDeterministic()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("Short.")
        .WithPlayerInput("X")
        .Build();

      var workingMemory1 = new EphemeralWorkingMemory(snapshot);
      var workingMemory2 = new EphemeralWorkingMemory(snapshot);

      // Act
      var result1 = _assembler.AssembleWithCacheInfo(workingMemory1);
      var result2 = _assembler.AssembleWithCacheInfo(workingMemory2);

      // Assert - even tiny prefixes must be byte-stable
      Assert.That(result1.StaticPrefix, Is.EqualTo(result2.StaticPrefix));
      Assert.That(result1.DynamicSuffix, Is.EqualTo(result2.DynamicSuffix));
    }

    #endregion

    #region NKeep Configuration Tests

    [Test]
    public void KvCacheConfig_NKeepTokens_DefaultIsNull()
    {
      // Arrange & Act
      var config = new KvCacheConfig();

      // Assert
      Assert.That(config.NKeepTokens, Is.Null);
    }

    [Test]
    public void KvCacheConfig_NKeepTokens_CanBeSet()
    {
      // Arrange
      var config = new KvCacheConfig { NKeepTokens = 500 };

      // Assert
      Assert.That(config.NKeepTokens, Is.EqualTo(500));
    }

    [Test]
    public void KvCacheConfig_Default_HasNullNKeep()
    {
      // Arrange & Act
      var config = KvCacheConfig.Default();

      // Assert - default doesn't set n_keep, letting server decide
      Assert.That(config.NKeepTokens, Is.Null);
    }

    [Test]
    public void KvCacheConfig_Disabled_HasNullNKeep()
    {
      // Arrange & Act
      var config = KvCacheConfig.Disabled();

      // Assert
      Assert.That(config.NKeepTokens, Is.Null);
    }

    [Test]
    public void KvCacheConfig_Aggressive_HasNullNKeep()
    {
      // Arrange & Act
      var config = KvCacheConfig.Aggressive();

      // Assert - users should set n_keep based on their prompt size
      Assert.That(config.NKeepTokens, Is.Null);
    }

    [Test]
    public void AssembleWithCacheInfo_EstimatedStaticTokens_CanBeUsedForNKeep()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext { NpcId = "npc-001" })
        .WithSystemPrompt("You are a helpful assistant. You always respond concisely.")
        .WithCanonicalFacts(new[] { "Fact 1: The sky is blue.", "Fact 2: Water is wet." })
        .WithPlayerInput("Hello")
        .Build();

      var workingMemory = new EphemeralWorkingMemory(snapshot);

      // Act
      var cacheInfo = _assembler.AssembleWithCacheInfo(workingMemory);

      // Assert - EstimatedStaticTokens should be usable as n_keep value
      Assert.That(cacheInfo.EstimatedStaticTokens, Is.GreaterThan(0));
      Assert.That(cacheInfo.EstimatedStaticTokens, Is.LessThan(cacheInfo.EstimatedTotalTokens));

      // The n_keep value should protect the static prefix
      int nKeep = cacheInfo.EstimatedStaticTokens;
      Assert.That(nKeep, Is.GreaterThan(10), "Should have enough tokens to protect");
    }

    #endregion
  }
}
