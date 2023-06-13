using System.Collections;
using System.Collections.Generic;
using Ubiq.XR;
using UnityEngine;

public class BrowserQuestionnaireExperimentCylinder : MonoBehaviour, IGraspable
{
    private Rigidbody body;
    private Hand grasped;
    private Transform target;

    public bool HasBeenGrasped { get; set; }

    public void Grasp(Hand controller)
    {
        grasped = controller;
        target.parent = controller.transform;
        target.position = transform.position;
        target.rotation = transform.rotation;
        body.isKinematic = true;
    }

    public void Release(Hand controller)
    {
        grasped = null;
        body.isKinematic = false;
        HasBeenGrasped = true;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        target = new GameObject(gameObject.name + "AttachmentPoint").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(grasped)
        {
            var impulse = target.position - transform.position;
            transform.position += impulse * Mathf.Min(Time.deltaTime * (1f/body.mass), 1f);
        }
    }

    public void SetWeight(float weight)
    {
        body.mass = weight;
    }
}
