using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Ubiq.Samples
{
    [CustomEditor(typeof(PrefabCatalogue))]
    public class PrefabCatalogueEditor : Editor
    {
        private ReorderableList prefabsReorderableList;

        private void OnEnable()
        {
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
            EditorGUI.PropertyField(rect, prefabsReorderableList.serializedProperty.GetArrayElementAtIndex(index));
        }
    }
}