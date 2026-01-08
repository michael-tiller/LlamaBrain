using System.Collections.Generic;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Result of importing a debug package from JSON.
  /// </summary>
  public sealed class ImportResult
  {
    /// <summary>
    /// Whether the import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The imported package if successful, null otherwise.
    /// </summary>
    public DebugPackage? Package { get; set; }

    /// <summary>
    /// Error message if import failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the package integrity hash was valid.
    /// Null if integrity validation was not performed.
    /// </summary>
    public bool? IntegrityValid { get; set; }

    /// <summary>
    /// Whether the package version is supported.
    /// </summary>
    public bool VersionSupported { get; set; } = true;

    /// <summary>
    /// Warning messages (e.g., version mismatch).
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();

    /// <summary>
    /// Whether the package was compressed.
    /// </summary>
    public bool WasCompressed { get; set; }

    /// <summary>
    /// Compressed size in bytes (if WasCompressed is true).
    /// </summary>
    public long CompressedSize { get; set; }

    /// <summary>
    /// Uncompressed size in bytes (if WasCompressed is true).
    /// </summary>
    public long UncompressedSize { get; set; }

    /// <summary>
    /// Compression ratio (UncompressedSize / CompressedSize) if compressed.
    /// Higher values indicate better compression.
    /// </summary>
    public double CompressionRatio => WasCompressed && CompressedSize > 0
      ? (double)UncompressedSize / CompressedSize
      : 1.0;

    /// <summary>
    /// Creates a successful import result.
    /// </summary>
    public static ImportResult Succeeded(DebugPackage package)
    {
      return new ImportResult
      {
        Success = true,
        Package = package
      };
    }

    /// <summary>
    /// Creates a failed import result.
    /// </summary>
    public static ImportResult Failed(string errorMessage)
    {
      return new ImportResult
      {
        Success = false,
        ErrorMessage = errorMessage
      };
    }
  }

  /// <summary>
  /// Result of validating a model fingerprint against the current model.
  /// </summary>
  public sealed class ModelFingerprintValidationResult
  {
    /// <summary>
    /// Whether the fingerprints are an exact match (same model + config).
    /// </summary>
    public bool IsExactMatch { get; set; }

    /// <summary>
    /// Whether the models are compatible (same model file, may have different config).
    /// </summary>
    public bool IsCompatible { get; set; }

    /// <summary>
    /// The fingerprint from the debug package.
    /// </summary>
    public ModelFingerprint? PackageFingerprint { get; set; }

    /// <summary>
    /// The current model fingerprint.
    /// </summary>
    public ModelFingerprint? CurrentFingerprint { get; set; }

    /// <summary>
    /// Description of the mismatch, if any.
    /// </summary>
    public string? MismatchDescription { get; set; }
  }
}
