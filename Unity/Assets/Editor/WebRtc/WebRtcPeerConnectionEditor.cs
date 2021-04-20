using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.WebRtc
{

    [CustomEditor(typeof(WebRtcPeerConnection))]
    public class WebRtcPeerConnectionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));

            serializedObject.ApplyModifiedProperties();

            var pc = target as WebRtcPeerConnection;

            if (GUILayout.Button("Negotiatiate"))
            {
                pc.OnRenegotiationNeeded();
            }

            if (GUILayout.Button("Add Local Audio Source"))
            {
                pc.AddLocalAudioSource();
            }

            var component = target as WebRtcPeerConnection;

            if (component.isReady)
            {
                EditorGUILayout.LabelField("Peer", component.stats.peer);
                EditorGUILayout.LabelField("Last Message", component.stats.lastMessageReceived);
                EditorGUILayout.LabelField("State", component.stats.signalingstate.ToString());
                EditorGUILayout.LabelField("Connection", component.stats.connectionstate.ToString());
            }
        }
    }
}