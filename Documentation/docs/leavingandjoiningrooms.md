# Leaving and Joining

When a `RoomClient` starts it is automatically a member of an empty, unidentified, room.

A `RoomClient` can create a new room or join an existing one at any time. Joining or leaving a room involes a *room change*. Changing the room is the same whether the client is going to or from an empty room, or between non-empty rooms.

When the room changes `RoomClient` will emit a number of events in order:

1. `OnLeftRoom` with the old room
2. `OnPeerRemoved` for any peers that are not in the new room
3. `OnJoinedRoom` with the new room
4. `OnRoom` with the new room
5. `OnPeer` for all updated peers

As usual, `OnPeer` and `OnRoom` may be called even if there is no change in the `PeerInfo` or `RoomInfo`.

## Synchronising Peers

Two peers attempting to join a room at the same time could result in a race condition. This is resolved at the server: when a peer joins a room, it receives a list of all other peers in the room. This list is always sent before the next peer joins.

When `OnJoinedRoom` is emitted, `RoomClient::Peers` will contain the existing peers in the room exactly, no more and no less.

This event can be used to distinguish then between 'old' or 'existing' and 'new' peers. Any peer that was in `RoomClient::Peers` when `OnJoinedRoom` was called, joined the room before `RoomClient`, and any peer that was not in `RoomClient::Peers` joined the room after.

For example, this is used by the audio chat manager to decide which peer will start the process of creating an audio channel, without those peers having to explicitly communicate, using only the rule that new peers must make the connection to existing ones.