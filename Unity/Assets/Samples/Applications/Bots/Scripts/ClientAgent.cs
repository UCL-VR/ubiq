using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;
using Ubiq.Networking;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Rooms.Messages;
using Ubiq.Dictionaries;
using Ubiq.Samples;
using System.Linq;
using UnityEngine.Profiling;

/*
public class ClientAgent : MonoBehaviour
{
    public Transform[] TargetPoints;
    public Transform currentTarget;
    NavMeshAgent agent;

    Dictionary<string,RoomInfo> Available;
    
    public RoomInfo roomInfo;

    public Grid grid;

    public Cell currentCell;

    bool joinRoom = false;
    bool updateObservers = false;

    public float Speed;

    public string localPrefabUuid;

    private AvatarArgs avatarArgs;

    public bool showAvatar;

    public float walkRadius;

    public GameObject myLocalTorso;
    public GameObject myRemoteAvatar;
    
    public float avatarLagDistance;

    LineRenderer lineRenderer;

    Dictionary<string,string> Properties = new Dictionary<string, string>();

    public float PositionServerUpdateFrequency = 5.0f;

    public float RequiredPositionChangeForServerUpdate = 3.0f;
    Vector3 LastUpdatePosition;

    float lastUpdate;

    public RoomClient client;

    public bool UpdateServerPosition = false;

    void Awake()
    {
        
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        
        Available = new Dictionary<string,RoomInfo>();
        GetComponent<CapsuleCollider>().enabled = false;

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

        foreach (var item in GetComponentsInChildren<MeshRenderer>())
        {
            item.enabled = showAvatar;
            if(myLocalTorso == null && item.gameObject.name.Contains("Torso"))
            {
                myLocalTorso = item.gameObject;
            }
        }

        foreach (var item in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            item.enabled = showAvatar;
        }

    }
    // Start is called before the first frame update
    void Start()
    {

        
        client.OnRoomsAvailable.AddListener(OnRoomsAvailable);
        client.OnCreatedRoom.AddListener(OnRoomCreated);
        client.OnJoinedRoom.AddListener(OnJoinedRoom);
        roomInfo = new RoomInfo();
        
        if(grid == null)
        {
            grid = FindObjectOfType<Grid>();
        }
        grid.OnObjectEnteredCell.AddListener(ObjectEnteredCell);
        

        localPrefabUuid = "Floating BodyA Avatar";
        avatarArgs = new AvatarArgs();
        avatarArgs.objectId = IdGenerator.GenerateUnique();
        avatarArgs.prefabUuid = localPrefabUuid;
        
        transform.parent.GetComponentInChildren<Ubiq.Avatars.Avatar>().Id = avatarArgs.objectId;
        
        
        client.Me["avatar-params"] = JsonUtility.ToJson(avatarArgs);
        if(UpdateServerPosition)
            client.Me["position"] = JsonUtility.ToJson(myLocalTorso.transform.position);

    }

    public void SetTargetPoints(Transform[] points)
    {
        TargetPoints = points;
    }

    private class AvatarArgs
    {
        public NetworkId objectId;
        public string prefabUuid;
        public SerializableDictionary properties;

        public AvatarArgs()
        {
            properties = new SerializableDictionary();
        }
    }

    void FixedUpdate()
    {
        

        
    }
    // Update is called once per frame
    void Update()
    {
        
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextPoint();
        }

        // if(Time.time - lastUpdate > PositionServerUpdateFrequency)
        // {
        //     Properties["position"] = JsonUtility.ToJson(myLocalTorso.transform.position);
        //     peerInfo = new PeerInfo(peerInfo.UUID, context.networkObject.Id, new SerializableDictionary(Properties));
        //     SendToServer("UpdatePeer", peerInfo);
        //     lastUpdate = Time.time;
        // }
        
        if(UpdateServerPosition && LastUpdatePosition == null || 
            Vector3.Distance(LastUpdatePosition, myLocalTorso.transform.position) > RequiredPositionChangeForServerUpdate ||
            Time.time - lastUpdate > PositionServerUpdateFrequency)
        {
            LastUpdatePosition = myLocalTorso.transform.position;
            // Properties["position"] = JsonUtility.ToJson(LastUpdatePosition);
            // peerInfo = new PeerInfo(peerInfo.UUID, context.networkObject.Id, new SerializableDictionary(Properties));
            client.Me["position"] = JsonUtility.ToJson(LastUpdatePosition);
            // SendToServer("UpdatePeer", peerInfo);
            lastUpdate = Time.time;
            
               
        }

        GetComponent<CapsuleCollider>().enabled = true;
        
        if(grid.PlayerCell == null || currentCell == null)
            return;

        if((grid.PlayerCell.CellUUID == currentCell.CellUUID || 
            client.RoomsToObserve.ContainsKey(grid.PlayerCell.CellUUID)) &&
            myRemoteAvatar == null)
        {
            myRemoteAvatar = GameObject.Find("Remote Avatar #" + avatarArgs.objectId.ToString());
        }

        if(myRemoteAvatar != null)
        {
            Vector3 torsoPosition = myRemoteAvatar.GetComponentInChildren<FloatingAvatar>().torso.position;
            avatarLagDistance = Vector3.Distance(myLocalTorso.transform.position, torsoPosition);
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, myLocalTorso.transform.position);
            lineRenderer.SetPosition(1, torsoPosition); 
            
        }
        else
        {
            lineRenderer.enabled = false;
        }
        
    }

    Vector3 GetRandomPointOnNavMesh()
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        
        int t = UnityEngine.Random.Range(0, triangulation.indices.Length-3);

        Vector3 point = Vector3.Lerp(triangulation.vertices[triangulation.indices[t]], 
                                    triangulation.vertices[triangulation.indices[t+1]], 
                                    UnityEngine.Random.value);
                                    
        Vector3.Lerp(point, triangulation.vertices[triangulation.indices[t+2]], UnityEngine.Random.value);

        return point;
    }

    void Wander()
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * walkRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, walkRadius, 1);
        Vector3 finalPosition = hit.position;
        agent.destination = finalPosition;
    }

    void GoToNextPoint()
    {
        if (TargetPoints == null || TargetPoints.Length == 0)
            return;
        
        var newTarget = TargetPoints[UnityEngine.Random.Range(0, TargetPoints.Length)];
        agent.destination = newTarget.position;
        currentTarget = newTarget;
    }



    public void ObjectEnteredCell(CellEventInfo info)
    {
        ClientAgent clientAgent = info.go.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is ClientAgent).FirstOrDefault() as ClientAgent;
       
        if(clientAgent != null && clientAgent.client.Me.UUID == client.Me.UUID)
        {
            currentCell = info.cell;
        
            if(client.Available.Count == 0 || !Available.ContainsKey(info.cell.CellUUID))
            {
                joinRoom = true;
                client.GetRooms();
            }
            else if(Available.Count > 0)
            {
                joinRoom = false;
                JoinRoom(Available[info.cell.CellUUID]);
                client.GetRooms();
            }
        }
    }


    public void JoinRoom(RoomInfo room)
    {
        if(roomInfo.UUID != null && roomInfo.UUID != "")
        {
            client.Observe(roomInfo);
        }
    }

    public void UpdateRoomObservers()
    {
        updateObservers = false;
        if(roomInfo.UUID == null || roomInfo.UUID == "")
        {
            return;
        }
       
        foreach(var item in currentCell.Neighbors)
        {
            if(Available.ContainsKey(item.Key) && !client.RoomsToObserve.ContainsKey(item.Key))
            {
                client.Observe(Available[item.Key]);
            }
            else if(!client.RoomsToObserve.ContainsKey(item.Key))
            {
               client.Observe(new RoomInfo(item.Value.name, item.Value.CellUUID, "", true, new Ubiq.Dictionaries.SerializableDictionary()));
            }
        }

        foreach (var item in client.RoomsToObserve)
        {
            if(!currentCell.Neighbors.ContainsKey(item.Key) && item.Value.UUID != currentCell.CellUUID)
            {
                client.StopObserve((RoomInfo) item.Value);
            }
        }


    }

    public void OnRoomsAvailable(List<IRoom> rooms)
    {
        bool roomFound = false;
        Available.Clear();
        foreach (var room in rooms)
        {
            Available[room.UUID] = (RoomInfo) room;
            if(joinRoom && currentCell != null && currentCell.CellUUID == room.UUID)
            {
                roomFound = true;
                JoinRoom((RoomInfo) room);
            }
        }

        if(grid.PlayerCell != null && joinRoom && !roomFound)
        {
            client.Create(currentCell.name ,true,"", currentCell.CellUUID);
            Available[currentCell.CellUUID] = new RoomInfo();
        }

        if(currentCell != null && !joinRoom)
        {
            UpdateRoomObservers();
        }

        joinRoom = false;
    }

    void OnRoomCreated(IRoom room)
    {
        if(room.UUID == currentCell.CellUUID)
            client.Join(room.JoinCode);
    }

    void OnJoinedRoom(IRoom room)
    {
        if(room.UUID != currentCell.CellUUID)
        {
            return;
        }
        
        if(room.UUID == "" || room.UUID == null)
        {
            return;
        }

        roomInfo = (RoomInfo) room;
        client.GetRooms();
    }
}

*/