using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.XR;
using Ubiq.Rooms;


/// <summary>
/// A puzzle piece belongs to a Puzzle GameObject in the scene. It is spawned by the NetworkSpawner in the Puzzle.
/// The local variable gets set by the spawner accordingly.
/// </summary>
public class PuzzlePiece : MonoBehaviour, INetworkObject, INetworkComponent, ISpawnable, IGraspable
{

    private TexturedObject texturedObject;

    private Vector3 localGrabPoint;
    private Quaternion localGrabRotation;
    private Quaternion grabHandRotation;
    private Transform follow;

    private Rigidbody body;
    
    private bool local;
    private bool owner;
    private bool owned;
    private bool isKinematic = false;
    private Vector3 previousPosition = Vector3.zero;

    private bool initPosForRecording = false;

    public NetworkId Id { get; set; } // the network Id will be set by the spawner, which will always be the one to instantiate the PuzzlePiece

    private NetworkContext context;
    private NetworkScene scene;
    private RoomClient roomClient;

    public struct Message
    {
        public TransformMessage transform;
        public bool owned;
        public bool isKinematic;

        public Message(Transform transform, bool owned, bool isKinematic)
        {
            this.transform = new TransformMessage(transform);
            this.owned = owned;
            this.isKinematic = isKinematic;
        }

    }

    //public void SetMaterial(Material mat)
    //{
    //    texturedObject.SetMaterial(mat);
    //}

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
        body = GetComponentInChildren<Rigidbody>();
        body.drag = 0.5f;
        body.mass = 0.5f;
        // needs to be here because we want to network messages immediately
        context = NetworkScene.Register(this);
        scene = NetworkScene.FindNetworkScene(this);
        roomClient = scene.gameObject.GetComponent<RoomClient>();
        
    }

    // Start is called before the first frame update
    void Start()
    {
        if (context == null)
        {
            context = NetworkScene.Register(this);
        }
    }
    public void Grasp(Hand controller)
    {
        // think about how to do it if you want to take a piece away from someone else
        if (!owned)
        { 
            var handTransform = controller.transform;
            localGrabPoint = handTransform.InverseTransformPoint(transform.position); //transform.InverseTransformPoint(handTransform.position);
            localGrabRotation = Quaternion.Inverse(handTransform.rotation) * transform.rotation;
            grabHandRotation = handTransform.rotation;
            follow = handTransform;
            owner = owned = true;
        }
    }
    public void Release(Hand controller)
    {
        follow = null;
        owner = owned = false;
        context.SendJson(new Message(transform, false, true));
    }

    public bool IsLocal()
    {
        return local;
    }

    public void OnSpawned(bool local)
    {
        this.local = local;
    }

    //public void SetNetworkedMaterial(Material mat)
    //{
    //    texturedObject.SetNetworkedMaterial(mat);
    //}

    public void SetNetworkedTexture(Texture2D tex)
    {
        texturedObject.SetNetworkedTexture(tex);
    }

    public void SetNetworkedTransform(Transform trafo)
    {
        transform.localPosition = trafo.localPosition;
        transform.localRotation = trafo.localRotation;
        context.SendJson(new Message(trafo, owner, true)); // isKinematic is true
        //Debug.Log("Set networked transform: " + trafo.localPosition.ToString());

    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        transform.localPosition = msg.transform.position; // The Message constructor will take the *local* properties of the passed transform.
        transform.localRotation = msg.transform.rotation;
        owned = msg.owned;
        isKinematic = msg.isKinematic;
        body.isKinematic = msg.isKinematic;
        //Debug.Log("Set networked transform remote: " + msg.transform.position.ToString());

    }

    // Update is called once per frame
    // physics is making things unpredictable...
    void Update()
    {
        // if someone is holding the puzzle piece the transform is controlled by the grabber and not physics (no matter who owns it, local or remote)
        if (follow != null) // means this is owner too
        {
            transform.position = follow.TransformPoint(localGrabPoint);
            transform.rotation = follow.rotation * localGrabRotation;
            //transform.position = follow.transform.position;
            //transform.rotation = follow.transform.rotation;
            body.isKinematic = true;
            context.SendJson(new Message(transform, owner, true));

        }
        else // follow is null, we do not own the piece
        {
            // but if it is owned by someone else we don't want the piece controlled by physics but by the transform sent by the owner of the piece
            if (owned) // (this should work if it is a recording too, because we have the information that someone owns it)
            {
                body.isKinematic = true;
            }
            else // unless nobody owns the piece
            {
                // then the piece should be controlled by the physics of the creator of the room
                if (roomClient.Me["creator"] == "1") // or local??? but what if local peer goes away?
                {
                    body.isKinematic = false;
                    // only send new message if the position has changed
                    if (!transform.position.Equals(previousPosition))
                    {
                        context.SendJson(new Message(transform, owned, true));
                        //Debug.Log("Transforms " + previousPosition.ToString() + " " + transform.position.ToString());
                        previousPosition = transform.position;
                    }
                    // then we need to check if we are recording and also send messages accordingly
                    else if (scene.recorder != null && scene.recorder.IsRecording())
                    {
                        if (!initPosForRecording)
                        {
                            previousPosition = Vector3.zero; // when recording is stopped reset previousPosition for potential new recording
                            initPosForRecording = true;
                        }
                    }
                    else
                    {
                        initPosForRecording = false;
                    }
                }
            }
            //Debug.Log("Transforms " + gameObject.name + " " + transform.position.ToString());


        }
    }
}
