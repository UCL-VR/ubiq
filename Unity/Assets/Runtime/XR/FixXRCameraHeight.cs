using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixXRCameraHeight : MonoBehaviour
{
    public Camera XRCamera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, -XRCamera.transform.localPosition.y, transform.localPosition.z);
    }
}
