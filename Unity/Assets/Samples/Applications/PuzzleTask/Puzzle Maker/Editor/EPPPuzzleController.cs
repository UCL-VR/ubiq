using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(PPPuzzleController))]
public class EPPPuzzleController : Editor {

    string[] FileNames = null;

    public void OnEnable()
    {
        if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
        {
            System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);
            AssetDatabase.Refresh();
        }

        FileNames = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/", "*.pm");
    }

    public override void OnInspectorGUI()
    {

        PPPuzzleController myTarget = (PPPuzzleController)target;

        EditorGUILayout.Space();

        myTarget.PiecesDisplayMode =  (EPieceDisplayMode) EditorGUILayout.EnumPopup("Pieces Display Mode", myTarget.PiecesDisplayMode);

        myTarget.UseFilePath = EditorGUILayout.Toggle("Use file", myTarget.UseFilePath);

        if (myTarget.UseFilePath)
        {
            //Populate files data in combobox

            if (FileNames == null)
            {
                EditorGUILayout.HelpBox("Currently no file present, please create file using PMFileCreator from Window->PuzzleMaker->CreatePMFile", MessageType.Error);
            }
            else if (FileNames.Length == 0)
            {
                EditorGUILayout.HelpBox("Currently no file present, please create file using PMFileCreator from Window->PuzzleMaker->CreatePMFile", MessageType.Error);
            }
            else if (FileNames.Length > 0)
            {

                GUIContent[] _contentList = new GUIContent[FileNames.Length];

                for (int i = 0; i < FileNames.Length; i++)
                {
                    _contentList[i] = new GUIContent(System.IO.Path.GetFileName(FileNames[i]));
                }

                myTarget._selectedFileIndex = EditorGUILayout.Popup(new GUIContent("PM File"), myTarget._selectedFileIndex, _contentList);

                myTarget.PMFilePath = FileNames[myTarget._selectedFileIndex];
            }

        }
        else
        {
            base.OnInspectorGUI();
        }


        EditorGUILayout.Space();
        myTarget.AnimateColorOnWrongPiece = EditorGUILayout.Toggle("AnimateColorOnWrongPiece ", myTarget.AnimateColorOnWrongPiece);

        if (myTarget.AnimateColorOnWrongPiece)
        {
            myTarget.WrongPieceAnimationColor = EditorGUILayout.ColorField("Animation Color", myTarget.WrongPieceAnimationColor);
            myTarget.NoOfTimesToBlink = EditorGUILayout.IntSlider("No Of Times To Blink", myTarget.NoOfTimesToBlink,
                    1, 5);
            myTarget.BlinkSpeed = ((float)EditorGUILayout.IntSlider("Effect speed", (int)(myTarget.BlinkSpeed * 100f), 1, 300)) / 100;
        }


        EditorGUILayout.Space();
        myTarget.ShufflePieces = EditorGUILayout.Toggle("Shuffle pieces ", myTarget.ShufflePieces);
        myTarget.ActualImageOnPuzzleComplete = EditorGUILayout.Toggle("Actual Image On Puzzle Complete", myTarget.ActualImageOnPuzzleComplete);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Leave empty if you dont want to play audio", MessageType.Info);
        myTarget.PiecePlacedSound = (AudioClip)EditorGUILayout.ObjectField("Piece Placed Sound ", myTarget.PiecePlacedSound, typeof(AudioClip));
        myTarget.WrongPlacementSound = (AudioClip)EditorGUILayout.ObjectField("Wrong Placement Sound ", myTarget.WrongPlacementSound, typeof(AudioClip));
        myTarget.PiecePickupSound = (AudioClip)EditorGUILayout.ObjectField("Piece Pickup Sound ", myTarget.PiecePickupSound, typeof(AudioClip));
        myTarget.PuzzleCompletionSound = (AudioClip)EditorGUILayout.ObjectField("Puzzle Completion Sound ", myTarget.PuzzleCompletionSound, typeof(AudioClip));
        myTarget.BackgroundMusic = (AudioClip)EditorGUILayout.ObjectField("Background Music ", myTarget.BackgroundMusic, typeof(AudioClip));

        myTarget.MusicVolume = EditorGUILayout.Slider("Music Volume ", myTarget.MusicVolume, 0, 1);
        myTarget.SFXVolume = EditorGUILayout.Slider("SFX Volume ", myTarget.SFXVolume, 0, 1);

        


        EditorUtility.SetDirty(target);

    }

	
}
