using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class myEnemyController : MonoBehaviour  {


    //[SyncVar(hook = "OnChangeEnemyPosSyn")]
    //private  Vector3 enemyPosSyn;
    private Transform myTransform;

/*
    [SyncVar] private Vector3 syncPos;
	[SyncVar] private float syncYRot;
	
	private Vector3 lastPos;
	private Quaternion lastRot;

	private float lerpRate  =10;
	private float posThreshold = 0.01f;
	private float rotThreshold = 5;
*/
	// Use this for initialization


	

	// Use this for initialization
	void Start () 
	{
		myTransform = transform;

	}

	public void MoveToTarget(Vector3 pos, Vector3 rot)
	{
		
		myTransform.position= pos;
        myTransform.eulerAngles= rot;
    }

/*
	// Update is called once per frame
	void Update () 
	{
	
		TransmitMotion();
		LerpMotion();
		//RpcChangeEnemyPos ();
		CChangeEnemyPos ();
	//	myJPPuzzleController.MovePieces (gameObject.GetComponent<myEnemyID>().Mycount, gameObject.transform.position);
	}
	
	void TransmitMotion()
	{
		if(!isServer)
		{
			return;
		}
		
		if(Vector3.Distance(myTransform.position, lastPos) > posThreshold || Quaternion.Angle(myTransform.rotation, lastRot)> rotThreshold)
		{
			lastPos = myTransform.position;
		//	lastRot = myTransform.rotation;
			
			syncPos = myTransform.position;
		//	syncYRot = myTransform.localEulerAngles.y;



		}
	}
	
	void LerpMotion()
	{
		if(isServer)
		{
			return;
		}
		
		myTransform.position = Vector3.Lerp(myTransform.position, syncPos, Time.deltaTime * lerpRate);
		
	//	Vector3 newRot = new Vector3(0, syncYRot, 0);
	//	myTransform.rotation = Quaternion.Lerp(myTransform.rotation, Quaternion.Euler(newRot), Time.deltaTime * lerpRate);
	}



	[ClientCallback]
	void CChangeEnemyPos()
	{
		TransmitMotion();
		LerpMotion();
	}

    */

}



	

