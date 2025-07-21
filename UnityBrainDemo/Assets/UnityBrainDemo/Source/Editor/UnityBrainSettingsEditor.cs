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
        public override void OnInspectorGUI()
        {
            var settings = (UnityBrainSettings)target;

            EditorGUILayout.LabelField("UnityBrain Settings", EditorStyles.boldLabel);
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