#Overview

The Room Server service consists of a number of objects that work together to route messages to either the command and control interface on the server itself, or to other peers.

![Object Graph of Client-Server](images/5041e3f3-bf2e-41df-837b-20546521b008.svg)

## RoomClient Connections

The `NetworkScene` itself does not create any connections.

In the Ubiq sample code, the `RoomClient` is responsible for making the initial connection to the rendezvous server. The RoomClient has a pre-shared IP and port, which it assumes points to an endpoint with a Room Server running. The Ubiq server Nexus runs a Room Server.

When `RoomClient` starts up, it instructs the `NetworkScene` to make the connection. The underlying connection may be over TCP, WebSockets, or any other supported protocol. Once established the `NetworkScene` will assume it can be used to exchange Ubiq messages however.

On the server, new connections are wrapped with a `RoomPeer` object. The `RoomPeer` is able to parse Ubiq messages. It also has references to the global `RoomServer` object. The `RoomPeer` acts as a gatekeeper. It parses messages intended for the `RoomServer` and calls the appropriate APIs. Other messages it forwards to the `Room` it is a member of.

The APIs it invokes in response to `RoomClient` messages will cause the `RoomServer` object to move the `RoomPeer` between different rooms (or remove it from all rooms).
