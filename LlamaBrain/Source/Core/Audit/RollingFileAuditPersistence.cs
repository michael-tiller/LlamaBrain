using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Persona;
using LlamaBrain.Utilities;
using Newtonsoft.Json;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Persists audit records to rolling JSON Lines files.
  /// Each record is written as a single JSON line, with files rotating
  /// based on size and automatic deletion of oldest files.
  /// </summary>
  /// <remarks>
  /// Thread-safety: This implementation is NOT thread-safe.
  /// External synchronization is required for concurrent access.
  ///
  /// File format: JSON Lines (.jsonl) - one JSON object per line.
  /// File naming: {prefix}-{yyyyMMdd-HHmmss}-{seq:000}.jsonl
  /// </remarks>
  public sealed class RollingFileAuditPersistence : IAuditPersistence
  {
    private const string FileExtension = ".jsonl";
    private const string TimestampFormat = "yyyyMMdd-HHmmss";

    private readonly IFileSystem _fileSystem;
    private readonly IClock _clock;
    private readonly string _logDirectory;
    private readonly RollingFileOptions _options;
    private readonly JsonSerializerSettings _jsonSettings;

    private string? _currentFilePath;
    private long _currentFileSize;
    private int _currentSequence;
    private List<string>? _knownFiles;

    /// <summary>
    /// Creates a new rolling file audit persistence instance.
    /// </summary>
    /// <param name="fileSystem">File system abstraction for I/O operations</param>
    /// <param name="clock">Clock for timestamp generation</param>
    /// <param name="logDirectory">Directory where log files will be stored</param>
    /// <param name="options">Rolling file options (optional, uses defaults if null)</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    public RollingFileAuditPersistence(
      IFileSystem fileSystem,
      IClock clock,
      string logDirectory,
      RollingFileOptions? options = null)
    {
      _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
      _clock = clock ?? throw new ArgumentNullException(nameof(clock));

      if (string.IsNullOrWhiteSpace(logDirectory))
        throw new ArgumentException("Log directory cannot be null or empty.", nameof(logDirectory));

      _logDirectory = logDirectory;
      _options = options ?? new RollingFileOptions();
      _options.Validate();

      _jsonSettings = new JsonSerializerSettings
      {
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.None
      };
    }

    /// <summary>
    /// Gets the directory where log files are stored.
    /// </summary>
    public string LogDirectory => _logDirectory;

    /// <summary>
    /// Gets the current active log file path, or null if no file has been created yet.
    /// </summary>
    public string? CurrentFilePath => _currentFilePath;

    /// <inheritdoc/>
    public void Persist(AuditRecord record)
    {
      if (record == null)
        throw new ArgumentNullException(nameof(record));

      EnsureDirectoryExists();
      EnsureCurrentFile();

      var json = JsonConvert.SerializeObject(record, _jsonSettings);
      var line = json + Environment.NewLine;
      var lineBytes = System.Text.Encoding.UTF8.GetByteCount(line);

      // Check if we need to rotate before writing
      if (_currentFileSize + lineBytes > _options.MaxFileSizeBytes && _currentFileSize > 0)
      {
        RotateFile();
      }

      _fileSystem.AppendAllText(_currentFilePath!, line);
      _currentFileSize += lineBytes;
    }

    /// <inheritdoc/>
    public void Flush()
    {
      // AppendAllText flushes on each call, so nothing to do here.
      // This method exists for implementations that buffer writes.
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetLogFiles()
    {
      RefreshKnownFiles();
      return _knownFiles!.AsReadOnly();
    }

    /// <inheritdoc/>
    public long GetTotalSizeBytes()
    {
      RefreshKnownFiles();
      long total = 0;

      foreach (var file in _knownFiles!)
      {
        if (_fileSystem.FileExists(file))
        {
          total += _fileSystem.GetFileInfo(file).Length;
        }
      }

      return total;
    }

    /// <summary>
    /// Ensures the log directory exists.
    /// </summary>
    private void EnsureDirectoryExists()
    {
      _fileSystem.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Ensures there is a current file to write to.
    /// </summary>
    private void EnsureCurrentFile()
    {
      if (_currentFilePath != null && _fileSystem.FileExists(_currentFilePath))
        return;

      RefreshKnownFiles();

      // Try to continue using the most recent file if it exists and has room
      if (_knownFiles!.Count > 0)
      {
        var lastFile = _knownFiles[_knownFiles.Count - 1];
        if (_fileSystem.FileExists(lastFile))
        {
          var size = _fileSystem.GetFileInfo(lastFile).Length;
          if (size < _options.MaxFileSizeBytes)
          {
            _currentFilePath = lastFile;
            _currentFileSize = size;
            _currentSequence = ExtractSequence(lastFile);
            return;
          }
        }
      }

      // Create a new file and enforce retention
      CreateNewFile();
      EnforceRetentionPolicy();
    }

    /// <summary>
    /// Creates a new log file with incremented sequence number.
    /// </summary>
    private void CreateNewFile()
    {
      _currentSequence++;
      var timestamp = new DateTime(_clock.UtcNowTicks, DateTimeKind.Utc).ToString(TimestampFormat);
      var fileName = $"{_options.FilePrefix}-{timestamp}-{_currentSequence:D3}{FileExtension}";
      _currentFilePath = _fileSystem.CombinePath(_logDirectory, fileName);
      _currentFileSize = 0;

      // Touch the file to ensure it exists
      _fileSystem.WriteAllText(_currentFilePath, "");

      // Update known files
      if (_knownFiles != null && !_knownFiles.Contains(_currentFilePath))
      {
        _knownFiles.Add(_currentFilePath);
      }
    }

    /// <summary>
    /// Rotates to a new file and enforces retention policy.
    /// </summary>
    private void RotateFile()
    {
      CreateNewFile();
      EnforceRetentionPolicy();
    }

    /// <summary>
    /// Deletes oldest files if count exceeds maximum.
    /// </summary>
    private void EnforceRetentionPolicy()
    {
      RefreshKnownFiles();

      while (_knownFiles!.Count > _options.MaxFileCount)
      {
        var oldest = _knownFiles[0];
        if (_fileSystem.FileExists(oldest))
        {
          _fileSystem.DeleteFile(oldest);
        }
        _knownFiles.RemoveAt(0);
      }
    }

    /// <summary>
    /// Refreshes the list of known log files from disk.
    /// </summary>
    private void RefreshKnownFiles()
    {
      if (_knownFiles == null)
      {
        _knownFiles = new List<string>();
      }

      try
      {
        var pattern = $"{_options.FilePrefix}-*{FileExtension}";
        var files = _fileSystem.GetFiles(_logDirectory, pattern);

        // Sort by filename (which includes timestamp and sequence)
        // This gives deterministic chronological ordering
        _knownFiles = files.OrderBy(f => f, StringComparer.Ordinal).ToList();

        // Update current sequence based on existing files
        if (_knownFiles.Count > 0)
        {
          var maxSeq = _knownFiles.Max(f => ExtractSequence(f));
          if (maxSeq >= _currentSequence)
          {
            _currentSequence = maxSeq;
          }
        }
      }
      catch
      {
        // Directory might not exist yet
        _knownFiles = new List<string>();
      }
    }

    /// <summary>
    /// Extracts the sequence number from a file name.
    /// </summary>
    private int ExtractSequence(string filePath)
    {
      try
      {
        var fileName = _fileSystem.GetFileNameWithoutExtension(filePath);
        // Format: {prefix}-{timestamp}-{sequence}
        var lastDash = fileName.LastIndexOf('-');
        if (lastDash > 0 && lastDash < fileName.Length - 1)
        {
          var seqStr = fileName.Substring(lastDash + 1);
          if (int.TryParse(seqStr, out var seq))
          {
            return seq;
          }
        }
      }
      catch
      {
        // Ignore parsing errors
      }

      return 0;
    }
  }
}
