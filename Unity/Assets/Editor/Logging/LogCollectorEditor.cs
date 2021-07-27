using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.Logging
{
    [CustomEditor(typeof(LogCollector))]
    public class LogCollectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as LogCollector;

            GUILayout.Label($"Entries: {component.Count}");

            GUI.enabled = component.NetworkEnabled;

            EditorGUILayout.LabelField("Collecting", component.Collecting.ToString());

            if (GUILayout.Button("Start Collection"))
            {
                component.StartCollection();
            }

            if (GUILayout.Button("Stop Collection"))
            {
                component.StopCollection();
            }

            GUI.enabled = true;

            if (GUILayout.Button("Open Folder"))
            {
                var path = Application.persistentDataPath;
                path = path.Replace("/","\\");
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
        }
    }
}