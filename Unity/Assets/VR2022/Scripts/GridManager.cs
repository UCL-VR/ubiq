using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Rooms.Messages;
using UnityEngine;


public class GridManager : MonoBehaviour
{
    public Dictionary<string, RoomInfo> availableRooms = new Dictionary<string, RoomInfo>();
    public Grid grid;
    public NetworkScene networkScene;
    public RoomClient client;

    private float timeRoomsLastDiscovered;
    public float RoomRefreshRate = 50.0f;

    bool joinRoom = false;

    void Awake()
    {
        if(networkScene == null)
        {
            NetworkScene n = FindObjectOfType<NetworkScene>();
            if(n!=null)
            {
                networkScene = n;
            }   
        }

        if(grid == null)
        {
            Grid g = FindObjectOfType<Grid>();
            if(g != null)
            {
                grid = g;
            }   
        }

        client = networkScene.GetComponentInChildren<RoomClient>();
    }

    // Start is called before the first frame update
    void Start()
    {
        client.OnJoinedRoom.AddListener(OnJoinedRoom);
        client.OnJoinedRoom.AddListener(OnRoomCreated);
        grid.OnPlayerCellChanged.AddListener(OnPlayerCellChanged);
        grid.OnObjectEnteredCell.AddListener(OnObjectChangedCell);
        grid.OnObjectLeftCell.AddListener(OnObjectLeftCell);
        grid.OnEnteredCellBorder.AddListener(OnCellBorder);
        grid.OnLeftCellBorder.AddListener(OnLeftCellBorder);
    }

    void OnObjectLeftCell(CellEventInfo info)
    {
        RoomObject roomObject = info.go.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject).FirstOrDefault() as RoomObject;
        if(roomObject != null)
        {
            roomObject.OnObjectLeftCell(info.cell);
        }
    }

    void OnObjectChangedCell(CellEventInfo info)
    {
        RoomObject roomObject = info.go.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject).FirstOrDefault() as RoomObject;
        if(roomObject != null)
        {
            if(availableRooms.ContainsKey(info.cell.CellUUID))
            {
                roomObject.UpdateRoom(availableRooms[info.cell.CellUUID]);
            }
            else
            {
                // Server will create a new room with the cell name
                roomObject.UpdateRoom(new RoomInfo(info.cell.name, info.cell.CellUUID, "", true, new Ubiq.Dictionaries.SerializableDictionary()));
            }
        }
    }
    void OnPlayerCellChanged(CellEventInfo info)
    {
        if(availableRooms.Count == 0 || !availableRooms.ContainsKey(info.cell.CellUUID))
        {
            joinRoom = true;
            timeRoomsLastDiscovered = Time.time;
        }
        else if(availableRooms.Count > 0)
        {
            joinRoom = false;
            JoinRoom(availableRooms[info.cell.CellUUID]);
            timeRoomsLastDiscovered = Time.time;
        }
    }

    void OnCellBorder(CellEventInfo info)
    {
        if(info.go.name == "Player" || info.go.tag == "Player")
        {
            /*
            if(!client.RoomsToObserve.ContainsKey(info.cell.CellUUID))
            {
                client.Observe(new RoomInfo(info.cell.Name, info.cell.CellUUID, "", true, new Ubiq.Dictionaries.SerializableDictionary()));
            }
            */
        }
    }
    void OnLeftCellBorder(CellEventInfo info)
    {
        if(info.go.name == "Player" || info.go.tag == "Player")
        {
            /*
            if(client.RoomsToObserve.ContainsKey(info.cell.CellUUID))
            {
                client.StopObserve(new RoomInfo(info.cell.Name, info.cell.CellUUID, "", true, new Ubiq.Dictionaries.SerializableDictionary()));
            }
            */
        }
    }

    void OnRoomsAvailable(List<IRoom> rooms)
    {
        // Debug.Log("GridManager: OnRoomsAvailable");
        bool roomFound = false;
        availableRooms.Clear();
        foreach (var room in rooms)
        {
            availableRooms[room.UUID] = (RoomInfo) room;
            if(joinRoom && grid.PlayerCell != null && grid.PlayerCell.CellUUID == room.UUID)
            {
                roomFound = true;
                JoinRoom((RoomInfo) room);
            }
        }

        if(grid.PlayerCell != null && joinRoom && !roomFound)
        {
            CreateRoom(grid.PlayerCell.Name, grid.PlayerCell.CellUUID);
            availableRooms[grid.PlayerCell.CellUUID] = new RoomInfo();
        }

        if(grid.PlayerCell != null && !joinRoom)
        {
            UpdateObservedRooms();
        }

        joinRoom = false;
    }

    public void UpdateObservedRooms()
    {

    }

    public void JoinRoom(RoomInfo room)
    {
        // Start observing to the current room
        if(client.Room.UUID != null && client.Room.UUID != "")
        {
            // Debug.Log("Observe current room");
        }
        // Join the new room, leaves the current room in the process
        // If I'm already observing to the new room, also stops observing it
        client.Join(room.JoinCode);
        
        return;
    }

    public void CreateRoom(string name, string uuid)
    {
    }

    void OnJoinedRoom(IRoom room)
    {
    }

    void OnRoomCreated(IRoom room)
    {
        JoinRoom((RoomInfo) room);
    }
}
