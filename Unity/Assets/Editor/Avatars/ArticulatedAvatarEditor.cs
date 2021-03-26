using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;

namespace Ubiq.Avatars
{
    [CustomEditor(typeof(ArticulatedAvatar))]
    public class ArticulatedAvatarEditor : Editor
    {
        private bool bonesFoldout;

        // https://sandordaemen.nl/blog/unity-3d-extending-the-editor-part-3/

        private ReorderableList bonesReorderableList;
        private SerializedProperty bonesProperty;

        private void OnEnable()
        {
            bonesProperty = serializedObject.FindProperty("bones");
            bonesReorderableList = new ReorderableList(serializedObject, bonesProperty, true, true, true, true);
            bonesReorderableList.drawHeaderCallback = DrawHeaderCallback;
            bonesReorderableList.drawElementCallback = DrawElementCallback;
            bonesReorderableList.elementHeightCallback += ElementHeightCallback;
            bonesReorderableList.onAddCallback += OnAddCallback;
            bonesReorderableList.onRemoveCallback += OnRemoveCallback;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bonesFoldout = EditorGUILayout.Foldout(bonesFoldout, "Bones " + bonesProperty.arraySize);
            if(bonesFoldout)
            {
                bonesReorderableList.DoLayoutList();
            }

            if (GUILayout.Button("Initialise Bones From SkinnedMeshRenderer"))
            {
                AddSkinnedMeshRendererBones(serializedObject);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static void AddSkinnedMeshRendererBones(SerializedObject serializedObject)
        {
            var avatar = serializedObject.targetObject as ArticulatedAvatar;

            List<Transform> bones = new List<Transform>();
            foreach (var item in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                bones.AddRange(item.bones);
            }
            bones = bones.Distinct().ToList();

            var array = serializedObject.FindProperty("bones");
            array.ClearArray();

            for (int i = 0; i < bones.Count; i++)
            {
                array.InsertArrayElementAtIndex(i);
                array.GetArrayElementAtIndex(i).objectReferenceValue = bones[i];
            }
        }

        private void DrawHeaderCallback(Rect rect)
        {
           // EditorGUI.LabelField(rect, "Bones");
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            //Get the element we want to draw from the list.
            SerializedProperty element = bonesReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;

            //We get the name property of our element so we can display this in our list.
            SerializedProperty elementName = element.FindPropertyRelative("name");

            //Draw the list item as a property field, just like Unity does internally.
            EditorGUI.PropertyField(
                new Rect(rect.x += 10, rect.y, Screen.width * .8f, EditorGUIUtility.singleLineHeight),
                element,
                new GUIContent(""),
                true);
        }

        private float ElementHeightCallback(int index)
        {
            //Gets the height of the element. This also accounts for properties that can be expanded, like structs.
            float propertyHeight = EditorGUI.GetPropertyHeight(bonesReorderableList.serializedProperty.GetArrayElementAtIndex(index), true);
            float spacing = EditorGUIUtility.singleLineHeight / 2;

            return propertyHeight + spacing;
        }

        private void OnAddCallback(ReorderableList list)
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            // https://answers.unity.com/questions/555724/serializedpropertydeletearrayelementatindex-leaves.html
            if (list.serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue != null)
            {
                list.serializedProperty.DeleteArrayElementAtIndex(list.index);
            }

            list.serializedProperty.DeleteArrayElementAtIndex(list.index);
            list.index = -1;
        }
    }
}