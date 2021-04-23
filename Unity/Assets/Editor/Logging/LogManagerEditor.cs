using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ubiq.Logging
{
    [CustomEditor(typeof(LogManager))]
    public class LogManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var component = target as LogManager;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Mode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Listen"));
            serializedObject.ApplyModifiedProperties();

            GUILayout.Label($"Memory: { ToMB(component.Memory) }/{ ToMB(component.MaxMemory) } Mb");
        }

        private float ToMB(int bytes)
        {
            return bytes / 1024f / 1024f;
        }
    }
}