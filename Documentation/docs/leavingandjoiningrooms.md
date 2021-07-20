# Leaving and Joining

When a `RoomClient` starts it is automatically a member of an empty, unidentified, room.

A `RoomClient` can create a new room or join an existing one at any time. Joining or leaving a room involes a *room change*. Changing the room is the same whether the client is going to or from an empty room, or between non-empty rooms.

When the room changes `RoomClient` will emit a number of events, in order:

1. `OnPeerRemoved` for any peers that go out of scope
2. `OnPeerAdded` for any peers that come into scope
3. `OnPeerUpdated` for any peers that are in scope but whose properties have changed
4. `OnJoinedRoom` with the new room
5. `OnRoomUpdated` with the new room

Whether a Peer is in *scope* means whether or not it is available to current Peer. If two Peers moved to another room at the same time, they would remain in scope because the Peers themselves remain in the same rooms, even though that room has changed. On the other hand, if a Peer joins a different Room, it will go out of scope, as it is no longer in the same room, even though it may still be connected to the server.

The purpose of rooms is to facilitate message exchanging between specific sets of peers. Most Components should use the Peer events to create, destroy and update objects. The Room events are typically used when code needs to control room membership, for example the UI panels for joining rooms.

There is no such thing as leaving a room; underneath, leaving a Room means joining a new, empty Room.