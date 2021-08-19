using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

using System.IO;

using PuzzleMaker;

public class myEnemyMove : MonoBehaviour {

//	[SerializeField] private Material EnemyGreen;
	//[SerializeField] private Material EnemyRed;
	// Use this for initialization
	private int health = 50;

	//private JPPuzzleController myJPPuzzleController;
	private AllTex _AllTex;
	//	[SyncVar] public  int MyColTrav;
	//	[SyncVar] public  int MyRowTrav;
	public int MyCount=0;

	 Texture2D myTexture;

	public void DeductHealth (int dmg)
	{
		health -= dmg;
	}


	int addTexture=0;
	void Start () {
	
	}
	


	void Update () {
		//if(!isServer)
		//{
		//	return;
		//}
		if (health<40) {
		//	ChangeEnemyMat (); //For the host player.
		//	RpcChangeEnemyAppearance ();
			health=60;
		}
	}
	
	void ChangeEnemyMat()
	{
	//	_AllTex= GameObject.Find("AllTexture").GetComponent<AllTex> ();
	//	myTexture=_AllTex.myImages[MyCount];
	//	gameObject.GetComponent<Renderer> ().material.mainTexture = myTexture;
		
	}
	
	
	void RpcChangeEnemyAppearance()
	{
		ChangeEnemyMat();
	//	print ("!!!!!!");
	
	}

	/*// Update is called once per frame
	void Update () {
		if(!isServer)
		{
			return;
		}
		
		//	ChangeEnemyMat (); //For the host player.
		//	RpcChangeEnemyAppearance ();
		
		
		
	}
	
	void ChangeEnemyMat()
	{
		GetComponent<Renderer>().material.mainTexture = mytexture;
		print ("AAAA");
		
	}
	
	
	
	[ClientRpc]
	void RpcChangeEnemyAppearance()
	{
		ChangeEnemyMat ();
		print ("BBBB");
		
		//GetComponent<Renderer>().material.mainTexture = mytexture;
	}*/

}
