const { Message, NetworkId, Schema, Uuid } = require("./ubiq");
const { EventEmitter } = require('events');
const { args } = require("commander");

const VERSION_STRING = "0.0.4";
const RoomServerReservedId = 1;

class PropertyDictionary{
    constructor(){
        this.dict = {};
    }

    set(key,value){
        if (value === ""){
            // Attempting to remove
            if (this.dict.hasOwnProperty(key)){
                delete this.dict.key;
                return true;
            }

            return false;
        }

        if (this.dict.hasOwnProperty(key) && this.dict[key] === value){
            return false;
        }

        this.dict[key] = value;
        return true;
    }

    get(key){
        if (this.dict.hasOwnProperty(key)){
            return this.dict[key];
        }
        return "";
    }

    toObject(){
        return { keys: Object.keys(object), values: Object.values(object) };
    }
}

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

// This is the primary server for rendezvous and bootstrapping. It accepts websocket connections,
// (immediately handing them over to RoomPeer instances) and performs book-keeping for finding
// and joining rooms.
class RoomServer extends EventEmitter{
    constructor(){
        super();
        this.roomDatabase = new RoomDatabase();
        this.version = VERSION_STRING;
        this.objectId = new NetworkId(RoomServerReservedId);
    }

    addServer(server){
        console.log("Added RoomServer port " + server.port);
        server.onConnection.push(this.onConnection.bind(this));

    }

    onConnection(wrapped){
        console.log("RoomServer: Client Connection from " + wrapped.endpoint().address + ":" + wrapped.endpoint().port);
        new RoomPeer(this, wrapped);
    }

    // Expects args from schema ubiq.rooms.joinargs
    async join(peer, args){

        var room = null;
        if(args.hasOwnProperty("uuid") && args.uuid != ""){
            // Room join request by uuid
            if (!Uuid.validate(args.uuid)){
                console.log(peer.uuid + " attempted to join room with uuid " + args.uuid + " but the we were expecting an RFC4122 v4 uuid.");
                peer.sendRejected(args,"Could not join room with uuid " + args.uuid + ". We require an RFC4122 v4 uuid.");
                return;
            }

            // Not a problem if no such room exists - we'll create one
            room = this.roomDatabase.uuid(args.uuid);
        }
        else if(args.hasOwnProperty("joincode") && args.joincode != ""){
            // Room join request by joincode
            room = this.roomDatabase.joincode(args.joincode);

            if (room === null) {
                console.log(peer.uuid + " attempted to join room with code " + args.joincode + " but no such room exists");
                peer.sendRejected(args,"Could not join room with code " + args.joincode + ". No such room exists.");
                return;
            }
        }

        if (room !== null && peer.room.uuid === room.uuid){
            console.log(peer.uuid + " attempted to join room with code " + args.joincode + " but peer is already in room");
            return;
        }

        if (room === null) {
            // Otherwise new room requested
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
            room = new Room(this,uuid,joincode,publish,name);
            this.roomDatabase.add(room);
            this.emit("create",room);

            console.log(room.uuid + " created with joincode " + joincode);
        }

        if (peer.room.uuid != null){
            peer.room.removePeer(peer);
        }
        room.addPeer(peer);

        console.log(peer.uuid + " joined room " + room.uuid);
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
        console.log("RoomServer: Deleting empty room " + room.uuid);
    }

    // Expects a blob from schema ubiq.rooms.blob
    setBlob(blob){
        var room = this.roomDatabase.uuid(blob.room);
        // only existing rooms may have blobs set
        if(room !== null) {
            room.blobs[blob.uuid] = blob.blob;
        }
    }

    // Expects a blob from schema ubiq.rooms.blob
    getBlob(blob){
        var room = this.roomDatabase.uuid(blob.room);
        // only existing rooms may have blobs set
        if(room !== null && room.blobs.hasOwnProperty(blob.uuid)){
            blob.blob = room.blobs[blob.uuid];
        }
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
        networkId: { $ref: "/ubiq.messaging.networkid"}
    },
    required: ["networkId"]
});

Schema.add({
    id: "/ubiq.rooms.discoverroomsargs",
    type: "object",
    properties: {
        networkId: { $ref: "/ubiq.messaging.networkid"},
        joincode: {type: "string"}
    },
    required: ["networkId"]
});

Schema.add({
    id: "/ubiq.rooms.setblobargs",
    type: "object",
    properties: {
        blob: { $ref: "/ubiq.rooms.blob"}
    },
    required: ["blob"]
});

Schema.add({
    id: "/ubiq.rooms.getblobargs",
    type: "object",
    properties: {
        networkId: { $ref: "/ubiq.messaging.networkid"},
        blob: { $ref: "/ubiq.rooms.blob"}
    },
    required: ["networkId","blob"]
});

Schema.add({
    id: '/ubiq.rooms.blob',
    type: "object",
    properties: {
        room: {type: "string"}, //room uuid
        uuid: {type: "string"}, //blob uuid
        blob: {type: "string"}  //blob contents
    },
    required: ["room","uuid","blob"]
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
        this.objectId = new NetworkId(Math.floor(Math.random()*2147483648),Math.floor(Math.random()*2147483648));
        this.uuid = "";
        this.properties = new PropertyDictionary();
        this.connection.onMessage.push(this.onMessage.bind(this));
        this.connection.onClose.push(this.onClose.bind(this));
        this.sessionId = Uuid.generate();
    }

    onMessage(message){
        if(NetworkId.Compare(message.objectId, this.server.objectId)){
            try {
                message.object = message.toObject();
            } catch {
                console.log("Peer " + this.uuid + ": Invalid JSON in message");
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
                    console.log("Peer " + this.uuid + ": Invalid JSON in message args");
                    return;
                }
            }

            switch(message.type){
                case "Join":
                    if (Schema.validate(message.args, "/ubiq.rooms.joinargs", this.onValidationFailure)) {
                        this.objectId = message.args.peer.networkId; // Join message always includes peer uuid and object id
                        this.uuid = message.args.peer.uuid;
                        this.server.join(this, message.args);
                    }
                    break;
                case "SetPeerProperty":
                    if (Schema.validate(message.args, "/ubiq.rooms.setpeerpropertyargs", this.onValidationFailure)) {
                        message.args.key.forEach(otherpeer => {
                            if (otherpeer !== peer){
                                otherpeer.sendPeerPropertySet(peer,key,value);
                            }
                        });
                        if (this.properties.set(message.args.key,message.args.value)){
                            this.room.broadcastPeerProperty(this,message.args.key);
                        }
                    }
                    break;
                case "SetRoomProperty":
                    if (Schema.validate(message.args, "/ubiq.rooms.setroompropertyargs", this.onValidationFailure)) {
                        this.room.setProperty(message.args.key,message.args.value);
                    }
                    break;
                case "DiscoverRooms":
                    if (Schema.validate(message.args, "/ubiq.rooms.discoverroomsargs", this.onValidationFailure)) {
                        this.objectId = message.args.networkId; // Needs a response: send network id in case not yet set
                        this.sendDiscoveredRooms({
                            rooms: this.server.discoverRooms(message.args).map(r => r.getRoomArgs()),
                            version: this.server.version,
                            request: message.args
                        });
                    }
                    break;
                case "SetBlob":
                    if (Schema.validate(message.args, "/ubiq.rooms.setblobargs", this.onValidationFailure)) {
                        this.server.setBlob(message.args.blob);
                    }
                    break;
                case "GetBlob":
                    if (Schema.validate(message.args, "/ubiq.rooms.getblobargs", this.onValidationFailure)) {
                        this.objectId = message.args.networkId; // Needs a response: send network id in case not yet set
                        this.server.getBlob(message.args.blob);
                        this.sendBlob(message.args.blob);
                    }
                    break;
                case "Ping":
                    if(Schema.validate(message.args,"/ubiq.rooms.ping",this.onValidationFailure)){
                        this.objectId = message.args.networkId; // Needs a response: send network id in case not yet set
                        this.sendPing();
                    }
                    break;
            };
        }else{
            this.room.processMessage(this, message);
        }
    }

    onValidationFailure(error){
        console.log(error.json);
        console.log(error.validation.message);
    }

    getPeerArgs(){
        return {
            uuid: this.uuid,
            networkId: this.objectId,
            properties: this.properties.toObject()
        }
    }

    getPingArgs(){
        return {
            sessionId: this.sessionId
        }
    }

    onClose(){
        this.room.removePeer(this);
    }

    setRoom(room){
        this.room = room;
        this.sendSetRoom();
    }

    sendRejected(joinArgs,reason){
        this.send(Message.Create(this.objectId,
        {
            type: "Rejected",
            args: JSON.stringify({
                reason: reason,
                joinArgs: joinArgs
            })
        }));
    }

    sendSetRoom(){
        this.send(Message.Create(this.objectId,
        {
            type: "SetRoom",
            args: JSON.stringify({
                room: this.room.getRoomArgs(),
                peers: this.room.getPeersArgs()
            })
        }));
    }

    sendDiscoveredRooms(rooms){
        this.send(Message.Create(this.objectId,
        {
            type: "Rooms",
            args: JSON.stringify(rooms)
        }));
    }

    sendPeerAdded(peer){
        this.send(Message.Create(this.objectId,
        {
            type: "PeerAdded",
            args: JSON.stringify({
                uuid:peer.uuid,
                networkId:peer.networkId,

            })
        }));
    }

    sendPeerRemoved(peer){
        this.send(Message.Create(this.objectId,
        {
            type: "PeerRemoved",
            args: JSON.stringify(peer.getPeerArgs())
        }));
    }

    sendRoomPropertySet(properties){
        this.send(Message.Create(this.objectId,
        {
            type: "RoomPropertySet",
            args: JSON.stringify({
                keys:Object.keys(properties),
                values:Object.values(properties)
            })
        }));
    }

    sendPeerPropertySet(peer,properties){
        this.send(Message.Create(this.objectId,
        {
            type: "PeerPropertySet",
            args: JSON.stringify({
                uuid: peer.uuid,
                keys:Object.keys(properties),
                values:Object.values(properties)
            })
        }));
    }

    sendBlob(blobArgs){
        this.send(Message.Create(this.objectId,
        {
            type: "Blob",
            args: JSON.stringify(blobArgs)
        }));
    }

    sendPing(){
        this.send(Message.Create(this.objectId,
        {
            type: "Ping",
            args: JSON.stringify(this.getPingArgs())
        }));
    }

    send(message){
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

    addPeer(peer){}

    broadcastPeerProperty(peer,key){}

    setProperty(key,value){}

    processMessage(peer, message){}

    getPeersArgs(){}

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
        this.properties = new PropertyDictionary();
        this.properties = {};
        this.blobs = {};
    }

    addPeer(peer){
        peer.setRoom(this);
        this.peers.forEach(otherpeer => {
            otherpeer.sendPeerAdded(peer,peer.properties);
        });
        this.peers.push(peer);
    }

    broadcastPeerProperty(peer,key,value){
        this.peers.forEach(otherpeer => {
            if (otherpeer !== peer){
                otherpeer.sendPeerPropertySet(peer,key,value);
            }
        });
    }

    setProperty(key,value){
        if (this.properties.set(key,value)){
            this.peers.forEach(peer => {
                peer.sendRoomPropertySet(key,value);
            });
        }
    }

    removePeer(peer){
        const index = this.peers.indexOf(peer);
        if (index > -1) {
          this.peers.splice(index, 1);
        }
        peer.setRoom(new EmptyRoom()); // signal that the leave is complete
        this.peers.forEach(otherpeer => {
            otherpeer.sendPeerRemoved(peer); // (no check here because peer was already removed from the list)
        });

        console.log(peer.uuid + " left room " + this.name);

        if(this.peers.length <= 0){
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

    getPeersArgs(){
        return this.peers.map(c => c.getPeerArgs());
    }

    processMessage(source, message){
        this.peers.forEach(peer =>{
            if(peer != source){
                peer.send(message);
            }
        })
    }
}

class RoomDatabase{
    constructor(){
        this.byUuid = {};
        this.byJoincode = {};
    }

    // Return all room objects in the database
    all(){
        return Object.keys(this.byUuid).map(k => this.byUuid[k]);
    }

    // Add room to the database
    add(room){
        this.byUuid[room.uuid] = room;
        this.byJoincode[room.joincode] = room;
    }

    // Remove room from the database by uuid
    remove(uuid){
        delete this.byJoincode[this.byUuid[uuid].joincode];
        delete this.byUuid[uuid];
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