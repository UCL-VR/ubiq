const { Message, NetworkId } = require('./messaging');
const { randString } = require('./joincode');
const { Validator } = require('jsonschema');
const { EventEmitter } = require('events');

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
    }

    addServer(server){
        console.log("Added RoomServer port " + server.port);
        server.onConnection.push(this.onConnection.bind(this));

    }

    onConnection(wrapped){
        console.log("RoomServer: Client Connection from " + wrapped.endpoint().address + ":" + wrapped.endpoint().port);
        new RoomPeer(this, wrapped);
    }

    async join(peer, args){

        var room = null;
        if(args.hasOwnProperty("joincode") && args.joincode != ""){
            // Room join request by joincode
            room = this.roomDatabase.joincode(args.joincode);
            if (room === null) {
                console.log(peer.uuid + " attempted to join room with code " + args.joincode + " but no such room exists");
                peer.sendRejected(args,"Could not join room with code " + args.joincode + ". No such room exists.");
                return;
            }

            if (peer.room.uuid === room.uuid){
                console.log(peer.uuid + " attempted to join room with code " + args.joincode + " but peer is already in room");
                return;
            }
        } else {
            // Otherwise new room requested
            var uuid = "";
            while(true){
                uuid = randString(32);
                if(this.roomDatabase.uuid(uuid) === null){
                    break;
                }
            }
            var joincode = "";
            while(true){
                joincode = randString(3);
                if (this.roomDatabase.joincode(joincode) === null){
                    break;
                }
            }
            var publish = true;
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

    getRoomArgs({publishable = true} = {}){
        if(publishable){
            return this.roomDatabase.all().filter(r => r.publish === true).map(r => r.getRoomArgs());
        } else {
            return this.roomDatabase.all().map(r => r.getRoomArgs());
        }
    }

    removeRoom(room){
        this.emit("destroy",room);
        this.roomDatabase.remove(room.uuid);
        console.log("RoomServer: Deleting empty room " + room.uuid);
    }

    setBlob(args){
        var room = this.roomDatabase.uuid(args.room);
        // only existing rooms may have blobs set
        if(room !== null) {
            room.blobs[args.uuid] = args.blob;
        }
    }

    getBlob(args){
        var room = this.roomDatabase.uuid(args.room);
        // only existing rooms may have blobs set
        if(room !== null && room.blobs.hasOwnProperty(args.uuid)){
            args.blob = room.blobs[args.uuid];
        }
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

// The RoomPeer class manages a WebSocketConnection to a RoomClient. This class
// interacts with the connection, formatting and parsing messages and calling the
// appropriate methods on RoomServer and others.
class RoomPeer{
    constructor(server, connection){
        this.server = server;
        this.connection = connection;
        this.room = new EmptyRoom();
        this.objectId;
        this.uuid = "";
        this.properties = [];
        this.connection.onMessage.push(this.onMessage.bind(this));
        this.connection.onClose.push(this.onClose.bind(this));

        this.validator = new Validator();
        this.validator.addSchema({
            "id": "/NetworkID",
            "type": "object",
            "properties": {
                "a": {"type": "integer"},
                "b": {"type": "integer"}
            },
            "required": ["a","b"]
        },'/NetworkID');
        this.validator.addSchema({
            "id": "/PeerInfo",
            "type": "object",
            "properties": {
                "uuid": {"type": "string"},
                "networkId": {"$ref": "/NetworkID"},
                "properties": {"type": "object"}
            },
            "required": ["uuid","networkId","properties"]
        },'/PeerInfo');
        this.validator.addSchema({
            "id": "/RoomInfo",
            "type": "object",
            "properties": {
                "uuid": {"type": "string"},
                "joincode": {"type": "string"},
                "publish": {"type": "boolean"},
                "name": {"type": "string"},
                "properties": {"type": "object"}
            },
            "required": ["uuid","joincode","publish","name","properties"]
        },'/RoomInfo');
        this.joinArgsSchema = {
            "id": "/JoinArgs",
            "type": "object",
            "properties": {
                "joincode": {"type": "string"},
                "name": {"type": "string"},
                "publish": {"type": "boolean"},
                "peer": {"$ref": "/PeerInfo"},
            },
            "required": ["joincode","peer"]
        };
        this.updatePeerArgsSchema = {
            "id": "/UpdatePeerArgs",
            "$ref": "/PeerInfo"
        };
        this.updateRoomArgsSchema = {
            "id": "/UpdateRoomArgs",
            "$ref": "/RoomInfo"
        };
        this.requestRoomsArgsSchema = {
            "id": "/RequestRoomsArgs"
        };
        this.setBlobArgsSchema = {
            "id": "/SetBlobArgs",
            "type": "object",
            "properties": {
                "room": {"type": "string"}, //room uuid
                "uuid": {"type": "string"}, //blob uuid
                "blob": {"type": "string"}  //blob contents
            },
            "required": ["room","uuid","blob"]
        };
        this.getBlobArgsSchema = {
            "id": "/GetBlobArgs",
            "type": "object",
            "properties": {
                "room": {"type": "string"}, //room uuid
                "uuid": {"type": "string"}, //blob uuid
                "blob": {"type": "string"}  //blob contents
            },
            "required": ["room","uuid","blob"]
        };
        this.serverMessageSchema = {
            "id": "/ServerMessage",
            "type": "object",
            "properties": {
                "type": {"type": "string"},
                "args": {"type": "string"}
            },
            "required": ["type","args"]
        };
    }

    onMessage(message){
        if(NetworkId.Compare(message.objectId, this.server.objectId) && message.componentId == this.server.componentId){
            try {
                message.object = message.toObject();
            } catch {
                console.log("Peer " + this.uuid + ": Invalid JSON in message");
                return;
            }

            var result = this.validator.validate(message.object,this.serverMessageSchema);
            if (!result.valid) {
                console.log(result.instance);
                console.log(result.errors);
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
                    var argsResult = this.validator.validate(message.args,this.joinArgsSchema);
                    if (argsResult.valid) {
                        // a join message always includes an update about the peer properties
                        this.setPeerArgs(message.args.peer);
                        this.server.join(this, message.args);
                    } else {
                        console.log(result.instance);
                        console.log(argsResult.instance);
                        console.log(argsResult.errors);
                    }
                    break;
                case "Leave":
                    this.server.leave(this);
                    break;
                case "UpdatePeer":
                    var argsResult = this.validator.validate(message.args,this.updatePeerArgsSchema);
                    if (argsResult.valid) {
                        this.setPeerArgs(message.args);
                        this.room.updatePeer(this);
                    } else {
                        console.log(result.instance);
                        console.log(argsResult.instance);
                        console.log(argsResult.errors);
                    }
                    break;
                case "UpdateRoom":
                    var argsResult = this.validator.validate(message.args,this.updateRoomArgsSchema);
                    if (argsResult.valid) {
                        this.room.updateRoom(message.args);
                    } else {
                        console.log(result.instance);
                        console.log(argsResult.instance);
                        console.log(argsResult.errors);
                    }
                    break;
                case "RequestRooms":
                    var argsResult = this.validator.validate(message.args,this.requestRoomsArgsSchema);
                    if (argsResult.valid) {
                        this.sendRooms({
                            rooms: this.server.getRoomArgs({publishable:true}),
                            version: this.server.version
                        });
                    } else {
                        console.log(result.instance);
                        console.log(argsResult.instance);
                        console.log(argsResult.errors);
                    }
                    break;
                case "SetBlob":
                    var argsResult = this.validator.validate(message.args,this.setBlobArgsSchema);
                    if (argsResult.valid) {
                        this.server.setBlob(message.args);
                    } else {
                        console.log(result.instance);
                        console.log(argsResult.instance);
                        console.log(argsResult.errors);
                    }
                    break;
                case "GetBlob":
                    var argsResult = this.validator.validate(message.args,this.getBlobArgsSchema);
                    if (argsResult.valid) {
                        this.server.getBlob(message.args);
                        this.sendBlob(message.args);
                    } else {
                        console.log(result.instance);
                        console.log(argsResult.instance);
                        console.log(argsResult.errors);
                    }
                    break;
                case "Ping":
                    this.sendPing();
                    break;
            };
        }else{
            this.room.processMessage(this, message);
        }
    }

    setPeerArgs(peer){
        this.objectId = peer.networkId;
        this.properties = peer.properties;
        this.uuid = peer.uuid;
    }

    getPeerArgs(){
        return {
            uuid: this.uuid,
            networkId: this.objectId,
            properties: this.properties
        }
    }

    onClose(){
        this.room.removePeer(this);
    }

    setRoom(room){
        this.room = room;
        this.sendAccepted();
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

    sendAccepted(){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "Accepted",
                    args: JSON.stringify({
                        room: this.room.getRoomArgs(),
                        peers: this.room.getPeersArgs()
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

    sendRooms(rooms){
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

    sendPing(){
        this.send(
            Message.Create(
                this.objectId,
                1,
                {
                    type: "Ping",
                    args: null
                }
            )
        )
    }

    send(message){
        this.connection.send(message);
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
        this.properties = {};
        this.blobs = {};
    }

    addPeer(peer){
        this.peers.push(peer);
        peer.setRoom(this);
        this.updatePeer(peer);
    }

    updatePeer(peer){ // The peer arguments are only stored in one place (RoomPeer), so from the rooms point of view, updating the peer is just sending a message
        this.peers.forEach(otherpeer => {
            otherpeer.sendPeerUpdate(peer);
        });
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

    updateRoom(args){
        if(args.uuid != this.uuid){
            console.log("Attempt to update room outside membership.");
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