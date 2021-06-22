using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Ubiq.Samples
{
    [CustomEditor(typeof(RoomSceneManager))]
    public class RoomSceneManagerEditor : Editor
    {
        private int selected = -1;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as RoomSceneManager;
            var scenenames = EditorBuildSettings.scenes.Select(s => s.path).ToArray();

            EditorGUI.BeginChangeCheck();

            selected = EditorGUILayout.Popup("Change Scene", selected, scenenames);

            if(EditorGUI.EndChangeCheck())
            {
                component.ChangeScene(scenenames[selected]);
            }

            if (GUILayout.Button("Update Scene"))
            {
                component.UpdateRoomScene();
            }
        }
    }
}