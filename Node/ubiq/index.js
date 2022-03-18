// The Ubiq Package contains a set of NodeJs classes and functions
// for interacting with the Ubiq Messaging layer.

// This package supports establishing, or listening for, a connection
// to a Ubiq Peer and exchanging Ubiq Messages with it. It also includes
// Helper functions, such as those for creating Unique Identifiers.

// This package is part of the Ubiq Server, but can also be used to 
// build NodeJs based (Client) Peers.

const { 
    WrappedWebSocketServer,
    WrappedTcpServer,
    WebSocketConnectionWrapper,
    TcpConnectionWrapper,
    UbiqTcpConnection
} = require("./connections")

const { 
    Message,
    NetworkId
} = require('./messaging');

const { 
    Schema
} = require("./schema");

const { 
    Uuid,
} = require("./uuid");

const {
    NetworkContext,
    NetworkScene
} = require("./networkscene");

const {
    SerialisedDictionary
} = require("./dictionary");

const{
    RoomClient
} = require("./roomclient");

const{
    LogCollector
} = require("./logcollector");

module.exports = {
    WebSocketConnectionWrapper,
    WrappedWebSocketServer,
    TcpConnectionWrapper,
    WrappedTcpServer,
    Message,
    NetworkId,
    NetworkContext,
    NetworkScene,
    RoomClient,
    LogCollector,
    Schema,
    SerialisedDictionary,
    Uuid,
    UbiqTcpConnection    
}