#nullable enable
using UnityEngine;
using LlamaBrain.Core;
using LlamaBrain.Persona;
using LlamaBrain.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// A LlamaBrain server that can be used to interact with a LlamaBrain server.
  /// </summary>
  public class BrainServer : MonoBehaviour
  {
    /// <summary>
    /// The settings for the LlamaBrain server.
    /// </summary>
    public BrainSettings? Settings;

    /// <summary>
    /// The server manager for the LlamaBrain server.
    /// </summary>
    private ServerManager? serverManager;
    /// <summary>
    /// The client manager for the LlamaBrain server.
    /// </summary>
    private ClientManager? clientManager;
    private CancellationTokenSource? _cancellationTokenSource;
    /// <summary>
    /// The startup task for observing exceptions.
    /// </summary>
    private Task? _startupTask;
    /// <summary>
    /// Whether the LlamaBrain server is initialized.
    /// </summary>
    private bool _isInitialized;
    /// <summary>
    /// The current ProcessConfig used for detecting server-level config changes.
    /// </summary>
    private ProcessConfig? _currentProcessConfig;

    /// <summary>
    /// Whether the LlamaBrain server is initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Event fired when BrainSettings are successfully reloaded.
    /// Passes the new LlmConfig to subscribers.
    /// </summary>
    public event Action<LlmConfig>? OnBrainSettingsReloaded;

    /// <summary>
    /// Whether the server is currently running and ready
    /// </summary>
    public bool IsServerRunning { get; private set; }

    /// <summary>
    /// The current connection status
    /// </summary>
    public string ConnectionStatus { get; private set; } = "Not Initialized";

    /// <summary>
    /// Last error message from server operations
    /// </summary>
    public string LastErrorMessage { get; private set; } = "";

    /// <summary>
    /// Server startup time in seconds
    /// </summary>
    public float ServerStartupTime { get; private set; } = 0f;

    /// <summary>
    /// Number of connection attempts made
    /// </summary>
    public int ConnectionAttempts { get; private set; } = 0;

    /// <summary>
    /// Actual GPU layers offloaded (parsed from server logs)
    /// </summary>
    public int ActualGpuLayersOffloaded { get; private set; } = -1;

    /// <summary>
    /// Total model layers (parsed from server logs)
    /// </summary>
    public int TotalModelLayers { get; private set; } = -1;

    /// <summary>
    /// Initializes the LlamaBrain server.
    /// </summary>
    public void Initialize()
    {
      if (_isInitialized)
      {
        UnityEngine.Debug.LogWarning("[LLM] LlamaBrainServer already initialized.");
        return;
      }

      try
      {
        if (Settings == null)
        {
          UnityEngine.Debug.LogWarning("[LLM] LlamaBrainServer.Settings is null. Server initialization will be skipped.");
          return;
        }

        var config = Settings.ToProcessConfig();
        if (config == null)
        {
          UnityEngine.Debug.LogError("[LLM] LlamaBrainServer.Settings.ToProcessConfig() returned null. Server initialization will be skipped.");
          return;
        }

        // Store current config for hot reload comparison
        _currentProcessConfig = config;

        // Validate paths
        var exePath = Path.GetFullPath(config.ExecutablePath);
        UnityEngine.Debug.Log($"[LLM] Resolved exePath: {exePath}, Exists: {File.Exists(exePath)}");

        var modelPath = Path.GetFullPath(config.Model);
        UnityEngine.Debug.Log($"[LLM] Resolved modelPath: {modelPath}, Exists: {File.Exists(modelPath)}");

        serverManager = new ServerManager(config);
        clientManager = new ClientManager(config);
        _cancellationTokenSource = new CancellationTokenSource();
#if !UNITY_INCLUDE_TESTS
        DontDestroyOnLoad(gameObject);
#endif

        // Subscribe to server output events so we can see llama.cpp logs in Unity
        serverManager.OnServerOutput += OnLlamaServerOutput;
        serverManager.OnServerError += OnLlamaServerStderrLine;

        _isInitialized = true;
        UnityEngine.Debug.Log("[LLM] LlamaBrainServer initialized successfully.");
      }
      catch (Exception ex)
      {
        UnityEngine.Debug.LogError($"[LLM] LlamaBrainServer.Initialize() failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
      }
    }

    /// <summary>
    /// Starts the LlamaBrain server.
    /// Stores the startup Task to observe exceptions and prevent unobserved Task exceptions.
    /// </summary>
    private void Start()
    {
      // Auto-initialize if not already done
      if (!_isInitialized)
      {
        UnityEngine.Debug.Log("[LLM] Auto-initializing LlamaBrainServer in Start()");
        Initialize();
      }

      if (!_isInitialized || serverManager == null || clientManager == null)
      {
        UnityEngine.Debug.LogWarning("[LLM] LlamaBrainServer not initialized. Start will be skipped.");
        return;
      }

      // Store the startup task to observe exceptions (prevents unobserved Task exceptions)
      _startupTask = StartAsync();
    }

    /// <summary>
    /// Async startup logic - separated from Start() to enable Task observation.
    /// </summary>
    private async Task StartAsync()
    {
      // Get cancellation token safely (may be disposed by StopServer() in race conditions)
      CancellationToken token;
      try
      {
        token = _cancellationTokenSource?.Token ?? CancellationToken.None;
      }
      catch (ObjectDisposedException)
      {
        // StopServer() was called during initialization - abort startup
        return;
      }

      try
      {
        // Check if managers are still available (StopServer() might have disposed them)
        if (serverManager == null || clientManager == null)
        {
          return; // StopServer() was called during initialization
        }

        // Start the llama-server process
        serverManager.StartServer();

        // Log the startup arguments to Unity console (DLL Logger doesn't reach Unity)
        UnityEngine.Debug.Log($"[LLM] Server started with arguments: {serverManager.LastStartupArguments}");

        // Give the server a moment to start up
        await Task.Delay(2000, token);
        
        // Re-check _isInitialized after await - StopServer() can flip it mid-flight
        if (!_isInitialized || serverManager == null || clientManager == null)
        {
          return; // StopServer() was called during delay
        }

        // Wait for the server to be ready
        await clientManager.WaitForAsync(token);
        
        // Re-check _isInitialized after await - StopServer() can flip it mid-flight
        if (!_isInitialized)
        {
          return; // StopServer() was called during wait
        }
      }
      catch (OperationCanceledException)
      {
        // Expected when the server is being destroyed, don't log as error
        UnityEngine.Debug.Log("[LLM] Server startup was canceled during cleanup.");
      }
      catch (ObjectDisposedException)
      {
        // Expected when StopServer() disposes the CTS or managers during startup
        UnityEngine.Debug.Log("[LLM] Server startup aborted during cleanup.");
      }
      catch (Exception ex)
      {
        // Only log if still initialized (not aborted by StopServer())
        if (_isInitialized)
        {
          UnityEngine.Debug.LogError($"[LLM] Failed to start server: {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Manually start the server process.
    /// This method starts the llama-server process but does not wait for it to be ready.
    /// Use WaitForServerAsync() after calling this to wait for the server to be ready.
    /// </summary>
    public void StartServer()
    {
      if (!_isInitialized || serverManager == null)
      {
        UnityEngine.Debug.LogWarning("[LLM] LlamaBrainServer not initialized. Cannot start server.");
        return;
      }

      try
      {
        // Start the llama-server process
        serverManager.StartServer();

        // Log the startup arguments to Unity console
        UnityEngine.Debug.Log($"[LLM] Server started with arguments: {serverManager.LastStartupArguments}");
      }
      catch (Exception ex)
      {
        UnityEngine.Debug.LogError($"[LLM] Failed to start server: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Destroys the LlamaBrain server.
    /// </summary>
    private void OnDestroy()
    {
      // If StopServer() was already called, cleanup is already done - skip to avoid double-dispose
      if (!_isInitialized)
      {
        return; // Already stopped/cleaned up
      }

      // Unsubscribe from events
      if (serverManager != null)
      {
        try
        {
          serverManager.OnServerOutput -= OnLlamaServerOutput;
          serverManager.OnServerError -= OnLlamaServerStderrLine;
        }
        catch
        {
          // Ignore unsubscribe errors
        }
      }

      // Cancel and dispose only if not already disposed
      try
      {
        _cancellationTokenSource?.Cancel();
      }
      catch
      {
        // Already disposed by StopServer() - ignore
      }
      
      try
      {
        _cancellationTokenSource?.Dispose();
      }
      catch
      {
        // Already disposed - ignore
      }
      
      try
      {
        serverManager?.Dispose();
      }
      catch
      {
        // Already disposed - ignore
      }
      
      try
      {
        clientManager?.Dispose();
      }
      catch
      {
        // Already disposed - ignore
      }

      // Observe startup task to prevent unobserved Task exceptions
      if (_startupTask != null)
      {
        try
        {
          // Observe any exceptions from the startup task
          if (_startupTask.IsFaulted)
          {
            _ = _startupTask.Exception; // Observe exception to prevent unobserved Task exception
          }
        }
        catch
        {
          // Ignore observation errors
        }
        _startupTask = null;
      }

      // Mark as uninitialized to avoid re-entry weirdness
      _isInitialized = false;
    }

    /// <summary>
    /// Handler for llama-server stdout messages
    /// </summary>
    private void OnLlamaServerOutput(string message)
    {
      ParseServerLogForGpuLayers(message);
      UnityEngine.Debug.Log($"[llama-server] {message}");
    }

    /// <summary>
    /// Handler for llama-server stderr messages
    /// Note: llama.cpp uses stderr for normal output (timings, status), not just errors
    /// </summary>
    private void OnLlamaServerStderrLine(string message)
    {
      ParseServerLogForGpuLayers(message);

      // llama.cpp uses stderr for normal output, so treat as log by default
      // Only escalate to Error/Warning if it matches known error patterns
      var lowerMessage = message.ToLowerInvariant();
      var isError = lowerMessage.Contains("error:") ||
                    lowerMessage.Contains("failed:") ||
                    lowerMessage.Contains("cannot") ||
                    lowerMessage.Contains("unable to") ||
                    lowerMessage.Contains("fatal") ||
                    lowerMessage.Contains("critical") ||
                    (lowerMessage.Contains("error") && (lowerMessage.Contains("at ") || lowerMessage.Contains("code")));

      if (isError)
      {
        UnityEngine.Debug.LogError($"[llama-server] {message}");
      }
      else
      {
        // Normal stderr output (timings, status) - log as info
        UnityEngine.Debug.Log($"[llama-server] {message}");
      }
    }

    /// <summary>
    /// List of agents to update when GPU layers are parsed
    /// </summary>
    private System.Collections.Generic.List<LlamaBrainAgent> registeredAgents = new System.Collections.Generic.List<LlamaBrainAgent>();

    /// <summary>
    /// Parse GPU layers information from server logs
    /// </summary>
    private void ParseServerLogForGpuLayers(string message)
    {
      // Parse "load_tensors: offloaded X/X layers to GPU"
      if (message.Contains("offloaded") && message.Contains("layers to GPU"))
      {
        var match = System.Text.RegularExpressions.Regex.Match(message, @"offloaded\s+(\d+)/(\d+)\s+layers");
        if (match.Success)
        {
          if (int.TryParse(match.Groups[1].Value, out int offloaded) &&
              int.TryParse(match.Groups[2].Value, out int total))
          {
            ActualGpuLayersOffloaded = offloaded;
            TotalModelLayers = total;
            UnityEngine.Debug.Log($"[LLM] Parsed GPU layers: {offloaded}/{total} layers offloaded to GPU");

            // Update all registered agents
            foreach (var agent in registeredAgents)
            {
              if (agent != null)
              {
                agent.UpdateGpuLayersOffloaded(offloaded, total);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Configures an agent with the server settings.
    /// </summary>
    /// <param name="agent">The agent to configure with server settings</param>
    public void ConfigureAgent(LlamaBrainAgent agent)
    {
      if (agent == null || Settings == null)
        return;

      var modelPath = Settings.ModelPath ?? "";
      var gpuLayers = Settings.GpuLayers;
      var batchSize = Settings.BatchSize;
      var uBatchSize = Settings.UBatchSize;
      var parallelSlots = Settings.ParallelSlots;

      agent.SetServerConfig(modelPath, gpuLayers, batchSize, uBatchSize, parallelSlots);

      // Register agent for GPU layer updates
      if (!registeredAgents.Contains(agent))
      {
        registeredAgents.Add(agent);
      }

      // Update with actual GPU layers if we've parsed them
      if (ActualGpuLayersOffloaded >= 0)
      {
        agent.UpdateGpuLayersOffloaded(ActualGpuLayersOffloaded, TotalModelLayers);
      }
    }

    /// <summary>
    /// Register an agent for GPU layer updates.
    /// This method registers the agent without configuring it.
    /// Use ConfigureAgent() if you also want to configure the agent with server settings.
    /// </summary>
    /// <param name="agent">The agent to register</param>
    public void RegisterAgent(LlamaBrainAgent agent)
    {
      if (agent == null)
        return;

      // Register agent for GPU layer updates
      if (!registeredAgents.Contains(agent))
      {
        registeredAgents.Add(agent);
      }

      // Update with actual GPU layers if we've parsed them
      if (ActualGpuLayersOffloaded >= 0)
      {
        agent.UpdateGpuLayersOffloaded(ActualGpuLayersOffloaded, TotalModelLayers);
      }
    }

    /// <summary>
    /// Unregister an agent (call when agent is destroyed).
    /// </summary>
    /// <param name="agent">The agent to unregister</param>
    public void UnregisterAgent(LlamaBrainAgent agent)
    {
      if (agent != null)
      {
        registeredAgents.Remove(agent);
      }
    }

    /// <summary>
    /// Creates a client for the LlamaBrain server.
    /// </summary>
    /// <returns>The client for the LlamaBrain server.</returns>
    public ApiClient? CreateClient()
    {
      if (!_isInitialized || clientManager == null)
      {
        UnityEngine.Debug.LogError("[LLM] LlamaBrainServer not initialized. Cannot create client.");
        return null;
      }
      return clientManager.CreateClient();
    }

    /// <summary>
    /// Check if the server is currently running and ready
    /// </summary>
    /// <returns>True if the server is running, false otherwise</returns>
    public async Task<bool> IsServerRunningAsync()
    {
      if (!_isInitialized || clientManager == null)
      {
        ConnectionStatus = "Not Initialized";
        return false;
      }

      try
      {
        ConnectionStatus = "Checking Server Status...";
        var isRunning = await clientManager.IsRunningAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);
        IsServerRunning = isRunning;
        ConnectionStatus = isRunning ? "Connected" : "Server Not Responding";
        return isRunning;
      }
      catch (Exception ex)
      {
        LastErrorMessage = ex.Message;
        ConnectionStatus = "Connection Error";
        IsServerRunning = false;
        UnityEngine.Debug.LogError($"[LLM] Error checking server status: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Wait for the server to be ready with timeout
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds (default: 30)</param>
    /// <returns>True if server became ready, false if timeout</returns>
    public async Task<bool> WaitForServerAsync(int timeoutSeconds = 30)
    {
      if (!_isInitialized || clientManager == null)
      {
        ConnectionStatus = "Not Initialized";
        return false;
      }

      var startTime = Time.time;
      try
      {
        ConnectionStatus = "Waiting for Server...";
        ConnectionAttempts = 0;

        using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
        using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token,
            _cancellationTokenSource?.Token ?? CancellationToken.None))
        {
          await clientManager.WaitForAsync(combinedCts.Token);

          ServerStartupTime = Time.time - startTime;
          IsServerRunning = true;
          ConnectionStatus = "Connected";
          LastErrorMessage = "";
          UnityEngine.Debug.Log($"[LLM] Server ready after {ServerStartupTime:F1} seconds");
          return true;
        }
      }
      catch (OperationCanceledException)
      {
        ServerStartupTime = Time.time - startTime;
        ConnectionStatus = "Timeout";
        LastErrorMessage = $"Server startup timed out after {timeoutSeconds} seconds";
        IsServerRunning = false;
        UnityEngine.Debug.LogWarning($"[LLM] {LastErrorMessage}");
        return false;
      }
      catch (Exception ex)
      {
        ServerStartupTime = Time.time - startTime;
        LastErrorMessage = ex.Message;
        ConnectionStatus = "Connection Failed";
        IsServerRunning = false;
        UnityEngine.Debug.LogError($"[LLM] Error waiting for server: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Manually check server health and update status
    /// </summary>
    public void CheckServerHealth()
    {
      CheckServerHealthAsync().Forget();
    }

    private async UniTaskVoid CheckServerHealthAsync()
    {
      try
      {
        await IsServerRunningAsync();
      }
      catch (Exception ex)
      {
        UnityEngine.Debug.LogError($"[LLM] Health check failed: {ex.Message}");
      }
    }

    /// <summary>
    /// Get detailed server status information
    /// </summary>
    /// <returns>Formatted status string</returns>
    public string GetServerStatus()
    {
      var status = new System.Text.StringBuilder();
      status.AppendLine("Server Status");
      status.AppendLine($"Initialized: {_isInitialized}");
      
      if (!_isInitialized)
      {
        return status.ToString();
      }

      status.AppendLine($"Status: {ConnectionStatus}");
      status.AppendLine($"Running: {IsServerRunning}");
      status.AppendLine($"Startup Time: {ServerStartupTime:F1}s");
      status.AppendLine($"Connection Attempts: {ConnectionAttempts}");
      status.AppendLine($"Active Agents: {registeredAgents.Count}");

      if (!string.IsNullOrEmpty(LastErrorMessage))
        status.AppendLine($"Last Error: {LastErrorMessage}");

      return status.ToString();
    }

    /// <summary>
    /// Stops the server process synchronously and idempotently.
    /// This ensures the llama-server process is terminated before the GameObject is destroyed.
    /// Call this explicitly in tests to prevent process leaks.
    /// 
    /// Requirements:
    /// - Multiple calls are safe (idempotent)
    /// - Returns only after the child process is dead (or confirmed not running)
    /// - Never logs errors on normal shutdown paths during tests
    /// </summary>
    public void StopServer()
    {
      if (!_isInitialized)
      {
        return; // Already stopped or never initialized (idempotent)
      }

      // Cancel first, never throw
      try
      {
        _cancellationTokenSource?.Cancel();
      }
      catch
      {
        // Ignore all exceptions during cancellation (idempotent)
      }

      // Stop process deterministically; ServerManager.StopServer() blocks until dead
      // This is idempotent - ServerManager checks if process is already stopped
      try
      {
        serverManager?.StopServer();
      }
      catch
      {
        // StopServer() should be idempotent and non-throwing, but catch any edge cases
      }

      // Unsubscribe safely
      try
      {
        if (serverManager != null)
        {
          serverManager.OnServerOutput -= OnLlamaServerOutput;
          serverManager.OnServerError -= OnLlamaServerStderrLine;
        }
      }
      catch
      {
        // Ignore unsubscribe errors (idempotent)
      }

      // Dispose quietly (no error logging on normal shutdown paths)
      try
      {
        _cancellationTokenSource?.Dispose();
      }
      catch
      {
        // Ignore disposal errors (idempotent)
      }
      
      try
      {
        clientManager?.Dispose();
      }
      catch
      {
        // Ignore disposal errors (idempotent)
      }
      
      try
      {
        serverManager?.Dispose();
      }
      catch
      {
        // Ignore disposal errors (idempotent)
      }

      // Observe startup task to prevent unobserved Task exceptions
      if (_startupTask != null)
      {
        try
        {
          // Observe any exceptions from the startup task
          if (_startupTask.IsFaulted)
          {
            _ = _startupTask.Exception; // Observe exception to prevent unobserved Task exception
          }
        }
        catch
        {
          // Ignore observation errors
        }
        _startupTask = null;
      }

      // Null fields to make idempotence complete (prevents accidental double-dispose elsewhere)
      _cancellationTokenSource = null;
      clientManager = null;
      serverManager = null;
      registeredAgents.Clear();

      IsServerRunning = false;
      ConnectionStatus = "Stopped";
      LastErrorMessage = "";
      _isInitialized = false;
    }

    /// <summary>
    /// Force a server restart
    /// </summary>
    public void RestartServer()
    {
      RestartServerAsync().Forget();
    }

    private async UniTaskVoid RestartServerAsync()
    {
      if (!_isInitialized || serverManager == null)
      {
        UnityEngine.Debug.LogWarning("[LLM] Cannot restart server - not initialized");
        return;
      }

      try
      {
        ConnectionStatus = "Restarting Server...";
        UnityEngine.Debug.Log("[LLM] Restarting server...");

        // Stop current server
        serverManager.StopServer();
        await Task.Delay(1000); // Give it time to stop

        // Start server again
        serverManager.StartServer();
        await Task.Delay(2000); // Give it time to start

        // Wait for it to be ready
        await WaitForServerAsync();
      }
      catch (Exception ex)
      {
        LastErrorMessage = ex.Message;
        ConnectionStatus = "Restart Failed";
        UnityEngine.Debug.LogError($"[LLM] Server restart failed: {ex.Message}");
      }
    }

    /// <summary>
    /// Hot reload BrainSettings by updating LlmConfig for all registered agents.
    /// Server-level config changes (GPU layers, model path, etc.) will log a warning.
    /// Returns true if reload succeeded, false if validation failed.
    /// </summary>
    public bool ReloadBrainSettings()
    {
      if (Settings == null)
      {
        UnityEngine.Debug.LogWarning("[HotReload] Cannot reload BrainSettings: Settings is null");
        return false;
      }

      try
      {
        // Convert to ProcessConfig and LlmConfig
        var newProcessConfig = Settings.ToProcessConfig();
        if (newProcessConfig == null)
        {
          UnityEngine.Debug.LogError("[HotReload] BrainSettings.ToProcessConfig() returned null");
          return false;
        }

        var newLlmConfig = Settings.ToLlmConfig();
        if (newLlmConfig == null)
        {
          UnityEngine.Debug.LogError("[HotReload] BrainSettings.ToLlmConfig() returned null");
          return false;
        }

        // Validate LlmConfig
        var errors = newLlmConfig.Validate();
        if (errors != null && errors.Length > 0)
        {
          UnityEngine.Debug.LogError($"[HotReload] BrainSettings validation failed: {string.Join(", ", errors)}");
          return false;
        }

        // Check if server-level config changed (requires restart)
        if (_currentProcessConfig != null && HasServerConfigChanged(_currentProcessConfig, newProcessConfig))
        {
          UnityEngine.Debug.LogWarning("[HotReload] Server-level settings changed (GPU layers, model path, context size, or batch size). Full restart required to apply these changes.");
          UnityEngine.Debug.LogWarning("[HotReload] LLM generation parameters (Temperature, MaxTokens, etc.) will be applied immediately.");
        }

        // Broadcast LlmConfig to all registered agents
        foreach (var agent in registeredAgents)
        {
          if (agent != null)
          {
            agent.UpdateLlmConfig(newLlmConfig);
          }
        }

        // Update current config
        _currentProcessConfig = newProcessConfig;

        // Fire event
        OnBrainSettingsReloaded?.Invoke(newLlmConfig);

        UnityEngine.Debug.Log($"[HotReload] BrainSettings reloaded successfully: Temperature={newLlmConfig.Temperature}, MaxTokens={newLlmConfig.MaxTokens}");
        return true;
      }
      catch (Exception ex)
      {
        UnityEngine.Debug.LogError($"[HotReload] Failed to reload BrainSettings: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Checks if server-level configuration has changed between two ProcessConfig instances.
    /// Server-level config includes: GPU layers, model path, context size, batch size.
    /// Changes to these parameters require a full server restart.
    /// </summary>
    /// <param name="oldConfig">The previous ProcessConfig</param>
    /// <param name="newConfig">The new ProcessConfig</param>
    /// <returns>True if server-level config changed, false otherwise</returns>
    public bool HasServerConfigChanged(ProcessConfig oldConfig, ProcessConfig newConfig)
    {
      if (oldConfig == null || newConfig == null)
      {
        return false;
      }

      // Compare server-level parameters that require restart
      return oldConfig.GpuLayers != newConfig.GpuLayers
          || oldConfig.Model != newConfig.Model
          || oldConfig.ContextSize != newConfig.ContextSize
          || oldConfig.BatchSize != newConfig.BatchSize;
    }

    /// <summary>
    /// Generates an A/B test report by aggregating metrics from all registered agents.
    /// Only includes agents that have prompt variants configured.
    /// </summary>
    /// <param name="testName">The name for this A/B test report</param>
    /// <returns>ABTestReport with aggregated metrics from all agents</returns>
    public ABTestReport GenerateABTestReport(string testName)
    {
      var report = new ABTestReport(testName);

      // Aggregate metrics from all registered agents
      foreach (var agent in registeredAgents)
      {
        if (agent == null || agent.PersonaConfig == null)
        {
          continue;
        }

        // Only include agents with variant testing enabled
        if (agent.PersonaConfig.SystemPromptVariants == null ||
            agent.PersonaConfig.SystemPromptVariants.Count == 0)
        {
          continue;
        }

        // Get metrics from agent's variant manager
        var agentMetrics = agent.GetVariantMetrics();
        if (agentMetrics != null)
        {
          foreach (var kvp in agentMetrics)
          {
            // Aggregate metrics by variant name across all agents
            var variantName = kvp.Key;
            var metrics = kvp.Value;

            // If we already have metrics for this variant, merge them
            if (report.HasVariant(variantName))
            {
              var existing = report.GetVariantMetrics(variantName);
              if (existing != null)
              {
                // Merge metrics (sum counts, average averages)
                var merged = new VariantMetrics
                {
                  SelectionCount = existing.SelectionCount + metrics.SelectionCount,
                  SuccessCount = existing.SuccessCount + metrics.SuccessCount,
                  ValidationFailureCount = existing.ValidationFailureCount + metrics.ValidationFailureCount,
                  FallbackCount = existing.FallbackCount + metrics.FallbackCount,
                  // Weighted average for latency and tokens
                  AvgLatencyMs = (existing.AvgLatencyMs * existing.SelectionCount +
                                 metrics.AvgLatencyMs * metrics.SelectionCount) /
                                 (existing.SelectionCount + metrics.SelectionCount),
                  AvgTokensGenerated = (existing.AvgTokensGenerated * existing.SelectionCount +
                                       metrics.AvgTokensGenerated * metrics.SelectionCount) /
                                       (existing.SelectionCount + metrics.SelectionCount)
                };
                report.AddVariantMetrics(variantName, merged);
              }
            }
            else
            {
              // First time seeing this variant, add it
              report.AddVariantMetrics(variantName, metrics);
            }
          }
        }
      }

      return report;
    }
  }
}
