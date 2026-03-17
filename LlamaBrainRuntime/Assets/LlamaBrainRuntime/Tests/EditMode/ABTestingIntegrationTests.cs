#nullable enable
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Persona;
using LlamaBrain.Config;

namespace LlamaBrain.Tests.EditMode
{
  [TestFixture]
  [Category("Integration")]
  public class ABTestingIntegrationTests
  {
    private GameObject? _agentObj;
    private LlamaBrainAgent? _agent;
    private PersonaConfig? _config;

    [SetUp]
    public void SetUp()
    {
      // Create agent GameObject
      _agentObj = new GameObject("TestAgent");
      _agent = _agentObj.AddComponent<LlamaBrainAgent>();

      // Create PersonaConfig with variants
      _config = ScriptableObject.CreateInstance<PersonaConfig>();
      _config.PersonaId = "abtest_npc_001";
      _config.Name = "AB Test NPC";
      _config.Description = "NPC for A/B testing";
      _config.SystemPrompt = "Default prompt (should be overridden by variant)";
      _config.Background = "Test background";
      _config.UseMemory = false;

      // Add 50/50 variants
      _config.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>
      {
        new PromptVariantConfig
        {
          VariantName = "VariantA",
          SystemPrompt = "You are variant A. Reply with VARIANT_A.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariantConfig
        {
          VariantName = "VariantB",
          SystemPrompt = "You are variant B. Reply with VARIANT_B.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      // Assign config to agent and initialize
      _agent.PersonaConfig = _config;
      _agent.ConvertConfigToProfile();
    }

    [TearDown]
    public void TearDown()
    {
      if (_agentObj != null)
      {
        Object.DestroyImmediate(_agentObj);
      }

      if (_config != null)
      {
        Object.DestroyImmediate(_config);
      }
    }

    [Test]
    public void SelectSystemPromptVariant_WithVariants_SelectsDeterministically()
    {
      // Arrange: Set InteractionCount to a specific value
      _agent!.TestSetInteractionCount(42); // Hypothetical test helper

      // Act: Select variant multiple times with same InteractionCount
      var variant1 = _agent.SelectSystemPromptVariant();
      _agent.TestSetInteractionCount(42); // Reset to same count
      var variant2 = _agent.SelectSystemPromptVariant();
      _agent.TestSetInteractionCount(42); // Reset to same count
      var variant3 = _agent.SelectSystemPromptVariant();

      // Assert: Same InteractionCount should select same variant
      Assert.AreEqual(variant1, variant2, "Same InteractionCount should select same variant");
      Assert.AreEqual(variant1, variant3, "Same InteractionCount should select same variant");
    }

    [Test]
    public void SelectSystemPromptVariant_DifferentInteractionCounts_MaySelectDifferent()
    {
      // Arrange & Act: Select variants with different InteractionCounts
      var variants = new System.Collections.Generic.HashSet<string>();
      for (int i = 0; i < 100; i++)
      {
        _agent!.TestSetInteractionCount(i);
        var variant = _agent.SelectSystemPromptVariant();
        variants.Add(variant);
      }

      // Assert: Should see both variants selected
      Assert.That(variants.Count, Is.GreaterThanOrEqualTo(2), "Different InteractionCounts should select different variants");
      Assert.IsTrue(variants.Contains("You are variant A. Reply with VARIANT_A."), "Should select VariantA");
      Assert.IsTrue(variants.Contains("You are variant B. Reply with VARIANT_B."), "Should select VariantB");
    }

    [Test]
    public void SelectSystemPromptVariant_NoVariants_ReturnsDefaultPrompt()
    {
      // Arrange: Remove variants
      _config!.SystemPromptVariants = null;
      _agent!.ConvertConfigToProfile(); // Re-initialize

      // Act: Select variant
      var variant = _agent.SelectSystemPromptVariant();

      // Assert: Should return default SystemPrompt
      Assert.AreEqual("Default prompt (should be overridden by variant)", variant,
        "Should return default SystemPrompt when no variants configured");
    }

    [Test]
    public void SelectSystemPromptVariant_EmptyVariantList_ReturnsDefaultPrompt()
    {
      // Arrange: Empty variant list
      _config!.SystemPromptVariants = new System.Collections.Generic.List<PromptVariantConfig>();
      _agent!.ConvertConfigToProfile(); // Re-initialize

      // Act: Select variant
      var variant = _agent.SelectSystemPromptVariant();

      // Assert: Should return default SystemPrompt
      Assert.AreEqual("Default prompt (should be overridden by variant)", variant,
        "Should return default SystemPrompt when variant list is empty");
    }

    [Test]
    public void SelectSystemPromptVariant_InactiveVariants_SkipsInactive()
    {
      // Arrange: Set one variant inactive
      _config!.SystemPromptVariants![0].IsActive = true;
      _config.SystemPromptVariants[1].IsActive = false; // Disable VariantB
      _config.SystemPromptVariants[0].TrafficPercentage = 100f; // VariantA gets 100%
      _agent!.ConvertConfigToProfile(); // Re-initialize

      // Act: Select variants multiple times
      var variants = new System.Collections.Generic.HashSet<string>();
      for (int i = 0; i < 100; i++)
      {
        _agent.TestSetInteractionCount(i);
        var variant = _agent.SelectSystemPromptVariant();
        variants.Add(variant);
      }

      // Assert: Should only select active variant
      Assert.AreEqual(1, variants.Count, "Should only select active variant");
      Assert.IsTrue(variants.Contains("You are variant A. Reply with VARIANT_A."), "Should only select VariantA");
    }

    [Test]
    public void GetLastVariantName_TracksSelectedVariant()
    {
      // Arrange: Set InteractionCount
      _agent!.TestSetInteractionCount(42);

      // Act: Select variant
      var systemPrompt = _agent.SelectSystemPromptVariant();
      var lastVariantName = _agent.LastVariantName;

      // Assert: LastVariantName should be set
      Assert.IsNotNull(lastVariantName, "LastVariantName should be set after variant selection");
      Assert.IsTrue(lastVariantName == "VariantA" || lastVariantName == "VariantB",
        "LastVariantName should be VariantA or VariantB");

      // Verify it matches the selected prompt
      if (lastVariantName == "VariantA")
      {
        Assert.That(systemPrompt, Does.Contain("VARIANT_A"));
      }
      else if (lastVariantName == "VariantB")
      {
        Assert.That(systemPrompt, Does.Contain("VARIANT_B"));
      }
    }

    [Test]
    public void SelectSystemPromptVariant_Distribution_Matches50_50Split()
    {
      // Arrange: 50/50 variants
      var counts = new System.Collections.Generic.Dictionary<string, int>
      {
        { "VariantA", 0 },
        { "VariantB", 0 }
      };

      // Act: Select variants 1000 times
      for (int i = 0; i < 1000; i++)
      {
        _agent!.TestSetInteractionCount(i);
        var systemPrompt = _agent.SelectSystemPromptVariant();
        var variantName = _agent.LastVariantName;

        if (variantName == "VariantA")
        {
          counts["VariantA"]++;
        }
        else if (variantName == "VariantB")
        {
          counts["VariantB"]++;
        }
      }

      // Assert: Distribution should be approximately 50/50 (within 10% tolerance)
      Assert.That(counts["VariantA"], Is.InRange(400, 600), "VariantA should be selected ~50% of the time");
      Assert.That(counts["VariantB"], Is.InRange(400, 600), "VariantB should be selected ~50% of the time");
    }
  }
}
#endif
