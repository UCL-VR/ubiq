// The PropertiesService sample demonstrates how to use the Peer and Room
// properties procedurally.
// This application creates a process that joins a room.
// It uses the RoomClient class to set properties in both the Peer and the
// Room. It will also respond to specific properties set on other Peers.

const { NetworkScene, UbiqTcpConnection } = require("ubiq");
const { RoomClient } = require("components");
const fs = require('fs')

const config = JSON.parse(fs.readFileSync(__dirname + "/../config.json"));

// Create a connection to a Server
const connection = UbiqTcpConnection(config.tcp.uri, config.tcp.port);

// A NetworkScene
const scene = new NetworkScene();
scene.addConnection(connection);

// A RoomClient to join a Room
const roomclient = new RoomClient(scene);

roomclient.addListener("OnJoinedRoom", room => {
    console.log("Joined Room with Join Code " + room.joincode);
});

roomclient.addListener("OnPeerAdded", peer =>{
    console.log("New Peer " + peer.uuid + " joined Room");
});

roomclient.addListener("OnPeerUpdated", peer =>{
    let value = peer.getProperty("propertiesservicekey");
    if(value !== undefined){
        console.log("New PropertiesService Key Value: " + value);
        roomclient.peer.setProperty("propertiesservicemirror",value);
    }
})

roomclient.join(config.room);