using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Networking;
using Ubiq.Messaging;

/// <summary>
/// Puts the current object on a different layer and hides it.
/// Hide layer: 8;
/// </summary>
public class ObjectHider : MonoBehaviour, INetworkComponent, ILayer
{
    private NetworkContext context;
    private int hideLayer = 8;
    private int defaultLayer = 0; // default
    private int currentLayer;
    private Transform[] childTransforms;

    public struct Message
    {
        public byte layer;

        public Message(int layer)
        {
            this.layer = (byte)layer;
        }
    }

    public int GetCurrentLayer()
    {
        return currentLayer;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        Message msg = message.FromJson<Message>();
        Debug.Log("Remote: SetLayer " + msg.layer);
        SetLayer(msg.layer);
    }
// Awake instead of Start because objects are hidden immediately after creation.
// Should be safe to use because a replay usually only takes place in an existing scene that has the context already set.
    void Awake()
    {
        Debug.Log("ObjectHider Awake()");
        context = NetworkScene.Register(this);
        childTransforms = GetComponentsInChildren<Transform>();
    }

    private void SetLayer(int layer)
    {
        currentLayer = layer;
        this.gameObject.layer = layer;
        foreach (Transform child in childTransforms)
        {
            child.gameObject.layer = layer;
        }
    }
    // only call locally
    public void Show()
    {
        Debug.Log("Show (layer) " + defaultLayer);
        context.SendJson(new Message(defaultLayer));
        SetLayer(defaultLayer);
    }

    // only call locally
    public void Hide()
    {
        Debug.Log("Hide (layer) " + hideLayer);
        context.SendJson(new Message(hideLayer));
        SetLayer(hideLayer);
    }

    //// Update is called once per frame
    void Update()
    {

    }
    public void SetObjectLayer(int layer)
    {
        if (layer == defaultLayer)
        {
            Show();
        }
        else if (layer == hideLayer)
        {
            Hide();
        }
        else
        {
            Debug.Log("Layer " + layer + " has no specified used here.");
        }
    }
}
