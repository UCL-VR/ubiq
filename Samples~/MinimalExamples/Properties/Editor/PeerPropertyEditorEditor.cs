using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Ubiq.Samples.Single.Properties
{
    [CustomEditor(typeof(PeerPropertyEditor))]
    public class PeerPropertyEditorEditor : Editor
    {
        ReorderableList propertiesControl;
        PeerPropertyEditor propertiesList;
        string newKey = "key";
        string newValue = "value";

        private void OnEnable()
        {
            propertiesList = (target as PeerPropertyEditor);
            propertiesControl = new ReorderableList(target as PeerPropertyEditor, typeof(KeyValuePair<string, string>));
            propertiesControl.drawElementCallback = DrawElement;
            propertiesControl.displayAdd = false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            propertiesControl.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            newKey = EditorGUILayout.TextField(newKey);
            newValue = EditorGUILayout.TextField(newValue);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add"))
            {
                propertiesList[newKey] = newValue;
                newKey = "key";
                newValue = "value";
            }
        }

        void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var pair = propertiesList[index];

            var value = EditorGUI.TextField(new Rect(
                rect.x,
                rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                rect.width,
                EditorGUIUtility.singleLineHeight
                ), 
                pair.Key, pair.Value);

            if(value != pair.Value)
            {
                propertiesList[pair.Key] = value;
            }
        }
    }
}