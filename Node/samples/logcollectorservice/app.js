// 
// The LogCollectorService sample creates a programmatic peer that joins a room and records all Experiment log events it encounters.
//
// To achieve this, we first create a NetworkScene (the Peer), and create a connection for it to the server (which is specified here as Nexus).
// Then we add a RoomClient and LogCollector component(s). These join a room and recieve log messages. 
// 
// The LogCollector uses the Id of the peers to decide where to write the events. It creates new files on demand, and closes them when
// the corresponding Peer has left the room.
//

// Import Ubiq types
const { NetworkScene, RoomClient, LogCollector, UbiqTcpConnection } = require("../../ubiq");
const fs = require('fs');

// Create a connection to a Server
const connection = UbiqTcpConnection("nexus.cs.ucl.ac.uk", 8005);

// A NetworkScene
const scene = new NetworkScene();
scene.addConnection(connection);

// A RoomClient to join a Room
const roomclient = new RoomClient(scene);
const logcollector = new LogCollector(scene);

roomclient.addListener("OnJoinedRoom", room => {
    console.log(room.joincode);
});

// The stream
var stream = undefined;

// This snippet opens a new filename, by attempting to open files with a given pattern until an unused name is found.
// One the file has been opened, the userEventStream is piped into it.
var counter = 0;
function startNewUserFileStream(){
    var filename = `UserApplicationLog_${counter}.log.json`;
    if(stream !== undefined){
        stream.close();
        stream = undefined;
    }
    stream = fs.createWriteStream(filename,{
        flags: "wx"
    });
    stream.on("error", function(error){
        if(error.code == "EEXIST"){
            counter++;
            startNewUserFileStream();
        }
    })
    stream.on("open", function(){
        
    });
}

roomclient.addListener("OnPeerAdded", function() {
    if(roomclient.peers.size == 1){
        // The only time that the number of peers here is 1, is when a second
        // peer has joined for the first time (i.e. there is maybe one real
        // peer in a room).
        // When this happens, start a new log file, to roughly split the events
        // into files corresponding to 'sessions' of different groups in a room/
        // Note that participants do unexpected things. Do not use this as the
        // main way to distinguish sessions; always include identifying information
        // with each log event.
        startNewUserFileStream();
    }
})

roomclient.addListener("OnPeerRemoved", function(){
    if(roomclient.peers.size == 0){
        stream.close();
        stream = undefined;
    }
})

// Register for log events from the log collector.
logcollector.addListener("OnLogMessage", (type,message) =>
{
    console.log(JSON.stringify(message));
});

// Calling startCollection() will start streaming from the LogManagers at existing and
// and new Peers.
// The events will be buffered in userEventStream and applicationEventStream.
logcollector.startCollection();

roomclient.join("6765c52b-3ad6-4fb0-9030-2c9a05dc4731"); // Join by UUID. Use an online generator to create a new one for your experiment.
