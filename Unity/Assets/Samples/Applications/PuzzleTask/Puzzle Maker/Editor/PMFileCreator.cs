using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PuzzleMaker;


public class PMFileCreator : EditorWindow {

    EFileCreateOptions _fileOptions = EFileCreateOptions.CreatePuzzleFile;
    bool _saveGeneratedBackground = false;

    string _pmFileName = "PMFile.pm";

    Texture2D _selectedTexture;
    Texture2D[] _pieceJointMaskImages = new Texture2D[1];
    int _piecesInRow = 3;
    int _piecesInCol = 3;

    int _noOfJointMasks = 0;


    [MenuItem("Window/PuzzleMaker/CreatePMFile")]
    static void Init ()
    {
        if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
        {
            System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);
            AssetDatabase.Refresh();
        }

        // Get existing open window or if none, make a new one:
		PMFileCreator window = (PMFileCreator)EditorWindow.GetWindow (typeof (PMFileCreator));

        if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
            System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);
        
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.Separator();

        GUIStyle helpStyle = new GUIStyle(GUI.skin.label);
        helpStyle.wordWrap = true;
        helpStyle.alignment = TextAnchor.UpperLeft;
        EditorGUILayout.LabelField("This window can be use to create Puzzle Maker file that can be used to load puzzle maker class data from file, or you can save only puzzle pieces images created by puzzle maker", helpStyle);

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        _piecesInRow = EditorGUILayout.IntSlider("Pieces in row : ",_piecesInRow, 2, 10);

        _piecesInCol = EditorGUILayout.IntSlider("Pieces in col : ", _piecesInCol, 2, 10);

        _selectedTexture = (Texture2D)EditorGUILayout.ObjectField("Puzzle Image", _selectedTexture, typeof(Texture2D), false);

        EditorGUILayout.Separator();

        _noOfJointMasks = EditorGUILayout.IntField("Joints Mask Images", _noOfJointMasks);

        if (_pieceJointMaskImages.Length != _noOfJointMasks)
            _pieceJointMaskImages = new Texture2D[_noOfJointMasks];

        for ( int i=0; i<_noOfJointMasks; i++ )
            _pieceJointMaskImages[i] = (Texture2D)EditorGUILayout.ObjectField("Joint Mask Image " + (i+1).ToString(), 
                                            _pieceJointMaskImages[i], typeof(Texture2D), false);

        EditorGUILayout.Separator();

        _fileOptions = (EFileCreateOptions)EditorGUILayout.EnumPopup("File Options", _fileOptions);

        if (_fileOptions == EFileCreateOptions.SavePiecesImage)
            _saveGeneratedBackground = EditorGUILayout.Toggle("Save Created Background Image : ", _saveGeneratedBackground);
        else
            _pmFileName = EditorGUILayout.TextField("Enter PM Filename" , _pmFileName);

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();



        if ( GUILayout.Button(_fileOptions == EFileCreateOptions.CreatePuzzleFile ? "Generate File" : "Save Pieces Images", GUILayout.Height(45)) )
        {
            bool JointMaskNull = false;

            for (int i = 0; i < _pieceJointMaskImages.Length; i++)
            {
                if (_pieceJointMaskImages[i] == null)
                {
                    JointMaskNull = true;
                    break;
                }
            }


            if (JointMaskNull)
            {
                EditorUtility.DisplayDialog("Error", "You must select all Joint Mask Images", "OK");
            }
            else if (_selectedTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "You must select puzzle image", "OK");
            }
            else
            {
                if (_fileOptions == EFileCreateOptions.CreatePuzzleFile)
                {
                    //string FilePath = EditorUtility.SaveFilePanel("Select file path", "c:\\", "PmFile.pm", "pm");

                    if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
                        System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);

                    if (_pmFileName == "" || _pmFileName.Length <= 3)
                    {
                        EditorUtility.DisplayDialog("Filename Error", "Filename cannot be empty", "Let me change");
                    }
                    else
                    {
                        Debug.Log(_pmFileName.Substring(_pmFileName.Length - 3, 3));

                        string FileName = _pmFileName.Substring(_pmFileName.Length - 3, 3) != ".pm" ?
                                                    _pmFileName + ".pm" : _pmFileName;

                        //Check if file already exists
                        if (System.IO.File.Exists(Application.streamingAssetsPath + "/" + FileName))
                        {
                            EditorUtility.DisplayDialog("File creation error", "File with name" + FileName + " already exists", "Let me change");
                        }
                        else
                        {
                            try
                            {
                                PuzzlePieceMaker Temp = new PuzzlePieceMaker(_selectedTexture, _pieceJointMaskImages,
                                                                    _piecesInRow, _piecesInCol);

                                Temp.SaveData(Application.streamingAssetsPath + "/" + FileName);

                                Debug.Log(FileName);

                                EditorUtility.DisplayDialog("Success", "File created succesfully", "Ok");
                                AssetDatabase.Refresh();
                            }
                            catch(UnityException ex)
                            {
                                EditorUtility.DisplayDialog("Failed", "File creation failed :" + ex.Message, "Ok");
                            }
                        }
                    }

                    
                }
                else if ( _fileOptions == EFileCreateOptions.SavePiecesImage )
                {

                    string FolderPath = EditorUtility.SaveFolderPanel("Select folder path", "c:\\PiecesData", "PiecesData");

                    if (FolderPath != "")
                    {
                        PuzzlePieceMaker Temp = new PuzzlePieceMaker(_selectedTexture, _pieceJointMaskImages,
                                                            _piecesInRow, _piecesInCol);

                        for (int RowTrav = 0; RowTrav < _piecesInCol; RowTrav++)
                        {
                            for (int ColTrav = 0; ColTrav < _piecesInRow; ColTrav++)
                            {
                                byte[] Epng = Temp._CreatedImagePiecesData[RowTrav, ColTrav].PieceImage.EncodeToPNG();
                                System.IO.File.WriteAllBytes(FolderPath + "\\Piece" + RowTrav.ToString() + "_" + 
                                                                ColTrav.ToString() + ".png", Epng);
                            }
                        }

                        if (_saveGeneratedBackground)
                        {
                            byte[] Epng = Temp.CreatedBackgroundImage.EncodeToPNG();
                            System.IO.File.WriteAllBytes(FolderPath + "\\Background Image.png", Epng);
                        }

                    }


                }
            }


        }
        

        EditorGUILayout.EndVertical();
    }

}

enum EFileCreateOptions
{
    CreatePuzzleFile,
    SavePiecesImage
}