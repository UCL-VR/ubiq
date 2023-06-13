using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

public class StreamingSampleCameraControls : MonoBehaviour
{
    private NetworkContext context;

    public NetworkId Id => new NetworkId("e09e9c92-30b82547");

    private struct ControlMessage
    {
        public float x;
        public float y;
    }

    private Vector2 previousMousePosition;
    private Transform yaw;

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this, Id);
        yaw = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var m = message.FromJson<ControlMessage>();
        var position = new Vector2(m.x, m.y);
        var deltaMousePosition = position - previousMousePosition;
        previousMousePosition = position;
        if (deltaMousePosition.magnitude > 100)
        {
            return;
        }

        transform.RotateAround(transform.position, transform.right, deltaMousePosition.y);
        yaw.RotateAround(yaw.position, Vector3.up, deltaMousePosition.x);
    }
}
