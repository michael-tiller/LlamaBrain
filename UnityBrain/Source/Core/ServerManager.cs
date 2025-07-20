using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityBrain.Utilities;

namespace UnityBrain.Core
{
  /// <summary>
  /// Manager for the llama-server process
  /// </summary>
  public sealed class ServerManager : IDisposable
  {
    /// <summary>
    /// The configuration for the process
    /// </summary>
    private readonly ProcessConfig _config;
    /// <summary>
    /// The process instance
    /// </summary>
    private Process? _process;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="config">The configuration for the process</param>
    public ServerManager(ProcessConfig config)
    {
      _config = config;
    }

    /// <summary>
    /// Start the llama-server process
    /// </summary>
    public void StartServer()
    {
      if (_process != null && !_process.HasExited)
      {
        Logger.Info("[Server] llama-server is already running");
        return;
      }

      if (string.IsNullOrEmpty(_config.ExecutablePath))
      {
        throw new InvalidOperationException("Executable path is not configured");
      }

      var exePath = Path.GetFullPath(_config.ExecutablePath);
      if (!File.Exists(exePath))
      {
        throw new FileNotFoundException($"Executable not found: {exePath}");
      }

      var modelPath = Path.GetFullPath(_config.Model);
      if (!File.Exists(modelPath))
      {
        throw new FileNotFoundException($"Model file not found: {modelPath}");
      }

      var arguments = $"--port {_config.Port} -m \"{modelPath}\" --ctx-size {_config.ContextSize}";

      Logger.Info($"[Server] Starting llama-server: {exePath}");
      Logger.Info($"[Server] Arguments: {arguments}");

      _process = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = exePath,
          Arguments = arguments,
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          CreateNoWindow = true
        }
      };

      _process.OutputDataReceived += (sender, e) =>
      {
        if (!string.IsNullOrEmpty(e.Data))
          Logger.Info($"[llama-server] {e.Data}");
      };

      _process.ErrorDataReceived += (sender, e) =>
      {
        if (!string.IsNullOrEmpty(e.Data))
          Logger.Error($"[llama-server] {e.Data}");
      };

      _process.Start();
      _process.BeginOutputReadLine();
      _process.BeginErrorReadLine();

      Logger.Info($"[Server] llama-server started with PID: {_process.Id}");
    }

    /// <summary>
    /// Stop the llama-server process
    /// </summary>
    public void StopServer()
    {
      if (_process != null && !_process.HasExited)
      {
        Logger.Info("[Server] Stopping llama-server process");
        _process.Kill();
        _process.WaitForExit(5000);
        _process.Dispose();
        _process = null;
      }
    }

    /// <summary>
    /// Check if the server process is running
    /// </summary>
    /// <returns>True if the process is running, false otherwise</returns>
    public bool IsServerRunning()
    {
      return _process != null && !_process.HasExited;
    }

    /// <summary>
    /// Dispose of the server manager
    /// </summary>
    public void Dispose()
    {
      StopServer();
    }
  }
}