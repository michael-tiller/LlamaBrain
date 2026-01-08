using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// Validates that static prefixes remain byte-stable across requests.
  /// Detects cache invalidation bugs where dynamic content accidentally
  /// appears in the static prefix.
  /// </summary>
  /// <remarks>
  /// Usage pattern:
  /// 1. After assembling a prompt with cache info, call ValidatePrefix()
  /// 2. If the prefix changed unexpectedly, a PrefixStabilityViolation is returned
  /// 3. The violation contains details about what changed
  ///
  /// Thread-safe: Uses lock for concurrent access.
  /// </remarks>
  public class PrefixStabilityValidator
  {
    private readonly object _lock = new object();
    private readonly Dictionary<string, PrefixRecord> _prefixRecords = new Dictionary<string, PrefixRecord>(StringComparer.Ordinal);
    private readonly List<PrefixStabilityViolation> _violations = new List<PrefixStabilityViolation>();

    /// <summary>
    /// Whether to throw on violation (default: false, just record).
    /// </summary>
    public bool ThrowOnViolation { get; set; } = false;

    /// <summary>
    /// Number of violations detected.
    /// </summary>
    public int ViolationCount
    {
      get
      {
        lock (_lock) { return _violations.Count; }
      }
    }

    /// <summary>
    /// Gets all recorded violations.
    /// </summary>
    public IReadOnlyList<PrefixStabilityViolation> Violations
    {
      get
      {
        lock (_lock) { return _violations.ToArray(); }
      }
    }

    /// <summary>
    /// Validates that a static prefix is stable for a given key (typically NPC ID).
    /// First call for a key establishes the baseline; subsequent calls compare against it.
    /// </summary>
    /// <param name="key">Unique key for this prefix context (e.g., NPC ID)</param>
    /// <param name="staticPrefix">The static prefix to validate</param>
    /// <param name="boundary">The boundary configuration used</param>
    /// <returns>Null if valid, or a violation if the prefix changed unexpectedly</returns>
    public PrefixStabilityViolation? ValidatePrefix(string key, string staticPrefix, StaticPrefixBoundary boundary)
    {
      if (string.IsNullOrEmpty(key))
        throw new ArgumentNullException(nameof(key));

      var hash = ComputeHash(staticPrefix ?? "");

      lock (_lock)
      {
        if (!_prefixRecords.TryGetValue(key, out var existing))
        {
          // First time seeing this key - establish baseline
          _prefixRecords[key] = new PrefixRecord
          {
            Hash = hash,
            Boundary = boundary,
            FirstSeenUtc = DateTime.UtcNow,
            SamplePrefix = Truncate(staticPrefix, 200),
            CheckCount = 1
          };
          return null;
        }

        // Check if boundary changed (expected to invalidate)
        if (existing.Boundary != boundary)
        {
          // Boundary change is expected to change prefix - update baseline
          _prefixRecords[key] = new PrefixRecord
          {
            Hash = hash,
            Boundary = boundary,
            FirstSeenUtc = DateTime.UtcNow,
            SamplePrefix = Truncate(staticPrefix, 200),
            CheckCount = 1
          };
          return null;
        }

        existing.CheckCount++;

        // Same boundary - prefix should be identical
        if (existing.Hash != hash)
        {
          var violation = new PrefixStabilityViolation
          {
            Key = key,
            Boundary = boundary,
            ExpectedHash = existing.Hash,
            ActualHash = hash,
            ExpectedSample = existing.SamplePrefix,
            ActualSample = Truncate(staticPrefix, 200),
            CheckNumber = existing.CheckCount,
            DetectedAtUtc = DateTime.UtcNow
          };

          _violations.Add(violation);

          if (ThrowOnViolation)
          {
            throw new PrefixStabilityException(violation);
          }

          return violation;
        }

        return null;
      }
    }

    /// <summary>
    /// Resets all recorded prefixes and violations.
    /// </summary>
    public void Reset()
    {
      lock (_lock)
      {
        _prefixRecords.Clear();
        _violations.Clear();
      }
    }

    /// <summary>
    /// Resets the baseline for a specific key.
    /// </summary>
    /// <param name="key">The key to reset (e.g., NPC ID).</param>
    public void ResetKey(string key)
    {
      lock (_lock)
      {
        _prefixRecords.Remove(key);
      }
    }

    /// <summary>
    /// Checks if any violations have been recorded.
    /// </summary>
    public bool HasViolations
    {
      get
      {
        lock (_lock) { return _violations.Count > 0; }
      }
    }

    private static string ComputeHash(string input)
    {
      using var sha256 = SHA256.Create();
      var bytes = Encoding.UTF8.GetBytes(input);
      var hashBytes = sha256.ComputeHash(bytes);
      return Convert.ToBase64String(hashBytes);
    }

    private static string Truncate(string? input, int maxLength)
    {
      if (string.IsNullOrEmpty(input)) return "";
      if (input.Length <= maxLength) return input;
      return input.Substring(0, maxLength) + "...";
    }

    private class PrefixRecord
    {
      public string Hash { get; set; } = "";
      public StaticPrefixBoundary Boundary { get; set; }
      public DateTime FirstSeenUtc { get; set; }
      public string SamplePrefix { get; set; } = "";
      public int CheckCount { get; set; }
    }
  }

  /// <summary>
  /// Represents a detected prefix stability violation.
  /// </summary>
  public class PrefixStabilityViolation
  {
    /// <summary>
    /// The key (e.g., NPC ID) where the violation occurred.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// The boundary configuration in use.
    /// </summary>
    public StaticPrefixBoundary Boundary { get; set; }

    /// <summary>
    /// Hash of the expected (baseline) prefix.
    /// </summary>
    public string ExpectedHash { get; set; } = "";

    /// <summary>
    /// Hash of the actual (changed) prefix.
    /// </summary>
    public string ActualHash { get; set; } = "";

    /// <summary>
    /// Sample of the expected prefix (truncated).
    /// </summary>
    public string ExpectedSample { get; set; } = "";

    /// <summary>
    /// Sample of the actual prefix (truncated).
    /// </summary>
    public string ActualSample { get; set; } = "";

    /// <summary>
    /// Which check number detected the violation.
    /// </summary>
    public int CheckNumber { get; set; }

    /// <summary>
    /// When the violation was detected.
    /// </summary>
    public DateTime DetectedAtUtc { get; set; }

    /// <summary>
    /// Returns a string representation of the violation.
    /// </summary>
    /// <returns>A formatted string describing the violation.</returns>
    public override string ToString()
    {
      return $"PrefixStabilityViolation[Key={Key}, Boundary={Boundary}, Check={CheckNumber}]: " +
             $"Prefix changed unexpectedly. Expected hash: {ExpectedHash.Substring(0, 8)}..., " +
             $"Actual hash: {ActualHash.Substring(0, 8)}...";
    }
  }

  /// <summary>
  /// Exception thrown when prefix stability validation fails and ThrowOnViolation is true.
  /// </summary>
  public class PrefixStabilityException : Exception
  {
    /// <summary>
    /// The violation that caused this exception.
    /// </summary>
    public PrefixStabilityViolation Violation { get; }

    /// <summary>
    /// Creates a new PrefixStabilityException with the specified violation.
    /// </summary>
    /// <param name="violation">The violation that caused this exception.</param>
    public PrefixStabilityException(PrefixStabilityViolation violation)
      : base(violation.ToString())
    {
      Violation = violation;
    }
  }
}
