using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Ubiq.Spawning
{
    [CustomEditor(typeof(PrefabCatalogue))]
    public class PrefabCatalogueEditor : Editor
    {
        private ReorderableList prefabsReorderableList;
        // private SerializedProperty uuids;
        // private SerializedProperty prefabs;

        private void OnEnable()
        {
            // Debug.Log("im enabled!");
            // uuids = serializedObject.FindProperty("uuids");
            // prefabs = serializedObject.FindProperty("prefabs");

            prefabsReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("prefabs"), true, true, true, true);
            prefabsReorderableList.drawElementCallback = ElementCallback;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            prefabsReorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void ElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            // var rectA = new Rect(rect.x,                     rect.y, rect.width * 0.5f, rect.height);
            // var rectB = new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, rect.height);
            // EditorGUI.PropertyField(rectA, uuids.GetArrayElementAtIndex(index), GUIContent.none);
            // EditorGUI.PropertyField(rectB, prefabs.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.PropertyField(rect, prefabsReorderableList.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);
        }
    }
}