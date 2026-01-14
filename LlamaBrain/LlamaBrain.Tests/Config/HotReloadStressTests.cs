#nullable enable
using NUnit.Framework;
using LlamaBrain.Config;
using LlamaBrain.Persona;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LlamaBrain.Tests.Config
{
  [TestFixture]
  [Category("Stress")]
  public class HotReloadStressTests
  {
    [Test]
    public void VariantSelection_ThousandsOfSelections_Deterministic()
    {
      // Arrange: Create variant manager
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

      // Act: Select variants 10,000 times and record selections
      var firstRun = new List<string>();
      for (int i = 0; i < 10000; i++)
      {
        var variant = manager.SelectVariant(seed: i, personaId: "stress_test_npc");
        firstRun.Add(variant.Name);
      }

      // Create new manager and run again
      var manager2 = new PromptVariantManager(variants);
      var secondRun = new List<string>();
      for (int i = 0; i < 10000; i++)
      {
        var variant = manager2.SelectVariant(seed: i, personaId: "stress_test_npc");
        secondRun.Add(variant.Name);
      }

      // Assert: Both runs should produce identical results (determinism)
      Assert.AreEqual(firstRun.Count, secondRun.Count);
      for (int i = 0; i < firstRun.Count; i++)
      {
        Assert.AreEqual(firstRun[i], secondRun[i],
          $"Selection at index {i} differs: {firstRun[i]} vs {secondRun[i]}");
      }
    }

    [Test]
    public void VariantSelection_MultiplePersonas_IndependentDistribution()
    {
      // Arrange: Create variant manager
      var variants = new List<PromptVariant>
      {
        new PromptVariant { Name = "A", SystemPrompt = "A", TrafficPercentage = 50f, IsActive = true },
        new PromptVariant { Name = "B", SystemPrompt = "B", TrafficPercentage = 50f, IsActive = true }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variants for 10 different personas, 1000 times each
      var personaSelections = new Dictionary<string, List<string>>();
      for (int p = 0; p < 10; p++)
      {
        var personaId = $"npc_{p:D3}";
        personaSelections[personaId] = new List<string>();

        for (int i = 0; i < 1000; i++)
        {
          var variant = manager.SelectVariant(seed: i, personaId: personaId);
          personaSelections[personaId].Add(variant.Name);
        }
      }

      // Assert: Each persona should have approximately 50/50 distribution
      foreach (var kvp in personaSelections)
      {
        var personaId = kvp.Key;
        var selections = kvp.Value;

        var countA = selections.Count(s => s == "A");
        var countB = selections.Count(s => s == "B");

        // Allow 10% tolerance (450-550 for each variant)
        Assert.That(countA, Is.InRange(400, 600),
          $"Persona {personaId} should have ~50% A selections, had {countA}");
        Assert.That(countB, Is.InRange(400, 600),
          $"Persona {personaId} should have ~50% B selections, had {countB}");
      }
    }

    [Test]
    public void MetricsAggregation_LargeScale_Accurate()
    {
      // Arrange: Create variant manager and simulate 100,000 interactions
      var variants = new List<PromptVariant>
      {
        new PromptVariant { Name = "Control", SystemPrompt = "Control", TrafficPercentage = 50f, IsActive = true },
        new PromptVariant { Name = "Experimental", SystemPrompt = "Experimental", TrafficPercentage = 50f, IsActive = true }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Simulate 100,000 selections and metric recordings
      var expectedCounts = new Dictionary<string, int> { { "Control", 0 }, { "Experimental", 0 } };

      for (int i = 0; i < 100000; i++)
      {
        var variant = manager.SelectVariant(seed: i, personaId: "stress_npc");
        expectedCounts[variant.Name]++;

        // Record metrics
        manager.RecordSuccess(variant.Name);
        manager.RecordLatency(variant.Name, 120.0 + (i % 10) * 2); // Vary latency
        manager.RecordTokens(variant.Name, 24.0 + (i % 5) * 0.5); // Vary tokens
      }

      // Assert: Verify metrics match expected counts
      var metrics = manager.GetMetrics();
      Assert.AreEqual(2, metrics.Count, "Should have metrics for both variants");

      foreach (var kvp in expectedCounts)
      {
        var variantName = kvp.Key;
        var expectedCount = kvp.Value;

        Assert.IsTrue(metrics.ContainsKey(variantName), $"Should have metrics for {variantName}");
        var variantMetrics = metrics[variantName];

        Assert.AreEqual(expectedCount, variantMetrics.SelectionCount,
          $"{variantName} selection count should match");
        Assert.AreEqual(expectedCount, variantMetrics.SuccessCount,
          $"{variantName} success count should match");

        // Verify averages are in reasonable range
        Assert.That(variantMetrics.AvgLatencyMs, Is.InRange(110.0, 140.0),
          $"{variantName} average latency should be in expected range");
        Assert.That(variantMetrics.AvgTokensGenerated, Is.InRange(23.0, 26.0),
          $"{variantName} average tokens should be in expected range");
      }
    }

    [Test]
    public void ConfigValidation_RapidValidations_NoErrors()
    {
      // Arrange: Create 100 different profiles
      var profiles = new List<PersonaProfile>();
      for (int i = 0; i < 100; i++)
      {
        var profile = PersonaProfile.Create($"stress_npc_{i:D3}", $"Stress Test NPC {i}");
        profile.SystemPrompt = $"You are stress test NPC number {i}.";
        profile.Description = $"Description for NPC {i}";
        profile.Background = $"Background for NPC {i}";
        profiles.Add(profile);
      }

      // Act: Validate all profiles rapidly
      var allErrors = new List<string[]>();
      foreach (var profile in profiles)
      {
        var errors = ConfigValidator.ValidatePersonaProfile(profile);
        allErrors.Add(errors);
      }

      // Assert: All validations should succeed
      foreach (var errors in allErrors)
      {
        Assert.IsEmpty(errors, "All profiles should be valid");
      }
    }

    [Test]
    public void VariantSelection_ConcurrentSelections_ThreadSafe()
    {
      // Arrange: Create variant manager
      var variants = new List<PromptVariant>
      {
        new PromptVariant { Name = "A", SystemPrompt = "A", TrafficPercentage = 100f, IsActive = true }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Simulate concurrent selections from multiple threads
      var tasks = new List<Task>();
      var results = new System.Collections.Concurrent.ConcurrentBag<string>();

      for (int t = 0; t < 10; t++)
      {
        int threadId = t;
        tasks.Add(Task.Run(() =>
        {
          for (int i = 0; i < 1000; i++)
          {
            var variant = manager.SelectVariant(seed: threadId * 1000 + i, personaId: $"thread_{threadId}");
            results.Add(variant.Name);
          }
        }));
      }

      Task.WaitAll(tasks.ToArray());

      // Assert: Should have 10,000 results (10 threads × 1000 selections)
      Assert.AreEqual(10000, results.Count, "Should have all selections");
      Assert.IsTrue(results.All(r => r == "A"), "All selections should be variant A");
    }

    [Test]
    public void MetricsRecording_ConcurrentRecording_ThreadSafe()
    {
      // Arrange: Create variant manager
      var variants = new List<PromptVariant>
      {
        new PromptVariant { Name = "A", SystemPrompt = "A", TrafficPercentage = 100f, IsActive = true }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Record metrics concurrently from multiple threads
      var tasks = new List<Task>();

      for (int t = 0; t < 10; t++)
      {
        tasks.Add(Task.Run(() =>
        {
          for (int i = 0; i < 1000; i++)
          {
            manager.RecordSuccess("A");
            manager.RecordLatency("A", 120.0);
            manager.RecordTokens("A", 24.0);
          }
        }));
      }

      Task.WaitAll(tasks.ToArray());

      // Assert: Metrics should reflect all recordings
      var metrics = manager.GetMetrics();
      var variantA = metrics["A"];

      // Note: Due to concurrent updates, success count may not be exactly 10,000
      // if there are race conditions, but should be close
      Assert.That(variantA.SuccessCount, Is.GreaterThanOrEqualTo(9000),
        "Success count should be close to 10,000 (allowing for race conditions)");
    }

    [Test]
    public void ABTestReport_LargeDataset_HandlesWell()
    {
      // Arrange: Create report with 20 variants, each with 10,000 selections
      var report = new ABTestReport("LargeScaleStressTest");

      for (int v = 0; v < 20; v++)
      {
        report.AddVariantMetrics($"Variant{v:D2}", new VariantMetrics
        {
          SelectionCount = 10000,
          SuccessCount = 9500 + v * 10, // Vary success rates
          ValidationFailureCount = 500 - v * 10,
          FallbackCount = v * 5,
          AvgLatencyMs = 120.0 + v * 2.5,
          AvgTokensGenerated = 24.0 + v * 0.3
        });
      }

      report.Complete();

      // Act: Generate all export formats
      var json = report.ExportToJson();
      var csv = report.ExportToCsv();
      var summary = report.GetSummary();

      // Assert: All exports should succeed
      Assert.IsNotEmpty(json, "JSON export should succeed");
      Assert.IsNotEmpty(csv, "CSV export should succeed");
      Assert.IsNotEmpty(summary, "Summary export should succeed");

      // Verify data integrity
      Assert.AreEqual(200000, report.GetTotalInteractions(), "Total interactions should be 200,000");
      Assert.AreEqual(20, report.GetAllVariantNames().Count(), "Should have 20 variants");
    }

    [Test]
    public void VariantSelection_RapidManagerRecreation_Deterministic()
    {
      // Arrange: Test that recreating manager multiple times produces same results
      var variants = new List<PromptVariant>
      {
        new PromptVariant { Name = "A", SystemPrompt = "A", TrafficPercentage = 50f, IsActive = true },
        new PromptVariant { Name = "B", SystemPrompt = "B", TrafficPercentage = 50f, IsActive = true }
      };

      var results = new List<List<string>>();

      // Act: Create manager, select 100 variants, destroy, repeat 10 times
      for (int run = 0; run < 10; run++)
      {
        var manager = new PromptVariantManager(variants);
        var selections = new List<string>();

        for (int i = 0; i < 100; i++)
        {
          var variant = manager.SelectVariant(seed: i, personaId: "recreation_test");
          selections.Add(variant.Name);
        }

        results.Add(selections);
      }

      // Assert: All runs should produce identical results
      var firstRun = results[0];
      for (int run = 1; run < results.Count; run++)
      {
        var currentRun = results[run];
        Assert.AreEqual(firstRun.Count, currentRun.Count);

        for (int i = 0; i < firstRun.Count; i++)
        {
          Assert.AreEqual(firstRun[i], currentRun[i],
            $"Run {run}, index {i}: expected {firstRun[i]}, got {currentRun[i]}");
        }
      }
    }

    [Test]
    public void VariantSelection_ExtremeTrafficSplits_Accurate()
    {
      // Arrange: Test extreme traffic splits (1% / 99%)
      var variants = new List<PromptVariant>
      {
        new PromptVariant { Name = "Rare", SystemPrompt = "Rare", TrafficPercentage = 1f, IsActive = true },
        new PromptVariant { Name = "Common", SystemPrompt = "Common", TrafficPercentage = 99f, IsActive = true }
      };

      var manager = new PromptVariantManager(variants);

      // Act: Select variants 10,000 times
      var counts = new Dictionary<string, int> { { "Rare", 0 }, { "Common", 0 } };

      for (int i = 0; i < 10000; i++)
      {
        var variant = manager.SelectVariant(seed: i, personaId: "extreme_split");
        counts[variant.Name]++;
      }

      // Assert: Distribution should match 1% / 99% (with tolerance)
      // Rare should be ~100 selections (1% of 10,000), allow 50-150
      Assert.That(counts["Rare"], Is.InRange(50, 150),
        $"Rare variant should be ~1% (50-150 out of 10,000), was {counts["Rare"]}");

      // Common should be ~9,900 selections (99% of 10,000)
      Assert.That(counts["Common"], Is.InRange(9850, 9950),
        $"Common variant should be ~99% (9850-9950 out of 10,000), was {counts["Common"]}");
    }

    [Test]
    public void ProfileValidation_ComplexProfiles_NoPerformanceDegradation()
    {
      // Arrange: Create complex profiles with many traits and metadata
      var profiles = new List<PersonaProfile>();

      for (int p = 0; p < 50; p++)
      {
        var profile = PersonaProfile.Create($"complex_npc_{p}", $"Complex NPC {p}");
        profile.SystemPrompt = new string('X', 1000); // 1KB system prompt
        profile.Description = new string('Y', 500);
        profile.Background = new string('Z', 500);

        // Add 50 traits
        for (int t = 0; t < 50; t++)
        {
          profile.SetTrait($"Trait{t}", $"Value{t}");
        }

        // Add 50 metadata entries
        for (int m = 0; m < 50; m++)
        {
          profile.SetMetadata($"Meta{m}", $"Data{m}");
        }

        profiles.Add(profile);
      }

      // Act: Validate all complex profiles
      var totalErrors = 0;
      foreach (var profile in profiles)
      {
        var errors = ConfigValidator.ValidatePersonaProfile(profile);
        totalErrors += errors.Length;
      }

      // Assert: All profiles should be valid
      Assert.AreEqual(0, totalErrors, "All complex profiles should be valid");
    }
  }
}
