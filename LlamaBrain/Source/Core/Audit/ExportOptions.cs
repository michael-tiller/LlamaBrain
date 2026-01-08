namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Options for configuring debug package export.
  /// </summary>
  public sealed class ExportOptions
  {
    /// <summary>
    /// Version of the game when records were captured.
    /// </summary>
    public string GameVersion { get; set; } = "";

    /// <summary>
    /// Name of the scene where most interactions occurred.
    /// </summary>
    public string SceneName { get; set; } = "";

    /// <summary>
    /// Optional notes from the creator (e.g., bug description).
    /// </summary>
    public string CreatorNotes { get; set; } = "";

    /// <summary>
    /// Model fingerprint for replay validation.
    /// </summary>
    public ModelFingerprint ModelFingerprint { get; set; } = new ModelFingerprint();

    /// <summary>
    /// Whether to use GZip compression when exporting to bytes.
    /// Compression typically reduces package size by 70-90%.
    /// Default: false (for compatibility with JSON text export).
    /// </summary>
    public bool UseCompression { get; set; } = false;

    /// <summary>
    /// GZip compression level (1-9).
    /// 1 = fastest, 9 = best compression.
    /// Default: 6 (balanced).
    /// </summary>
    public int CompressionLevel { get; set; } = 6;
  }
}
