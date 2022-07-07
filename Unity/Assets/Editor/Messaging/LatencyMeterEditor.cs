using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.Messaging
{
    [CustomEditor(typeof(LatencyMeter))]
    public class LatencyMeterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            
            var component = target as LatencyMeter;
            if (GUILayout.Button("Measure Latencies"))
            {
                component.MeasurePeerLatencies();
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}