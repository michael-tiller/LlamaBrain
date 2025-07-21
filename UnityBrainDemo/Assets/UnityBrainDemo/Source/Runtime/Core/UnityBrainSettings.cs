using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityBrain.Core;

namespace UnityBrainDemo.Runtime.Core
{
    /// <summary>
    /// Settings for the UnityBrain server.
    /// </summary>
    [CreateAssetMenu(menuName = "UnityBrain/UnityBrainSettings")]
    public class UnityBrainSettings : ScriptableObject
    {
        [Header("Server Configuration")]
        public string ExecutablePath;
        public string ModelPath;
        public int Port = 5000;
        public int ContextSize = 2048;

        public ProcessConfig ToProcessConfig()
        {
            var config = new ProcessConfig();
            config.Host = "localhost";
            config.Port = Port;
            config.Model = ModelPath ?? "";
            config.ExecutablePath = ExecutablePath ?? "";
            config.ContextSize = ContextSize;
            return config;
        }
    }
}
