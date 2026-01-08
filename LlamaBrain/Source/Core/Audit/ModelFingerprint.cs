using System;
using System.Text;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Identifies a specific LLM model configuration for audit replay validation.
  /// Contains both model file identity and server configuration.
  /// </summary>
  /// <remarks>
  /// ModelFingerprint enables two levels of validation:
  /// 1. Exact match (Matches): Same FingerprintHash means identical model + config
  /// 2. Compatible (IsCompatibleWith): Same model file, may have different server config
  ///
  /// For accurate replay, exact match is preferred. Compatible match may produce
  /// slightly different outputs due to config differences (e.g., context length, batch size).
  /// </remarks>
  [Serializable]
  public sealed class ModelFingerprint
  {
    #region Model File Identity

    /// <summary>
    /// Full path to the model file.
    /// </summary>
    public string ModelPath { get; set; } = "";

    /// <summary>
    /// Model filename (without path).
    /// </summary>
    public string ModelFileName { get; set; } = "";

    /// <summary>
    /// Size of the model file in bytes.
    /// </summary>
    public long ModelFileSizeBytes { get; set; }

    /// <summary>
    /// First 8 characters of the SHA256 hash of the model file.
    /// Optional - computing full file hash is expensive.
    /// </summary>
    public string ModelFileHashPrefix { get; set; } = "";

    #endregion

    #region Server Configuration

    /// <summary>
    /// Model quantization type (e.g., "Q4_K_M", "Q5_K_S", "fp16").
    /// </summary>
    public string QuantizationType { get; set; } = "";

    /// <summary>
    /// Context window length in tokens.
    /// </summary>
    public int ContextLength { get; set; }

    /// <summary>
    /// Number of GPU layers.
    /// </summary>
    public int GpuLayers { get; set; }

    /// <summary>
    /// Batch size for inference.
    /// </summary>
    public int BatchSize { get; set; }

    #endregion

    #region Combined Fingerprint

    /// <summary>
    /// Combined hash of all fingerprint properties.
    /// Used for exact match validation.
    /// </summary>
    public string FingerprintHash { get; set; } = "";

    #endregion

    #region Matching Methods

    /// <summary>
    /// Checks if this fingerprint exactly matches another.
    /// Exact match means same model file AND same server configuration.
    /// </summary>
    /// <param name="other">The fingerprint to compare against.</param>
    /// <returns>True if fingerprints are identical.</returns>
    public bool Matches(ModelFingerprint other)
    {
      if (other == null) return false;
      return FingerprintHash == other.FingerprintHash;
    }

    /// <summary>
    /// Checks if this fingerprint is compatible with another.
    /// Compatible means same model file, but server config may differ.
    /// </summary>
    /// <param name="other">The fingerprint to compare against.</param>
    /// <returns>True if models are compatible (same file).</returns>
    public bool IsCompatibleWith(ModelFingerprint other)
    {
      if (other == null) return false;

      // Same model file means compatible, even if server config differs
      return ModelFileName == other.ModelFileName &&
             ModelFileSizeBytes == other.ModelFileSizeBytes;
    }

    #endregion

    #region Hash Computation

    /// <summary>
    /// Computes the combined fingerprint hash from all properties.
    /// Call this after setting all properties to generate the hash.
    /// </summary>
    public void ComputeFingerprintHash()
    {
      var sb = new StringBuilder();
      sb.Append($"ModelFileName:{ModelFileName}|");
      sb.Append($"ModelFileSizeBytes:{ModelFileSizeBytes}|");
      sb.Append($"ModelFileHashPrefix:{ModelFileHashPrefix}|");
      sb.Append($"QuantizationType:{QuantizationType}|");
      sb.Append($"ContextLength:{ContextLength}|");
      sb.Append($"GpuLayers:{GpuLayers}|");
      sb.Append($"BatchSize:{BatchSize}");

      FingerprintHash = AuditHasher.ComputeSha256Prefix(sb.ToString(), 16);
    }

    #endregion
  }
}
