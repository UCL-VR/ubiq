using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneChanger : MonoBehaviour {

    public Button btnSlidingPuzzle;
    public Button btnMemoryPuzzle;
    public Button btnPickAndPlace;
    public Button btnJoinPieces;

    private const string JoinPieces = "PM Join Pieces Example";
    private const string PickAndPlace = "PM Pick And Place Example";
    private const string MemoryPuzzle = "PM Memory Puzzle Example";
    private const string SlidingPieces = "PM Sliding Pieces Example";

	void Start () {

#if UNITY_EDITOR

        UnityEditor.EditorBuildSettingsScene[] Temp = UnityEditor.EditorBuildSettings.scenes;
        if (Temp.Length < 4)
        {
            Debug.LogError("Please add all example scenes in build settings");
            DisableAllButtons();
            return;
        }

#endif

	    //Disable button for current loaded scene
        switch (Application.loadedLevelName)
        {
            case JoinPieces:
                btnJoinPieces.gameObject.SetActive(false);
                break;

            case MemoryPuzzle:
                btnMemoryPuzzle.gameObject.SetActive(false);
                break;

            case PickAndPlace:
                btnPickAndPlace.gameObject.SetActive(false);
                break;

            case SlidingPieces:
                btnSlidingPuzzle.gameObject.SetActive(false);
                break;

            default:
                break;
        }

	}

    void DisableAllButtons()
    {
        btnSlidingPuzzle.gameObject.SetActive(false);
        btnMemoryPuzzle.gameObject.SetActive(false);
        btnPickAndPlace.gameObject.SetActive(false);
        btnJoinPieces.gameObject.SetActive(false);
    }

    public void GotoSlidingPuzzle()
    {
        Application.LoadLevel(SlidingPieces);
    }

    public void GotoPickAndPlace()
    {
        Application.LoadLevel(PickAndPlace);
    }

    public void GotoMemoryPuzzle()
    {
        Application.LoadLevel(MemoryPuzzle);
    }

    public void GotoJoinPieces()
    {
        Application.LoadLevel(JoinPieces);
    }



}
