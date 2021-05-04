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

            if (component.IsReady)
            {
                EditorGUILayout.LabelField("Peer", component.State.Peer);
                EditorGUILayout.LabelField("Last Message", component.State.LastMessageReceived);
                EditorGUILayout.LabelField("State", component.State.SignalingState.ToString());
                EditorGUILayout.LabelField("Connection", component.State.ConnectionState.ToString());
                EditorGUILayout.LabelField("Ice", component.State.IceState.ToString());
            }
        }
    }
}