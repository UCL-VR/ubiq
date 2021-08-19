using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MPPuzzleController))]
public class EMPPuzzleController : Editor {

    //flipped rows with cols for different screen sizes
    private bool _swapRowsCol = false;


    public override void OnInspectorGUI()
    {

        MPPuzzleController myTarget = (MPPuzzleController)target;

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Flipped piece image will be shown when the piece is not selected open by user.", MessageType.None);

        EditorGUIUtility.LookLikeInspector();
        myTarget.FlippedPieceImage =
            (Texture2D)EditorGUILayout.ObjectField("FlippedPieceImage", myTarget.FlippedPieceImage, typeof(Texture2D), false);
        EditorGUIUtility.LookLikeControls();


        int ChngNoPiecesToFind = myTarget.NoPiecesToFind;
        int ChngTotalTypes = myTarget.TotalTypes;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Total pieces a user will have to flip open  for matches to be successfull.", MessageType.None);
        myTarget.NoPiecesToFind = EditorGUILayout.IntSlider("NoPiecesToFind", myTarget.NoPiecesToFind, 2, 5);


        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Total different types of images hidden in this puzzle", MessageType.None);
        myTarget.TotalTypes = EditorGUILayout.IntSlider("TotalPieceTypes", myTarget.TotalTypes, 2, 10);

        if (myTarget.HiddenImages.Length != myTarget.TotalTypes)
        {
            Texture2D[] prevImages = myTarget.HiddenImages;
            myTarget.HiddenImages = new Texture2D[myTarget.TotalTypes];
            
            //Get previous images back
            int Range = prevImages.Length > myTarget.HiddenImages.Length ? myTarget.HiddenImages.Length : prevImages.Length;

            for (int i = 0; i < Range; i++)
            {
                myTarget.HiddenImages[i] = prevImages[i];
            }
            
        }

        //Get these images
        bool ImagesNotNull = true;

        EditorGUIUtility.LookLikeInspector();
        for (int i = 0; i < myTarget.TotalTypes; i++)
        {
            myTarget.HiddenImages[i] = (Texture2D)EditorGUILayout.ObjectField("Hidden Image " + (i + 1).ToString(),
                                            myTarget.HiddenImages[i], typeof(Texture2D), false);

            ImagesNotNull = myTarget.HiddenImages[i] != null && ImagesNotNull;
        }
        EditorGUIUtility.LookLikeControls();

        if (!ImagesNotNull)
            EditorGUILayout.HelpBox("You must provide all hidden images", MessageType.Error);


        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Swap TotalRows And cols for final puzzle which will be created", MessageType.None);
        bool TempIsFlip = _swapRowsCol;
        _swapRowsCol = EditorGUILayout.Toggle("SwapRowCol", _swapRowsCol);
        if (_swapRowsCol != TempIsFlip)
        {
            int TempSVar = myTarget.TotalRows;
            myTarget.TotalRows = myTarget.TotalCols;
            myTarget.TotalCols = TempSVar;
        }

        //Calculate total rows and cols in the grid according to previous selections
        if (ChngNoPiecesToFind != myTarget.NoPiecesToFind || ChngTotalTypes != myTarget.TotalTypes)
            FindClosestProduct(myTarget.NoPiecesToFind * myTarget.TotalTypes, myTarget.NoPiecesToFind, myTarget.TotalTypes,
                        out myTarget.TotalRows, out myTarget.TotalCols);


        //Display resulted total rows and cols
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Final puzzle will have " + myTarget.TotalRows + " rows and " + myTarget.TotalCols + " columns", MessageType.None);

        EditorGUILayout.Space();
        myTarget.AnimateColorOnWrongPiece = EditorGUILayout.Toggle("AnimateColorOnWrongPiece ", myTarget.AnimateColorOnWrongPiece);

        if (myTarget.AnimateColorOnWrongPiece)
        {
            myTarget.WrongPieceAnimationColor = EditorGUILayout.ColorField("Animation Color", myTarget.WrongPieceAnimationColor);
            myTarget.NoOfTimesToBlink = EditorGUILayout.IntSlider("No Of Times To Blink", myTarget.NoOfTimesToBlink,
                    1, 5);
            myTarget.BlinkSpeed = ((float)EditorGUILayout.IntSlider("Effect speed", (int)(myTarget.BlinkSpeed * 100f), 1, 300)) / 100;
        }

        //Audio editor setup
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Leave empty if you dont want to play audio", MessageType.Info);
        myTarget.CorrectPairOpened = (AudioClip)EditorGUILayout.ObjectField("Correct Pair Sound ", myTarget.CorrectPairOpened, typeof(AudioClip));
        myTarget.IncorrectPairOpened= (AudioClip)EditorGUILayout.ObjectField("Incorrect Pair Sound ", myTarget.IncorrectPairOpened, typeof(AudioClip));
        myTarget.PuzzleCompletionSound = (AudioClip)EditorGUILayout.ObjectField("Puzzle Completion Sound ", myTarget.PuzzleCompletionSound, typeof(AudioClip));
        myTarget.BackgroundMusic = (AudioClip)EditorGUILayout.ObjectField("Background Music ", myTarget.BackgroundMusic, typeof(AudioClip));

        myTarget.MusicVolume = EditorGUILayout.Slider("Music Volume ", myTarget.MusicVolume, 0, 1);
        myTarget.SFXVolume = EditorGUILayout.Slider("SFX Volume ", myTarget.SFXVolume, 0, 1);

        EditorUtility.SetDirty(target);
    }
    

    private void FindClosestProduct(int Number, int NoPiecesToFind, int TotalTypes, out int num1, out int num2)
    {
        num1 = NoPiecesToFind;
        num2 = TotalTypes;

        for (int i = 1; i <= Number; i++)
        {
            for (int j = 1; j <= Number; j++)
            {

                if (i * j == Number)
                {
                    if (Mathf.Abs(i - j) < Mathf.Abs(num1 - num2))
                    {
                        num1 = i;
                        num2 = j;
                    }
                }
            }
        }

    }


}
