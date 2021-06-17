using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Ubiq.Samples
{
    [CustomEditor(typeof(AvatarMenuController))]
    public class AvatarMenuControllerEditor : Editor
    {
        private ReorderableList avatarList;

        private void OnEnable()
        {
            avatarList = new ReorderableList(serializedObject, serializedObject.FindProperty("avatars"), true, false, true, true);
            avatarList.drawHeaderCallback = HeaderCallbackDelegate;
            avatarList.drawElementCallback = ElementCallbackDelegate;
            avatarList.onSelectCallback = SelectCallbackDelegate;
        }

        void ElementCallbackDelegate(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.PropertyField(rect, avatarList.serializedProperty.GetArrayElementAtIndex(index));
        }

        void HeaderCallbackDelegate(Rect rect)
        {
            EditorGUI.LabelField(rect, "Avatar");
        }

        void SelectCallbackDelegate(ReorderableList list)
        {
            var component = target as AvatarMenuController;
            component.ChangeAvatar(list.serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue as GameObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manager"));
            avatarList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}