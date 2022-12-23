using System;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Networking;
using Ubiq.Messaging;
using Ubiq.Dictionaries;
using System.Linq;
using Ubiq.Rooms.Messages;

namespace Ubiq.Rooms
{
    /// <summary>
    /// Implements a Room Server. Intended to be used for debugging and development.
    /// The server only supports one room which all clients connect to regardless of their join arguments.
    /// </summary>
    public class RoomServer : MonoBehaviour
    {
        // private NetworkId objectId = new NetworkId(1);

        // private Room room;
        // public ConnectionDefinition connection;

        // private INetworkConnectionServer server;
        // private List<Client> clients;

        // private List<Action> actions;

        // public DiscoverRoomsResponse AvailableRooms
        // {
        //     get
        //     {
        //         DiscoverRoomsResponse args = new DiscoverRoomsResponse();
        //         args.rooms = new List<RoomInfo>();
        //         args.rooms.Add(room.GetRoomArgs());
        //         args.version = "0.0.4";
        //         return args;
        //     }
        // }

        // private class Room
        // {
        //     public string uuid;
        //     public string name;
        //     public List<PeerInfo> peers = new List<PeerInfo>();
        //     public SerializableDictionary properties = new SerializableDictionary();
        //     public List<Client> clients = new List<Client>();

        //     public RoomInfo GetRoomArgs()
        //     {
        //         return new RoomInfo(name, uuid, "", false, properties);
        //     }

        //     public void Join(Client client)
        //     {
        //         if(peers.Any(p => p.uuid == client.peer.uuid)) // already joined
        //         {
        //             SendRoomUpdate();
        //             return;
        //         }

        //         // join the room - for now this just means adding to the locked list
        //         peers.Add(client.peer);

        //         clients.Add(client);
        //         client.room = this;

        //         // send confirmation to the client
        //         client.SendSetRoom(new SetRoom()
        //         {
        //             room = GetRoomArgs(),
        //             peers = peers,
        //         });

        //         SendPeerUpdate(client.peer);
        //     }

        //     public void SendPeerUpdate(PeerInfo args)
        //     {
        //         foreach (var item in clients)
        //         {
        //             item.SendPeer(args);
        //         }
        //     }

        //     public void SetRoomArgs(RoomInfo args)
        //     {
        //         this.uuid = args.UUID;
        //         this.name = args.Name;
        //         this.properties = new SerializableDictionary(args);
        //         SendRoomUpdate();
        //     }

        //     public void SendRoomUpdate()
        //     {
        //         foreach (var item in clients)
        //         {
        //             item.SendRoom(GetRoomArgs());
        //         }
        //     }
        // }

        // private class Client
        // {
        //     public INetworkConnection connection;
        //     public PeerInfo peer;
        //     public Room room;

        //     public Client(INetworkConnection connection)
        //     {
        //         this.connection = connection;
        //     }

        //     public void Send(string type, object argument)
        //     {
        //         var msg = ReferenceCountedSceneGraphMessage.Rent(JsonUtility.ToJson(new Message(type, argument)));
        //         msg.objectid = peer.networkId;
        //         Send(msg);
        //     }

        //     public void Send(ReferenceCountedSceneGraphMessage m)
        //     {
        //         connection.Send(m.buffer);
        //     }

        //     public void SendSetRoom(SetRoom args)
        //     {
        //         Send("SetRoom", args);
        //     }

        //     public void SendRoom(RoomInfo args)
        //     {
        //         Send("UpdateRoom", args);
        //     }

        //     public void SendPeer(PeerInfo args)
        //     {
        //         Send("UpdatePeer", args);
        //     }

        //     public void SendRooms(DiscoverRoomsResponse args)
        //     {
        //         Send("Rooms", args);
        //     }

        //     public void SendPing(PingResponse args)
        //     {
        //         Send("Ping", args);
        //     }
        // }

        // private void Awake()
        // {
        //     room = new Room();
        //     room.uuid = Guid.NewGuid().ToString();
        //     room.name = "Sample Room";

        //     clients = new List<Client>();
        //     actions = new List<Action>();

        //     server = new TCPServer(connection.listenOnIp, connection.listenOnPort);
        //     server.OnConnection = OnConnection;
        // }

        // private void OnDestroy()
        // {
        //     server.Dispose();
        // }

        // private void OnConnection(INetworkConnection connection)
        // {
        //     clients.Add(new Client(connection)); // add new clients to the outstanding clients list. once negotiated, they will be moved to a room.
        // }

        // private void Update()
        // {
        //     foreach (var client in clients)
        //     {
        //         while (true)
        //         {
        //             var buffer = client.connection.Receive();

        //             if(buffer == null)
        //             {
        //                 break;
        //             }

        //             try
        //             {
        //                 var message = new ReferenceCountedSceneGraphMessage(buffer);

        //                 // check if this message is meant for us, or if we are to forward it

        //                 if (message.objectid == this.objectId)
        //                 {
        //                     var container = JsonUtility.FromJson<Message>(message.ToString());
        //                     switch (container.type)
        //                     {
        //                         case "Join":
        //                             var joinArgs = JsonUtility.FromJson<JoinRequest>(container.args);
        //                             client.peer = joinArgs.peer;
        //                             room.Join(client);
        //                             break;
        //                         case "UpdatePeer":
        //                             client.peer = JsonUtility.FromJson<PeerInfo>(container.args);
        //                             if (client.room != null)
        //                             {
        //                                 client.room.SendPeerUpdate(client.peer);
        //                             }
        //                             break;
        //                         case "UpdateRoom":
        //                             room.SetRoomArgs(JsonUtility.FromJson<RoomInfo>(container.args));
        //                             break;
        //                         case "RequestRooms":
        //                             client.SendRooms(AvailableRooms);
        //                             break;
        //                         case "Ping":
        //                             client.SendPing(new PingResponse() { sessionId = "sampleroom" });
        //                             break;
        //                         case "":

        //                             break;
        //                     }
        //                 }
        //                 else
        //                 {
        //                     if (client.room != null) // if it is a member of a room...
        //                     {
        //                         foreach (var item in client.room.clients)
        //                         {
        //                             if (item != client)
        //                             {
        //                                 message.Acquire();
        //                                 item.Send(message);
        //                             }
        //                         }
        //                     }
        //                 }
        //             }
        //             finally
        //             {
        //                 buffer.Release();
        //             }
        //         }
        //     }

        //     // actions to be taken once outside the enumators (e.g. removing items from the lists...)

        //     foreach (var item in actions)
        //     {
        //         item();
        //     }
        //     actions.Clear();
        // }


    }
}