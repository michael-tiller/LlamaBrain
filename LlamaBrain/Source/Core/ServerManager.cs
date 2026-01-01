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
    /// The last arguments used to start the server
    /// </summary>
    private string _lastArguments = "";
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
    /// Gets the last arguments used to start the server
    /// </summary>
    public string LastStartupArguments => _lastArguments;

    /// <summary>
    /// Event fired when the server outputs a log message (stdout)
    /// </summary>
    public event Action<string>? OnServerOutput;

    /// <summary>
    /// Event fired when the server outputs an error message (stderr)
    /// </summary>
    public event Action<string>? OnServerError;

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

        // Store arguments for external access (e.g., Unity logging)
        _lastArguments = arguments;

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

            // Filter out verbose/unnecessary messages
            if (!ShouldFilterServerLog(sanitizedOutput))
            {
              Logger.Info($"[llama-server] {sanitizedOutput}");
              // Fire event for external subscribers (e.g., Unity)
              OnServerOutput?.Invoke(sanitizedOutput);
            }
          }
        };

        _process.ErrorDataReceived += (sender, e) =>
        {
          if (!string.IsNullOrEmpty(e.Data))
          {
            var sanitizedError = SanitizeProcessOutput(e.Data);

            // Filter out verbose/unnecessary messages (errors are usually important, but some are just verbose)
            if (!ShouldFilterServerLog(sanitizedError))
            {
              Logger.Error($"[llama-server] {sanitizedError}");
              // Fire event for external subscribers (e.g., Unity)
              OnServerError?.Invoke(sanitizedError);
            }
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

      // Idempotent: if process is null or already exited, return silently
      if (_process == null || _process.HasExited)
      {
        return;
      }

      try
      {
        Logger.Info($"[Server] Stopping llama-server (PID: {_process.Id})");

        // Try graceful shutdown first
        try
        {
          if (!_process.CloseMainWindow())
          {
            // Force kill if graceful shutdown fails (kill entire process tree on Windows)
            KillProcessTree(_process);
          }
        }
        catch (InvalidOperationException)
        {
          // Process already exited - ignore (idempotent)
        }

        // Wait for the process to exit with bounded timeout
        if (!_process.WaitForExit(MaxShutdownTimeoutSeconds * 1000))
        {
          // Process did not exit within timeout - force kill entire tree
          try
          {
            KillProcessTree(_process);
            // Wait again after kill
            _process.WaitForExit(5000); // Wait a bit more after kill
          }
          catch (InvalidOperationException)
          {
            // Process already exited - ignore (idempotent)
          }
        }

        // Verify process is actually dead (invariant: process must be dead before return)
        if (!_process.HasExited)
        {
          Logger.Warn("[Server] Process still running after kill attempt, forcing final kill");
          try
          {
            KillProcessTree(_process);
            _process.WaitForExit(2000); // Final wait
          }
          catch (InvalidOperationException)
          {
            // Process exited during kill - ignore (idempotent)
          }
        }

        Logger.Info("[Server] llama-server stopped successfully");
      }
      catch (InvalidOperationException)
      {
        // Process already exited or disposed - swallow (idempotent, normal shutdown path)
      }
      catch (Exception ex)
      {
        // Only log unexpected errors (not "already exited" cases)
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
      var argsList = new List<string>
      {
        $"--port {_config.Port}",
        $"-m \"{modelPath}\"",
        $"--ctx-size {_config.ContextSize}"
      };

      // GPU offload (critical for performance)
      if (_config.GpuLayers > 0)
      {
        argsList.Add($"-ngl {_config.GpuLayers}");
      }

      // CPU threads
      if (_config.Threads > 0)
      {
        argsList.Add($"-t {_config.Threads}");
      }

      // Batch sizes
      if (_config.BatchSize > 0)
      {
        argsList.Add($"-b {_config.BatchSize}");
      }

      if (_config.UBatchSize > 0)
      {
        argsList.Add($"--ubatch-size {_config.UBatchSize}");
      }

      // Memory locking
      if (_config.UseMlock)
      {
        argsList.Add("--mlock");
      }

      // Parallel slots (default to 1 for lowest latency, higher for throughput)
      // Using 1 slot reduces scheduling overhead significantly
      var parallelSlots = _config.ParallelSlots > 0 ? _config.ParallelSlots : 1;
      argsList.Add($"--parallel {parallelSlots}");

      // Continuous batching can help with multi-request scenarios
      if (_config.UseContinuousBatching)
      {
        argsList.Add("--cont-batching");
      }

      var arguments = string.Join(" ", argsList);

      if (arguments.Length > MaxArgumentsLength)
        throw new ArgumentException($"Arguments too long: {arguments.Length} characters (max: {MaxArgumentsLength})");

      // Validate that arguments don't contain dangerous characters
      var dangerousChars = new[] { '&', '|', ';', '`', '$', '(', ')', '{', '}', '[', ']', '<', '>' };
      if (dangerousChars.Any(c => arguments.Contains(c)))
        throw new ArgumentException("Arguments contain dangerous characters");

      return arguments;
    }

    /// <summary>
    /// Check if a server log line should be filtered out (verbose/unnecessary messages)
    /// </summary>
    internal bool ShouldFilterServerLog(string line)
    {
      if (string.IsNullOrWhiteSpace(line))
        return true;

      // Filter out verbose tensor loading messages
      if (line.Contains("llama_model_loader: - tensor") ||
          line.Contains("llama_model_loader: - type") ||
          line.Contains("llama_model_loader: - kv") ||
          line.Contains("llama_model_loader: Dumping metadata"))
        return true;

      // Filter out verbose model info dumps
      if (line.Contains("print_info:") && (
          line.Contains("arch") ||
          line.Contains("vocab_only") ||
          line.Contains("n_ctx_train") ||
          line.Contains("n_embd") ||
          line.Contains("n_layer") ||
          line.Contains("n_head") ||
          line.Contains("n_rot") ||
          line.Contains("n_ff") ||
          line.Contains("f_norm") ||
          line.Contains("rope") ||
          line.Contains("causal attn") ||
          line.Contains("pooling type") ||
          line.Contains("model type") ||
          line.Contains("model params") ||
          line.Contains("vocab type") ||
          line.Contains("n_vocab") ||
          line.Contains("n_merges") ||
          line.Contains("token") && line.Contains("token")))
        return true;

      // Filter out chat template dumps
      if (line.Contains("load_model: chat template") ||
          line.Contains("chat_template:") ||
          line.Contains("example_format:"))
        return true;

      // Filter out verbose loading progress (keep important ones)
      if (line.Contains("load_tensors: loading model tensors") ||
          line.Contains("load_tensors: - tensor") ||
          line.Contains("llama_context: constructing") ||
          line.Contains("llama_context: n_") ||
          line.Contains("llama_context: causal_attn") ||
          line.Contains("llama_context: flash_attn") ||
          line.Contains("llama_context: kv_unified") ||
          line.Contains("llama_context: freq_") ||
          line.Contains("llama_kv_cache: size =") ||
          line.Contains("llama_kv_cache:      CUDA") ||
          line.Contains("llama_context:      CUDA") ||
          line.Contains("llama_context: graph nodes") ||
          line.Contains("llama_context: graph splits") ||
          line.Contains("slot   load_model: id") ||
          line.Contains("slot   launch_slot_") ||
          line.Contains("slot   update_slots:") ||
          line.Contains("slot   print_timing:") ||
          line.Contains("slot      release:") ||
          line.Contains("slot get_availabl:") ||
          line.Contains("slot launch_slot_:") ||
          line.Contains("slot update_slots:") ||
          line.Contains("slot print_timing:") ||
          line.Contains("srv  update_slots:") ||
          line.Contains("srv  get_availabl:") ||
          line.Contains("srv   prompt_save:") ||
          line.Contains("srv          load:") ||
          line.Contains("srv        update:") ||
          line.Contains("srv  log_server_r:"))
        return true;

      // Filter out verbose system info dumps
      if (line.Contains("system_info:") && (
          line.Contains("n_threads =") ||
          line.Contains("CUDA : ARCHS") ||
          line.Contains("CPU : SSE") ||
          line.Contains("USE_GRAPHS") ||
          line.Contains("PEER_MAX")))
        return true;

      // Filter out tiny internal eval timing lines (not real requests)
      // These are internal llama.cpp events with <= 2 tokens or 0.00ms eval time
      if (line.Contains("eval time =") || line.Contains("prompt eval time =") || line.Contains("total time ="))
      {
        // Extract token count from timing line (e.g., "eval time = 0.00 ms / 1 tokens" or "total time = 0.41 ms / 2 tokens")
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(line, @"/\s+(\d+)\s+tokens");
        if (tokenMatch.Success && int.TryParse(tokenMatch.Groups[1].Value, out int tokenCount))
        {
          if (tokenCount <= 2)
            return true; // Tiny internal event, not a real request
        }

        // Filter lines with 0.00 ms eval time (internal events)
        if (line.Contains("eval time =       0.00 ms") || line.Contains("eval time = 0.00 ms"))
          return true;
      }

      // Keep important messages
      return false;
    }

    /// <summary>
    /// Sanitize process output
    /// </summary>
    internal string SanitizeProcessOutput(string output)
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
    /// <summary>
    /// Kills the process and its entire process tree (all child processes).
    /// On Windows, uses entireProcessTree parameter if available (.NET 5.0+).
    /// </summary>
    private void KillProcessTree(Process process)
    {
      if (process == null || process.HasExited)
        return;

      try
      {
#if NET5_0_OR_GREATER
        // .NET 5.0+ supports entireProcessTree parameter
        process.Kill(entireProcessTree: true);
#else
        // Fallback for older .NET versions - just kill the process
        // Note: Child processes may leak on older .NET versions
        process.Kill();
#endif
      }
      catch (InvalidOperationException)
      {
        // Process already exited - ignore (idempotent)
      }
      catch (Exception ex)
      {
        // Log but don't throw - we'll verify HasExited later
        Logger.Warn($"[Server] Error killing process tree: {ex.Message}");
      }
    }

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