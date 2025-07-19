using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityBrain.Core;

namespace UnityBrainDemo.Runtime.Core
{

    [CreateAssetMenu(menuName = "UnityBrain/UnityBrainSettings")]
    public class UnityBrainSettings : ScriptableObject
    {
        public string ExecutablePath;
        public string ModelPath;
        public int Port = 5000;

        public ProcessConfig ToProcessConfig() =>
            new ProcessConfig
            {
                Host = "localhost",
                Port = Port,
                Model = ModelPath
            };
    }
}
