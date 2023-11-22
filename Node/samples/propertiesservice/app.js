// The PropertiesService sample demonstrates how to use the Peer and Room
// properties procedurally.
// This application creates a process that joins a room, and listens for changes
// to a specific Property - "propertiesservicekey" - on all Peers in the Room.

import { NetworkScene, UbiqTcpConnection } from 'ubiq'
import { RoomClient } from 'components';
import nconf from 'nconf'

// This sample must be started from the root of the Node directory. I.e.,
// > node --loader ts-node/esm samples/propertiesservice/app.js

nconf.file('default', "config/samples.json")
const config = nconf.get()

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