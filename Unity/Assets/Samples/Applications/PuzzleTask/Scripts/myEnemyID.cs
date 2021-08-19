using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class myEnemyID : MonoBehaviour  {
	public string EnemyID;
	private Transform myTransform;



	// Use this for initialization
	void Start () {
		myTransform = transform;
	
	}
	
	// Update is called once per frame
	void Update () {
		SetIdentity();

	}

	void SetIdentity()
	{
		if(myTransform.name == "" || myTransform.name == "JPPiece(Clone)")
		{
			myTransform.name = EnemyID;
		}
	}








}
