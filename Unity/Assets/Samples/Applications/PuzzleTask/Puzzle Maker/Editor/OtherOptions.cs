using UnityEngine;
using UnityEditor;
using System.Collections;

public class OtherOptions : MonoBehaviour {


    [MenuItem("Window/PuzzleMaker/Documentation")]
    static void GotoDocumentation()
    {
        Application.OpenURL(@"http://tiny.cc/puzzlemaker3docs");
    }

}
