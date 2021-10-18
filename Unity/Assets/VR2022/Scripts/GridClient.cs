// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System;
// using UnityEngine;
// using Ubiq.Avatars;
// using Ubiq.Messaging;
// using Ubiq.Rooms;
// using Ubiq.Rooms.Messages;
// using Ubiq.Dictionaries;
// public class GridClient : MonoBehaviour
// {
//     [SerializeField] public NetworkScene networkScene;
//     private RoomClient client;

//     public HexCell hexCell;

//     public List<RoomInfo> neighborRooms = new List<RoomInfo>();

//     public GridManager gridManager;
    

//     // Start is called before the first frame update
//     void Start()
//     {
//         gridManager = GetComponentInParent<GridManager>();
//         client = gridManager.roomClient;
//         client.OnJoinedRoom.AddListener(OnJoinedRoom);
//         client.OnCreatedRoom.AddListener(OnRoomCreated);
//     }

//     public void UpdateObservedRooms()
//     {
//         // Debug.Log("GridClient: UpdateObservedRooms");
//         if(client.Room.UUID == null || client.Room.UUID == "")
//         {
//             return;
//         }
//          foreach(var item in gridManager.grid.PlayerCell.Neighbors)
//         {
//             if(gridManager.availableRooms.ContainsKey(item.Key) && !client.RoomsToObserve.ContainsKey(item.Key))
//             {
//                 client.Observe(gridManager.availableRooms[item.Key]);
//             }
//             else if(!client.RoomsToObserve.ContainsKey(item.Key))
//             {
//                 client.Observe(new RoomInfo(item.Value.Name, item.Value.CellUUID, "", true, new Ubiq.Dictionaries.SerializableDictionary()));
//             }
//         }

//         foreach (var item in client.RoomsToObserve)
//         {
//             if(!gridManager.grid.PlayerCell.Neighbors.ContainsKey(item.Key) && item.Value.Name != gridManager.grid.PlayerCell.Name)
//             {
//                 client.StopObserve(new RoomInfo(item.Value.Name, item.Value.UUID, 
//                                                 item.Value.JoinCode, item.Value.Publish, 
//                                                 new SerializableDictionary()));
//             }
//         }
//     }

//     public void JoinRoom(RoomInfo room)
//     {
//         // Start observing to the current room
//         if(client.Room.UUID != null && client.Room.UUID != "")
//         {
//             // Debug.Log("Observe current room");
//             client.Observe(client.Room);
//         }
//         // Join the new room, leaves the current room in the process
//         // If I'm already observing to the new room, also stops observing it
//         client.Join(room.JoinCode);
        
//         return;
//     }

//     public void CreateRoom(string name, string uuid)
//     {
//         client.Create(name,true,"", uuid);
//     }

//     void OnJoinedRoom(IRoom room)
//     {
//         // Debug.Log("GridClient: OnJoinedRoom");
//         if(room.UUID != gridManager.grid.PlayerCell.CellUUID)
//         {
//             // Debug.Log("GridClient: OnJoinedRoom: Room uuid not the same as player cell uuid");
//             return;
//         }
        
//         if(room.UUID == "" || room.UUID == null)
//         {
//             // Debug.Log("GridClient: OnJoinedRoom: Room uuid null");
//             return;
//         }

//         client.GetRooms();
//     }

//     void OnRoomCreated(IRoom room)
//     {
//         JoinRoom((RoomInfo) room);
//     }
// }
