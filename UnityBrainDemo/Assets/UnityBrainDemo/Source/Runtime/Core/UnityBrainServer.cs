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

        private ClientManager client;
        private CancellationTokenSource _cancellationTokenSource;

        private void Awake()
        {
            var exePath = Path.GetFullPath(Settings.ExecutablePath);
            UnityEngine.Debug.Log($"[LLM] Resolved exePath: {exePath}, Exists: {File.Exists(exePath)}");

            client = new ClientManager(Settings.ToProcessConfig());
            _cancellationTokenSource = new CancellationTokenSource();
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await client.WaitForAsync(_cancellationTokenSource.Token);
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            client?.Dispose();
        }

        public ApiClient CreateClient() => client.CreateClient();
    }
}
