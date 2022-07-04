using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.Samples.UnitTests.Logging
{
    [CustomEditor(typeof(LoggingDiagnostics))]
    public class LoggingDiagnosticsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("Check Logs"))
            {
                var component = target as LoggingDiagnostics;
                component.AnalyseLogsInDefaultFolder();
            }
        }
    }
}