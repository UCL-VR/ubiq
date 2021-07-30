using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Networking;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Spawning;
using Avatar = Ubiq.Avatars.Avatar;
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
    private Avatar avatar;
    private NetworkSpawner spawner;

    public struct Message
    {
        public int layer;
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

    void Start()
    {
        if (context == null)
        {
            context = NetworkScene.Register(this);
        }

        avatar = GetComponent<Avatar>();
        spawner = NetworkSpawner.FindNetworkSpawner(context.scene);
    }

    public void SetLayer(int layer) // not networked
    {
        currentLayer = layer;
        this.gameObject.layer = layer;
        foreach (Transform child in childTransforms)
        {
            child.gameObject.layer = layer;
        }
    }
    // only call locally
    public void NetworkedShow()
    {
        Debug.Log("Show (layer) " + defaultLayer);
        context.SendJson(new Message() { layer = defaultLayer });
        SetLayer(defaultLayer);
    }

    // only call locally
    public void NetworkedHide()
    {
        Debug.Log("Hide (layer) " + hideLayer);
        context.SendJson(new Message() { layer = hideLayer });
        SetLayer(hideLayer);
    }

    public void SetNetworkedObjectLayer(int layer)
    {

        if (layer == defaultLayer)
        {
            // for actual peer avatars this method won't do anything because the peer avatars are in the AvatarManager and not in the NetworkSpawner
            spawner.UpdateProperties(avatar.Id, "UpdateVisibility", true);
            NetworkedShow();
        }
        else if (layer == hideLayer)
        {
            spawner.UpdateProperties(avatar.Id, "UpdateVisibility", false);
            NetworkedHide();
        }
        else
        {
            Debug.Log("Layer " + layer + " has no specified use here.");
        }
    }

    void Update()
    {
        if (avatar.IsLocal)
        {
            SetNetworkedObjectLayer(0);
        }
    }
}
