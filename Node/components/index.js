// The Components Package contains a set of NodeJs classes and functions
// implementing common Ubiq Peer Components.

// The components include a RoomClient and LogCollector, and more will be
// added in the future.

// This package can be used to build typical Ubiq Peers for Node.

const { LogCollector} = require("./logcollector")
const { RoomClient } = require("./roomclient")
const { PeerConnectionManager } = require("./peerconnectionmanager")
const { AvatarManager, ThreePointTrackedAvatar } = require("./avatarmanager")

module.exports = {
    LogCollector,
    RoomClient,
    PeerConnectionManager,
    AvatarManager,
    ThreePointTrackedAvatar
}