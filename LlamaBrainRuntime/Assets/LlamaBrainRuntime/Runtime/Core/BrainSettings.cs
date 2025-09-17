using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Core;

namespace LlamaBrain.Runtime.Core
{
    /// <summary>
    /// Settings for the LlamaBrain server.
    /// </summary>
    [CreateAssetMenu(menuName = "LlamaBrain/BrainSettings")]
    public class BrainSettings : ScriptableObject
    {
        /// <summary>
        /// The path to the llama.cpp executable
        /// </summary>
        [Header("Server Configuration")]
        public string ExecutablePath;

        /// <summary>
        /// The path to the llama.cpp model
        /// </summary>
        public string ModelPath;

        /// <summary>
        /// The port to use for the llama.cpp server
        /// </summary>
        public int Port = 5000;

        /// <summary>
        /// The context size to use for the llama.cpp server
        /// </summary>
        public int ContextSize = 2048;

        /// <summary>
        /// The maximum number of tokens to generate
        /// </summary>
        [Header("LLM Generation Settings")]
        [Tooltip("Maximum number of tokens to generate")]
        public int MaxTokens = 64;

        /// <summary>
        /// The temperature to use for the llama.cpp server
        /// </summary>
        [Tooltip("Controls randomness in generation (0.0 = deterministic, 1.0 = very random)")]
        [Range(0.0f, 2.0f)]
        public float Temperature = 0.7f;

        /// <summary>
        /// The top-p value to use for the llama.cpp server
        /// </summary>
        [Tooltip("Controls diversity via nucleus sampling (0.0 = deterministic, 1.0 = very diverse)")]
        [Range(0.0f, 1.0f)]
        public float TopP = 0.9f;

        [Tooltip("Limits the number of tokens considered for each step (0 = disabled)")]
        [Range(0, 100)]
        public int TopK = 40;

        [Tooltip("Penalty for repeating tokens (1.0 = no penalty, higher values = stronger penalty)")]
        [Range(1.0f, 2.0f)]
        public float RepeatPenalty = 1.1f;

        [Header("Stop Sequences")]
        [Tooltip("Sequences that will stop generation when encountered")]
        public string[] StopSequences = new string[]
        {
            "</s>",
            "Human:",
            "Assistant:",
            "Player:",
            "NPC:",
            "Step 1:",
            "Step 2:",
            "Step 3:",
            "\n\n",
            "\nPlayer:",
            "\nNPC:"
        };

        /// <summary>
        /// Converts the current settings to a ProcessConfig
        /// </summary>
        /// <returns>The ProcessConfig</returns>
        public ProcessConfig ToProcessConfig()
        {
            var config = new ProcessConfig();
            config.Host = "localhost";
            config.Port = Port;
            config.Model = ModelPath ?? "";
            config.ExecutablePath = ExecutablePath ?? "";
            config.ContextSize = ContextSize;

            // Create LLM configuration from settings
            config.LlmConfig = ToLlmConfig();

            return config;
        }

        /// <summary>
        /// Creates a LlmConfig from the current settings
        /// </summary>
        /// <returns>The LLM configuration</returns>
        public LlmConfig ToLlmConfig()
        {
            return new LlmConfig
            {
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                TopP = TopP,
                TopK = TopK,
                RepeatPenalty = RepeatPenalty,
                StopSequences = StopSequences
            };
        }
    }
}
