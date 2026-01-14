#nullable enable
using NUnit.Framework;
using LlamaBrain.Config;
using LlamaBrain.Persona;
using LlamaBrain.Core;
using System.Diagnostics;
using System.Collections.Generic;

namespace LlamaBrain.Tests.Config
{
  [TestFixture]
  [Category("Performance")]
  public class HotReloadPerformanceTests
  {
    [Test]
    public void ConfigValidation_MeetsLatencyTarget()
    {
      // Arrange: Create a valid PersonaProfile
      var profile = PersonaProfile.Create("perf_test_001", "Performance Test NPC");
      profile.SystemPrompt = "You are a performance test NPC.";
      profile.Description = "Testing validation performance";
      profile.Background = "Test background";

      // Act: Measure validation time (should be < 10ms)
      var stopwatch = Stopwatch.StartNew();
      var errors = ConfigValidator.ValidatePersonaProfile(profile);
      stopwatch.Stop();

      // Assert
      Assert.IsEmpty(errors, "Validation should succeed for valid profile");
      Assert.Less(stopwatch.ElapsedMilliseconds, 10,
        $"Config validation should be < 10ms, was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void LlmConfigValidation_MeetsLatencyTarget()
    {
      // Arrange: Create a valid LlmConfig
      var config = new LlmConfig
      {
        MaxTokens = 64,
        Temperature = 0.7f,
        TopP = 0.9f,
        TopK = 40,
        RepeatPenalty = 1.1f
      };

      // Act: Measure validation time
      var stopwatch = Stopwatch.StartNew();
      var errors = config.Validate();
      stopwatch.Stop();

      // Assert
      Assert.IsEmpty(errors, "Validation should succeed for valid config");
      Assert.Less(stopwatch.ElapsedMilliseconds, 10,
        $"LlmConfig validation should be < 10ms, was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void VariantSelection_MeetsOverheadTarget()
    {
      // Arrange: Create variant manager with 2 variants
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

      // Act: Measure variant selection time (should be < 1ms per selection)
      var stopwatch = Stopwatch.StartNew();
      for (int i = 0; i < 1000; i++)
      {
        manager.SelectVariant(seed: i, personaId: "perf_test_npc");
      }
      stopwatch.Stop();

      var avgTimePerSelection = stopwatch.Elapsed.TotalMilliseconds / 1000.0;

      // Assert
      Assert.Less(avgTimePerSelection, 1.0,
        $"Variant selection should be < 1ms per selection, was {avgTimePerSelection:F3}ms");
    }

    [Test]
    public void VariantSelection_LargeVariantList_AcceptablePerformance()
    {
      // Arrange: Create variant manager with 10 variants (stress test)
      var variants = new List<PromptVariant>();
      for (int i = 0; i < 10; i++)
      {
        variants.Add(new PromptVariant
        {
          Name = $"Variant{i}",
          SystemPrompt = $"You are variant {i}.",
          TrafficPercentage = 10f,
          IsActive = true
        });
      }

      var manager = new PromptVariantManager(variants);

      // Act: Measure variant selection time with large variant list
      var stopwatch = Stopwatch.StartNew();
      for (int i = 0; i < 1000; i++)
      {
        manager.SelectVariant(seed: i, personaId: "perf_test_npc");
      }
      stopwatch.Stop();

      var avgTimePerSelection = stopwatch.Elapsed.TotalMilliseconds / 1000.0;

      // Assert: Even with 10 variants, should be < 5ms per selection
      Assert.Less(avgTimePerSelection, 5.0,
        $"Variant selection with 10 variants should be < 5ms per selection, was {avgTimePerSelection:F3}ms");
    }

    [Test]
    public void MetricsAggregation_MeetsLatencyTarget()
    {
      // Arrange: Create report and add metrics for 10 variants
      var report = new ABTestReport("PerformanceTest");
      for (int i = 0; i < 10; i++)
      {
        report.AddVariantMetrics($"Variant{i}", new VariantMetrics
        {
          SelectionCount = 1000,
          SuccessCount = 950,
          ValidationFailureCount = 50,
          AvgLatencyMs = 120.5,
          AvgTokensGenerated = 24.3
        });
      }

      // Act: Measure aggregation time
      var stopwatch = Stopwatch.StartNew();
      var totalInteractions = report.GetTotalInteractions();
      var summary = report.GetSummary();
      stopwatch.Stop();

      // Assert
      Assert.AreEqual(10000, totalInteractions);
      Assert.Less(stopwatch.ElapsedMilliseconds, 10,
        $"Metrics aggregation should be < 10ms, was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void JsonExport_MeetsLatencyTarget()
    {
      // Arrange: Create report with realistic data
      var report = new ABTestReport("JsonExportPerformance");
      for (int i = 0; i < 5; i++)
      {
        report.AddVariantMetrics($"Variant{i}", new VariantMetrics
        {
          SelectionCount = 500,
          SuccessCount = 475,
          ValidationFailureCount = 25,
          AvgLatencyMs = 125.0 + i * 5,
          AvgTokensGenerated = 24.0 + i * 0.5
        });
      }
      report.Complete();

      // Act: Measure JSON export time
      var stopwatch = Stopwatch.StartNew();
      var json = report.ExportToJson();
      stopwatch.Stop();

      // Assert
      Assert.IsNotEmpty(json);
      Assert.Less(stopwatch.ElapsedMilliseconds, 50,
        $"JSON export should be < 50ms, was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void CsvExport_MeetsLatencyTarget()
    {
      // Arrange: Create report with realistic data
      var report = new ABTestReport("CsvExportPerformance");
      for (int i = 0; i < 5; i++)
      {
        report.AddVariantMetrics($"Variant{i}", new VariantMetrics
        {
          SelectionCount = 500,
          SuccessCount = 475,
          ValidationFailureCount = 25,
          AvgLatencyMs = 125.0 + i * 5,
          AvgTokensGenerated = 24.0 + i * 0.5
        });
      }
      report.Complete();

      // Act: Measure CSV export time
      var stopwatch = Stopwatch.StartNew();
      var csv = report.ExportToCsv();
      stopwatch.Stop();

      // Assert
      Assert.IsNotEmpty(csv);
      Assert.Less(stopwatch.ElapsedMilliseconds, 50,
        $"CSV export should be < 50ms, was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void ProfileConversion_MeetsLatencyTarget()
    {
      // Arrange: Create PersonaProfile with realistic data
      var profile = PersonaProfile.Create("perf_test_002", "Performance Test NPC 2");
      profile.SystemPrompt = "You are a performance test NPC with a moderately long system prompt that includes multiple sentences and provides detailed instructions for behavior.";
      profile.Description = "A detailed description of this NPC including background, personality traits, and behavior patterns.";
      profile.Background = "An extensive background story covering the NPC's history, motivations, and relationships.";

      // Add some traits
      for (int i = 0; i < 10; i++)
      {
        profile.SetTrait($"Trait{i}", $"Value{i}");
      }

      // Add some metadata
      for (int i = 0; i < 10; i++)
      {
        profile.SetMetadata($"Meta{i}", $"Data{i}");
      }

      // Act: Measure validation time
      var stopwatch = Stopwatch.StartNew();
      var errors = ConfigValidator.ValidatePersonaProfile(profile);
      stopwatch.Stop();

      // Assert
      Assert.IsEmpty(errors);
      Assert.Less(stopwatch.ElapsedMilliseconds, 10,
        $"Profile validation with traits/metadata should be < 10ms, was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void VariantTrafficValidation_MeetsLatencyTarget()
    {
      // Arrange: Create 5 variants that sum to 100%
      var variants = new List<PromptVariant>();
      for (int i = 0; i < 5; i++)
      {
        variants.Add(new PromptVariant
        {
          Name = $"Variant{i}",
          SystemPrompt = $"You are variant {i}.",
          TrafficPercentage = 20f,
          IsActive = true
        });
      }

      // Act: Measure validation time (this happens in constructor)
      var stopwatch = Stopwatch.StartNew();
      var manager = new PromptVariantManager(variants);
      stopwatch.Stop();

      // Assert
      Assert.Less(stopwatch.ElapsedMilliseconds, 5,
        $"Variant manager creation/validation should be < 5ms, was {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void MetricsRecording_MinimalOverhead()
    {
      // Arrange: Create variant manager
      var variants = new List<PromptVariant>
      {
        new PromptVariant { Name = "A", SystemPrompt = "A", TrafficPercentage = 100f, IsActive = true }
      };
      var manager = new PromptVariantManager(variants);

      // Act: Measure overhead of metrics recording
      var stopwatch = Stopwatch.StartNew();
      for (int i = 0; i < 10000; i++)
      {
        manager.RecordSuccess("A");
        manager.RecordLatency("A", 120.5);
        manager.RecordTokens("A", 24);
      }
      stopwatch.Stop();

      var avgTimePerRecord = stopwatch.Elapsed.TotalMilliseconds / 10000.0;

      // Assert: Metrics recording should have minimal overhead (< 0.01ms per record)
      Assert.Less(avgTimePerRecord, 0.01,
        $"Metrics recording should be < 0.01ms per record, was {avgTimePerRecord:F6}ms");
    }
  }
}
