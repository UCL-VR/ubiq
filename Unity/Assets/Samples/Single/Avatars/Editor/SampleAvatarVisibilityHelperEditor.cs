using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.Samples
{
    [CustomEditor(typeof(SampleAvatarVisibilityHelper))]
    public class SampleAvatarVisibilityHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as SampleAvatarVisibilityHelper;

            if (GUILayout.Button("Show Player 1"))
            {
                component.ShowPlayer1();
            }
            if (GUILayout.Button("Hide Player 1"))
            {
                component.HidePlayer1();
            }
        }
    }
}