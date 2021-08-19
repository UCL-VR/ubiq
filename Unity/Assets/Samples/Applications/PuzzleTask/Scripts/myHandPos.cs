using UnityEngine;
using System.Collections;

public class myHandPos : MonoBehaviour {

    [SerializeField]
    private GameObject myController;
    private Rigidbody rbHand;
    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        rbHand = GetComponent<Rigidbody>();

        rbHand.transform.position = new Vector3(myController.transform.position.x, myController.transform.position.y, myController.transform.position.z-0.1f);
        rbHand.transform.rotation = myController.transform.rotation;

    }
}
