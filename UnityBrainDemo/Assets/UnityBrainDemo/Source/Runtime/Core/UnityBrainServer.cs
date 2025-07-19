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
    public class UnityBrainServer : MonoBehaviour
    {
        public UnityBrainSettings Settings;

        private ServerManager _server;
        private CancellationTokenSource _cancellationTokenSource;

        private void Awake()
        {
            var exePath = Path.GetFullPath(Settings.ExecutablePath);
            UnityEngine.Debug.Log($"[LLM] Resolved exePath: {exePath}, Exists: {File.Exists(exePath)}");

            _server = new LlamaCppServerManager(Settings.ToProcessConfig());
            _cancellationTokenSource = new CancellationTokenSource();
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await _server.StartAsync(_cancellationTokenSource.Token);
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _server?.Stop();
        }

        public ApiClient CreateClient() => new ApiClient("localhost", Settings.Port);
    }
}
