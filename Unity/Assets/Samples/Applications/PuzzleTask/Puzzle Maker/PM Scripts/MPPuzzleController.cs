using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MPPuzzleController : MonoBehaviour
{

#region "Public Variables"

    public Texture2D FlippedPieceImage;

    public int NoPiecesToFind = 2;
    public int TotalTypes = 3;
    public int TotalRows = 3;
    public int TotalCols = 3;
    
    public Texture2D[] HiddenImages;

    public bool AnimateColorOnWrongPiece = true;
    public Color WrongPieceAnimationColor = Color.red;
    public float BlinkSpeed = 0.1f;
    public int NoOfTimesToBlink = 2;


    //Music and sfx variables
    [HideInInspector]
    public AudioClip IncorrectPairOpened;
    [HideInInspector]
    public AudioClip CorrectPairOpened;
    [HideInInspector]
    public AudioClip PuzzleCompletionSound;
    [HideInInspector]
    public AudioClip BackgroundMusic;
    [HideInInspector]
    public float MusicVolume = 1;
    [HideInInspector]
    public float SFXVolume = 1;

#endregion

#region "Private Variables"

    private GameObject _masterPieceObj;
    private GameObject _spFrame;
    private GameObject[][] _pieceInstances;
    private MPPieceController[][] _pieceControllerInstances;
    private Vector3[][] _piecePositions;
    private MPPieceController _pieceControllers;

    private int _correctOpenedPieces = 0;

    private List<GameObject> _openedPieces = new List<GameObject>();

    //Holds audio source instances for audio playback
    private AudioSource _musicPlaybackSource;
    private AudioSource _sfxPlaybackSource;

    private const float PiecesSpacing = 0.01f;
    private const float PieceWidthHeightInWorld = 1f;


#endregion


    void Awake ()
    {
        _musicPlaybackSource = gameObject.AddComponent<AudioSource>();
        _musicPlaybackSource.playOnAwake = false;
        _musicPlaybackSource.loop = true;
        _musicPlaybackSource.volume = MusicVolume;
        _musicPlaybackSource.clip = BackgroundMusic;
        if (BackgroundMusic != null)
            _musicPlaybackSource.Play();

        _sfxPlaybackSource = gameObject.AddComponent<AudioSource>();
        _sfxPlaybackSource.playOnAwake = false;
        _sfxPlaybackSource.loop = false;
        _sfxPlaybackSource.volume = SFXVolume;
    }

	void Start () {
	    
        //Get master piece gameobject
        _masterPieceObj = gameObject.transform.Find("MPPiece").gameObject;

        //Seperate from cam for cam resizing to adjust whole puzzle in cam view
        Transform parentCamTransform = gameObject.transform.parent;
        Camera parentCam = parentCamTransform.GetComponent<Camera>();
        gameObject.transform.parent = null;

        //Find frame gameobject
        _spFrame = gameObject.transform.Find("SPFrameObj").gameObject;


#region "Generate Pieces"

        //Create Temporary queue with copies of images to be assigned
        List<Texture2D> TempHIList = new List<Texture2D>();
        for (int HPTrav = 0; HPTrav < HiddenImages.Length; HPTrav++)
            for (int PieceCopies = 0; PieceCopies < NoPiecesToFind; PieceCopies++)
                TempHIList.Add(HiddenImages[HPTrav]);


        _pieceInstances = new GameObject[TotalRows][];
        _pieceControllerInstances = new MPPieceController[TotalRows][];
        _piecePositions = new Vector3[TotalRows][];

        for (int Rowtrav = 0; Rowtrav < TotalRows; Rowtrav++)
        {
            _pieceInstances[Rowtrav] = new GameObject[TotalCols];
            _pieceControllerInstances[Rowtrav] = new MPPieceController[TotalCols];
            _piecePositions[Rowtrav] = new Vector3[TotalCols];


            for (int Coltrav = 0; Coltrav < TotalCols; Coltrav++)
            {
                //Instantiate new piece
                _pieceInstances[Rowtrav][Coltrav] = GameObject.Instantiate(_masterPieceObj) as GameObject;

                //Get piece controller
                MPPieceController TempPCInst = _pieceInstances[Rowtrav][Coltrav].GetComponent<MPPieceController>();
                _pieceControllerInstances[Rowtrav][Coltrav] = TempPCInst;

                //Name this piece instance
                _pieceInstances[Rowtrav][Coltrav].name = ArrIndexToPieceName(Rowtrav, Coltrav);

                //Make child of main gameobject
                _pieceInstances[Rowtrav][Coltrav].transform.parent = gameObject.transform;

                //Assign images
                TempPCInst.FlippedPieceImage = FlippedPieceImage;
                int SelectedPiece = Random.Range(0, TempHIList.Count);
                TempPCInst.HiddenPieceImage = TempHIList[SelectedPiece];
                TempHIList.RemoveAt(SelectedPiece);


                //Scale piece
                _pieceInstances[Rowtrav][Coltrav].transform.localScale = new Vector3(PieceWidthHeightInWorld, PieceWidthHeightInWorld, 1);


                //Place in position
                float PositionX = //gameObject.transform.position.x + 
                    (Coltrav * PieceWidthHeightInWorld) + (Coltrav * PiecesSpacing);
                float PositionY = //gameObject.transform.position.y + 
                    (Rowtrav * PieceWidthHeightInWorld) + (Rowtrav * PiecesSpacing);

                Vector3 CalcPosition = new Vector3(PositionX, PositionY, 0);

                _pieceInstances[Rowtrav][Coltrav].transform.localPosition = CalcPosition;

                _piecePositions[Rowtrav][Coltrav] = CalcPosition;



                //Enable instance
                _pieceInstances[Rowtrav][Coltrav].SetActive(true);

            }
        }

#endregion

        //Resize camera to display whole puzzle in camera view
        float widthToBeSeen = TotalCols * PieceWidthHeightInWorld + (PiecesSpacing * (TotalCols - 1));
        float heightToBeSeen = TotalRows * PieceWidthHeightInWorld + (PiecesSpacing * (TotalRows - 1));

        parentCam.orthographicSize = widthToBeSeen > heightToBeSeen ? widthToBeSeen * 0.4f : heightToBeSeen * 0.4f;

        //Position camera in centre of puzzle
        float CalcCameraX = ((_pieceInstances[1][TotalCols - 1].transform.position.x -
            _pieceInstances[1][0].transform.position.x) / 2) + _pieceInstances[1][0].transform.position.x;
        float CalcCameraY = ((_pieceInstances[TotalRows - 1][0].transform.position.y -
            _pieceInstances[0][0].transform.position.y) / 2) + _pieceInstances[0][0].transform.position.y;

        parentCamTransform.position = new Vector3(CalcCameraX, CalcCameraY, _pieceInstances[1][0].transform.position.z - 3);



        //Resize Frame
        float xScaleOfFrame = (_pieceInstances[0][0].transform.localScale.x * TotalCols) * 1.10f + (PiecesSpacing * (TotalCols - 1));
        float yScaleOfFrame = (_pieceInstances[0][0].transform.localScale.y * TotalRows) * 1.15f + (PiecesSpacing * (TotalRows - 1));
        _spFrame.transform.localScale = new Vector3(xScaleOfFrame, yScaleOfFrame, 1);

        //Position Frame
        float xPosFrame = parentCamTransform.position.x;
        float yPosFrame = parentCamTransform.position.y;
        float zPosFrame = parentCamTransform.position.z + 1;
        _spFrame.transform.position = new Vector3(xPosFrame, yPosFrame, zPosFrame);

        _spFrame.SetActive(true);

	}

    void Update() 
    {

        if (Input.GetMouseButtonDown(0))
        {

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 30f))
            {
                if (hit.transform.name.Contains("Piece"))
                {
                    MPPieceController Temp = hit.transform.GetComponent<MPPieceController>();

                    if (Temp.IsPieceHidden)
                    {
                        _openedPieces.Add(hit.transform.gameObject);

                        Temp.FlipPiece();

                        if (_openedPieces.Count == NoPiecesToFind)
                        {
                            if (MatchOpenedPieces())
                            {
                                _correctOpenedPieces += _openedPieces.Count;

                                //Check for all pieces opened correctly
                                if (_correctOpenedPieces == TotalCols * TotalRows)
                                {
                                    PlaySFXSound(PuzzleCompletionSound);
                                    OnAllPiecesOpened();
                                }
                                else
                                {
                                    OnMatchedPiecesOpened();
                                    PlaySFXSound(CorrectPairOpened);
                                }
                            }
                            else
                            {
                                if ( AnimateColorOnWrongPiece )
                                    GetComponent<ColorAnimator>().StartEffect(Temp.gameObject, WrongPieceAnimationColor, 
                                         NoOfTimesToBlink, BlinkSpeed);

                                CloseOpenedPieces();


                                PlaySFXSound(IncorrectPairOpened);
                                OnWrongPiecesOpened();
                            }

                            _openedPieces.Clear();
                        }
                    }

                }

                
            }

        }

	}

    void OnGUI () {

    }


    /// <summary>
    /// Matches opened pieces in opened pieces list.
    /// </summary>
    /// <returns>Returns true if all pieces match</returns>
    private bool MatchOpenedPieces()
    {
        for (int i = 0; i < _openedPieces.Count - 1; i++)
        {
            MPPieceController Controller1 = _openedPieces[i].gameObject.GetComponent<MPPieceController>();
            MPPieceController Controller2 = _openedPieces[i+1].gameObject.GetComponent<MPPieceController>();

            if (!Object.ReferenceEquals(Controller1.HiddenPieceImage, Controller2.HiddenPieceImage))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Use to close all the pieces opened wrong
    /// </summary>
    private void CloseOpenedPieces()
    {
        int i = 0;
        for (i = 0; i < _openedPieces.Count; i++)
        {
            MPPieceController Controller = _openedPieces[i].gameObject.GetComponent<MPPieceController>();
            Controller.FlipPiece();
            Controller.HideOnShow();
        }

    }

    /// <summary>
    /// Merges provided row and column number to form piece name
    /// </summary>
    /// <param name="Row">Row number to be inserted in name . Value should be greate then -1</param>
    /// <param name="Col">Col number to be inserted in name . Value should be greate then -1</param>
    /// <returns></returns>
    private string ArrIndexToPieceName(int Row, int Col)
    {
        return "MPPiece" + Row.ToString() + "_" + Col.ToString();
    }

    private void PlaySFXSound(AudioClip Clip)
    {
        if (Clip != null)
            _sfxPlaybackSource.PlayOneShot(Clip);
    }



    /// <summary>
    /// Called when all the wrong pieces are opened
    /// </summary>
    public void OnWrongPiecesOpened()
    {
        Debug.Log("Wrong Pieces Opened");
    }

    /// <summary>
    /// Called when last piece with matching to previous pieces is opened
    /// </summary>
    public void OnMatchedPiecesOpened()
    {
        Debug.Log("Correct Pieces Opened");
    }

    /// <summary>
    /// Called when all pieces are opened
    /// </summary>
    public void OnAllPiecesOpened()
    {
        Debug.Log("All Pieces Opened");
    }


#region "Methods Which Gives Control On Puzzle Controls"

    public void ToggleSFX(bool value)
    {
        _sfxPlaybackSource.mute = !value;
    }

    public void ToggleMusic(bool value)
    {
        _musicPlaybackSource.mute = !value;
    }

    /// <summary>
    /// Resets puzzle to start
    /// </summary>
    public void Reset()
    {
        //Close all opened pieces
        for (int rowTrav = 0; rowTrav < TotalRows; rowTrav++)
        {
            for (int colTrav = 0; colTrav < TotalCols; colTrav++)
            {
                MPPieceController ThisPieceController = _pieceControllerInstances[rowTrav][colTrav];
                if (!ThisPieceController.IsPieceHidden)
                    ThisPieceController.FlipPiece();
            }
        }

        _openedPieces.Clear();
        _correctOpenedPieces = 0;


        //Reposition pieces places randomly
        for (int rowTrav = 0; rowTrav < TotalRows; rowTrav++) //Shuffle every row
        {
            for (int colTrav = 0; colTrav < TotalCols; colTrav++)
            {
                int r = Random.Range(0, TotalCols);

                //Swap positions
                Vector3 TempPosition = _pieceInstances[rowTrav][colTrav].transform.position;
                _pieceInstances[rowTrav][colTrav].transform.position = _pieceInstances[rowTrav][r].transform.position;
                _pieceInstances[rowTrav][r].transform.position = TempPosition;

                //Swap inside arrays
                GameObject TempGameobject = _pieceInstances[rowTrav][colTrav];
                _pieceInstances[rowTrav][colTrav] = _pieceInstances[rowTrav][r];
                _pieceInstances[rowTrav][r] = TempGameobject;

                MPPieceController TempMPPieceController = _pieceControllerInstances[rowTrav][colTrav];
                _pieceControllerInstances[rowTrav][colTrav] = _pieceControllerInstances[rowTrav][r];
                _pieceControllerInstances[rowTrav][r] = TempMPPieceController;

                Vector3 TempPiecePositions = _piecePositions[rowTrav][colTrav];
                _piecePositions[rowTrav][colTrav] = _piecePositions[rowTrav][r];
                _piecePositions[rowTrav][r] = TempPiecePositions;


            }

        }

    }

#endregion

}
