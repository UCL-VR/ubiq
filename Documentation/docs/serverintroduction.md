# Servers and Rooms

Ubiq provides a Rooms system and a server. These work together to provide a fixed endpoint that allows users to rendezvous over the internet, where service discovery is impossible.

The Rooms system - the RoomClient and RoomServer - provide the concepts of Rooms and Peers. A Peer is a remote player. A Room is a place that multiple Peers can join. The example Server implements a RoomServer that allows Peers to find and join Rooms, and facilitates message exchange between the Rooms members.

## Rooms and Messaging

The purpose of rooms is to control which users exchange messages with which.

At the messaging layer, Ubiq transmits messages across a Connection and expects the network to deliver them to matching objects. The connection made by the RoomClient to the Server is the same one used to deliver messages between networked objects. When the server receives a message, it forwards it to all other peers in the room.

After connecting to the server, a client begins in an empty room. Its messages will not be forwarded to anyone.

The RoomClient can join an existing room by exchanging messages with the RoomServer. The RoomServer is a 'virtual' networked object that exists on the server and listens for messages addressed to a particular Object/Component Id.

Once the RoomClient has sucessfully joined a room via the RoomServer, messages will begin to be forwarded between that client and the other members.

## Rooms

Rooms themselves are objects that exist in the RoomServer. Rooms contain a list of Peers, as well as a general purpose dictionary for storing low-frequency persistent information. Each Peer object contains the Socket used to communicate with the client.

## Services

The RoomServer is one service that exists on the server. Other services include simple Blob management.