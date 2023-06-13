using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

public class ImuComponent : MonoBehaviour
{
    public NetworkId NetworkId => new NetworkId("5221c34a-94fec9b5");

    private NetworkContext context;

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private struct Frame
    {
        public Vector3 acceleration;
        public Vector3 rotation;
    }


    public void ProcessMessage(ReferenceCountedSceneGraphMessage m)
    {
        var frame = m.FromJson<Frame>();
        UpdateGravity(frame.acceleration.normalized);
    }

    private void UpdateGravity(Vector3 g)
    {
        Physics.gravity = g * 9.81f;
    }
}
