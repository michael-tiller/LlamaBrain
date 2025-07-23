using System;
using System.Diagnostics;
using System.IO;
using LlamaBrain.Utilities;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LlamaBrain.Core
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
            WorkingDirectory = Path.GetDirectoryName(exePath)
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
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        Logger.Info($"[Server] llama-server started with PID: {_process.Id}");
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
        throw new ObjectDisposedException(nameof(ServerManager));

      if (_process == null || _process.HasExited)
      {
        Logger.Info("[Server] llama-server is not running");
        return;
      }

      try
      {
        Logger.Info($"[Server] Stopping llama-server (PID: {_process.Id})");

        // Try graceful shutdown first
        if (!_process.CloseMainWindow())
        {
          // Force kill if graceful shutdown fails
          _process.Kill();
        }

        // Wait for the process to exit
        if (!_process.WaitForExit(MaxShutdownTimeoutSeconds * 1000))
        {
          Logger.Warn("[Server] Process did not exit within timeout, force killing");
          _process.Kill();
          _process.WaitForExit(5000); // Wait a bit more
        }

        Logger.Info("[Server] llama-server stopped successfully");
      }
      catch (Exception ex)
      {
        Logger.Error($"[Server] Error stopping llama-server: {ex.Message}");
      }
      finally
      {
        CleanupProcess();
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
    /// Get detailed server status information
    /// </summary>
    /// <returns>Server status information</returns>
    public ServerStatus GetServerStatus()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(ServerManager));

      var status = new ServerStatus
      {
        IsRunning = false,
        ProcessId = -1,
        StartTime = null,
        MemoryUsage = 0,
        CpuUsage = 0,
        ThreadCount = 0,
        Responding = false,
        Uptime = TimeSpan.Zero
      };

      if (_process == null)
        return status;

      try
      {
        status.IsRunning = !_process.HasExited;
        if (status.IsRunning)
        {
          status.ProcessId = _process.Id;
          status.StartTime = _process.StartTime;
          status.MemoryUsage = _process.WorkingSet64;
          status.ThreadCount = _process.Threads.Count;
          status.Responding = _process.Responding;

          if (status.StartTime.HasValue)
          {
            status.Uptime = DateTime.Now - status.StartTime.Value;
          }

          // Try to get CPU usage (this might not work on all systems)
          try
          {
            // PerformanceCounter is not available in .NET Standard 2.1
            // CPU usage will remain 0 for compatibility
          }
          catch
          {
            // CPU usage not available, leave as 0
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn($"[Server] Error getting server status: {ex.Message}");
      }

      return status;
    }

    /// <summary>
    /// Wait for the server to be ready
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds</param>
    /// <returns>True if the server is ready, false otherwise</returns>
    public async Task<bool> WaitForServerReadyAsync(int timeoutSeconds = 30)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(ServerManager));

      if (timeoutSeconds <= 0)
        throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

      try
      {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        while (DateTime.UtcNow - startTime < timeout)
        {
          if (IsServerRunning())
          {
            // Additional check: try to connect to the server
            try
            {
              using var client = new System.Net.Http.HttpClient();
              client.Timeout = TimeSpan.FromSeconds(5);
              var response = await client.GetAsync($"http://localhost:{_config.Port}/health");
              if (response.IsSuccessStatusCode)
              {
                Logger.Info($"[Server] Server is ready and responding on port {_config.Port}");
                return true;
              }
            }
            catch
            {
              // Server might not be ready yet, continue waiting
            }

            await Task.Delay(1000); // Check every second
          }
          else
          {
            await Task.Delay(100); // Check more frequently if not running
          }
        }

        Logger.Warn($"[Server] Timeout waiting for server to be ready after {timeoutSeconds} seconds");
        return false;
      }
      catch (Exception ex)
      {
        Logger.Error($"[Server] Error waiting for server to be ready: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Validate server configuration with enhanced checks
    /// </summary>
    /// <returns>Validation result</returns>
    public ServerValidationResult ValidateServerConfiguration()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(ServerManager));

      var result = new ServerValidationResult
      {
        IsValid = true,
        Errors = new List<string>(),
        Warnings = new List<string>()
      };

      try
      {
        // Validate executable path
        if (!ProcessUtils.ValidateProcessConfig(_config.ExecutablePath))
        {
          result.IsValid = false;
          result.Errors.Add("Executable path is invalid or file does not exist");
        }

        // Validate model path
        if (string.IsNullOrWhiteSpace(_config.Model) || !File.Exists(_config.Model))
        {
          result.IsValid = false;
          result.Errors.Add("Model path is invalid or file does not exist");
        }

        // Validate port
        if (_config.Port <= 0 || _config.Port > 65535)
        {
          result.IsValid = false;
          result.Errors.Add($"Invalid port number: {_config.Port}. Must be between 1 and 65535.");
        }

        // Check if port is already in use
        try
        {
          var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, _config.Port);
          listener.Start();
          listener.Stop();
        }
        catch
        {
          result.Warnings.Add($"Port {_config.Port} might already be in use");
        }

        // Validate context size
        if (_config.ContextSize <= 0 || _config.ContextSize > 32768)
        {
          result.IsValid = false;
          result.Errors.Add($"Invalid context size: {_config.ContextSize}. Must be between 1 and 32768.");
        }

        // Check available disk space for model
        if (!string.IsNullOrWhiteSpace(_config.Model) && File.Exists(_config.Model))
        {
          var driveInfo = new DriveInfo(Path.GetPathRoot(_config.Model) ?? "");
          var availableSpace = driveInfo.AvailableFreeSpace;
          var modelSize = new FileInfo(_config.Model).Length;

          if (availableSpace < modelSize * 2) // Need at least 2x model size for safety
          {
            result.Warnings.Add($"Low disk space. Available: {availableSpace / (1024 * 1024)}MB, Model size: {modelSize / (1024 * 1024)}MB");
          }
        }
      }
      catch (Exception ex)
      {
        result.IsValid = false;
        result.Errors.Add($"Validation error: {ex.Message}");
      }

      return result;
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