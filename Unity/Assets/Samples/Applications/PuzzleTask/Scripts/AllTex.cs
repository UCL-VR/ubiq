using UnityEngine;
using System.Collections;
using System.IO;
using PuzzleMaker;

public class AllTex : MonoBehaviour {


	public Texture2D[] myImages; 

	
	public Texture2D PuzzleImage;
	[HideInInspector]
	public bool UseFilePath = false;        //if set to true prefab will load file created by puzzle maker to create new instance of puzzle maker
	[HideInInspector]
	public string PMFilePath = "";
	[HideInInspector]
	public int _selectedFileIndex = 0;
	
	
	public Texture2D[] JointMaskImage;
	
	[Range(3, 10)]
	public int PiecesInRow = 3;
	[Range(3, 10)]
	public int PiecesInCol = 3;


	private PuzzlePieceMaker _PuzzleMaker;

	

	void Awake()
	{

		_PuzzleMaker = new PuzzlePieceMaker (PuzzleImage, JointMaskImage, PiecesInRow, PiecesInCol);
		
		myImages = new Texture2D[PiecesInRow * PiecesInCol]; 
		
		for (int RowTrav = 0; RowTrav < PiecesInCol; RowTrav++) {
			for (int ColTrav = 0; ColTrav < PiecesInRow; ColTrav++) {
				int myNumber = (RowTrav * PiecesInRow) + ColTrav;
				myImages [myNumber] = _PuzzleMaker._CreatedImagePiecesData [RowTrav, ColTrav].PieceImage;
			}
		}
		
	}


}
