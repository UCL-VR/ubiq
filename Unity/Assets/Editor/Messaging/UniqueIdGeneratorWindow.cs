using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UniqueIdGeneratorWindow : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Ubiq/Id Generator")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        UniqueIdGeneratorWindow window = (UniqueIdGeneratorWindow)EditorWindow.GetWindow(typeof(UniqueIdGeneratorWindow));
        window.Show();
    }

    string Ids = "";

    void OnGUI()
    {
        if(GUILayout.Button("Generate"))
        {
            Ids += IdGenerator.GenerateUnique().ToString() + "\n";
        }

        Ids = EditorGUILayout.TextArea(Ids);
    }
}