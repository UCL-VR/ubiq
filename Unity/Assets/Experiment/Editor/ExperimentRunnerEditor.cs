using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ExperimentRunner))]
public class ExperimentRunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var component = target as ExperimentRunner;

        if(GUILayout.Button("Clear Conditions"))
        {
            component.Conditions.Clear();
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        if(GUILayout.Button("Generate Conditions"))
        {
            int[] paddings = new int[] { 0, 500, 1500 };
            int[] updaterates = new int[] { 10, 60, 500, 1000 };

            component.Conditions.Clear();

            foreach (var padding in paddings)
            {
                foreach (var updaterate in updaterates)
                {
                    component.Conditions.Add(new ExperimentRunner.Condition()
                    {
                        Padding = padding,
                        UpdateRate = updaterate,
                        ManageRooms = true
                    });
                }
            }

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        if(GUILayout.Button("Begin"))
        {
            component.Begin();
        }
    }
}
