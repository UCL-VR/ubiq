using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Samples;
using Avatar = Ubiq.Avatars.Avatar;

public class RenderToggle : MonoBehaviour, INetworkComponent
{
    private NetworkContext context;
    //private Renderer[] renderers;

    public bool rendering = true;
    //private bool toggle = true;

    public struct Message
    {
        public bool rend;
        public Message(bool rend) { this.rend = rend; }
    }    
    
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        rendering = msg.rend;
    }
    public void Send()
    {
        context.SendJson(new Message(rendering));
        //renderers = GetComponentsInChildren<Renderer>();
    }

    public void Rendering(bool rendering)
    {
        this.rendering = rendering;
    }

    void Awake()
    {
        context = NetworkScene.Register(this);
    }

    void Update()
    {
        //if (rendering)
        //{
        //    toggle = false;
        //}
        //if (!rendering)
        //{
        //    foreach (var renderer in renderers)
        //    {
        //        renderer.enabled = false;
        //    }
        //    toggle = true;
        //}
        //Send();
    }



}
