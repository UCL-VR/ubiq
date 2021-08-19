using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class myFixRotation : MonoBehaviour {

	Vector3 avatarRotation;

	public void OnStartServer()      
	{
		avatarRotation = transform.rotation.eulerAngles;

	}
	
	public void OnStartClient()
	{
		transform.rotation = Quaternion.Euler(avatarRotation);
	}
}
