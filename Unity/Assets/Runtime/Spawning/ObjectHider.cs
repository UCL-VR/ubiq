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
    private NetworkScene scene;
    private RoomClient roomClient;

    private int hideLayer = 8;
    private int defaultLayer = 0; // default
    private int currentLayer = 0;
    private bool needsUpdate = true;
    private Transform[] childTransforms;
    private Avatar avatar;
    private NetworkId objectid;
    private NetworkSpawner spawner;
    private ISpawnable spawnableObject;

    public struct Message
    {
        public int layer;
    }

    public void UpdateLayer(int layer)
    {
        currentLayer = layer;
        needsUpdate = true;
    }

    public int GetCurrentLayer()
    {
        return currentLayer;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        //Debug.Log(Time.unscaledTime + " Process Message: " + message.length + " " + System.String.Join(" ", message.bytes));
        //Debug.Log(Time.unscaledTime + " Process Message: " + message.ToString());

        Message msg = message.FromJson<Message>();
        //Debug.Log("Remote: SetLayer " + msg.layer);
        SetLayer(msg.layer);
    }
// Awake instead of Start because objects are hidden immediately after creation.
// Should be safe to use because a replay usually only takes place in an existing scene that has the context already set.
    void Awake()
    {
        Debug.Log("ObjectHider Awake()");
        scene = NetworkScene.FindNetworkScene(this);
        context = scene.RegisterComponent(this);

        roomClient = NetworkScene.FindNetworkScene(this).GetComponent<RoomClient>();
        childTransforms = GetComponentsInChildren<Transform>();
        roomClient.OnPeerUpdated.AddListener(OnPeerUpdated);
        //roomClient.OnPeerAdded.AddListener(OnPeerAdded);
        //roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
        //roomClient.OnPeerRemoved.AddListener(OnPeerRemoved);
    }

    void Start()
    {
        // not all objects are avatars!!!!
        if (TryGetComponent<Avatar>(out avatar) && avatar.Peer.UUID != null)
        {
            avatar.Peer["visible"] = "1";
        }

            if (context == null)
        {
            context = NetworkScene.Register(this);
        }

        objectid = context.networkObject.Id;
        spawnableObject = GetComponent<ISpawnable>();
        spawner = NetworkSpawner.FindNetworkSpawner(context.scene);
    }

    public void OnPeerUpdated(IPeer peer)
    {
        if (avatar != null)
        {
            Debug.Log("ObjectHider OnPeerUpdated");
            Debug.Log(peer.UUID + " " + avatar.Peer.UUID);
            if (peer.UUID == avatar.Peer.UUID)
            {
                if (peer["visible"] == "0")
                {
                    Debug.Log("Hide");
                    SetLayer(hideLayer);
                }
                if (peer["visible"] == "1")
                {
                    Debug.Log("Show");
                    SetLayer(defaultLayer);
                }
            }
        }
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
        //Debug.Log("Show (layer) " + defaultLayer);
        context.SendJson(new Message() { layer = defaultLayer });
        //roomClient.Me["visible"] = "1";
        SetLayer(defaultLayer);
    }

    // only call locally
    public void NetworkedHide()
    {
        //Debug.Log("Hide (layer) " + hideLayer);
        context.SendJson(new Message() { layer = hideLayer });
        //roomClient.Me["visible"] = "0";
        SetLayer(hideLayer);
    }

    public void SetNetworkedObjectLayer(int layer)
    {

        if (layer == defaultLayer)
        {
            // for actual peer avatars this method won't do anything because the peer avatars are in the AvatarManager and not in the NetworkSpawner
            if (objectid != null && layer != currentLayer)
            {
                spawner.UpdateProperties(objectid, "UpdateVisibility", true);
            }
            NetworkedShow();
        }
        else if (layer == hideLayer)
        {
            if (objectid != null && layer != currentLayer)
            {
                spawner.UpdateProperties(objectid, "UpdateVisibility", false);
            }
            NetworkedHide();
        }
        else
        {
            Debug.Log("Layer " + layer + " has no specified use here.");
        }
    }

    void Update()
    {
        // recorded objects that are not avatars can be local
        if (spawnableObject.IsLocal())
        {
            if (scene.recorder != null && scene.recorder.IsRecording())
            {
                SetNetworkedObjectLayer(currentLayer);
            }
        }
    }
}
