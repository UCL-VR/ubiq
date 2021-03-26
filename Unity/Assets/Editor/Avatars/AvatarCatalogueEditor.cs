using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Ubiq.Avatars
{
    [CustomEditor(typeof(AvatarCatalogue))]
    public class AvatarCatalogueEditor : Editor
    {
        private ReorderableList avatarList;

        private void OnEnable()
        {
            avatarList = new ReorderableList(serializedObject, serializedObject.FindProperty("prefabs"), true, false, true, true);
            avatarList.drawElementCallback = ElementCallbackDelegate;
        }

        void ElementCallbackDelegate(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.PropertyField(rect, avatarList.serializedProperty.GetArrayElementAtIndex(index));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            avatarList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}