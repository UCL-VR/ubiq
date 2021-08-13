using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
// Attach to objects that should use an outline, e.g. recorded avatars/objects
/// <summary>
/// The outline is created when the recorded objects are instantiated in the NetworkSpawner
/// </summary>
public class Outliner : MonoBehaviour, INetworkComponent
{
    NetworkContext context;
    private bool hasOutline = false;
    private Outline[] outlines;

    public struct Message
    {
        public bool hasOutline;

        public Message(bool hasOutline)
        {
            this.hasOutline = hasOutline;
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        Message msg = message.FromJson<Message>();
        hasOutline = msg.hasOutline;
        Outline();
    }

    // Start is called before the first frame update
    void Awake()
    {
        context = NetworkScene.Register(this);
        outlines = GetComponentsInChildren<Outline>();

    }

    public void SetOutline(bool outlined) // sets outline only locally and does not network it
    {
        hasOutline = outlined;
        Outline();
       
    }

    private void Outline()
    {
        foreach (var ol in outlines)
        { 
            ol.SetErase(!hasOutline);
        }
    }

    public void NetworkedSetOutline(bool outlined)
    {
        hasOutline = outlined;
        context.SendJson(new Message(hasOutline));
        Outline();
    }

    //void Update()
    //{
    //    if (hasOutline)
    //    {
    //        Debug.Log(gameObject.name + " has an outline!");
    //    }
    //}
     

}
