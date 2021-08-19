using UnityEngine;
using System.Collections;

public class NewBehaviourScript : MonoBehaviour {
	public GameObject cube;
	Vector3 targetPosition;
	// Use this for initialization
	void Start () {
		targetPosition = transform.position;

	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown(0)){
			
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			
			if (Physics.Raycast(ray, out hit)){
				print("There is something in front of the object!");
				targetPosition = hit.point;
				cube.transform.position = targetPosition;
			}
		}
	}
}
