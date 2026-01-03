using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LlamaBrain.Utilities
{

  /// <summary>
  /// Utility class for process operations
  /// </summary>
  public static class ProcessUtils
  {
    /// <summary>
    /// Maximum number of processes to return in search results
    /// </summary>
    private const int MaxProcessSearchResults = 100;

    /// <summary>
    /// Maximum process name length
    /// </summary>
    private const int MaxProcessNameLength = 260;

    /// <summary>
    /// Check if a process is running
    /// </summary>
    /// <param name="processName">The name of the process to check</param>
    /// <returns>True if the process is running, false otherwise</returns>
    public static bool IsProcessRunning(string processName)
    {
      if (string.IsNullOrWhiteSpace(processName))
        throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

      if (processName.Length > MaxProcessNameLength)
        throw new ArgumentException($"Process name too long: {processName.Length} characters (max: {MaxProcessNameLength})", nameof(processName));

      try
      {
        var processes = Process.GetProcessesByName(processName);
        return processes.Length > 0;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error checking if process '{processName}' is running: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Get all running processes with a specific name
    /// </summary>
    /// <param name="processName">The name of the process to find</param>
    /// <returns>List of running processes</returns>
    public static List<Process> GetRunningProcesses(string processName)
    {
      if (string.IsNullOrWhiteSpace(processName))
        throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

      if (processName.Length > MaxProcessNameLength)
        throw new ArgumentException($"Process name too long: {processName.Length} characters (max: {MaxProcessNameLength})", nameof(processName));

      try
      {
        var processes = Process.GetProcessesByName(processName);
        return processes.Take(MaxProcessSearchResults).ToList();
      }
      catch (Exception ex)
      {
        Logger.Error($"Error getting running processes for '{processName}': {ex.Message}");
        return new List<Process>();
      }
    }

    /// <summary>
    /// Kill a process by name
    /// </summary>
    /// <param name="processName">The name of the process to kill</param>
    /// <param name="forceKill">Whether to force kill the process</param>
    /// <returns>True if the process was killed, false otherwise</returns>
    public static bool KillProcess(string processName, bool forceKill = false)
    {
      if (string.IsNullOrWhiteSpace(processName))
        throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

      try
      {
        var processes = GetRunningProcesses(processName);
        if (processes.Count == 0)
        {
          Logger.Info($"No processes found with name '{processName}' to kill");
          return false;
        }

        var killedCount = 0;
        foreach (var process in processes)
        {
          try
          {
            if (forceKill)
            {
              process.Kill();
            }
            else
            {
              process.CloseMainWindow();
              if (!process.WaitForExit(5000)) // Wait 5 seconds
              {
                process.Kill();
              }
            }
            killedCount++;
          }
          catch (Exception ex)
          {
            Logger.Error($"Error killing process {process.Id} ({processName}): {ex.Message}");
          }
          finally
          {
            process.Dispose();
          }
        }

        Logger.Info($"Killed {killedCount} processes with name '{processName}'");
        return killedCount > 0;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error killing processes with name '{processName}': {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Get process information
    /// </summary>
    /// <param name="processName">The name of the process</param>
    /// <returns>List of process information</returns>
    public static List<ProcessInfo> GetProcessInfo(string processName)
    {
      if (string.IsNullOrWhiteSpace(processName))
        throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

      try
      {
        var processes = GetRunningProcesses(processName);
        var processInfos = new List<ProcessInfo>();

        foreach (var process in processes)
        {
          try
          {
            var info = new ProcessInfo
            {
              Id = process.Id,
              ProcessName = process.ProcessName,
              StartTime = process.StartTime,
              WorkingSet = process.WorkingSet64,
              VirtualMemorySize = process.VirtualMemorySize64,
              PrivateMemorySize = process.PrivateMemorySize64,
              ThreadCount = process.Threads.Count,
              Responding = process.Responding,
              HasExited = process.HasExited
            };

            processInfos.Add(info);
          }
          catch (Exception ex)
          {
            Logger.Error($"Error getting info for process {process.Id}: {ex.Message}");
          }
          finally
          {
            process.Dispose();
          }
        }

        return processInfos;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error getting process info for '{processName}': {ex.Message}");
        return new List<ProcessInfo>();
      }
    }

    /// <summary>
    /// Wait for a process to start
    /// </summary>
    /// <param name="processName">The name of the process to wait for</param>
    /// <param name="timeoutSeconds">Timeout in seconds</param>
    /// <returns>True if the process started within the timeout, false otherwise</returns>
    public static async Task<bool> WaitForProcessStartAsync(string processName, int timeoutSeconds = 30)
    {
      if (string.IsNullOrWhiteSpace(processName))
        throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

      if (timeoutSeconds <= 0)
        throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

      try
      {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        while (DateTime.UtcNow - startTime < timeout)
        {
          if (IsProcessRunning(processName))
          {
            Logger.Info($"Process '{processName}' started successfully");
            return true;
          }

          await Task.Delay(100); // Check every 100ms
        }

        Logger.Warn($"Timeout waiting for process '{processName}' to start after {timeoutSeconds} seconds");
        return false;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error waiting for process '{processName}' to start: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Wait for a process to exit
    /// </summary>
    /// <param name="processName">The name of the process to wait for</param>
    /// <param name="timeoutSeconds">Timeout in seconds</param>
    /// <returns>True if the process exited within the timeout, false otherwise</returns>
    public static async Task<bool> WaitForProcessExitAsync(string processName, int timeoutSeconds = 30)
    {
      if (string.IsNullOrWhiteSpace(processName))
        throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

      if (timeoutSeconds <= 0)
        throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

      try
      {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        while (DateTime.UtcNow - startTime < timeout)
        {
          if (!IsProcessRunning(processName))
          {
            Logger.Info($"Process '{processName}' exited successfully");
            return true;
          }

          await Task.Delay(100); // Check every 100ms
        }

        Logger.Warn($"Timeout waiting for process '{processName}' to exit after {timeoutSeconds} seconds");
        return false;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error waiting for process '{processName}' to exit: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Validate process configuration
    /// </summary>
    /// <param name="executablePath">Path to the executable</param>
    /// <param name="arguments">Process arguments</param>
    /// <returns>True if the configuration is valid, false otherwise</returns>
    public static bool ValidateProcessConfig(string executablePath, string? arguments = null)
    {
      if (string.IsNullOrWhiteSpace(executablePath))
        return false;

      try
      {
        // Check if executable exists
        if (!System.IO.File.Exists(executablePath))
          return false;

        // Check if executable is accessible
        var fileInfo = new System.IO.FileInfo(executablePath);
        if (!fileInfo.Exists || fileInfo.Length == 0)
          return false;

        // Validate arguments length if provided
        if (!string.IsNullOrWhiteSpace(arguments) && arguments.Length > 32768)
          return false;

        return true;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error validating process config: {ex.Message}");
        return false;
      }
    }
  }

  /// <summary>
  /// Information about a process
  /// </summary>
  public class ProcessInfo
  {
    /// <summary>
    /// Process ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Process name
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Process start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Working set size in bytes
    /// </summary>
    public long WorkingSet { get; set; }

    /// <summary>
    /// Virtual memory size in bytes
    /// </summary>
    public long VirtualMemorySize { get; set; }

    /// <summary>
    /// Private memory size in bytes
    /// </summary>
    public long PrivateMemorySize { get; set; }

    /// <summary>
    /// Number of threads
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// Whether the process is responding
    /// </summary>
    public bool Responding { get; set; }

    /// <summary>
    /// Whether the process has exited
    /// </summary>
    public bool HasExited { get; set; }
  }
}