using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// A debug package containing audit records for replay and bug reproduction.
  /// This is the exportable format for sharing bug reports.
  /// </summary>
  /// <remarks>
  /// DebugPackage captures:
  /// - Model fingerprint for validation during replay
  /// - All audit records from the ring buffer
  /// - Statistics about validation failures and fallbacks
  /// - Integrity hash to detect tampering
  ///
  /// Export format is JSON, which can be imported into RedRoom for replay.
  /// </remarks>
  [Serializable]
  public sealed class DebugPackage
  {
    /// <summary>
    /// Current version of the DebugPackage schema.
    /// Increment when making breaking changes to the package structure.
    /// </summary>
    public const int CurrentVersion = 1;

    #region Package Metadata

    /// <summary>
    /// Schema version of this package.
    /// </summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// Unique identifier for this package.
    /// </summary>
    public string PackageId { get; set; } = "";

    /// <summary>
    /// UTC ticks when this package was created.
    /// </summary>
    public long CreatedAtUtcTicks { get; set; }

    /// <summary>
    /// Optional notes from the creator (e.g., bug description).
    /// </summary>
    public string CreatorNotes { get; set; } = "";

    #endregion

    #region Model Identity

    /// <summary>
    /// Fingerprint of the model used when these records were captured.
    /// Used to validate model compatibility during replay.
    /// </summary>
    public ModelFingerprint ModelFingerprint { get; set; } = new ModelFingerprint();

    #endregion

    #region Game Context

    /// <summary>
    /// Version of the game when records were captured.
    /// </summary>
    public string GameVersion { get; set; } = "";

    /// <summary>
    /// Name of the scene where most interactions occurred.
    /// </summary>
    public string SceneName { get; set; } = "";

    /// <summary>
    /// List of NPC IDs involved in this package.
    /// </summary>
    public List<string> NpcIds { get; set; } = new List<string>();

    #endregion

    #region Audit Records

    /// <summary>
    /// Audit records ordered by capture time.
    /// </summary>
    public List<AuditRecord> Records { get; set; } = new List<AuditRecord>();

    #endregion

    #region Statistics

    /// <summary>
    /// Total number of interactions in this package.
    /// </summary>
    public int TotalInteractions { get; set; }

    /// <summary>
    /// Number of interactions that failed validation.
    /// </summary>
    public int ValidationFailures { get; set; }

    /// <summary>
    /// Number of interactions where fallback was used.
    /// </summary>
    public int FallbacksUsed { get; set; }

    #endregion

    #region Integrity

    /// <summary>
    /// SHA256 hash of package content for integrity verification.
    /// </summary>
    public string PackageIntegrityHash { get; set; } = "";

    #endregion

    #region Methods

    /// <summary>
    /// Computes the integrity hash string from package content without mutating state.
    /// </summary>
    /// <returns>The computed SHA256 hash string.</returns>
    private string GetIntegrityHashString()
    {
      var sb = new StringBuilder();

      // Include identifying info
      sb.AppendLine($"Version:{Version}");
      sb.AppendLine($"PackageId:{PackageId}");
      sb.AppendLine($"CreatedAtUtcTicks:{CreatedAtUtcTicks}");
      sb.AppendLine($"GameVersion:{GameVersion}");
      sb.AppendLine($"ModelFingerprint:{ModelFingerprint.FingerprintHash}");

      // Include NPC IDs
      foreach (var npcId in NpcIds.OrderBy(id => id, StringComparer.Ordinal))
      {
        sb.AppendLine($"NpcId:{npcId}");
      }

      // Include record hashes (not full records, for performance)
      foreach (var record in Records.OrderBy(r => r.RecordId, StringComparer.Ordinal))
      {
        sb.AppendLine($"Record:{record.RecordId}|{record.OutputHash}");
      }

      return AuditHasher.ComputeSha256(sb.ToString());
    }

    /// <summary>
    /// Computes the integrity hash from package content.
    /// Call this after setting all properties and before export.
    /// </summary>
    public void ComputeIntegrityHash()
    {
      PackageIntegrityHash = GetIntegrityHashString();
    }

    /// <summary>
    /// Validates the package integrity by recomputing and comparing the hash.
    /// </summary>
    /// <returns>True if the package has not been tampered with.</returns>
    public bool ValidateIntegrity()
    {
      if (string.IsNullOrEmpty(PackageIntegrityHash))
        return false;

      var computedHash = GetIntegrityHashString();
      return PackageIntegrityHash == computedHash;
    }

    /// <summary>
    /// Updates the statistics from the current records.
    /// Call this after adding records and before export.
    /// </summary>
    public void UpdateStatistics()
    {
      TotalInteractions = Records.Count;
      ValidationFailures = Records.Count(r => !r.ValidationPassed);
      FallbacksUsed = Records.Count(r => r.FallbackUsed);
    }

    #endregion
  }
}
