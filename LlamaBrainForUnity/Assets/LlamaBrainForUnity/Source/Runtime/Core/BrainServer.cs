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
    }
}
