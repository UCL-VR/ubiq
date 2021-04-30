# Servers and Rooms

As described in the Getting Started Guide, the server is a central service that clients can use to rendezvous over the internet. Rooms contain sets of users who can exchange messages.

A public server at a fixed location is necessary to allow users to rendezvous over the internet, where service discovery is impossible.

## Rooms and Messaging

The purpose of rooms is to control which users exchange messages with which.

At the messaging layer, Ubiq blindly transmits messages across a Connection and expects the network to deliver them to matching objects. The connection made by the RoomClient to the Server is the same one used to deliver messages between networked objects. When the server receives a message, it forwards it to all other peers in the room.

After connecting to the server, a client begins in an empty room, and so its messages will not be forwarded to anyone.

The RoomClient can join an existing room by exchanging messages with the RoomServer. The RoomServer is a 'virtual' networked object that exists on the server and listens for messages addressed to a particular Object/Component Id.

Once the RoomClient has sucessfully joined a room via the RoomServer, messages will begin to be forwarded between that client and the other members.

## Rooms

Rooms themselves are objects that exist in the RoomServer. Rooms contain a list of Peers, as well as a general purpose dictionary for storing low-frequency persistent information. Each Peer object contains the Socket used to communicate with the client.

## Services

The RoomServer is one service that exists on the server. Other services include simple Blob management.