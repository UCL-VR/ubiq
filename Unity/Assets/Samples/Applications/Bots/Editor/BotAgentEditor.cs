using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.Samples.Bots
{
    [CustomEditor(typeof(BotAgent))]
    public class BotAgentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as BotAgent;

            if(GUILayout.Button("Wander"))
            {
                component.Wander();
            }
        }
    }
}