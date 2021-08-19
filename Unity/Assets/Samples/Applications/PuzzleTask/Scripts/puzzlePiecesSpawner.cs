using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using PuzzleMaker;

// make this networked
public class puzzlePiecesSpawner : MonoBehaviour {

    public GameObject _jpPuzzlePieceInst;
    [Range(3, 10)]
    public int PiecesInRow = 3;
    [Range(3, 10)]
    public int PiecesInCol = 3;
    // Use this for initialization
    void Start () {
	
	}

    public void Spawn()
    {


        //   _PuzzleMaker = new PuzzlePieceMaker(PuzzleImage, JointMaskImage, PiecesInRow, PiecesInCol);

        spawnNewPuzzle();



    }
    

    public void spawnNewPuzzle() { 
        for (int counter = 0; counter<PiecesInCol* PiecesInRow* 2; counter++)
        {


            var spawnPosition = new Vector3(0, 0.9f, 0);

    var spawnRotation = Quaternion.Euler(90.0f,
        0,
        0);

    GameObject MyTemp = (GameObject)Instantiate(_jpPuzzlePieceInst, spawnPosition, spawnRotation);

    MyTemp.GetComponent<myEnemyID>().EnemyID = "Piece" + counter.ToString();
            //	_PuzzlePieces[(RowTrav * PiecesInRow) + ColTrav] = Temp;
            MyTemp.GetComponent<myEnemyMove>().MyCount = counter;
            //NetworkServer.Spawn(MyTemp);



        }
}
}