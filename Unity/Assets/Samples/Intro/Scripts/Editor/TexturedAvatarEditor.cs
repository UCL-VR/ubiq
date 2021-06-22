using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.Samples
{
    [CustomEditor(typeof(TexturedAvatar))]
    public class TexturedAvatarEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if(GUILayout.Button("Clear Saved Settings"))
            {
                (target as TexturedAvatar).ClearSettings();
            }
        }
    }
}
