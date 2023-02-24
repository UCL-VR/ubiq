using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

public class WebSampleComponent : MonoBehaviour
{
    private NetworkContext context;

    public NetworkId NetworkId { get; private set; } = new NetworkId("d184c9e2-d4e096fd");

    private void Start()
    {
        context = NetworkScene.Register(this);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage m)
    {
        Debug.Log(m.ToString());
    }

    public void Update()
    {
        
    }
}
