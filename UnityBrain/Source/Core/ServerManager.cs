using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityBrain.Utilities;
using System.Linq; // Added for .Contains()

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
    /// Whether the manager has been disposed
    /// </summary>
    private bool _disposed = false;
    /// <summary>
    /// Maximum process startup timeout in seconds
    /// </summary>
    private const int MaxStartupTimeoutSeconds = 30;
    /// <summary>
    /// Maximum process shutdown timeout in seconds
    /// </summary>
    private const int MaxShutdownTimeoutSeconds = 10;
    /// <summary>
    /// Maximum executable path length
    /// </summary>
    private const int MaxExecutablePathLength = 260;
    /// <summary>
    /// Maximum model path length
    /// </summary>
    private const int MaxModelPathLength = 260;
    /// <summary>
    /// Maximum arguments length
    /// </summary>
    private const int MaxArgumentsLength = 1000;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="config">The configuration for the process</param>
    public ServerManager(ProcessConfig config)
    {
      _config = config ?? throw new ArgumentNullException(nameof(config));
      ValidateConfiguration();
    }

    /// <summary>
    /// Start the llama-server process
    /// </summary>
    public void StartServer()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(ServerManager));

      if (_process != null && !_process.HasExited)
      {
        Logger.Info("[Server] llama-server is already running");
        return;
      }

      try
      {
        // Validate executable path
        if (string.IsNullOrWhiteSpace(_config.ExecutablePath))
        {
          throw new InvalidOperationException("Executable path is not configured");
        }

        var exePath = ValidateAndResolveExecutablePath(_config.ExecutablePath);
        if (!File.Exists(exePath))
        {
          throw new FileNotFoundException($"Executable not found: {exePath}");
        }

        // Validate model path
        if (string.IsNullOrWhiteSpace(_config.Model))
        {
          throw new InvalidOperationException("Model path is not configured");
        }

        var modelPath = ValidateAndResolveModelPath(_config.Model);
        if (!File.Exists(modelPath))
        {
          throw new FileNotFoundException($"Model file not found: {modelPath}");
        }

        // Validate and sanitize arguments
        var arguments = BuildAndValidateArguments(modelPath);

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
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(exePath) // Set working directory to executable location
          }
        };

        // Set up output handlers
        _process.OutputDataReceived += (sender, e) =>
        {
          if (!string.IsNullOrEmpty(e.Data))
          {
            var sanitizedOutput = SanitizeProcessOutput(e.Data);
            Logger.Info($"[llama-server] {sanitizedOutput}");
          }
        };

        _process.ErrorDataReceived += (sender, e) =>
        {
          if (!string.IsNullOrEmpty(e.Data))
          {
            var sanitizedError = SanitizeProcessOutput(e.Data);
            Logger.Error($"[llama-server] {sanitizedError}");
          }
        };

        // Start the process
        if (!_process.Start())
        {
          throw new InvalidOperationException("Failed to start llama-server process");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Wait a bit to see if the process starts successfully
        if (!_process.WaitForExit(1000) && !_process.HasExited)
        {
          Logger.Info($"[Server] llama-server started with PID: {_process.Id}");
        }
        else
        {
          var exitCode = _process.ExitCode;
          throw new InvalidOperationException($"llama-server process exited immediately with code: {exitCode}");
        }
      }
      catch (Exception ex)
      {
        Logger.Error($"[Server] Failed to start llama-server: {ex.Message}");
        CleanupProcess();
        throw;
      }
    }

    /// <summary>
    /// Stop the llama-server process
    /// </summary>
    public void StopServer()
    {
      if (_disposed)
        return;

      if (_process != null && !_process.HasExited)
      {
        try
        {
          Logger.Info("[Server] Stopping llama-server process");

          // Try graceful shutdown first
          _process.CloseMainWindow();

          if (!_process.WaitForExit(MaxShutdownTimeoutSeconds * 1000))
          {
            Logger.Warn("[Server] Graceful shutdown failed, forcing process termination");
            _process.Kill();
            _process.WaitForExit(MaxShutdownTimeoutSeconds * 1000);
          }

          Logger.Info("[Server] llama-server process stopped");
        }
        catch (Exception ex)
        {
          Logger.Error($"[Server] Error stopping llama-server process: {ex.Message}");
        }
        finally
        {
          CleanupProcess();
        }
      }
    }

    /// <summary>
    /// Check if the server process is running
    /// </summary>
    /// <returns>True if the process is running, false otherwise</returns>
    public bool IsServerRunning()
    {
      if (_disposed || _process == null)
        return false;

      try
      {
        return !_process.HasExited;
      }
      catch (Exception ex)
      {
        Logger.Warn($"[Server] Error checking process status: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Dispose of the server manager
    /// </summary>
    public void Dispose()
    {
      if (!_disposed)
      {
        StopServer();
        _disposed = true;
      }
    }

    /// <summary>
    /// Validate the configuration
    /// </summary>
    private void ValidateConfiguration()
    {
      if (_config.Port <= 0 || _config.Port > 65535)
        throw new ArgumentException($"Invalid port number: {_config.Port}. Must be between 1 and 65535.");

      if (_config.ContextSize <= 0 || _config.ContextSize > 32768)
        throw new ArgumentException($"Invalid context size: {_config.ContextSize}. Must be between 1 and 32768.");

      if (string.IsNullOrWhiteSpace(_config.ExecutablePath))
        throw new ArgumentException("Executable path cannot be null or empty.");

      if (string.IsNullOrWhiteSpace(_config.Model))
        throw new ArgumentException("Model path cannot be null or empty.");
    }

    /// <summary>
    /// Validate and resolve executable path
    /// </summary>
    private string ValidateAndResolveExecutablePath(string executablePath)
    {
      if (string.IsNullOrWhiteSpace(executablePath))
        throw new ArgumentException("Executable path cannot be null or empty", nameof(executablePath));

      if (executablePath.Length > MaxExecutablePathLength)
        throw new ArgumentException($"Executable path too long: {executablePath.Length} characters (max: {MaxExecutablePathLength})");

      // Check for path traversal attempts
      if (executablePath.Contains("..") || executablePath.Contains("//"))
        throw new ArgumentException("Executable path contains invalid characters");

      var fullPath = Path.GetFullPath(executablePath);

      // Validate file extension
      var extension = Path.GetExtension(fullPath).ToLowerInvariant();
      var allowedExtensions = new[] { ".exe", ".bat", ".cmd" };
      if (!allowedExtensions.Contains(extension))
        throw new ArgumentException($"Invalid executable extension: {extension}. Allowed: {string.Join(", ", allowedExtensions)}");

      return fullPath;
    }

    /// <summary>
    /// Validate and resolve model path
    /// </summary>
    private string ValidateAndResolveModelPath(string modelPath)
    {
      if (string.IsNullOrWhiteSpace(modelPath))
        throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));

      if (modelPath.Length > MaxModelPathLength)
        throw new ArgumentException($"Model path too long: {modelPath.Length} characters (max: {MaxModelPathLength})");

      // Check for path traversal attempts
      if (modelPath.Contains("..") || modelPath.Contains("//"))
        throw new ArgumentException("Model path contains invalid characters");

      var fullPath = Path.GetFullPath(modelPath);

      // Validate file extension
      var extension = Path.GetExtension(fullPath).ToLowerInvariant();
      var allowedExtensions = new[] { ".gguf", ".bin", ".model" };
      if (!allowedExtensions.Contains(extension))
        throw new ArgumentException($"Invalid model file extension: {extension}. Allowed: {string.Join(", ", allowedExtensions)}");

      return fullPath;
    }

    /// <summary>
    /// Build and validate process arguments
    /// </summary>
    private string BuildAndValidateArguments(string modelPath)
    {
      var arguments = $"--port {_config.Port} -m \"{modelPath}\" --ctx-size {_config.ContextSize}";

      if (arguments.Length > MaxArgumentsLength)
        throw new ArgumentException($"Arguments too long: {arguments.Length} characters (max: {MaxArgumentsLength})");

      // Validate that arguments don't contain dangerous characters
      var dangerousChars = new[] { '&', '|', ';', '`', '$', '(', ')', '{', '}', '[', ']', '<', '>' };
      if (dangerousChars.Any(c => arguments.Contains(c)))
        throw new ArgumentException("Arguments contain dangerous characters");

      return arguments;
    }

    /// <summary>
    /// Sanitize process output
    /// </summary>
    private string SanitizeProcessOutput(string output)
    {
      if (string.IsNullOrEmpty(output))
        return string.Empty;

      // Remove control characters and limit length
      var sanitized = new string(output.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());

      if (sanitized.Length > 1000)
        sanitized = sanitized.Substring(0, 1000) + "... [truncated]";

      return sanitized;
    }

    /// <summary>
    /// Clean up the process
    /// </summary>
    private void CleanupProcess()
    {
      try
      {
        _process?.Dispose();
        _process = null;
      }
      catch (Exception ex)
      {
        Logger.Warn($"[Server] Error disposing process: {ex.Message}");
      }
    }
  }
}