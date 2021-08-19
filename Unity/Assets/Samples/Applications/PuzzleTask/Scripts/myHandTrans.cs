using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class myHandTrans : MonoBehaviour {

    public GameObject handsposition;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        gameObject.transform.position = handsposition.transform.position;
        gameObject.transform.rotation = handsposition.transform.rotation;
    }
}
