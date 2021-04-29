The NetworkScene itself does not create any connections.

In the Ubiq sample code, the RoomClient is responsible for making the connection to the rendezvous server. The RoomClient has a pre-shared IP and port, and it assumes that this server has a RoomServer running.

The Ubiq server Nexus runs a RoomServer. The RoomServer is implemented in NodeJs. The RoomServer's primary job is to enable rendezvous and forward messages.

## RoomClient Connections

The RoomServer listens for connections on a number of endpoints, including TCP ports and WebSockets. When one of these listening ports receives a new connection, an object is created to manage it: RoomPeer.

RoomPeer acts as a "virtual peer"; a mini-network is created with the user's machine, and the RoomPeer instance, and these peers can exchange network messages.

The RoomPeer also has a connection to the wider RoomServer however, and acts as a gatekeeper. RoomPeer, like a regular peer, has a set of Network Objects that can recieve messages. One of these is a counterpart to RoomClient. RoomClient can address the RoomServer via this object. RoomPeer will translate between the user's machine and the RoomServer instance, enabling the RoomClient to join a room. When RoomPeer is a member of a room, it will forward messages from its counterpart to the others in that room.

