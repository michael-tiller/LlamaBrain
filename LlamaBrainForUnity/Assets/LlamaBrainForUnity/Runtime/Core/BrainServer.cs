using UnityEngine;
using LlamaBrain.Core;
using LlamaBrain.Persona;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LlamaBrain.Unity.Runtime.Core
{
    /// <summary>
    /// A LlamaBrain server that can be used to interact with a LlamaBrain server.
    /// </summary>
    public class BrainServer : MonoBehaviour
    {
        /// <summary>
        /// The settings for the LlamaBrain server.
        /// </summary>
        public BrainSettings Settings;

        /// <summary>
        /// The server manager for the LlamaBrain server.
        /// </summary>
        private ServerManager serverManager;
        /// <summary>
        /// The client manager for the LlamaBrain server.
        /// </summary>
        private ClientManager clientManager;
        private CancellationTokenSource _cancellationTokenSource;
        /// <summary>
        /// Whether the LlamaBrain server is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Whether the LlamaBrain server is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

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

                // Validate paths
                var exePath = Path.GetFullPath(config.ExecutablePath);
                UnityEngine.Debug.Log($"[LLM] Resolved exePath: {exePath}, Exists: {File.Exists(exePath)}");

                var modelPath = Path.GetFullPath(config.Model);
                UnityEngine.Debug.Log($"[LLM] Resolved modelPath: {modelPath}, Exists: {File.Exists(modelPath)}");

                serverManager = new ServerManager(config);
                clientManager = new ClientManager(config);
                _cancellationTokenSource = new CancellationTokenSource();
                DontDestroyOnLoad(gameObject);

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
        /// </summary>
        private async void Start()
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

            try
            {
                // Start the llama-server process
                serverManager.StartServer();

                // Give the server a moment to start up
                await Task.Delay(2000, _cancellationTokenSource.Token);

                // Wait for the server to be ready
                await clientManager.WaitForAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when the server is being destroyed, don't log as error
                UnityEngine.Debug.Log("[LLM] Server startup was canceled during cleanup.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LLM] Failed to start server: {ex.Message}");
            }
        }

        /// <summary>
        /// Destroys the LlamaBrain server.
        /// </summary>
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            serverManager?.Dispose();
            clientManager?.Dispose();
        }

        /// <summary>
        /// Creates a client for the LlamaBrain server.
        /// </summary>
        /// <returns>The client for the LlamaBrain server.</returns>
        public ApiClient CreateClient()
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
        public async void CheckServerHealth()
        {
            await IsServerRunningAsync();
        }

        /// <summary>
        /// Get detailed server status information
        /// </summary>
        /// <returns>Formatted status string</returns>
        public string GetServerStatus()
        {
            if (!_isInitialized)
                return "Server not initialized";

            var status = new System.Text.StringBuilder();
            status.AppendLine($"Status: {ConnectionStatus}");
            status.AppendLine($"Running: {IsServerRunning}");
            status.AppendLine($"Startup Time: {ServerStartupTime:F1}s");
            status.AppendLine($"Connection Attempts: {ConnectionAttempts}");

            if (!string.IsNullOrEmpty(LastErrorMessage))
                status.AppendLine($"Last Error: {LastErrorMessage}");

            return status.ToString();
        }

        /// <summary>
        /// Force a server restart
        /// </summary>
        public async void RestartServer()
        {
            if (!_isInitialized || serverManager == null)
            {
                UnityEngine.Debug.LogWarning("[LLM] Cannot restart server: not initialized");
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
    }
}
