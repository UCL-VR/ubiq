// This is the main entry point for the Samples
// The Samples directory contains a set of classes that implmement
// Ubiq Sample functionality in Node
// The app shows how to use them.

// Import Ubiq types
const { NetworkScene, UbiqTcpConnection } = require("../ubiq");
const { RoomClient } = require("./roomclient");
const { LogCollector, MyStream } = require("./logcollector");
const fs = require('fs');

// Create a connection to a Server
const connection = UbiqTcpConnection("nexus.cs.ucl.ac.uk", 8005);

// A NetworKScene
const scene = new NetworkScene();
scene.addConnection(connection);

// A RoomClient to join a Room
const roomclient = new RoomClient(scene);
const logcollector = new LogCollector(scene);

roomclient.addListener("OnJoinedRoom", room => {
    console.log(room.joincode);
})

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
        logcollector.userEventStream.unpipe(); // This disconnects all streams (don't use this code as-is if you want to route the events elsewhere too)
        logcollector.userEventStream.pipe(this); // There is no race condition here because Node is single threaded, so the new pipe will be established before any new messages are processed
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

// Manually start the user stream before creating a log file. This will avoid race conditions between calling logcollector.start()
// and the first stream being created when peers join the room.
logcollector.userEventStream.resume();

// Calling startCollection() will start streaming from the LogManagers at existing and
// and new Peers.
// The events will be buffered in userEventStream and applicationEventStream.
logcollector.startCollection();

roomclient.join("6765c52b-3ad6-4fb0-9030-2c9a05dc4731"); // Join by UUID. Use an online generator to create a new one for your experiment.
