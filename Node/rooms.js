const { Message, NetworkId, Schema, Uuid } = require("./ubiq");
const { EventEmitter } = require('events');
const { Info, Performance } = require('./logging');
const { required } = require("nconf");

const VERSION_STRING = "0.0.4";
const RoomServerReservedId = 1;
const RoomServerReservedComponent = 1;

class SerialisedDictionary{
    static From(dictionary){
        return Object.assign(...dictionary.keys.map((k,i) => ({[k]: dictionary.values[i]})));
    }

    static To(object){
        return { keys: Object.keys(object), values: Object.values(object) };
    }
}

// https://stackoverflow.com/questions/1349404/generate-random-string-characters-in-javascript
// Proof of concept - not crypto secure
function JoinCode() {
    var result           = '';
    var characters       = 'abcdefghijklmnopqrstuvwxyz0123456789';
    var charactersLength = characters.length;
    for ( var i = 0; i < 3; i++ ) {
       result += characters.charAt(Math.floor(Math.random() * charactersLength));
    }
    return result;
}

function arrayRemove(array,element){
    const index = array.indexOf(element);
    if (index > -1) {
        array.splice(index, 1);
    }
}

// This is the primary server for rendezvous and bootstrapping. It accepts websocket connections,
// (immediately handing them over to RoomPeer instances) and performs book-keeping for finding
// and joining rooms.
class RoomServer extends EventEmitter{
    constructor(){
        super();
        this.roomDatabase = new RoomDatabase();
        this.version = VERSION_STRING;
        this.objectId = new NetworkId(RoomServerReservedId);
        this.componentId = RoomServerReservedComponent;
        this.statistics = {
            numRooms: 0,
            numPeers: 0,
            messagesIn: 0,
            messagesOut: 0,
            bytesIn: 0,
            bytesOut: 0
        }
        this.statisticsTimer = setInterval(this.updateStatistics.bind(this), 1000);
    }

    addServer(server){
        Info.log("Added RoomServer port " + server.port);
        server.onConnection.push(this.onConnection.bind(this));
    }

    onConnection(wrapped){
        Info.log("RoomServer: Client Connection from " + wrapped.endpoint().address + ":" + wrapped.endpoint().port);
        new RoomPeer(this, wrapped);
    }

    async observe(peer, rooms){
        for(var room of rooms){
            room = this.findOrCreateRoom({uuid: room});
            room.addObserver(peer);
        }
    }

    async unobserve(peer, rooms){
        for(var room of rooms){
            room = this.findOrCreateRoom({uuid: room});
            room.removeObserver(peer);
        }
    }

    async join(peer, args){

        var room = null;
        if(args.hasOwnProperty("uuid") && args.uuid != ""){
            // Room join request by uuid
            if (!Uuid.validate(args.uuid)){
                console.log(peer.uuid + " attempted to join room with uuid " + args.uuid + " but we were expecting an RFC4122 uuid.");
                peer.sendRejected(args,"Could not join room with uuid " + args.uuid + ". We require an RFC4122 uuid.");
                return;
            }

            // Not a problem if no such room exists - we'll create one
            room = this.findOrCreateRoom(args);
        }
        else if(args.hasOwnProperty("joincode") && args.joincode != ""){
            // Room join request by joincode
            room = this.roomDatabase.joincode(args.joincode);

            if (room === null) {
                Info.log(peer.uuid + " attempted to join room with code " + args.joincode + " but no such room exists");
                peer.sendRejected(args,"Could not join room with code " + args.joincode + ". No such room exists.");
                return;
            }
        }

        if (room !== null && peer.room.uuid === room.uuid){
            console.log(peer.uuid + " attempted to join room with code " + args.joincode + " but peer is already in room");
            return;
        }

        if (peer.room.uuid != null){
            peer.room.removePeer(peer);
        }
        room.addPeer(peer);
    }

    findOrCreateRoom(args){
        if(!args.hasOwnProperty("uuid")){
            throw "findOrCreateRoom can only work with rooms identified by UUIDs";
        }
        var room = this.roomDatabase.uuid(args.uuid);
        if(room === null){
            room = this.createRoom(args);
        }
        return room;
    }

    createRoom(args){
        var uuid = "";
        if(args.hasOwnProperty("uuid") && args.uuid != ""){
            // Use specified uuid
            // we're sure it's correctly formatted and isn't already in db
            uuid = args.uuid;
        } else {
            // Create new uuid if none specified
            while(true){
                uuid = Uuid.generate();
                if(this.roomDatabase.uuid(uuid) === null){
                    break;
                }
            }
        }
        var joincode = "";
        while(true){
            joincode = JoinCode();
            if (this.roomDatabase.joincode(joincode) === null){
                break;
            }
        }
        var publish = false;
        if (args.hasOwnProperty("publish")) {
            publish = args.publish;
        }
        var name = "Unnamed Room";
        if (args.hasOwnProperty("name")) {
            name = args.name;
        }
        var room = new Room(this,uuid,joincode,publish,name);
        this.roomDatabase.add(room);
        this.emit("create",room);

        Info.log(room.uuid + " created with joincode " + joincode);

        return room;
    }

    async leave(peer){
        peer.room.removePeer(peer);
    }

    getRooms(){
        return this.roomDatabase.all();
    }

    // Return requested rooms for publishable rooms
    // Optionally uses joincode to filter, in which case room need not be publishable
    // Expects args from schema ubiq.rooms.discoverroomargs
    discoverRooms(args){
        if(args.hasOwnProperty("joincode") && args.joincode != "") {
            return this.roomDatabase.all().filter(r => r.joincode === args.joincode);
        } else {
            return this.roomDatabase.all().filter(r => r.publish === true);
        }
    }

    removeRoom(room){
        this.emit("destroy",room);
        this.roomDatabase.remove(room.uuid);
        Info.log("RoomServer: Deleting empty room " + room.uuid);
    }

    // Expects args from schema ubiq.rooms.setblobargs
    setBlob(args){
        var room = this.roomDatabase.uuid(args.room);
        // only existing rooms may have blobs set
        if(room !== null) {
            room.blobs[args.uuid] = args.blob;
        }
    }

    // Expects args from schema ubiq.rooms.getblobargs
    getBlob(args){
        var room = this.roomDatabase.uuid(args.room);
        // only existing rooms may have blobs set
        if(room !== null && room.blobs.hasOwnProperty(args.uuid)){
            args.blob = room.blobs[args.uuid];
        }
    }

    updateStatistics(){
        this.statistics.numRooms = this.roomDatabase.num;
        Performance.logProperties("RoomServerStatistics", this.statistics);
    }
}

// Beware that while jsonschema can resolve forward declared references, initialisation is order dependent, and "alias" schemas must be defined after
// their concrete counterpart.

Schema.add({
    id: "/ubiq.rooms.servermessage",
    type: "object",
    properties: {
        type: {"type": "string"},
        args: {"type": "string"}
    },
    required: ["type","args"]
});

Schema.add({
    id: "/ubiq.rooms.joinargs",
    type: "object",
    properties: {
        joincode: {type: "string"},
        uuid: {type: "string"},
        name: {type: "string"},
        publish: {type: "boolean"},
        peer: {$ref: "/ubiq.rooms.peerinfo"},
    },
    required: ["peer"]
});

Schema.add({
    id: "/ubiq.rooms.setobserved",
    type: "object",
    properties: {
        rooms: {type:"array"},
        peer: {$ref: "/ubiq.rooms.peerinfo"},
    },
    required: ["rooms","peer"]
})

Schema.add({
    id: "/ubiq.rooms.leaverequestargs",
    type: "object",
    properties: {
        peer: {$ref: "/ubiq.rooms.peerinfo"},
    },
    required: ["peer"]
});

Schema.add({
    id: "/ubiq.rooms.peerinfo",
    type: "object",
    properties: {
        uuid: {type: "string"},
        networkId: {$ref: "/ubiq.messaging.networkid"},
        properties: {type: "object"}
    },
    required: ["uuid","networkId","properties"]
});

Schema.add({
    id: "/ubiq.rooms.roominfo",
    type: "object",
    properties: {
        uuid: {type: "string"},
        joincode: {type: "string"},
        publish: {type: "boolean"},
        name: {type: "string"},
        properties: {type: "object"}
    },
    required: ["uuid","joincode","publish","name","properties"]
});

Schema.add({
    id: "/ubiq.rooms.ping",
    type: "object",
    properties: {
        id: { $ref: "/ubiq.messaging.networkid"}
    }
});

Schema.add({
    id: "/ubiq.rooms.discoverroomsargs",
    type: "object",
    properties: {
        joincode: {type: "string"},
        networkId: {$ref: "/ubiq.messaging.networkid"},
    }
});

Schema.add({
    id: "/ubiq.rooms.setblobargs",
    type: "object",
    properties: {
        "room": {type: "string"}, //room uuid
        "uuid": {type: "string"}, //blob uuid
        "blob": {type: "string"}  //blob contents
    },
    required: ["room","uuid","blob"]
});

Schema.add({
    id: "/ubiq.rooms.getblobargs",
    $ref: "/ubiq.rooms.setblobargs"
});

Schema.add({
    id: "/ubiq.rooms.updateroomargs",
    $ref: "/ubiq.rooms.roominfo"
});

Schema.add({
    id: "/ubiq.rooms.updatepeerargs",
    $ref: "/ubiq.rooms.peerinfo"
});

// The RoomPeer class manages a Connection to a RoomClient. This class
// interacts with the connection, formatting and parsing messages and calling the
// appropriate methods on RoomServer and others.
class RoomPeer{
    constructor(server, connection){
        this.server = server;
        this.connection = connection;
        this.room = new EmptyRoom();
        this.observed = [];
        this.objectId;
        this.uuid = "";
        this.properties = [];
        this.connection.onMessage.push(this.onMessage.bind(this));
        this.connection.onClose.push(this.onClose.bind(this));
        this.sessionId = Uuid.generate();
        this.server.statistics.numPeers++;
    }

    onMessage(message){
        this.server.statistics.messagesIn++;
        this.server.statistics.bytesIn += message.length;
        if(NetworkId.Compare(message.objectId, this.server.objectId) && message.componentId == this.server.componentId){
            try {
                message.object = message.toObject();
            } catch {
                Info.log("Peer " + this.uuid + ": Invalid JSON in message");
                return;
            }

            if(!Schema.validate(message.object,"/ubiq.rooms.servermessage",this.onValidationFailure)){
                return;
            }

            message.type = message.object.type;

            if(message.object.args){
                try {
                    message.args = JSON.parse(message.object.args);
                } catch {
                    Info.log("Peer " + this.uuid + ": Invalid JSON in message args");
                    return;
                }
            }

            switch(message.type){
                case "Join":
                    if (Schema.validate(message.args, "/ubiq.rooms.joinargs", this.onValidationFailure)) {
                        this.setPeerArgs(message.args.peer);    // a join message always includes an update of the peer properties
                        this.server.join(this, message.args);
                    }
                    break;
                case "Leave":
                    if (Schema.validate(message.args, "/ubiq.rooms.leaverequestargs", this.onValidationFailure)) {
                        this.setPeerArgs(message.args.peer);
                        if(this.room.uuid != null){
                            this.server.leave(this);
                        }else{
                            this.setRoom(new EmptyRoom());
                        }
                    }
                    break;
                case "SetObserved":
                    if (Schema.validate(message.args, "/ubiq.rooms.setobserved", this.onValidationFailure)){
                        this.setPeerArgs(message.args.peer);
                        this.changeObserved(message.args.rooms); // Name collision with the internal api
                    }
                    break;
                case "UpdatePeer":
                    if (Schema.validate(message.args, "/ubiq.rooms.updatepeerargs", this.onValidationFailure)) {
                        this.setPeerArgs(message.args);
                        this.room.updatePeer(this);
                    }
                    break;
                case "UpdateRoom":
                    if (Schema.validate(message.args, "/ubiq.rooms.updateroomargs", this.onValidationFailure)) {
                        this.room.updateRoom(message.args);
                    }
                    break;
                case "DiscoverRooms":
                    if (Schema.validate(message.args, "/ubiq.rooms.discoverroomsargs", this.onValidationFailure)) {
                        this.objectId = message.args.networkId;
                        this.sendDiscoveredRooms({
                            rooms: this.server.discoverRooms(message.args).map(r => r.getRoomArgs()),
                            version: this.server.version,
                            request: message.args
                        });
                    }
                    break;
                case "SetBlob":
                    if (Schema.validate(message.args, "/ubiq.rooms.setblobargs", this.onValidationFailure)) {
                        this.server.setBlob(message.args);
                    }
                    break;
                case "GetBlob":
                    if (Schema.validate(message.args, "/ubiq.rooms.getblobargs", this.onValidationFailure)) {
                        this.server.getBlob(message.args);
                        this.sendBlob(message.args);
                    }
                    break;
                case "Ping":
                    if(Schema.validate(message.args,"/ubiq.rooms.ping",this.onValidationFailure)){
                        this.sendPing(message.args.id);
                    }
                    break;
            };
        }else{
            this.room.processMessage(this, message);
        }
    }

    onValidationFailure(error){
        Info.log(error.json);
        Info.log(error.validation.message);
    }

    setPeerArgs(peer){
        this.objectId = peer.networkId;
        this.properties = peer.properties;
        this.uuid = peer.uuid;
    }

    changeObserved(rooms){
        
        this.server.unobserve(this, this.observed.filter(existing => !rooms.includes(existing.uuid)).map(x => x.uuid));
        this.server.observe(this, rooms);
    }

    getPeerArgs(){
        return {
            uuid: this.uuid,
            networkId: this.objectId,
            properties: this.properties
        }
    }

    getPingArgs(){
        return {
            sessionId: this.sessionId
        }
    }

    onClose(){
        this.room.removePeer(this);
        this.server.statistics.numPeers--;
    }

    setRoom(room){
        this.room = room;
        this.sendSetRoom();
    }

    setObserved(room){
        this.observed.push(room);
    }

    unsetObserved(room){
        arrayRemove(this.observed, room);
    }

    sendRejected(joinArgs,reason){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "Rejected",
                    args: JSON.stringify({
                        reason: reason,
                        joinArgs: joinArgs
                    })
                }
            )
        );
    }

    sendSetRoom(){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "SetRoom",
                    args: JSON.stringify({
                        room: this.room.getRoomArgs(),
                    })
                }
            )
        );
    }

    sendRoomUpdate(){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "UpdateRoom",
                    args: JSON.stringify(this.room.getRoomArgs())
                }
            )
        );
    }

    sendDiscoveredRooms(rooms){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "Rooms",
                    args: JSON.stringify(rooms)
                }
            )
        )
    }

    /* Inform this Peer about another Peer that it should be aware of */
    sendPeerUpdate(peer){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "UpdatePeer",
                    args: JSON.stringify(peer.getPeerArgs())
                }
            )
        )
    }

    /* Inform this Peer that a Peer it was previously aware of has left the session */
    sendPeerRemoved(peer){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "RemovedPeer",
                    args: JSON.stringify(peer.getPeerArgs())
                }
            )
        )
    }

    sendBlob(blobArgs){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "Blob",
                    args: JSON.stringify(blobArgs)
                }
            )
        )
    }

    sendPing(networkid){
        this.send(
            Message.Create(
                networkid,
                1,
                {
                    type: "Ping",
                    args: JSON.stringify(this.getPingArgs())
                }
            )
        )
    }

    send(message){
        this.server.statistics.messagesOut++;
        this.server.statistics.bytesOut += message.length;
        this.connection.send(message);
    }
}

// When peers are not in a room, their room member is set to an instance of EmptyRoom, which contains
// callbacks and basic information to signal that they are not members of any room.
class EmptyRoom{
    constructor(){
        this.uuid = null;
    }

    removePeer(peer){}

    updatePeer(peer){}

    processMessage(peer, message){}

    getPeersArgs(){}

    updateRoom(roomargs){
        this.roomargs = roomargs;
    }

    getRoomArgs(){
        return {
            uuid: "",
            joincode: "",
            publish: false,
            name: "",
            properties: []
        }
    }
}

class Room{
    constructor(server, uuid, joincode, publish, name){
        this.server = server;
        this.uuid = uuid;
        this.joincode = joincode;
        this.publish = publish;
        this.name = name;
        this.peers = [];
        this.observers = [];
        this.properties = {};
        this.blobs = {};
    }

    addPeer(peer){
        // If the peer is an observer, then upgrade the peer in place...
        if(this.observers.includes(peer)){
            arrayRemove(this.observers, peer);
            peer.unsetObserved(this);
        }
        this.peers.push(peer);
        peer.setRoom(this);
        
        for(var existing of this.peers){ // Tell the Peers about eachother
            if(existing !== peer){
                existing.sendPeerUpdate(peer); // Tell the existing peer that the new Peer has joined
                peer.sendPeerUpdate(existing); // And the new Peer about the existing one
            }
        };
        this.observers.forEach(existing =>{
            existing.sendPeerUpdate(peer); // Tell existing observers about the new Peer
        })
        Info.log(peer.uuid + " joined room " + this.name);
    }

    addObserver(peer){
        if(!this.observers.includes(peer)){
            this.observers.push(peer);
            peer.setObserved(this);
            this.peers.forEach(member => {
                peer.sendPeerUpdate(member); // Tell the new observer about the existing peers
            })
            Info.log(peer.uuid + " observed room " + this.uuid);
        }
    }

    removePeer(peer){
        arrayRemove(this.peers, peer);
        peer.setRoom(new EmptyRoom()); // signal that the leave is complete
        for(var existing of this.peers){
            existing.sendPeerRemoved(peer); // Tell the remaining peers about the missing peer (no check here because the peer was already removed from the list)
            peer.sendPeerRemoved(existing);
        }
        for(var existing of this.observers) {
            existing.sendPeerRemoved(peer); // Tell the observers about the missing peer
        };
        Info.log(peer.uuid + " left room " + this.name);
        this.checkRoom();
    }

    removeObserver(peer){
        if(this.observers.includes(peer)){
            arrayRemove(this.observers, peer);
            peer.unsetObserved(this);
            this.peers.forEach(existing => {
                peer.sendPeerRemoved(existing); // Once the Observer is no longer observing the room, it should no longer see the rooms peers
            });
            Info.log(peer.uuid + " stopped observing room " + this.uuid);
        }
        this.checkRoom();
    }

    // Every time a peer or observer leaves, check if the room should still exist
    checkRoom(){
        if(this.peers.length <= 0 && this.observers.length <= 0){
            this.server.removeRoom(this);
        }
    }

    getRoomArgs(){
        return {
            uuid: this.uuid,
            joincode: this.joincode,
            publish: this.publish,
            name: this.name,
            properties : SerialisedDictionary.To(this.properties)
        };
    }

    updateRoom(args){
        if(args.uuid != this.uuid){
            Info.log("Attempt to update room outside membership.");
        }
        this.name = args.name;
        Object.assign(this.properties, SerialisedDictionary.From(args.properties));  // This line converts the key/value properties array into a JS object, and merges it with the existing properties.
        this.peers.forEach(peer =>{
            peer.sendRoomUpdate();
        });
    }

    getPeersArgs(){
        return this.peers.map(c => c.getPeerArgs());
    }

    processMessage(source, message){
        this.peers.forEach(peer =>{
            if(peer != source){
                peer.send(message);
            }
        })
        this.observers.forEach(peer =>{ // Observers receive messages emanating from this room, but emit messages from their own room
            if(peer != source){
                peer.send(message);
            }
        });
    }
}

class RoomDatabase{
    constructor(){
        this.byUuid = {};
        this.byJoincode = {};
        this.num = 0;
    }

    // Return all room objects in the database
    all(){
        return Object.keys(this.byUuid).map(k => this.byUuid[k]);
    }

    // Add room to the database
    add(room){
        this.byUuid[room.uuid] = room;
        this.byJoincode[room.joincode] = room;
        this.num++;
    }

    // Remove room from the database by uuid
    remove(uuid){
        delete this.byJoincode[this.byUuid[uuid].joincode];
        delete this.byUuid[uuid];
        this.num--;
    }

    // Return room object with given uuid, or null if not present
    uuid(uuid) {
        if (this.byUuid.hasOwnProperty(uuid)) {
            return this.byUuid[uuid];
        }
        return null;
    }

    // Return room object with given joincode, or null if not present
    joincode(joincode) {
        if (this.byJoincode.hasOwnProperty(joincode)) {
            return this.byJoincode[joincode];
        }
        return null;
    }
}

module.exports = {
    RoomServer
}