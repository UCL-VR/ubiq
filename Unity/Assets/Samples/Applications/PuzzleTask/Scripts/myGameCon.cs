using UnityEngine;
using System.Collections;
using PuzzleMaker;

public class myGameCon : MonoBehaviour {
    [Range(3, 10)]
    public int PiecesInRow = 3;
    [Range(3, 10)]
    public int PiecesInCol = 3;
    public Texture2D[] PuzzleImageArr;
    private PuzzlePieceMaker _PuzzleMaker;
    public Texture2D[] JointMaskImage;
    Texture2D PuzzleImage;
    Texture2D[] myPieces;


    public void newPuzzle(int puzzleNum) {


        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Piece");
        //Making the first part of puzzle
                            int myTextureNumber = (puzzleNum) * 2;
                            PuzzleImage = PuzzleImageArr[myTextureNumber];
                            _PuzzleMaker = new PuzzlePieceMaker(PuzzleImage, JointMaskImage, PiecesInRow, PiecesInCol);
                            myPieces = new Texture2D[PiecesInRow * PiecesInCol];

                            for (int RowTrav = 0; RowTrav < PiecesInCol; RowTrav++) {
                                for (int ColTrav = 0; ColTrav < PiecesInRow; ColTrav++) {
                                    myPieces[(RowTrav * PiecesInRow) + ColTrav] = _PuzzleMaker._CreatedImagePiecesData[RowTrav, ColTrav].PieceImage;
                                }
                            }
                            for (int i = 0; i < enemies.Length/2; i++) {
                                int myNumber = enemies[i].GetComponent<myEnemyMove>().MyCount;
                                enemies[i].GetComponent<Renderer>().material.mainTexture = myPieces[myNumber];
                                enemies[i].transform.position = new Vector3(Random.Range(-0.3f, 0.3f),
                                             0.9f,
                                             Random.Range(-0.3f, 0.3f));
                            }

                        //makeing the second part of puzzle
                        myTextureNumber = (puzzleNum) * 2+1 ;
                        PuzzleImage = PuzzleImageArr[myTextureNumber];
                        _PuzzleMaker = new PuzzlePieceMaker(PuzzleImage, JointMaskImage, PiecesInRow, PiecesInCol);
                        myPieces = new Texture2D[PiecesInRow * PiecesInCol];
                        for (int RowTrav = 0; RowTrav < PiecesInCol; RowTrav++) {
                            for (int ColTrav = 0; ColTrav < PiecesInRow; ColTrav++) {
                                myPieces[(RowTrav * PiecesInRow) + ColTrav] = _PuzzleMaker._CreatedImagePiecesData[RowTrav, ColTrav].PieceImage;
                            }
                        }

                        for (int i = enemies.Length / 2; i < enemies.Length; i++) {
                                    int myNumber = enemies[i].GetComponent<myEnemyMove>().MyCount;
                                    enemies[i].GetComponent<Renderer>().material.mainTexture = myPieces[myNumber- (enemies.Length / 2)];
                                    enemies[i].transform.position = new Vector3(Random.Range(-0.3f+1.5f, 0.3f+1.5f),
                                                     0.9f,
                                                     Random.Range(-0.3f, 0.3f));
                                }
          




    }


    public void endOfPuzzle() {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Piece");
        for (int i = 0; i < enemies.Length; i++) {
            int myNumber = enemies[i].GetComponent<myEnemyMove>().MyCount;
            enemies[i].GetComponent<Renderer>().material.mainTexture = null;


        }

    }

}

