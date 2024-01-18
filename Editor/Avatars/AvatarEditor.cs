using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ubiq.Avatars
{
    [CustomEditor(typeof(Avatar))]
    public class AvatarEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as Avatar;
            if (component.IsLocal)
            {
                EditorGUILayout.HelpBox("Local", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Remote", MessageType.Info);
            }
        }
    }
}