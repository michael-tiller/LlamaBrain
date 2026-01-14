#nullable enable
using NUnit.Framework;
using LlamaBrain.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlamaBrain.Tests.Config
{
  [TestFixture]
  [Category("Domain")]
  public class ABTestReportTests
  {
    [Test]
    public void Create_WithTestName_SetsTestName()
    {
      // Act
      var report = new ABTestReport("TestExperiment001");

      // Assert
      Assert.AreEqual("TestExperiment001", report.TestName);
    }

    [Test]
    public void Create_SetsStartTime()
    {
      // Arrange
      var beforeCreate = DateTime.UtcNow;

      // Act
      var report = new ABTestReport("Test");
      var afterCreate = DateTime.UtcNow;

      // Assert
      Assert.That(report.StartTime, Is.GreaterThanOrEqualTo(beforeCreate));
      Assert.That(report.StartTime, Is.LessThanOrEqualTo(afterCreate));
    }

    [Test]
    public void AddVariantMetrics_StoresMetrics()
    {
      // Arrange
      var report = new ABTestReport("Test");
      var metrics = new VariantMetrics
      {
        SelectionCount = 100,
        SuccessCount = 95,
        ValidationFailureCount = 5,
        AvgLatencyMs = 120.5,
        AvgTokensGenerated = 24.3
      };

      // Act
      report.AddVariantMetrics("VariantA", metrics);

      // Assert
      Assert.IsTrue(report.HasVariant("VariantA"));
      var retrieved = report.GetVariantMetrics("VariantA");
      Assert.IsNotNull(retrieved);
      Assert.AreEqual(100, retrieved.SelectionCount);
      Assert.AreEqual(95, retrieved.SuccessCount);
      Assert.AreEqual(5, retrieved.ValidationFailureCount);
      Assert.AreEqual(120.5, retrieved.AvgLatencyMs);
      Assert.AreEqual(24.3, retrieved.AvgTokensGenerated);
    }

    [Test]
    public void AddVariantMetrics_MultipleVariants_StoresAll()
    {
      // Arrange
      var report = new ABTestReport("Test");
      var metricsA = new VariantMetrics { SelectionCount = 100 };
      var metricsB = new VariantMetrics { SelectionCount = 200 };

      // Act
      report.AddVariantMetrics("VariantA", metricsA);
      report.AddVariantMetrics("VariantB", metricsB);

      // Assert
      Assert.AreEqual(2, report.GetAllVariantNames().Count());
      Assert.IsTrue(report.HasVariant("VariantA"));
      Assert.IsTrue(report.HasVariant("VariantB"));
    }

    [Test]
    public void GetTotalInteractions_SumsSelectionCounts()
    {
      // Arrange
      var report = new ABTestReport("Test");
      report.AddVariantMetrics("VariantA", new VariantMetrics { SelectionCount = 100 });
      report.AddVariantMetrics("VariantB", new VariantMetrics { SelectionCount = 200 });
      report.AddVariantMetrics("VariantC", new VariantMetrics { SelectionCount = 300 });

      // Act
      var total = report.GetTotalInteractions();

      // Assert
      Assert.AreEqual(600, total);
    }

    [Test]
    public void Finalize_SetsEndTime()
    {
      // Arrange
      var report = new ABTestReport("Test");
      var beforeFinalize = DateTime.UtcNow;

      // Act
      report.Finalize();
      var afterFinalize = DateTime.UtcNow;

      // Assert
      Assert.IsNotNull(report.EndTime);
      Assert.That(report.EndTime.Value, Is.GreaterThanOrEqualTo(beforeFinalize));
      Assert.That(report.EndTime.Value, Is.LessThanOrEqualTo(afterFinalize));
    }

    [Test]
    public void GetDurationSeconds_BeforeFinalize_ReturnsZero()
    {
      // Arrange
      var report = new ABTestReport("Test");

      // Act
      var duration = report.GetDurationSeconds();

      // Assert
      Assert.AreEqual(0, duration);
    }

    [Test]
    public void GetDurationSeconds_AfterFinalize_ReturnsCorrectDuration()
    {
      // Arrange
      var report = new ABTestReport("Test");
      System.Threading.Thread.Sleep(100); // Wait 100ms

      // Act
      report.Finalize();
      var duration = report.GetDurationSeconds();

      // Assert
      Assert.That(duration, Is.GreaterThanOrEqualTo(0.1)); // At least 100ms = 0.1s
      Assert.That(duration, Is.LessThan(1.0)); // Should be less than 1 second
    }

    [Test]
    public void ExportToJson_SerializesCorrectly()
    {
      // Arrange
      var report = new ABTestReport("JsonExportTest");
      report.AddVariantMetrics("VariantA", new VariantMetrics
      {
        SelectionCount = 500,
        SuccessCount = 480,
        ValidationFailureCount = 20,
        AvgLatencyMs = 125.5,
        AvgTokensGenerated = 24.8
      });
      report.AddVariantMetrics("VariantB", new VariantMetrics
      {
        SelectionCount = 500,
        SuccessCount = 490,
        ValidationFailureCount = 10,
        AvgLatencyMs = 130.2,
        AvgTokensGenerated = 25.1
      });
      report.Finalize();

      // Act
      var json = report.ExportToJson();

      // Assert
      Assert.IsNotNull(json);
      Assert.That(json, Does.Contain("JsonExportTest"));
      Assert.That(json, Does.Contain("VariantA"));
      Assert.That(json, Does.Contain("VariantB"));
      Assert.That(json, Does.Contain("500")); // SelectionCount
      Assert.That(json, Does.Contain("480")); // SuccessCount
    }

    [Test]
    public void ExportToJson_RoundTrips()
    {
      // Arrange
      var report = new ABTestReport("RoundTripTest");
      report.AddVariantMetrics("VariantA", new VariantMetrics { SelectionCount = 100 });
      report.Finalize();

      // Act: Export to JSON and deserialize back
      var json = report.ExportToJson();
      var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

      // Assert: Basic structure is preserved
      Assert.IsNotNull(deserialized);
      Assert.IsTrue(deserialized!.ContainsKey("testName"));
      Assert.IsTrue(deserialized.ContainsKey("variants"));
    }

    [Test]
    public void ExportToCsv_GeneratesValidCsv()
    {
      // Arrange
      var report = new ABTestReport("CsvExportTest");
      report.AddVariantMetrics("VariantA", new VariantMetrics
      {
        SelectionCount = 500,
        SuccessCount = 480,
        ValidationFailureCount = 20,
        FallbackCount = 5,
        AvgLatencyMs = 125.5,
        AvgTokensGenerated = 24.8
      });
      report.AddVariantMetrics("VariantB", new VariantMetrics
      {
        SelectionCount = 500,
        SuccessCount = 490,
        ValidationFailureCount = 10,
        FallbackCount = 3,
        AvgLatencyMs = 130.2,
        AvgTokensGenerated = 25.1
      });
      report.Finalize();

      // Act
      var csv = report.ExportToCsv();

      // Assert
      Assert.IsNotNull(csv);

      var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
      Assert.That(lines.Length, Is.GreaterThanOrEqualTo(3)); // Header + 2 data rows

      // Check header
      var header = lines[0];
      Assert.That(header, Does.Contain("VariantName"));
      Assert.That(header, Does.Contain("SelectionCount"));
      Assert.That(header, Does.Contain("SuccessCount"));
      Assert.That(header, Does.Contain("ValidationFailureCount"));
      Assert.That(header, Does.Contain("AvgLatencyMs"));
      Assert.That(header, Does.Contain("AvgTokensGenerated"));

      // Check data rows contain variant names
      Assert.That(csv, Does.Contain("VariantA"));
      Assert.That(csv, Does.Contain("VariantB"));
    }

    [Test]
    public void GetSuccessRate_CalculatesCorrectly()
    {
      // Arrange
      var report = new ABTestReport("Test");
      report.AddVariantMetrics("VariantA", new VariantMetrics
      {
        SelectionCount = 100,
        SuccessCount = 95
      });

      // Act
      var successRate = report.GetSuccessRate("VariantA");

      // Assert
      Assert.AreEqual(0.95, successRate, 0.001); // 95%
    }

    [Test]
    public void GetSuccessRate_ZeroSelections_ReturnsZero()
    {
      // Arrange
      var report = new ABTestReport("Test");
      report.AddVariantMetrics("VariantA", new VariantMetrics
      {
        SelectionCount = 0,
        SuccessCount = 0
      });

      // Act
      var successRate = report.GetSuccessRate("VariantA");

      // Assert
      Assert.AreEqual(0, successRate);
    }

    [Test]
    public void GetSuccessRate_NonExistentVariant_ReturnsZero()
    {
      // Arrange
      var report = new ABTestReport("Test");

      // Act
      var successRate = report.GetSuccessRate("NonExistent");

      // Assert
      Assert.AreEqual(0, successRate);
    }

    [Test]
    public void Summary_IncludesKeyMetrics()
    {
      // Arrange
      var report = new ABTestReport("SummaryTest");
      report.AddVariantMetrics("VariantA", new VariantMetrics
      {
        SelectionCount = 500,
        SuccessCount = 475,
        AvgLatencyMs = 120.5
      });
      report.AddVariantMetrics("VariantB", new VariantMetrics
      {
        SelectionCount = 500,
        SuccessCount = 490,
        AvgLatencyMs = 130.2
      });
      report.Finalize();

      // Act
      var summary = report.GetSummary();

      // Assert
      Assert.IsNotNull(summary);
      Assert.That(summary, Does.Contain("SummaryTest"));
      Assert.That(summary, Does.Contain("1000")); // Total interactions
      Assert.That(summary, Does.Contain("VariantA"));
      Assert.That(summary, Does.Contain("VariantB"));
    }
  }
}
