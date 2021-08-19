using UnityEngine;
using System.IO;
using System.Collections;
using PuzzleMaker;


public class JPPuzzleController : MonoBehaviour
{

    #region "Public Variables"
    [HideInInspector]
    public Texture2D PuzzleImage;
    [HideInInspector]
    public bool UseFilePath = false;        //if set to true prefab will load file created by puzzle maker to create new instance of puzzle maker
    [HideInInspector]
    public string PMFilePath = "";
    [HideInInspector]
    public int _selectedFileIndex = 0;

    public GameObject _jpPuzzlePieceInst;
    public Texture2D[] JointMaskImage;

    [Range(3, 10)]
    public int PiecesInRow = 3;
    [Range(3, 10)]
    public int PiecesInCol = 3;

    [Range(0.001f, 0.2f)]
    public float PieceJoinSensitivity = 0.8f;

    /// 
    public Texture2D[] PuzzleImageArr;
    Texture2D[] myPieces;

    #endregion

    private PuzzlePieceMaker _PuzzleMaker;

    private GameObject[] _PuzzlePieces;     //Holds PuzzlePiecePrefab instances

    //Variables use to move pieces to place
    private GameObject _CurrentHoldingPiece = null;

    //Holds main instance of puzzle piece gameobject
    // private GameObject _jpPuzzlePieceInst;


    public int PuzzleMakerPieceWidthWithoutJoint
    {
        get { return _PuzzleMaker.PieceWidthWithoutJoint; }
    }

    public int PuzzleMakerPieceHeightWithoutJoint
    {
        get { return _PuzzleMaker.PieceHeightWithoutJoint; }
    }

    public float PieceWidthInWorld
    {
        get { return 1f; }
    }

    public float PieceHeightInWorld
    {
        get { return ((float)PuzzleImage.height / (float)PuzzleImage.width); }
    }




    public void newPuzzle(int puzzleNum)
    {
  
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Piece");
       
        //Making the first part of puzzle
        int myTextureNumber = (puzzleNum) * 2;
        PuzzleImage = PuzzleImageArr[myTextureNumber];
        _PuzzleMaker = new PuzzlePieceMaker(PuzzleImage, JointMaskImage, PiecesInRow, PiecesInCol);
    //    myPieces = new Texture2D[PiecesInRow * PiecesInCol];

                for (int RowTrav = 0; RowTrav < PiecesInCol; RowTrav++)
                {
                    for (int ColTrav = 0; ColTrav < PiecesInRow; ColTrav++)
                    {
                        // myPieces[(RowTrav * PiecesInRow) + ColTrav] = _PuzzleMaker._CreatedImagePiecesData[RowTrav, ColTrav].PieceImage;
                        Texture2D Img = _PuzzleMaker._CreatedImagePiecesData[RowTrav, ColTrav].PieceImage;

                        float PieceScaleX = (float)Img.width / (float)_PuzzleMaker.PieceWidthWithoutJoint;
                        float PieceScaleY = this.PieceHeightInWorld * ((float)Img.height / (float)_PuzzleMaker.PieceHeightWithoutJoint);
               
                        GameObject Temp = enemies[((RowTrav * PiecesInRow) + ColTrav)];

                     //   JPPieceController TempPieceControllerInst = Temp.GetComponent<JPPieceController>();
                     //   TempPieceControllerInst.JpPuzzleControllerInstance = this;


                        //Get this piece information
                     //   SPieceInfo ThisPieceData = _PuzzleMaker._CreatedImagePiecesData[RowTrav, ColTrav].PieceMetaData.MakeCopy();
                      //  TempPieceControllerInst.ThisPieceData = ThisPieceData;

                        //Assign image to piece


                        //Resize piece in world
                        Temp.transform.localScale = new Vector3(PieceScaleX / 5, PieceScaleY / 5, 0.001f);
                        Temp.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0);



                        Temp.transform.position = new Vector3(Random.Range(-0.3f, 0.3f),
                         0.9f,
                         Random.Range(-0.3f, 0.3f));

              Temp.transform.rotation = Quaternion.Euler(90.0f, 0, 0);
                Temp.GetComponent<Renderer>().material.mainTexture = Img;
                //Enable collider for this piece
                Temp.GetComponent<BoxCollider>().enabled = true;
                     //   TempPieceControllerInst.enabled = true;

                        //Enable piece
                        Temp.SetActive(true);
                    }
                }


        //makeing the second part of puzzle
        myTextureNumber = (puzzleNum) * 2 + 1;
        PuzzleImage = PuzzleImageArr[myTextureNumber];
        _PuzzleMaker = new PuzzlePieceMaker(PuzzleImage, JointMaskImage, PiecesInRow, PiecesInCol);
        //  myPieces = new Texture2D[PiecesInRow * PiecesInCol];

        for (int RowTrav = 0; RowTrav < PiecesInCol; RowTrav++)
        {
            for (int ColTrav = 0; ColTrav < PiecesInRow; ColTrav++)
            {
                // myPieces[(RowTrav * PiecesInRow) + ColTrav] = _PuzzleMaker._CreatedImagePiecesData[RowTrav, ColTrav].PieceImage;
                Texture2D Img = _PuzzleMaker._CreatedImagePiecesData[RowTrav, ColTrav].PieceImage;

                float PieceScaleX = (float)Img.width / (float)_PuzzleMaker.PieceWidthWithoutJoint;
                float PieceScaleY = this.PieceHeightInWorld * ((float)Img.height / (float)_PuzzleMaker.PieceHeightWithoutJoint);

                GameObject Temp = enemies[((RowTrav * PiecesInRow) + ColTrav) + PiecesInCol * PiecesInRow];

               // JPPieceController TempPieceControllerInst = Temp.GetComponent<JPPieceController>();
               // TempPieceControllerInst.JpPuzzleControllerInstance = this;


                //Get this piece information
                //SPieceInfo ThisPieceData = _PuzzleMaker._CreatedImagePiecesData[RowTrav, ColTrav].PieceMetaData.MakeCopy();
                //TempPieceControllerInst.ThisPieceData = ThisPieceData;

                //Assign image to piece


                //Resize piece in world
                Temp.transform.localScale = new Vector3(PieceScaleX / 5, PieceScaleY / 5, 0.001f);
                Temp.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0);

                Temp.GetComponent<myEnemyID>().EnemyID = "Piece" + ((RowTrav * PiecesInRow) + ColTrav + PiecesInCol * PiecesInRow).ToString();

                Temp.GetComponent<myEnemyMove>().MyCount = ((RowTrav * PiecesInRow) + ColTrav + PiecesInCol * PiecesInRow);

                Temp.transform.position = new Vector3(Random.Range(-0.3f + 1.5f, 0.3f + 1.5f),
                             0.9f,
                             Random.Range(-0.3f, 0.3f));
            Temp.transform.rotation = Quaternion.Euler(90.0f,0,0);
                Temp.GetComponent<Renderer>().material.mainTexture = Img;
                //Enable collider for this piece
                Temp.GetComponent<BoxCollider>().enabled = true;
               // TempPieceControllerInst.enabled = true;

                //Enable piece
                Temp.SetActive(true);
            }
        }

    }



    public void UnholdPiece()
    {
        _CurrentHoldingPiece = null;
    }

    public bool IsHoldingPiece()
    {
        return _CurrentHoldingPiece != null;
    }

    public int HoldingPieceID()
    {
        if (_CurrentHoldingPiece != null)
        {
            return _CurrentHoldingPiece.GetComponent<JPPieceController>().ThisPieceData.ID;
        }

        return -1;

    }


    public void WrongPieceCollision(GameObject Obj)
    {
        //GetComponent<ColorAnimator>().StartEffect(Obj, WrongPieceAnimationColor );
    }

}
