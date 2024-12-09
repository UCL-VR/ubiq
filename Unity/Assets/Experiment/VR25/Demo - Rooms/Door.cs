using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        print("Door OnTriggerEnter");
        Debug.Log("Door OnTriggerEnter");
    }

}
