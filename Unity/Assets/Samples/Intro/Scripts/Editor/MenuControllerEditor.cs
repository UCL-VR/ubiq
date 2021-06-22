using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Ubiq.Samples
{
    [CustomEditor(typeof(MenuController))]
    public class MenuControllerEditor : Editor
    {
        private ReorderableList pagesList;

        private void OnEnable()
        {
            pagesList = new ReorderableList(serializedObject, serializedObject.FindProperty("pages"), true, false, true, true);
            pagesList.drawElementCallback = ElementCallbackDelegate;
            pagesList.elementHeight = EditorGUIUtility.singleLineHeight * 2f;
        }

        void ElementCallbackDelegate(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, pagesList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("button"));
            rect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, pagesList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("page"));

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            pagesList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}