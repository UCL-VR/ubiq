// This is the main entry point for the Samples
// The Samples directory contains a set of classes that implmement
// Ubiq Sample functionality in Node
// The app shows how to use them.

// Import Ubiq types
const { NetworkScene, UbiqTcpConnection } = require("../ubiq");
const { RoomClient } = require("./roomclient");
const { LogCollector } = require("./logcollector")

// Create a connection to a Server
const connection = UbiqTcpConnection("localhost", 8002);

// A NetworKScene
const scene = new NetworkScene();
scene.addConnection(connection);

// A RoomClient to join a Room
const roomclient = new RoomClient(scene);
const logcollector = new LogCollector(scene);

// Configure

roomclient.addListener("OnPing", ()=>{
    console.log("Received Ping");
})

roomclient.addListener("OnJoinedRoom", room => {
    console.log(room.joincode);
})

logcollector.startCollection();

roomclient.join(); // no parameters means create a new room
