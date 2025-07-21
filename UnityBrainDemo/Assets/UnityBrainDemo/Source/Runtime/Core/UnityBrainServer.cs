using UnityEngine;
using UnityBrain.Core;
using UnityBrain.Persona;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBrainDemo.Runtime.Core
{
    /// <summary>
    /// A UnityBrain server that can be used to interact with a UnityBrain server.
    /// </summary>
    public class UnityBrainServer : MonoBehaviour
    {
        /// <summary>
        /// The settings for the UnityBrain server.
        /// </summary>
        public UnityBrainSettings Settings;

        /// <summary>
        /// The server manager for the UnityBrain server.
        /// </summary>
        private ServerManager serverManager;
        /// <summary>
        /// The client manager for the UnityBrain server.
        /// </summary>
        private ClientManager clientManager;
        private CancellationTokenSource _cancellationTokenSource;
        /// <summary>
        /// Whether the UnityBrain server is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Whether the UnityBrain server is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the UnityBrain server.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                UnityEngine.Debug.LogWarning("[LLM] UnityBrainServer already initialized.");
                return;
            }

            try
            {
                if (Settings == null)
                {
                    UnityEngine.Debug.LogWarning("[LLM] UnityBrainServer.Settings is null. Server initialization will be skipped.");
                    return;
                }

                var config = Settings.ToProcessConfig();
                if (config == null)
                {
                    UnityEngine.Debug.LogError("[LLM] UnityBrainServer.Settings.ToProcessConfig() returned null. Server initialization will be skipped.");
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
                UnityEngine.Debug.Log("[LLM] UnityBrainServer initialized successfully.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LLM] UnityBrainServer.Initialize() failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Starts the UnityBrain server.
        /// </summary>
        private async void Start()
        {
            // Auto-initialize if not already done
            if (!_isInitialized)
            {
                UnityEngine.Debug.Log("[LLM] Auto-initializing UnityBrainServer in Start()");
                Initialize();
            }

            if (!_isInitialized || serverManager == null || clientManager == null)
            {
                UnityEngine.Debug.LogWarning("[LLM] UnityBrainServer not initialized. Start will be skipped.");
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
        /// Destroys the UnityBrain server.
        /// </summary>
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            serverManager?.Dispose();
            clientManager?.Dispose();
        }

        /// <summary>
        /// Creates a client for the UnityBrain server.
        /// </summary>
        /// <returns>The client for the UnityBrain server.</returns>
        public ApiClient CreateClient()
        {
            if (!_isInitialized || clientManager == null)
            {
                UnityEngine.Debug.LogError("[LLM] UnityBrainServer not initialized. Cannot create client.");
                return null;
            }
            return clientManager.CreateClient();
        }
    }
}
