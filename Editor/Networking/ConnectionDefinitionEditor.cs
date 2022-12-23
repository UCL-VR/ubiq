using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Ubiq.Networking
{
    [CustomEditor(typeof(ConnectionDefinition))]
    public class ConnectionDefinitionEditor : Editor
    {
        private ReorderableList platformsReorderableList;
        private SerializedProperty platformsProperty;

        private void OnEnable()
        {
            platformsProperty = serializedObject.FindProperty("platforms");
            platformsReorderableList = new ReorderableList(serializedObject, platformsProperty, false, true, true, true);
            platformsReorderableList.drawElementCallback += DrawElementCallback;
            platformsReorderableList.elementHeightCallback += ElementHeightCallbackDelegate;
            platformsReorderableList.onAddCallback += AddCallbackDelegate;
            platformsReorderableList.drawHeaderCallback += HeaderCallbackDelegate;
            platformsReorderableList.onRemoveCallback += RemoveCallbackDelegate;
        }

        void HeaderCallbackDelegate(Rect rect)
        {
            EditorGUI.LabelField(rect, "Platform Overrides");
        }

        void RemoveCallbackDelegate(ReorderableList list)
        {
            var serialisedProperty = list.serializedProperty.GetArrayElementAtIndex(list.index);
            var obj = serialisedProperty.FindPropertyRelative("connection").objectReferenceValue;
            AssetDatabase.RemoveObjectFromAsset(obj);
            Object.DestroyImmediate(obj, true);
            list.serializedProperty.DeleteArrayElementAtIndex(list.index);
        }

        void AddCallbackDelegate(ReorderableList list)
        {
            list.serializedProperty.InsertArrayElementAtIndex(0);
            var serialisedProperty = list.serializedProperty.GetArrayElementAtIndex(0);

            var definition = ScriptableObject.CreateInstance<ConnectionDefinition>(); // This will create a local definition which is saved/embedded in the scene.
            definition.name = "Platform Connection";

            AssetDatabase.AddObjectToAsset(definition, target);

            serialisedProperty.FindPropertyRelative("connection").objectReferenceValue = definition;
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            var serialisedProperty = platformsProperty.GetArrayElementAtIndex(index);
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, serialisedProperty.FindPropertyRelative("platform"));
            rect.y += rect.height;
            EditorGUI.PropertyField(rect, serialisedProperty.FindPropertyRelative("connection"));
        }

        float ElementHeightCallbackDelegate(int index)
        {
            if (index >= platformsProperty.arraySize)
            {
                return 0;
            }
            var property = platformsProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("connection"), false) + EditorGUIUtility.singleLineHeight;
        }

        public override void OnInspectorGUI()
        {
            var component = target as ConnectionDefinition;

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));

            switch (component.type)
            {
                case ConnectionType.WebSocket:
                case ConnectionType.TcpClient:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sendToIp"), new GUIContent("Ip"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sendToPort"), new GUIContent("Port"));
                    break;
                case ConnectionType.TcpServer:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("listenOnIp"), new GUIContent("Listen Ip"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("listenOnPort"), new GUIContent("Listen Port"));
                    break;
                case ConnectionType.UDP:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Send");
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sendToIp"), GUIContent.none);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sendToPort"), GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Recieve");
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("listenOnIp"), GUIContent.none);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("listenOnPort"), GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                    break;
                default:
                    break;
            }

            platformsReorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}