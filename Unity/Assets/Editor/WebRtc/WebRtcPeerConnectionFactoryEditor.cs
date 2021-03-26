using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditorInternal;

namespace Ubiq.WebRtc
{
    [CustomEditor(typeof(WebRtcPeerConnectionFactory))]
    public class WebRtcPeerConnectionFactoryEditor : Editor
    {
        private ReorderableList managerReorderableList;
        private SerializedProperty serversProperty;
        private SerializedProperty userProperty;
        private SerializedProperty passwordProperty;

        private void OnEnable()
        {
            serversProperty = serializedObject.FindProperty("turnServers");
            managerReorderableList = new ReorderableList(serializedObject, serversProperty, false, false, true, true);
            managerReorderableList.drawElementCallback += DrawElementCallback;
            managerReorderableList.drawHeaderCallback += HeaderCallbackDelegate;
            managerReorderableList.elementHeightCallback += ElementHeightCallbackDelegate;

            userProperty = serializedObject.FindProperty("turnUser");
            passwordProperty = serializedObject.FindProperty("turnPassword");
        }

        private void HeaderCallbackDelegate(Rect rect)
        {
            EditorGUI.LabelField(rect, "TURN Servers");
        }

        private float ElementHeightCallbackDelegate(int index)
        {
            float propertyHeight = EditorGUI.GetPropertyHeight(serversProperty.GetArrayElementAtIndex(index), true);
            float spacing = EditorGUIUtility.singleLineHeight / 2;
            return propertyHeight + spacing;
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            rect.y += EditorGUIUtility.singleLineHeight / 4;
            EditorGUI.PropertyField(rect, serversProperty.GetArrayElementAtIndex(index), new GUIContent(""), true);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            managerReorderableList.DoLayoutList();

            EditorGUILayout.PropertyField(userProperty,new GUIContent("Turn User"));
            EditorGUILayout.PropertyField(passwordProperty,new GUIContent("Turn Password"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.HelpBox("The WebRtcPeerConnectionFactory will be created on demand, but creating one ahead of time means initialisation will be performed at start-up, instead of run-time.", MessageType.Info);

            var factory = target as WebRtcPeerConnectionFactory;
            if (factory.ready)
            {
                factory.playoutHelper.Select(EditorGUILayout.Popup("Playout Device", factory.playoutHelper.selected, factory.playoutHelper.names.ToArray()));
                factory.recordingHelper.Select(EditorGUILayout.Popup("Recording Device", factory.recordingHelper.selected, factory.recordingHelper.names.ToArray()));
            }
        }
    }
}