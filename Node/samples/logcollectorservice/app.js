// The LogCollectorService sample creates a programmatic peer that joins a room 
// and records all Experiment Log Events (0x2) it encounters.
//
// To achieve this, we first create a NetworkScene (the Peer), and create a 
// connection for it to the server (which is specified here as Nexus). Then we 
// add a RoomClient and LogCollector component(s). These join a room and recieve 
// log messages.
// 
// The LogCollector uses the Id of the peers to decide where to write the events.
// It creates new files on demand, and closes them when the corresponding Peer 
// has left the room.

// Import Ubiq types
const { NetworkScene, UbiqTcpConnection } = require("ubiq");
const { LogCollector, RoomClient } = require("components");
const fs = require('fs');

const config = JSON.parse(fs.readFileSync(__dirname + "/../config.json"));

// Configuration
eventType = 2;

// Create a connection to a Server
const connection = UbiqTcpConnection(config.tcp.uri, config.tcp.port);

// A NetworkScene
const scene = new NetworkScene();
scene.addConnection(connection);

// A RoomClient to join a Room
const roomclient = new RoomClient(scene);
const logcollector = new LogCollector(scene);

// A list of open files to write events for particular peers into (we can close
// these when the peers leave the room)
const files = {};

function writeEventToPeerFile(peer, message){
    if(!files.hasOwnProperty(peer)){
        files[peer] = fs.createWriteStream(`${peer}.log.json`,{
            flags: "a"
        });
    }
    files[peer].write(JSON.stringify(message));
}

function closePeerFile(peer){
    if(files.hasOwnProperty(peer)){
        delete files[peer];
    }
}

roomclient.addListener("OnJoinedRoom", room => {
    console.log("Joined Room with Join Code " + room.joincode);
});

roomclient.addListener("OnPeerAdded", peer =>{
    console.log("New Peer " + peer.uuid + " joined Room");
});

roomclient.addListener("OnPeerRemoved", peer =>{
    closePeerFile(peer);
});

// Register for log events from the log collector.
logcollector.addListener("OnLogMessage", (type,message) => {
    if(type == eventType){ // Experiment
        peer = message.peer; // All log messages include the emitting peer
        writeEventToPeerFile(peer,message);
    }
});

// Calling startCollection()/lockCollection() will start streaming from the 
// LogManagers at existing and and new Peers. Call this before joining a new 
// room.
// lockCollection() is like startCollection(), but the Collector will 
// automatically maintain its status as the primary collector for as long as it
// runs.
logcollector.lockCollection();

// Join by UUID. Use an online generator to create a new one for your 
// experiment.
roomclient.join(config.room); 
