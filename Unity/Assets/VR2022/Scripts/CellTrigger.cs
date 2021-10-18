using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public struct CellTriggerInfo
{
    public CellTrigger trigger;
    public GameObject triggeredObject;
}


[RequireComponent(typeof(Collider))]
public class CellTrigger: MonoBehaviour
{
    public class CellTriggerEvent : UnityEvent<CellTriggerInfo> { };

    public CellTriggerEvent OnTriggerEntered;
    public CellTriggerEvent OnTriggerExited;
    public Vector3[] BorderPoints;

    public void SetupTrigger()
    {
        if(OnTriggerEntered == null)
        {
            OnTriggerEntered = new CellTriggerEvent();
        }

        if(OnTriggerExited == null)
        {
            OnTriggerExited = new CellTriggerEvent();
        }

        GetComponent<Collider>().isTrigger = true;
        GetComponent<Collider>().enabled = true;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if(meshRenderer != null)
        {
            Destroy(meshRenderer);
        }  
    }

    public Vector3[] GetBorderPoints()
    {
        var col = GetComponent<BoxCollider>();
        var trans = col.transform;
        var min = col.center - col.size * 0.5f;
        var max = col.center + col.size * 0.5f;
        var vertices = new Vector3[2];
        // Debug.Log(trans.TransformPoint(new Vector3(min.x, min.y, min.z)));
        // vertices[0] = trans.TransformPoint(new Vector3(min.x, min.y, min.z));
        // vertices[1] = trans.TransformPoint(new Vector3(min.x, min.y, max.z));
        vertices[0] = trans.TransformPoint(new Vector3((max.x + min.x) /2, min.y, max.z));
        // vertices[2] = trans.TransformPoint(new Vector3(min.x, max.y, min.z));
        // vertices[3] = trans.TransformPoint(new Vector3(min.x, max.y, max.z));
        // vertices[2] = trans.TransformPoint(new Vector3(max.x, min.y, min.z));
        // vertices[3] = trans.TransformPoint(new Vector3(max.x, min.y, max.z));
        vertices[1] = trans.TransformPoint(new Vector3((max.x + min.x) /2, min.y, min.z));
        // vertices[6] = trans.TransformPoint(new Vector3(max.x, max.y, min.z));
        // vertices[7] = trans.TransformPoint(new Vector3(max.x, max.y, max.z));
        return vertices;
        // foreach (var item in vertices)
        // {
        //     Debug.Log("Collider vertices: " + item);
        //     GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //     go.transform.position = item;
        // }
    }
    void OnTriggerEnter(Collider other)
    {
        if(OnTriggerEntered == null)
            return;
            
        OnTriggerEntered.Invoke(new CellTriggerInfo {
                trigger = this,
                triggeredObject = other.gameObject
            });
        return;
    }

    void OnTriggerExit(Collider other)
    {
        if(OnTriggerExited == null)
            return;
        
        // Debug.Log(this.name + " OnTriggerExit: " + other.gameObject.name);
        OnTriggerExited.Invoke(new CellTriggerInfo {
                trigger = this,
                triggeredObject = other.gameObject
            });
        return;
    }
}
