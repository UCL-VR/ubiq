using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using Ubiq.Networking;
using Ubiq.Rooms;
using Ubiq.Rooms.Messages;
using Ubiq.Samples;
using System.Linq;
using System;


[Serializable]
public enum RoomObjectPersistencyLevel
{
    // Persistent level objects remain in the room even when there are no peers/observers in the room
    Persistent,
    // Owner level objects remain in the room as long as the owner of the object is in the room
    Owner,
    // Room level objects remain in the room as long as there are peers or observers in the room
    Room
}
 public static class RoomObjectPersistencyLevelExtensions
 {
    public static string EnumToString(this RoomObjectPersistencyLevel me)
    {
        switch(me)
        {
        case RoomObjectPersistencyLevel.Persistent:
            return "persistent";
        case RoomObjectPersistencyLevel.Owner:
            return "owner";
        case RoomObjectPersistencyLevel.Room:
            return "room";
        default:
            return "persistent";
        }
    }

    public static RoomObjectPersistencyLevel StringToEnum(this string me)
    {
        switch(me)
        {
        case "persistent":
            return RoomObjectPersistencyLevel.Persistent;
        case "owner":
            return RoomObjectPersistencyLevel.Owner;
        case "room":
            return RoomObjectPersistencyLevel.Room;
        default:
            return RoomObjectPersistencyLevel.Persistent;
        }
    }
 }

public abstract class RoomObjectInterface
{
    public RoomObjectInterface(string name, RoomInfo room = new RoomInfo())
    {
        Name = name;
        Room = room;
        MyPeer = new PeerInfo();
        OwnerPeerUUID = "";
        properties = new SerializableDictionary();
    }

    public string Name { get; protected set; }
    public string UUID { get; protected set; }
    protected NetworkId NetworkId;
    protected ushort ComponentId;
    protected string RoomDictionaryKey;
    protected RoomObjectPersistencyLevel PersistencyLevel;
    protected RoomInfo Room;
    protected PeerInfo MyPeer;
    protected string OwnerPeerUUID;

    protected SerializableDictionary properties;

    public RoomObjectInterface()
    {
        properties = new SerializableDictionary();
    }

    public string this[string key]
    {
        get => properties[key];
        set => properties[key] = value;
    }

    public IEnumerable<KeyValuePair<string, string>> Properties
    {
        get => properties.Enumerator;
    }

    public RoomObjectInfo GetRoomObjectInfo()
    {
        return new RoomObjectInfo(Name, NetworkId, ComponentId,
                                Room, MyPeer, OwnerPeerUUID,
                                PersistencyLevel.EnumToString(), RoomDictionaryKey, properties);
    }
}

[Serializable]
public struct RoomObjectInfo
{
    public string Name
    {
        get => name;
    }

    public NetworkId Id
    {
        get => networkId;
    }

    public ushort ComponentId
    {
        get => componentId;
    }

    public RoomInfo Room
    {
        get => room;
    }

    public PeerInfo MyPeer
    {
        get => myPeer;
    }

    public string OwnerPeerUUID
    {
        get => ownerPeerUUID;
    }

    public string PersistencyLevel
    {
        get => persistencyLevel;
    }

    public string RoomDictionaryKey
    {
        get => roomDictionaryKey;
    }
    
    public string this[string key]
    {
        get => properties != null ? properties[key] : null;
    }

    public IEnumerable<KeyValuePair<string, string>> Properties
    {
        get
        {
            return properties != null ? properties.Enumerator : Enumerable.Empty<KeyValuePair<string, string>>();
        }
    }

    [SerializeField]
    private string name;

    [SerializeField]
    private NetworkId networkId;

    [SerializeField]
    private ushort componentId;

    [SerializeField]
    private RoomInfo room;

    [SerializeField]
    private PeerInfo myPeer;

    [SerializeField]
    private string ownerPeerUUID;

    [SerializeField]
    private string persistencyLevel;

    [SerializeField]
    private string roomDictionaryKey;

    [SerializeField]
    private SerializableDictionary properties;

    public RoomObjectInfo(string name, 
                        NetworkId networkId, ushort componentId, 
                        RoomInfo room, PeerInfo myPeer, 
                        string ownerPeerUUID, string persistencyLevel,
                        string roomDictionaryKey, SerializableDictionary properties)
    {
        this.name = name;
        this.networkId = networkId;
        this.componentId = componentId;
        this.room = room;
        this.myPeer = myPeer;
        this.ownerPeerUUID = ownerPeerUUID;
        this.persistencyLevel = persistencyLevel;
        this.roomDictionaryKey = roomDictionaryKey;
        this.properties = properties;
    }
}

public abstract class RoomObject : MonoBehaviour, INetworkComponent, INetworkObject, ISpawnable
{

    [Serializable]
    protected struct Message
    {
        public string type;
        public string args;

        public Message(string type, object args)
        {
            this.type = type;
            this.args = JsonUtility.ToJson(args);
        }
    }

    public class RoomObjectInterfaceFriend : RoomObjectInterface
    {
        public RoomObjectInterfaceFriend(string uuid, string name):base(name)
        {
        }

        public void Update(RoomObjectInfo args)
        {
            Name = args.Name;
            NetworkId = args.Id;
            ComponentId = args.ComponentId;
            RoomDictionaryKey = args.RoomDictionaryKey;
            PersistencyLevel = args.PersistencyLevel.StringToEnum();
            OwnerPeerUUID = args.OwnerPeerUUID;
            properties = new SerializableDictionary(args.Properties); 
        }

        public void SetRoom(RoomInfo room)
        {
            Room = room;
        }

        public void SetMyPeer(PeerInfo peer)
        {
            MyPeer = peer;
        }

        public void SetOwnerUUID(string uuid)
        {
            OwnerPeerUUID = uuid;
        }

        public void SetNetworkId(NetworkId id)
        {
            NetworkId = id;
        }

        public void SetRoomDictionaryKey(string key)
        {
            RoomDictionaryKey = key;
        }

        public void SetComponentId(ushort id)
        {
            ComponentId = id;
        }

        public void SetPersistencyLevel(RoomObjectPersistencyLevel persistencyLevel)
        {
            PersistencyLevel = persistencyLevel;
        }
    }   

    [Serializable]
    public class RoomDictionaryInfoMessage<T> where T : IRoomObjectState // This data will be stored in the room dictionary
    {
        public int catalogueIndex;
        public NetworkId networkId;
        public string persistencyLevel;
        public T state; 
    } 

    [Serializable]
    public struct RoomObjectOwnerMessage // Structure for updating object onwer message
    {
        public PeerInfo myPeer;
        public string uuid;
        public RoomObjectInfo objectInfo;
    }    

    [Serializable]
    public struct RoomObjectRoomMessage // Structure for updating object room message
    {
        public PeerInfo myPeer;
        public RoomInfo room;
        public RoomObjectInfo objectInfo;
    }

    [Serializable]
    public class RoomObjectStateMessage // Structure for updating object state message
    {
        public RoomObjectInfo objectInfo;
        public IRoomObjectState state;
    }     

    // Marker interface to enable function/struct inheritance
    // Child class that creates a new State structure should mark the struct as IRoomObjectState
    [Serializable]
    public class IRoomObjectState {}
    

    [Serializable]
    public class RoomObjectState: IRoomObjectState // Structure for object state, child objects likely to have their own state implementation
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
    }

    private NetworkId RoomServerObjectId = new NetworkId(1);
    private ushort RoomServerComponentId = 1;

    public NetworkId Id 
    { 
        get
        {
            return Me.GetRoomObjectInfo().Id;
        } 

        set
        {
            
            Me.SetNetworkId(value);
            if(roomDictionaryKey == null || roomDictionaryKey == "")
            {
                roomDictionaryKey = $"SpawnedObject-{ value }";
            }
            
        }
    }

    public string roomDictionaryKey
    {
        get
        {
            return Me.GetRoomObjectInfo().RoomDictionaryKey;
        }

        set
        {
            Me.SetRoomDictionaryKey(value);
        }
    }

    public RoomObjectPersistencyLevel persistencyLevel;

    public int catalogueIndex;

    public bool owner = false;

    public bool spawned = false;
    public NetworkContext context;
    public RoomObjectInterfaceFriend Me { get; private set; }

    public bool updateServer = false;
    public Rigidbody rb;

    private Vector3 previousPosition;
    public Vector3 currentVelocity;
    public float currentSpeed;

    public string RoomName;
    public string OwnerUUID;
    public string MyPeerUUID;

    public bool notInRoom = false;
    public bool inEmptyRoom = false;
    private bool destroyMethodInvoked = false;
    private bool needsSpawning = true;
    
    public class ObjectEvent: UnityEvent<RoomObjectInfo> { };
    public ObjectEvent OnObjectDestroyed;
    
    private RoomInfo RoomToAssign;
    private PeerInfo OwnerToAssign;

    protected bool messageProcessed = false;

    bool stopDestoryLocalObject = false;

    public float timeToDestroyObject = 10;
    protected  float timeRemaining;

    protected MeshRenderer meshRenderer;

    public void Awake() 
    {
        Debug.Log("RoomObject: Awake");

        if (OnObjectDestroyed == null)
        {
            OnObjectDestroyed = new ObjectEvent();
        }
        
        Me = new RoomObjectInterfaceFriend(Guid.NewGuid().ToString(), gameObject.name);   
        
        if(context == null)
        {
            context = NetworkScene.Register(this);
            
            Me.SetComponentId(context.componentId);
        }
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        meshRenderer = GetComponent<MeshRenderer>();

        Me.SetPersistencyLevel(persistencyLevel);
    }

    // Start is called before the first frame update
    public void Start()
    {
        Debug.Log("RoomObject: Start");
        if(context == null)
        {
            context = NetworkScene.Register(this);
            Me.SetComponentId(context.componentId);
        }

        if(Me.GetRoomObjectInfo().MyPeer.uuid == null || Me.GetRoomObjectInfo().MyPeer.uuid == "")
        {
            GetMyPeer();
        }
        

        if(rb == null)
        {
            previousPosition = transform.position;
            currentVelocity = Vector3.zero;
        }
        else
        {
            currentVelocity = rb.velocity;
        }

    }

    // Update is called once per frame
    public void Update()
    {
        // if(Me.GetRoomObjectInfo().PersistencyLevel != this.persistencyLevel.EnumToString()){
        //     Me.SetPersistencyLevel(this.persistencyLevel);
        // }
        if(rb == null)
        {
            Vector3 currFrameVelocity = (transform.position - previousPosition) / Time.deltaTime;
            currentVelocity = Vector3.Lerp(currentVelocity, currFrameVelocity, 0.1f);
            previousPosition = transform.position;
        }
        else
        {
            currentVelocity = rb.velocity;
        }
        currentSpeed = currentVelocity.magnitude;
        if(inEmptyRoom && timeRemaining > 0 && owner)
        {
            timeRemaining -= Time.deltaTime;
            // UpdateObject();
        } 
        else if(inEmptyRoom && owner)
        {
            DestroyLocalObject();
        }

        if(!inEmptyRoom && notInRoom && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }

        else if(!inEmptyRoom && notInRoom && timeRemaining <= 0 && !owner)
        {
            DestroyLocalObject();
        }

        else if(!inEmptyRoom && notInRoom && timeRemaining <= 0 && owner)
        {
            RemoveObject();
        }
    }

    public virtual void OnObjectLeftCell(Cell cell)
    {
        if(Me.GetRoomObjectInfo().Room.UUID == cell.CellUUID)
        {
            timeRemaining = timeToDestroyObject;
            notInRoom = true;
        }
    }

    protected void RemoveObject()
    {
        Debug.Log("RoomObject: RemoveObject");
        SetObjectProperties(true);
        SendToServer("RemoveObject", Me.GetRoomObjectInfo());
        OnObjectDestroyed.Invoke(Me.GetRoomObjectInfo());
        Destroy(this.gameObject, 0.5f);
    }

    public void DestroyLocalObject()
    {
        Debug.Log("RoomObject: DestroyLocalObject");

        if(stopDestoryLocalObject)
        {
            return;
        }
        
        if(owner)
        {
            Me.SetOwnerUUID("");
            Me["inEmptyRoom"] = JsonUtility.ToJson(false);
            SetObjectProperties(true);
            SendToServer("LocalObjectDestroyed", Me.GetRoomObjectInfo());
        }
        inEmptyRoom = false;
        owner = false;
        destroyMethodInvoked = false;
        OnObjectDestroyed.Invoke(Me.GetRoomObjectInfo());
        // transform.position = new Vector3(transform.position.x, transform.position.y - 100.0f, transform.position.z);
        Destroy(this.gameObject, 0.5f);

    }


    virtual protected void SetObjectProperties(bool setVelocityZero = false)
    {
        RoomObjectInfo info = Me.GetRoomObjectInfo();
        var objectState = new RoomObjectState(){
            position = this.transform.position,
            rotation = this.transform.rotation,
            velocity = setVelocityZero ? Vector3.zero : this.currentVelocity
        };
        Me[info.RoomDictionaryKey] = JsonUtility.ToJson( new RoomDictionaryInfoMessage<RoomObjectState>(){
            catalogueIndex = this.catalogueIndex,
            networkId = this.Id,
            persistencyLevel = info.PersistencyLevel,
            state = objectState
        });
    }

    virtual protected void SetRoomObjectInfo<T>(RoomDictionaryInfoMessage<T> msg) where T : IRoomObjectState
    {
        // RoomDictionaryInfoMessage<RoomObjectState> info = msg<RoomObjectState>;
        var info = msg;
        if(info.state is RoomObjectState)
        {
        
            // T state = (T) msg.state;
            RoomObjectState state = msg.state as RoomObjectState;
            // Debug.Log("RoomObject: SetRoomObjectInfo: catalogueIndex: " + info.catalogueIndex + ", networkId: " + info.networkId + ", position: " + info.state.position + ", rotation: " + info.state.rotation + ", velocity: " + info.state.velocity);
            this.transform.position = state.position;
            this.transform.rotation = state.rotation;
            
            if(rb != null)
            {
                rb.velocity = state.velocity;
            }
            this.currentVelocity = state.velocity;
            this.catalogueIndex = info.catalogueIndex;
            Id = info.networkId;
            
            this.persistencyLevel = info.persistencyLevel.StringToEnum();
            Me.SetPersistencyLevel(this.persistencyLevel);
        }
    }

    protected void GetMyPeer()
    {
        SendToServer("GetObjectPeer",  Me.GetRoomObjectInfo());
    }


    public void UpdateRoom(RoomInfo newRoom)
    {
        notInRoom = false;
        timeRemaining = timeToDestroyObject;
        RoomObjectInfo info = Me.GetRoomObjectInfo();
        if(!owner && info.OwnerPeerUUID != "" && spawned)
        {
            Debug.Log("RoomObject: UpdateRoom: I'm not the owner, return");
            return;
        }
        Debug.Log("RoomObject: UpdateRoom: current room: " + info.Room.Name + ", new room: " + newRoom.Name);
        
        if(info.Room.UUID == newRoom.UUID || RoomToAssign.UUID == newRoom.UUID)
        {
            return;
        }
        RoomToAssign = newRoom;
        if(Id == new NetworkId())
        {
            Debug.Log("RoomObject ID is null, create new one");
            
            Id = IdGenerator.GenerateFromName(this.name+newRoom.Name);
            Me.SetNetworkId(Id);
        }

        SetObjectProperties();
        Debug.Log("Me[roomDictionaryKey]: " + Me[roomDictionaryKey]);

        SendToServer("UpdateObjectRoom", new RoomObjectRoomMessage(){
            room = newRoom,
            objectInfo = Me.GetRoomObjectInfo()
        });
    }

    public void SetMyPeer(PeerInfo peer)
    {
        Me.SetMyPeer(peer);
        if(owner)
        {
            OwnerUUID = peer.uuid;
            Me.SetOwnerUUID(peer.uuid);
        }
    }

    public void UpdateOwner(string ownerUUID)
    {
        if(!owner)
        {
            return;
        } 

        SetObjectProperties();

        SendToServer("UpdateObjectOwner", new RoomObjectOwnerMessage(){
            uuid = ownerUUID,
            objectInfo = Me.GetRoomObjectInfo()
        });
    }


    public void UpdateObject()
    {
        Debug.Log("UpdateObject");
        SetObjectProperties();
        var info = Me.GetRoomObjectInfo();
        Debug.Log("UpdateObject: Me[roomDictionaryKey]: " + Me[roomDictionaryKey]);
        SendToServer("UpdateObject", info);
    }

    protected void SendToServer(string type, object argument)
    {
        if(context == null)
        {
            context = NetworkScene.Register(this);
            Me.SetComponentId(context.componentId);
        }

        Debug.Log("RoomObject: SendToServer: type: " + type + ", args: " + argument.ToString());
        context.Send(RoomServerObjectId, RoomServerComponentId, JsonUtility.ToJson(new Message(type, argument)));
    }


    public virtual void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        messageProcessed = false;
        var container = JsonUtility.FromJson<Message>(message.ToString());
        switch (container.type)
        {
            case "SetMyPeer":
            {
                Debug.Log("RoomObject: " + name + " received message to set my peer, owner: "+owner);
                var peer = JsonUtility.FromJson<PeerInfo>(container.args);
                Me.SetMyPeer(peer);
                MyPeerUUID = peer.uuid;

                RoomObjectInfo info = Me.GetRoomObjectInfo();
                if(owner && info.Room.UUID != null && info.Room.UUID != "")
                {
                    UpdateOwner(peer.uuid);
                }
                
                if(owner)
                {
                    Me.SetOwnerUUID(peer.uuid);
                    OwnerUUID = peer.uuid;
                }

                messageProcessed = true;
            }
            break;
            case "OwnerUpdated":
            {
                Debug.Log("RoomObject: " + name + " received message to update owner");
                var args = JsonUtility.FromJson<RoomObjectOwnerMessage>(container.args);
                Debug.Log("RoomObject: " + name + ": msg.myPeer: " + args.myPeer.uuid);
                Debug.Log("RoomObject: " + name + ": msg.ownerPeer: " + args.uuid);
                Debug.Log("RoomObject: " + name + ": msg.objectInfo.Name: " + args.objectInfo.Name);
                
                if(inEmptyRoom && args.myPeer.uuid != args.uuid)
                {
                    owner = false;
                    DestroyLocalObject();
                }
                inEmptyRoom = false;
                Me["inEmptyRoom"] = JsonUtility.ToJson(false);
                if(owner && args.myPeer.uuid == args.uuid)
                {
                    if(meshRenderer != null)
                    {
                        meshRenderer.enabled = true;
                    }
                    OwnerUUID = args.uuid;
                    Me.SetOwnerUUID(args.uuid);
                    messageProcessed = true;
                    return;
                }
                owner = args.myPeer.uuid == args.uuid;
                OwnerUUID = args.uuid;
                Me.SetOwnerUUID(args.uuid);
                if(owner)
                {
                    
                    Me.Update(args.objectInfo);
                    Debug.Log("RoomObject: object info: " + args.objectInfo);
                    Debug.Log("RoomObject: RoomDictionaryKey: " + Me.GetRoomObjectInfo().RoomDictionaryKey);
                    Debug.Log("RoomObject: Properties to json: " + Me[Me.GetRoomObjectInfo().RoomDictionaryKey]);
                    RoomDictionaryInfoMessage<IRoomObjectState> info = JsonUtility.FromJson<RoomDictionaryInfoMessage<IRoomObjectState>>(Me[Me.GetRoomObjectInfo().RoomDictionaryKey]);

                    // this.catalogueIndex = info.catalogueIndex;
                    SetRoomObjectInfo(info);
                }
                messageProcessed = true;
            }
            break;
            case "RoomUpdated":
            {
                Debug.Log("RoomObject: " + name + " received message to update room");
                var args = JsonUtility.FromJson<RoomObjectRoomMessage>(container.args);
                Debug.Log("RoomObject: Assign new room: " + args.room.Name);
                Debug.Log("RoomObject: " + name + ": msg.myPeer: " + args.myPeer.uuid);
                Debug.Log("RoomObject: " + name + ": msg.objectInfo.Name: " + args.objectInfo.Name);
                Me.SetRoom(args.room);
                RoomName = args.room.Name;
                notInRoom = (args.room.UUID == "" || args.room.UUID == null);
                if(!notInRoom)
                {
                    timeRemaining = timeToDestroyObject;
                }
                RoomToAssign = new RoomInfo();
                RoomObjectInfo info = Me.GetRoomObjectInfo();
                if(owner && info.OwnerPeerUUID != info.MyPeer.uuid && info.Room.UUID != null && info.Room.UUID != "")
                {
                    UpdateOwner(args.myPeer.uuid);
                }

                //The owner of the object updated the room, update this peer object server side
                if(!owner && args.room.UUID != null && args.room.UUID != "")
                {
                    Debug.Log("RoomObject: I'm not owner check if my peer is in the same room as the object");
                    SetObjectProperties();

                    SendToServer("CheckPeerInObjectRoom",  Me.GetRoomObjectInfo());
                }
                messageProcessed = true;
            }
            break;
            case "ObjectUpdated":
            {
                Debug.Log("RoomObject: " + name + " received message to update object");
                var args = JsonUtility.FromJson<RoomObjectRoomMessage>(container.args);
            
                Me.SetRoom(args.room);
                Me.Update(args.objectInfo);
                messageProcessed = true;
            }
            break;
            case "InEmptyRoom":
            {
                Debug.Log("RoomObject: " + name + " received message InEmptyRoom");
                var args = JsonUtility.FromJson<RoomObjectRoomMessage>(container.args);
                Me["inEmptyRoom"] = JsonUtility.ToJson(true);
                inEmptyRoom = true;
                timeRemaining = timeToDestroyObject;
                
                if(meshRenderer != null)
                {
                    meshRenderer.enabled = false;
                }
                
                messageProcessed = true;

            }
            break;
            case "DestroyLocalObject":
            {
                Debug.Log("RoomObject: " + name + " received message DestroyLocalObject");
                var args = JsonUtility.FromJson<RoomObjectInfo>(container.args);

                DestroyLocalObject();
                messageProcessed = true;
            }
            break;
            case "ObjectOwnerLeft":
            {
                Debug.Log("RoomObject: " + name + " received message ObjectOwnerLeft");
                rb.isKinematic = false;
                messageProcessed = true;
            }
            break;
        }
    }

    public virtual void ObjectSpawned(RoomInfo room, string jsonObjectInfo)
    {
        Debug.Log("RoomObject: ObjectSpawned: room " + room.Name);
        RoomDictionaryInfoMessage<IRoomObjectState> objectInfo = JsonUtility.FromJson<RoomDictionaryInfoMessage<IRoomObjectState>>(jsonObjectInfo);
        Debug.Log("RoomObject: ObjectSpawned: jsonObjectInfo: " + jsonObjectInfo);
        SetRoomObjectInfo(objectInfo);
        
        Me.SetRoom(room);
        RoomName = room.Name;
    }

    public virtual void OnSpawned(bool local)
    {
        spawned = true;
        needsSpawning = false;
        owner = local;
    }
}