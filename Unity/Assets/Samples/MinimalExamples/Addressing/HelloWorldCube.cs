using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq;
using Ubiq.Messaging;

public class HelloWorldCube : MonoBehaviour
{
    private NetworkContext context;

    private Vector3 lastPosition;

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.localPosition != lastPosition)
        {
            lastPosition = transform.localPosition;
            context.SendJson<Vector3>(lastPosition);
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var newPosition = message.FromJson<Vector3>();
        lastPosition = newPosition;
        transform.localPosition = lastPosition;
    }
}
