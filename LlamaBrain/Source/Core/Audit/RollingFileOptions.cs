using System;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Configuration options for rolling file audit persistence.
  /// </summary>
  public sealed class RollingFileOptions
  {
    /// <summary>
    /// Default maximum file size before rotation (10 MB).
    /// </summary>
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Default maximum number of files to retain.
    /// </summary>
    public const int DefaultMaxFileCount = 10;

    /// <summary>
    /// Default file name prefix.
    /// </summary>
    public const string DefaultFilePrefix = "audit";

    /// <summary>
    /// Maximum file size in bytes before rotating to a new file.
    /// Default is 10 MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;

    /// <summary>
    /// Maximum number of log files to retain.
    /// Oldest files are deleted when this limit is exceeded.
    /// Default is 10.
    /// </summary>
    public int MaxFileCount { get; set; } = DefaultMaxFileCount;

    /// <summary>
    /// Prefix for log file names.
    /// Files are named: {FilePrefix}-{timestamp}-{sequence}.jsonl
    /// Default is "audit".
    /// </summary>
    public string FilePrefix { get; set; } = DefaultFilePrefix;

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when values are out of valid range</exception>
    /// <exception cref="ArgumentNullException">Thrown when FilePrefix is null</exception>
    public void Validate()
    {
      if (MaxFileSizeBytes < 1024)
        throw new ArgumentOutOfRangeException(nameof(MaxFileSizeBytes), MaxFileSizeBytes,
          "MaxFileSizeBytes must be at least 1024 bytes.");

      if (MaxFileCount < 1)
        throw new ArgumentOutOfRangeException(nameof(MaxFileCount), MaxFileCount,
          "MaxFileCount must be at least 1.");

      if (FilePrefix == null)
        throw new ArgumentNullException(nameof(FilePrefix));

      if (string.IsNullOrWhiteSpace(FilePrefix))
        throw new ArgumentException("FilePrefix cannot be empty or whitespace.", nameof(FilePrefix));
    }
  }
}
