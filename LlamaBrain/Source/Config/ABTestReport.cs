#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LlamaBrain.Config
{
  /// <summary>
  /// Aggregates A/B testing metrics and provides export functionality.
  /// Used to analyze performance differences between prompt variants.
  /// </summary>
  public class ABTestReport
  {
    private readonly Dictionary<string, VariantMetrics> _variantMetrics;

    /// <summary>
    /// The name of the A/B test.
    /// </summary>
    public string TestName { get; }

    /// <summary>
    /// The time when the test started (UTC).
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// The time when the test ended (UTC). Null if not finalized.
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Creates a new ABTestReport with the specified test name.
    /// </summary>
    /// <param name="testName">The name of the A/B test</param>
    public ABTestReport(string testName)
    {
      TestName = testName;
      StartTime = DateTime.UtcNow;
      _variantMetrics = new Dictionary<string, VariantMetrics>();
    }

    /// <summary>
    /// Adds metrics for a specific variant.
    /// </summary>
    /// <param name="variantName">The name of the variant</param>
    /// <param name="metrics">The metrics for this variant</param>
    public void AddVariantMetrics(string variantName, VariantMetrics metrics)
    {
      _variantMetrics[variantName] = metrics;
    }

    /// <summary>
    /// Checks if the report contains metrics for a specific variant.
    /// </summary>
    /// <param name="variantName">The name of the variant</param>
    /// <returns>True if metrics exist for this variant</returns>
    public bool HasVariant(string variantName)
    {
      return _variantMetrics.ContainsKey(variantName);
    }

    /// <summary>
    /// Gets the metrics for a specific variant.
    /// </summary>
    /// <param name="variantName">The name of the variant</param>
    /// <returns>The metrics, or null if variant not found</returns>
    public VariantMetrics? GetVariantMetrics(string variantName)
    {
      return _variantMetrics.TryGetValue(variantName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets the names of all variants in this report.
    /// </summary>
    /// <returns>Enumerable of variant names</returns>
    public IEnumerable<string> GetAllVariantNames()
    {
      return _variantMetrics.Keys;
    }

    /// <summary>
    /// Gets the total number of interactions across all variants.
    /// </summary>
    /// <returns>Total selection count</returns>
    public int GetTotalInteractions()
    {
      return _variantMetrics.Values.Sum(m => m.SelectionCount);
    }

    /// <summary>
    /// Completes the report by setting the end time.
    /// Call this when the A/B test is complete.
    /// </summary>
    public void Finalize()
    {
      EndTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the report by setting the end time.
    /// Alias for Finalize() to avoid naming conflicts.
    /// </summary>
    public void Complete()
    {
      Finalize();
    }

    /// <summary>
    /// Gets the duration of the test in seconds.
    /// Returns 0 if the test has not been finalized.
    /// </summary>
    /// <returns>Duration in seconds</returns>
    public double GetDurationSeconds()
    {
      if (EndTime == null)
      {
        return 0;
      }

      return (EndTime.Value - StartTime).TotalSeconds;
    }

    /// <summary>
    /// Gets the success rate for a specific variant.
    /// </summary>
    /// <param name="variantName">The name of the variant</param>
    /// <returns>Success rate (0.0 to 1.0), or 0 if variant not found or no selections</returns>
    public double GetSuccessRate(string variantName)
    {
      if (!_variantMetrics.TryGetValue(variantName, out var metrics))
      {
        return 0;
      }

      if (metrics.SelectionCount == 0)
      {
        return 0;
      }

      return (double)metrics.SuccessCount / metrics.SelectionCount;
    }

    /// <summary>
    /// Exports the report to JSON format.
    /// </summary>
    /// <returns>JSON string representation of the report</returns>
    public string ExportToJson()
    {
      var reportData = new Dictionary<string, object>
      {
        { "testName", TestName },
        { "startTime", StartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
        { "endTime", EndTime?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? "null" },
        { "durationSeconds", GetDurationSeconds() },
        { "totalInteractions", GetTotalInteractions() },
        { "variants", _variantMetrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new Dictionary<string, object>
            {
              { "selectionCount", kvp.Value.SelectionCount },
              { "successCount", kvp.Value.SuccessCount },
              { "validationFailureCount", kvp.Value.ValidationFailureCount },
              { "fallbackCount", kvp.Value.FallbackCount },
              { "avgLatencyMs", kvp.Value.AvgLatencyMs },
              { "avgTokensGenerated", kvp.Value.AvgTokensGenerated },
              { "successRate", GetSuccessRate(kvp.Key) }
            }
          )
        }
      };

      return JsonConvert.SerializeObject(reportData, Formatting.Indented);
    }

    /// <summary>
    /// Exports the report to CSV format.
    /// </summary>
    /// <returns>CSV string representation of the report</returns>
    public string ExportToCsv()
    {
      var csv = new StringBuilder();

      // Header
      csv.AppendLine("VariantName,SelectionCount,SuccessCount,ValidationFailureCount,FallbackCount,AvgLatencyMs,AvgTokensGenerated,SuccessRate");

      // Data rows
      foreach (var kvp in _variantMetrics.OrderBy(x => x.Key))
      {
        var variantName = kvp.Key;
        var metrics = kvp.Value;
        var successRate = GetSuccessRate(variantName);

        csv.AppendLine($"{variantName}," +
                      $"{metrics.SelectionCount}," +
                      $"{metrics.SuccessCount}," +
                      $"{metrics.ValidationFailureCount}," +
                      $"{metrics.FallbackCount}," +
                      $"{metrics.AvgLatencyMs:F2}," +
                      $"{metrics.AvgTokensGenerated:F2}," +
                      $"{successRate:F4}");
      }

      return csv.ToString();
    }

    /// <summary>
    /// Gets a human-readable summary of the report.
    /// </summary>
    /// <returns>Summary string</returns>
    public string GetSummary()
    {
      var summary = new StringBuilder();

      summary.AppendLine($"A/B Test Report: {TestName}");
      summary.AppendLine($"Started: {StartTime:yyyy-MM-dd HH:mm:ss} UTC");
      if (EndTime != null)
      {
        summary.AppendLine($"Ended: {EndTime:yyyy-MM-dd HH:mm:ss} UTC");
        summary.AppendLine($"Duration: {GetDurationSeconds():F1} seconds");
      }
      else
      {
        summary.AppendLine("Status: In Progress");
      }

      summary.AppendLine($"Total Interactions: {GetTotalInteractions()}");
      summary.AppendLine();

      summary.AppendLine("Variant Performance:");
      foreach (var kvp in _variantMetrics.OrderBy(x => x.Key))
      {
        var variantName = kvp.Key;
        var metrics = kvp.Value;
        var successRate = GetSuccessRate(variantName);

        summary.AppendLine($"  {variantName}:");
        summary.AppendLine($"    Selections: {metrics.SelectionCount}");
        summary.AppendLine($"    Success Rate: {successRate:P2}");
        summary.AppendLine($"    Avg Latency: {metrics.AvgLatencyMs:F2}ms");
        summary.AppendLine($"    Avg Tokens: {metrics.AvgTokensGenerated:F2}");
        if (metrics.ValidationFailureCount > 0)
        {
          summary.AppendLine($"    Validation Failures: {metrics.ValidationFailureCount}");
        }
        if (metrics.FallbackCount > 0)
        {
          summary.AppendLine($"    Fallbacks: {metrics.FallbackCount}");
        }
      }

      return summary.ToString();
    }
  }
}
