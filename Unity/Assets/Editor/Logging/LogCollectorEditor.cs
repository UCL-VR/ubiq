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

            if (Application.isPlaying)
            {
                GUILayout.Label($"Mode: {component.Mode}");
            }

            var memoryInMb = (float)component.Memory / (1024 * 1024);
            var maxMemoryInMb = component.MaxMemory / (1024 * 1024);
            GUILayout.Label($"Memory: {memoryInMb} / {maxMemoryInMb} Mb");


            GUILayout.Label($"Written: {component.Written}");
            GUILayout.Label($"Id : {component.Id.ToString()}");

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