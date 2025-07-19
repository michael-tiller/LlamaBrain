using System.Diagnostics;

/// <summary>
/// This namespace contains utility classes for process operations
/// </summary>
namespace UnityBrain.Utilities
{

  /// <summary>
  /// Utility class for process operations
  /// </summary>
  public static class ProcessUtils
  {
    /// <summary>
    /// Check if a process is running
    /// </summary>
    public static bool IsProcessRunning(string processName)
    {
      var ps = Process.GetProcessesByName(processName);
      return ps.Length > 0;
    }
  }
}