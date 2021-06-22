# Heartbeat

`RoomClient` instances will ping the RoomServer at a 1 Hz. If they do not receive a response after 5 seconds, they consider the server to have timed out, and will notify the user.

# Timeouts

If a client drops a connection to a server, the server will inform all other peers by emitting `OnPeerRemoved`.

The server will only remove a client when the underlying TCP connection has been closed. Until this time the client may re-appear. 

If a client connection breaks without shutting down cleanly, there may be a delay during which the Peer is in the room, but cannot exchange messages. Components should be robust to this. How they behave will depend on their use-case. For example, if a peer existed in the room but had no effect on it, there would be no need to detect this case at all. The Avatar class uses interruptions in its transform stream to detect a disconnected peer, and will hide an Avatar after a few seconds if no data is received.

An easy way for Components to handle this case is to ensure they are created under a Peer's Avatar, in which case they will be enabled and disabled with the Avatar.

`RoomClient` instances do not necessarily control the connections to `RoomServer` and so cannot detect if these are lost. `RoomClient` instances ping the `RoomServer` routinely and if they do not get a response after a set time, will disconnect the connections they are responsible before and consider the room left.