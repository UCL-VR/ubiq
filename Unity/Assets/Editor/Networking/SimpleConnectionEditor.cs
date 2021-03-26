using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ubiq.Networking;

[CustomEditor(typeof(SimpleConnection))]
public class SimpleConnectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("def.type"));
        serializedObject.ApplyModifiedProperties();

        var component = target as SimpleConnection;
        switch (component.def.type)
        {
            case ConnectionType.websocket:
            case ConnectionType.tcp_client:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("def.send_to_ip"), new GUIContent("Ip"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("def.send_to_port"), new GUIContent("Port"));
                break;
            case ConnectionType.tcp_server:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("def.listen_on_ip"), new GUIContent("Ip"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("def.listen_on_port"), new GUIContent("Port"));
                break;
            case ConnectionType.udp:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("def.listen_on_ip"), new GUIContent("Bind Ip"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("def.listen_on_port"), new GUIContent("Bind Port"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("def.send_to_ip"), new GUIContent("Send Ip"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("def.send_to_port"), new GUIContent("Send Port"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
