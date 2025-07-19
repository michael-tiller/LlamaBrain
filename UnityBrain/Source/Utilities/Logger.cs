using System;

namespace UnityBrain.Utilities
{
  /// <summary>
  /// Logger class
  /// </summary>
  public static class Logger
  {
    /// <summary>
    /// Log an info message
    /// </summary>
    /// <param name="msg">The message to log</param>
    public static void Info(string msg) => Console.WriteLine($"[INFO] {msg}");
    /// <summary>
    /// Log a warning message
    /// </summary>
    /// <param name="msg">The message to log</param>
    public static void Warn(string msg) => Console.WriteLine($"[WARN] {msg}");
    /// <summary>
    /// Log an error message
    /// </summary>
    /// <param name="msg">The message to log</param>
    public static void Error(string msg) => Console.WriteLine($"[ERROR] {msg}");
  }
}