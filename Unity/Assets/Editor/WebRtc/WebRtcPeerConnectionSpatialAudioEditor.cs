using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.WebRtc
{
    [CustomEditor(typeof(WebRtcPeerConnectionSpatialAudio))]
    public class WebRtcPeerConnectionSpatialAudioEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.CurveField(
                serializedObject.FindProperty("falloff"),
                Color.red,
                new Rect(),
                GUILayout.Height(120)
                );

            serializedObject.ApplyModifiedProperties();
        }
    }
}