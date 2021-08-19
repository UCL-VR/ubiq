using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.XR;

public class PuzzlePiece : MonoBehaviour, INetworkObject, INetworkComponent, ISpawnable, IGraspable
{

    private TexturedObject texturedObject;
    
    private Hand follow;
    private Rigidbody body;
    
    private bool local;
    private bool owner;

    public NetworkId Id { get; set; } // the network Id will be set by the spawner, which will always be the one to instantiate the PuzzlePiece

    private NetworkContext context;

    public struct Message
    {
        public TransformMessage transform;

        public Message(Transform transform)
        {
            this.transform = new TransformMessage(transform);
        }

    }

    public void SetTexture(Texture2D tex)
    {
        texturedObject.SetTexture(tex);
    }

    public Texture2D GetTexture()
    {
        return texturedObject.GetTexture();
    }

    void Awake()
    {
        texturedObject = GetComponent<TexturedObject>();
        body = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }
    public void Grasp(Hand controller)
    {

        follow = controller;
        owner = true;
       
    }
    public void Release(Hand controller)
    {
        follow = null;
        owner = false;
    }

    public bool IsLocal()
    {
        return local;
    }

    public void OnSpawned(bool local)
    {
        this.local = local;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        transform.localPosition = msg.transform.position; // The Message constructor will take the *local* properties of the passed transform.
        transform.localRotation = msg.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (follow != null)
        {
            transform.position = follow.transform.position;
            transform.rotation = follow.transform.rotation;
            body.isKinematic = true;
        }
        else
        {
            body.isKinematic = false;
        }
        if (owner)
        {
            context.SendJson(new Message(transform));
        }
    }
}
