#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityBrainDemo.Runtime.Core;

namespace UnityBrainDemo.Editor
{
    [CustomEditor(typeof(LlamaCppSettings))]
    public class UnityBrainSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = (LlamaCppSettings)target;

            EditorGUILayout.LabelField("Llama.cpp Backend Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            settings.ExecutablePath = EditorGUILayout.TextField("Executable Path", settings.ExecutablePath);
            settings.ModelPath = EditorGUILayout.TextField("Model Path", settings.ModelPath);
            settings.Port = EditorGUILayout.IntField("Port", settings.Port);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
            }
        }
    }   

}
#endif