#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityBrainDemo.Runtime.Core;

namespace UnityBrainDemo.Editor
{
    /// <summary>
    /// Custom editor for the UnityBrainSettings class.
    /// </summary>
    [CustomEditor(typeof(UnityBrainSettings))]
    public class UnityBrainSettingsEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Whether to show the server configuration section
        /// </summary>
        private bool _showServerConfig = true;
        /// <summary>
        /// Whether to show the LLM generation settings section
        /// </summary>
        private bool _showLlmConfig = true;
        /// <summary>
        /// Whether to show the stop sequences section
        /// </summary>
        private bool _showStopSequences = true;

        /// <summary>
        /// Draws the inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            var settings = (UnityBrainSettings)target;

            EditorGUILayout.LabelField("UnityBrain Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            // Server Configuration Section
            _showServerConfig = EditorGUILayout.Foldout(_showServerConfig, "Server Configuration", true);
            if (_showServerConfig)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox("Configure the llama.cpp server connection and model settings.", MessageType.Info);
                
                settings.ExecutablePath = EditorGUILayout.TextField("Executable Path", settings.ExecutablePath);
                settings.ModelPath = EditorGUILayout.TextField("Model Path", settings.ModelPath);
                settings.Port = EditorGUILayout.IntField("Port", settings.Port);
                settings.ContextSize = EditorGUILayout.IntField("Context Size", settings.ContextSize);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            // LLM Generation Settings Section
            _showLlmConfig = EditorGUILayout.Foldout(_showLlmConfig, "LLM Generation Settings", true);
            if (_showLlmConfig)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox("Configure how the language model generates responses.", MessageType.Info);
                
                settings.MaxTokens = EditorGUILayout.IntField("Max Tokens", settings.MaxTokens);
                if (settings.MaxTokens < 1)
                {
                    EditorGUILayout.HelpBox("Max Tokens should be at least 1.", MessageType.Warning);
                }
                
                settings.Temperature = EditorGUILayout.Slider("Temperature", settings.Temperature, 0.0f, 2.0f);
                EditorGUILayout.HelpBox($"Temperature: {(settings.Temperature == 0.0f ? "Deterministic" : settings.Temperature < 0.5f ? "Low randomness" : settings.Temperature < 1.0f ? "Moderate randomness" : "High randomness")}", MessageType.None);
                
                settings.TopP = EditorGUILayout.Slider("Top P", settings.TopP, 0.0f, 1.0f);
                EditorGUILayout.HelpBox($"Top P: {(settings.TopP == 0.0f ? "Deterministic" : settings.TopP < 0.5f ? "Low diversity" : settings.TopP < 0.9f ? "Moderate diversity" : "High diversity")}", MessageType.None);
                
                settings.TopK = EditorGUILayout.IntSlider("Top K", settings.TopK, 0, 100);
                EditorGUILayout.HelpBox($"Top K: {(settings.TopK == 0 ? "Disabled" : $"Consider top {settings.TopK} tokens")}", MessageType.None);
                
                settings.RepeatPenalty = EditorGUILayout.Slider("Repeat Penalty", settings.RepeatPenalty, 1.0f, 2.0f);
                EditorGUILayout.HelpBox($"Repeat Penalty: {(settings.RepeatPenalty == 1.0f ? "No penalty" : settings.RepeatPenalty < 1.2f ? "Light penalty" : settings.RepeatPenalty < 1.5f ? "Moderate penalty" : "Strong penalty")}", MessageType.None);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            // Stop Sequences Section
            _showStopSequences = EditorGUILayout.Foldout(_showStopSequences, "Stop Sequences", true);
            if (_showStopSequences)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox("Sequences that will stop generation when encountered. Add or remove sequences as needed.", MessageType.Info);
                
                DrawStopSequencesList(settings);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            // Validation and Warnings
            DrawValidationWarnings(settings);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
            }
        }

        /// <summary>
        /// Draws the stop sequences list
        /// </summary>
        /// <param name="settings">The settings to draw the stop sequences list for</param>
        private void DrawStopSequencesList(UnityBrainSettings settings)
        {
            if (settings.StopSequences == null)
            {
                settings.StopSequences = new string[0];
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            for (int i = 0; i < settings.StopSequences.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                settings.StopSequences[i] = EditorGUILayout.TextField($"Sequence {i + 1}", settings.StopSequences[i]);
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    var newArray = new string[settings.StopSequences.Length - 1];
                    for (int j = 0; j < i; j++)
                        newArray[j] = settings.StopSequences[j];
                    for (int j = i + 1; j < settings.StopSequences.Length; j++)
                        newArray[j - 1] = settings.StopSequences[j];
                    settings.StopSequences = newArray;
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Stop Sequence"))
            {
                var newArray = new string[settings.StopSequences.Length + 1];
                for (int i = 0; i < settings.StopSequences.Length; i++)
                    newArray[i] = settings.StopSequences[i];
                newArray[settings.StopSequences.Length] = "";
                settings.StopSequences = newArray;
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the validation warnings
        /// </summary>
        /// <param name="settings">The settings to draw the validation warnings for</param>
        private void DrawValidationWarnings(UnityBrainSettings settings)
        {
            bool hasWarnings = false;
            var warnings = new System.Collections.Generic.List<string>();

            if (string.IsNullOrEmpty(settings.ExecutablePath))
            {
                warnings.Add("Executable Path is not set");
                hasWarnings = true;
            }

            if (string.IsNullOrEmpty(settings.ModelPath))
            {
                warnings.Add("Model Path is not set");
                hasWarnings = true;
            }

            if (settings.Port <= 0 || settings.Port > 65535)
            {
                warnings.Add("Port must be between 1 and 65535");
                hasWarnings = true;
            }

            if (settings.ContextSize <= 0)
            {
                warnings.Add("Context Size must be greater than 0");
                hasWarnings = true;
            }

            if (settings.MaxTokens <= 0)
            {
                warnings.Add("Max Tokens must be greater than 0");
                hasWarnings = true;
            }

            if (hasWarnings)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Configuration Issues:", MessageType.Warning);
                foreach (var warning in warnings)
                {
                    EditorGUILayout.HelpBox($"• {warning}", MessageType.Warning);
                }
            }
        }
    }   

}
#endif