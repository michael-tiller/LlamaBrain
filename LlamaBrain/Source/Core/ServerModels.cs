using System;
using System.Collections.Generic;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Server status information
  /// </summary>
  public class ServerStatus
  {
    /// <summary>
    /// Whether the server is running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Process ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Server start time
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public float CpuUsage { get; set; }

    /// <summary>
    /// Number of threads
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// Whether the server is responding
    /// </summary>
    public bool Responding { get; set; }

    /// <summary>
    /// Server uptime
    /// </summary>
    public TimeSpan Uptime { get; set; }
  }

  /// <summary>
  /// Server validation result
  /// </summary>
  public class ServerValidationResult
  {
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();
  }
}