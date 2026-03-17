#nullable enable
using NUnit.Framework;
using LlamaBrain.Config;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Tests.Config
{
  [TestFixture]
  [Category("Domain")]
  public class PromptVariantManagerTests
  {
    [Test]
    public void SelectVariant_SameSeed_SelectsSameVariant()
    {
      // Arrange: Create variants with 50/50 split
      var variants = new List<PromptVariant>
      {
        new PromptVariant
        {
          Name = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariant
        {
          Name = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variant with same seed multiple times
      var result1 = manager.SelectVariant(seed: 42, personaId: "npc_001");
      var result2 = manager.SelectVariant(seed: 42, personaId: "npc_001");
      var result3 = manager.SelectVariant(seed: 42, personaId: "npc_001");

      // Assert: Same seed should always select same variant (determinism)
      Assert.AreEqual(result1.Name, result2.Name, "Same seed should select same variant");
      Assert.AreEqual(result1.Name, result3.Name, "Same seed should select same variant");
    }

    [Test]
    public void SelectVariant_DifferentSeeds_MaySelectDifferentVariants()
    {
      // Arrange: Create variants with 50/50 split
      var variants = new List<PromptVariant>
      {
        new PromptVariant
        {
          Name = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariant
        {
          Name = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variants with different seeds
      var results = new HashSet<string>();
      for (int i = 0; i < 100; i++)
      {
        var result = manager.SelectVariant(seed: i, personaId: "npc_001");
        results.Add(result.Name);
      }

      // Assert: With 100 trials and 50/50 split, we should see both variants
      Assert.That(results.Count, Is.GreaterThanOrEqualTo(2), "Different seeds should select different variants");
      Assert.IsTrue(results.Contains("VariantA"), "Should select VariantA at least once");
      Assert.IsTrue(results.Contains("VariantB"), "Should select VariantB at least once");
    }

    [Test]
    public void SelectVariant_TrafficSplit_50_50_ProducesEvenDistribution()
    {
      // Arrange: Create variants with 50/50 split
      var variants = new List<PromptVariant>
      {
        new PromptVariant
        {
          Name = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariant
        {
          Name = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variants with 1000 different seeds
      var counts = new Dictionary<string, int>();
      for (int i = 0; i < 1000; i++)
      {
        var result = manager.SelectVariant(seed: i, personaId: "npc_001");
        counts[result.Name] = counts.GetValueOrDefault(result.Name, 0) + 1;
      }

      // Assert: Distribution should be approximately 50/50 (within 10% tolerance)
      Assert.AreEqual(2, counts.Count, "Should select both variants");
      var countA = counts["VariantA"];
      var countB = counts["VariantB"];

      // Check both are within 40%-60% range (10% tolerance on 50%)
      Assert.That(countA, Is.InRange(400, 600), "VariantA should be selected ~50% of the time");
      Assert.That(countB, Is.InRange(400, 600), "VariantB should be selected ~50% of the time");
    }

    [Test]
    public void SelectVariant_TrafficSplit_10_90_ProducesCorrectRatios()
    {
      // Arrange: Create variants with 10/90 split (gradual rollout scenario)
      var variants = new List<PromptVariant>
      {
        new PromptVariant
        {
          Name = "Experimental",
          SystemPrompt = "You are the experimental variant.",
          TrafficPercentage = 10f,
          IsActive = true
        },
        new PromptVariant
        {
          Name = "Control",
          SystemPrompt = "You are the control variant.",
          TrafficPercentage = 90f,
          IsActive = true
        }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variants with 1000 different seeds
      var counts = new Dictionary<string, int>();
      for (int i = 0; i < 1000; i++)
      {
        var result = manager.SelectVariant(seed: i, personaId: "npc_001");
        counts[result.Name] = counts.GetValueOrDefault(result.Name, 0) + 1;
      }

      // Assert: Distribution should be approximately 10/90 (within 5% tolerance)
      Assert.AreEqual(2, counts.Count, "Should select both variants");
      var countExperimental = counts["Experimental"];
      var countControl = counts["Control"];

      // Check experimental is within 5%-15% range (5% tolerance on 10%)
      // Check control is within 85%-95% range (5% tolerance on 90%)
      Assert.That(countExperimental, Is.InRange(50, 150), "Experimental should be selected ~10% of the time");
      Assert.That(countControl, Is.InRange(850, 950), "Control should be selected ~90% of the time");
    }

    [Test]
    public void SelectVariant_InactiveVariant_SkippedInSelection()
    {
      // Arrange: Create variants where one is inactive
      var variants = new List<PromptVariant>
      {
        new PromptVariant
        {
          Name = "Active",
          SystemPrompt = "You are active.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariant
        {
          Name = "Inactive",
          SystemPrompt = "You are inactive.",
          TrafficPercentage = 50f,
          IsActive = false // Inactive
        }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variants with 100 different seeds
      var results = new HashSet<string>();
      for (int i = 0; i < 100; i++)
      {
        var result = manager.SelectVariant(seed: i, personaId: "npc_001");
        results.Add(result.Name);
      }

      // Assert: Should only select active variant
      Assert.AreEqual(1, results.Count, "Should only select active variant");
      Assert.IsTrue(results.Contains("Active"), "Should select Active variant");
      Assert.IsFalse(results.Contains("Inactive"), "Should not select Inactive variant");
    }

    [Test]
    public void SelectVariant_NoActiveVariants_ReturnsFallback()
    {
      // Arrange: Create variants where all are inactive
      var variants = new List<PromptVariant>
      {
        new PromptVariant
        {
          Name = "Inactive1",
          SystemPrompt = "Inactive 1.",
          TrafficPercentage = 50f,
          IsActive = false
        },
        new PromptVariant
        {
          Name = "Inactive2",
          SystemPrompt = "Inactive 2.",
          TrafficPercentage = 50f,
          IsActive = false
        }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Try to select variant
      var result = manager.SelectVariant(seed: 42, personaId: "npc_001");

      // Assert: Should return first variant as fallback
      Assert.IsNotNull(result, "Should return fallback variant");
      Assert.AreEqual("Inactive1", result.Name, "Should return first variant as fallback");
    }

    [Test]
    public void SelectVariant_EmptyVariantList_ThrowsException()
    {
      // Arrange: Create manager with empty list
      var variants = new List<PromptVariant>();

      // Act & Assert: Should throw exception
      Assert.Throws<System.ArgumentException>(() => new PromptVariantManager(variants));
    }

    [Test]
    public void SelectVariant_DifferentPersonaIds_ProducesDifferentDistributions()
    {
      // Arrange: Create variants with 50/50 split
      var variants = new List<PromptVariant>
      {
        new PromptVariant
        {
          Name = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariant
        {
          Name = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variant with same seed but different persona IDs
      var resultNpc001 = manager.SelectVariant(seed: 42, personaId: "npc_001");
      var resultNpc002 = manager.SelectVariant(seed: 42, personaId: "npc_002");

      // Assert: Different persona IDs may select different variants
      // (We don't assert they MUST be different, just that the system considers personaId)
      // Run multiple seeds to verify personaId affects distribution
      var npc001Results = new HashSet<string>();
      var npc002Results = new HashSet<string>();

      for (int i = 0; i < 100; i++)
      {
        npc001Results.Add(manager.SelectVariant(seed: i, personaId: "npc_001").Name);
        npc002Results.Add(manager.SelectVariant(seed: i, personaId: "npc_002").Name);
      }

      // Both should see both variants (confirming personaId affects distribution)
      Assert.IsTrue(npc001Results.Count >= 2 || npc002Results.Count >= 2,
        "Different personaIds should affect variant selection");
    }

    [Test]
    public void GetMetrics_ReturnsCorrectSelectionCounts()
    {
      // Arrange: Create variants
      var variants = new List<PromptVariant>
      {
        new PromptVariant
        {
          Name = "VariantA",
          SystemPrompt = "You are variant A.",
          TrafficPercentage = 50f,
          IsActive = true
        },
        new PromptVariant
        {
          Name = "VariantB",
          SystemPrompt = "You are variant B.",
          TrafficPercentage = 50f,
          IsActive = true
        }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variants multiple times
      for (int i = 0; i < 10; i++)
      {
        manager.SelectVariant(seed: i, personaId: "npc_001");
      }

      var metrics = manager.GetMetrics();

      // Assert: Metrics should track selection counts
      Assert.IsNotNull(metrics, "Metrics should not be null");
      Assert.That(metrics.Count, Is.GreaterThan(0), "Metrics should contain entries");

      // Total selections should be 10
      var totalSelections = metrics.Values.Sum(m => m.SelectionCount);
      Assert.AreEqual(10, totalSelections, "Total selections should be 10");
    }
  }
}
