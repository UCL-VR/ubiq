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
    private Avatar avatar;
    private NetworkId objectid;
    private NetworkSpawner spawner;
    private ISpawnable spawnableObject;
    
    private Transform[] childTransforms;
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;

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
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();

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
            if (avatar.Peer["visible"] == null || avatar.Peer["visible"] == "1")
            {
                Debug.Log("Show");
                SetLayer(defaultLayer);
            }
            else
            {
                Debug.Log("Hide");
                SetLayer(hideLayer);
            }
            //avatar.Peer["visible"] = "1";
        }

            if (context == null)
        {
            context = NetworkScene.Register(this);
        }

        objectid = context.networkObject.Id;
        spawnableObject = GetComponent<ISpawnable>();
        spawner = NetworkSpawner.FindNetworkSpawner(context.scene);

        roomClient.OnPeerAdded.AddListener(OnPeerAdded);
    }

    public void OnPeerAdded(IPeer peer) // so the added peer gets the information about visibility of other peers
    {
        if (avatar == null || peer.UUID != avatar.Peer.UUID)
        {
            return;
        }
        
        Debug.Log("ObjectHider OnPeerAdded");
        Debug.Log(avatar.Peer["visible"]);
        if (avatar.Peer["visible"] == null || avatar.Peer["visible"]  == "1")
        {
            Debug.Log("Show");
            SetLayer(defaultLayer);
        }
        else
        {
            Debug.Log("Hide");
            SetLayer(hideLayer);

        }
        //OnPeerUpdated(peer);
    }

    public void OnPeerUpdated(IPeer peer)
    {
        if (avatar == null || peer.UUID != avatar.Peer.UUID)
        {
            return;
        }

        Debug.Log("ObjectHider OnPeerUpdated");
        Debug.Log(peer.UUID + " " + avatar.Peer.UUID);

        if (peer["visible"] == null || peer["visible"] == "1")
        {
            Debug.Log("Show");
            SetLayer(defaultLayer);
        }
        else 
        {
            Debug.Log("Hide");
            SetLayer(hideLayer);
        }
    }

    public void SetLayer(int layer) // not networked
    {
        var previousLayer = currentLayer;
        currentLayer = layer;
        this.gameObject.layer = layer;

        foreach (var child in childTransforms)
        {
            child.gameObject.layer = layer;
        }
        // don't set this every frame if nothing changed
        if (previousLayer != layer)
        {
            DisableEnablePhysics();
        }
    }
    public void DisableEnablePhysics()
    {
        if (currentLayer == hideLayer)
        {
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var co in colliders)
            {
                co.enabled = false;
            }
        }
        else if (currentLayer == defaultLayer)
        {
            foreach (var co in colliders)
            {
                co.enabled = true;
            }
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
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
