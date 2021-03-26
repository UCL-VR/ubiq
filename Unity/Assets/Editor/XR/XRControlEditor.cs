using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ubiq.XR
{
    [CustomEditor(typeof(XRControl))]
    public class XRControlEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Initialise XR"))
            {
                (target as XRControl).InitialiseXR();
            }
        }
    }
}