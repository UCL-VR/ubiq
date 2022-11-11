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
           // base.OnInspectorGUI();

            var component = target as LogCollector;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("EventsFilter"));

            if (Application.isPlaying)
            {
                GUILayout.Label($"Mode: {component.Mode}");
            }

            var memoryInMb = (float)component.Memory / (1024 * 1024);
            var maxMemoryInMb = component.MaxMemory / (1024 * 1024);
            GUILayout.Label($"Memory: {memoryInMb} / {maxMemoryInMb} Mb");

            GUILayout.Label($"Written: {component.Written}");
            GUILayout.Label($"Id : {component.Id.ToString()}");
            GUILayout.Label($"Destination: {component.Destination.ToString()}");

            GUI.enabled = component.OnNetwork;

            if (!component.isPrimary)
            {

                if (GUILayout.Button("Start Collection"))
                {
                    component.StartCollection();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Collection"))
                {
                    component.StopCollection();
                }
            }

            if (GUILayout.Button("Ping Active Collector"))
            {
                component.Ping((response) =>
                    {
                        var message = $"Ping Response - Collector: {response.ActiveCollector} RTT: {response.EndTime - response.StartTime} Error: {response.Aborted}";
                        if(response.Aborted)
                        {
                            Debug.LogError(message);
                        }
                        else
                        {
                            Debug.Log(message);
                        }
                    }
                );
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