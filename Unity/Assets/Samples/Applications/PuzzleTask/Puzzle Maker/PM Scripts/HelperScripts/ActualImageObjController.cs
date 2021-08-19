using UnityEngine;
using System.Collections;

/// <summary>
/// Used by JPPuzzle controller to give player ability to move completed actual image.
/// </summary>
public class ActualImageObjController : MonoBehaviour {	

	void Update () {

        if (Input.GetMouseButton(0))
        {

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;


            if (Physics.Raycast(ray, out hit, 100f))
            {
                if (hit.collider.transform.name.Contains("ActualPuzzle"))
                {
                    float MousePositionX = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
                    float MousePositionY = Camera.main.ScreenToWorldPoint(Input.mousePosition).y;


                    transform.position = new Vector3(MousePositionX, MousePositionY,
                                                    transform.position.z);
                }
            }

        }

	}

}
