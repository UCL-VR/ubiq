const { Message, NetworkId, Schema, Uuid } = require("ubiq");
const { EventEmitter } = require('events');
const fs = require('fs');

const VERSION_STRING = "0.0.4";
const RoomServerReservedId = 1;

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

class PropertyDictionary{
    constructor(){
        this.dict = {};
    }

    append(keys,values){
        var response = {
            keys: [],
            values: []
        };

        if (keys === undefined || values === undefined){
            return response;
        }

        var set = function (key,value,dict) {
            if (value === ""){
                // Attempting to remove
                if (dict.hasOwnProperty(key)){
                    delete dict[key];
                    return true;
                }

                return false;
            }

            if (dict.hasOwnProperty(key) && dict[key] === value){
                return false;
            }

            dict[key] = value;
            return true;
        }

        if ((typeof keys === 'string' || keys instanceof String)
            && (typeof values === 'string' || values instanceof String)) {

            if (set(keys,values,this.dict)){
                response.keys = [keys],
                response.values = [values]
            }
            return response;
        }

        if (!Array.isArray(keys) || !Array.isArray(values)){
            return response;
        }

        // Set for uniqueness - if modified multiple times, last value is used
        var modified = new Set();
        var dict = this.dict;
        keys.forEach(function(key,i){
            if (values.length <= i){
                return;
            }

            var value = values[i];
            if (set(key,value,dict)){
                modified.add(key);
            }
        });

        response.keys = Array.from(modified);
        response.values = response.keys.map((key) => this.get(key));
        return response;
    }

    set(keys,values){
        this.dict = {};
        append(keys,values);
    }

    get(key){
        if (this.dict.hasOwnProperty(key)){
            return this.dict[key];
        }
        return "";
    }

    keys(){
        return Object.keys(this.dict);
    }

    values(){
        return Object.values(this.dict);
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

// This is the primary server for rendezvous and bootstrapping. It accepts websocket connections,
// (immediately handing them over to RoomPeer instances) and performs book-keeping for finding
// and joining rooms.
class RoomServer extends EventEmitter{
    constructor(){
        super();
        this.roomDatabase = new RoomDatabase();
        this.version = VERSION_STRING;
        this.networkId = new NetworkId(RoomServerReservedId);
        this.status = {
            connections: 0,
            rooms: 0,
            messages: 0,
            bytesIn: 0,
            bytesOut: 0,
            time: 0,
        }
        this.statusStream = undefined;
        this.statusStreamTime = 0;
        this.intervals = [];
        this.T = Room;
    }

    addStatusStream(filename){
        if(filename != undefined){
            this.statusStream = fs.createWriteStream(filename);
            this.intervals.push(setInterval(this.statusPoll.bind(this), 100));
        }
    }

    updateStatus(){
        this.status.rooms = Object.keys(this.roomDatabase.byUuid).length;
        this.status.time = (Date.now() * 10000) + 621355968000000000; // This snippet converts Js ticks to .NET ticks making them directly comparable with Ubiq's logging timestamps
        var structuredLog = JSON.stringify(this.status, (key,value)=>
            typeof value === "bigint" ? value.toString() : value
        );
        this.statusStream.write(structuredLog + "\n");
    }

    // Called by onMessage to see if we need to update the status log.
    // The status should be updated every 100 ms or so.
    statusPoll(){
        if(this.statusStream != undefined){
            var time = Date.now();
            var interval = time - this.statusStreamTime;
            if(interval > 100){
                this.statusStreamTime = time;
                this.updateStatus();
            }
        }
    }

    addServer(server){
        if(server.status == "LISTENING"){
            console.log("Added RoomServer port " + server.port);
            server.onConnection.push(this.onConnection.bind(this));
        }
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
            var name = uuid;
            if (args.hasOwnProperty("name") && args.name.length != 0) {
                name = args.name;
            }
            room = new this.T(this);
            room.uuid = uuid;
            room.joincode = joincode;
            room.publish = publish;
            room.name = name;
            this.roomDatabase.add(room);
            this.emit("create",room);

            console.log(room.uuid + " created with joincode " + joincode);
        }

        if (peer.room.uuid != null){
            peer.room.removePeer(peer);
        }
        room.addPeer(peer);
    }

    findOrCreateRoom(args){
        var room = this.roomDatabase.uuid(args.uuid);
        if (room === null) {
            var joincode = "";
            while(true){
                joincode = JoinCode();
                if (this.roomDatabase.joincode(joincode) === null){
                    break;
                }
            }
            var publish = false;
            var name = args.uuid;
            var uuid = args.uuid;
            room = new this.T(this);
            room.uuid = uuid;
            room.joincode = joincode;
            room.publish = publish;
            room.name = name;
            this.roomDatabase.add(room);
            this.emit("create",room);

            console.log(room.uuid + " created with joincode " + joincode);
        }
        return room;
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

    exit(callback){
        for(var id of this.intervals){
            clearInterval(id);
        }
        if(this.statusStream != undefined){
            console.log("Closing status stream...");
            this.statusStream.on("finish", callback);
            this.statusStream.end();
        }
        else
        {
            callback();
        }
    }
}

// The RoomPeer class manages a Connection to a RoomClient. This class
// interacts with the connection, formatting and parsing messages and calling the
// appropriate methods on RoomServer and others.
class RoomPeer{
    constructor(server, connection){
        this.server = server;
        this.server.status.connections += 1;
        this.connection = connection;
        this.room = new EmptyRoom();
        this.peers = {};
        this.networkSceneId = new NetworkId(Math.floor(Math.random()*2147483648),Math.floor(Math.random()*2147483648));
        this.clientid;
        this.uuid = "";
        this.properties = new PropertyDictionary();
        this.connection.onMessage.push(this.onMessage.bind(this));
        this.connection.onClose.push(this.onClose.bind(this));
        this.sessionId = Uuid.generate();
        this.observed = [];
    }

    onMessage(message){
        this.server.status.messages += 1;
        this.server.status.bytesIn += message.length;
        this.server.statusPoll();
        if(NetworkId.Compare(message.networkId, this.server.networkId)){
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
                        this.networkSceneId = message.args.peer.sceneid; // Join message always includes peer uuid and object id
                        this.clientid = message.args.peer.clientid;
                        this.uuid = message.args.peer.uuid;
                        this.properties.append(message.args.peer.keys, message.args.peer.values);
                        this.server.join(this, message.args);
                    }
                    break;
                case "AppendPeerProperties":
                    if (Schema.validate(message.args, "/ubiq.rooms.appendpeerpropertiesargs", this.onValidationFailure)) {
                        this.appendProperties(message.args.keys, message.args.values);
                    }
                    break;
                case "AppendRoomProperties":
                    if (Schema.validate(message.args, "/ubiq.rooms.appendroompropertiesargs", this.onValidationFailure)) {
                        this.room.appendProperties(message.args.keys,message.args.values);
                    }
                    break;
                case "DiscoverRooms":
                    if (Schema.validate(message.args, "/ubiq.rooms.discoverroomsargs", this.onValidationFailure)) {
                        this.clientid = message.args.clientid; // Needs a response: send network id in case not yet set
                        this.sendDiscoveredRooms({
                            rooms: this.server.discoverRooms(message.args).map(r => r.getRoomArgs()),
                            version: this.server.version,
                            request: message.args
                        });
                    }
                    break;
                case "SetBlob":
                    if (Schema.validate(message.args, "/ubiq.rooms.setblobargs", this.onValidationFailure)) {
                        this.server.setBlob(message.args.uuid,message.args.blob);
                    }
                    break;
                case "GetBlob":
                    if (Schema.validate(message.args, "/ubiq.rooms.getblobargs", this.onValidationFailure)) {
                        this.clientid = message.args.clientid; // Needs a response: send network id in case not yet set
                        this.sendBlob(this.room.getBlob(message.args.uuid));
                    }
                    break;
                case "Ping":
                    if(Schema.validate(message.args,"/ubiq.rooms.ping",this.onValidationFailure)){
                        this.clientid = message.args.clientid; // Needs a response: send network id in case not yet set
                        this.sendPing();
                    }
                    break;
                default:
                    this.room.processRoomMessage(this, message);
            };
        }else{
            this.room.processMessage(this, message);
        }
    }

    onValidationFailure(error){
        error.validation.errors.forEach(error => {
            console.error("Validation error in " + error.schema + "; " + error.message);
        });
        console.error("Message Json: " +  JSON.stringify(error.json));
    }

    getPeerArgs(){
        return {
            uuid: this.uuid,
            sceneid: this.networkSceneId,
            clientid: this.clientid,
            keys: this.properties.keys(),
            values: this.properties.values()
        }
    }

    onClose(){
        this.room.removePeer(this);
        this.observed.forEach(room => room.removeObserver(this));
        this.server.status.connections -= 1;
    }

    setRoom(room){
        this.room = room;
        this.sendSetRoom();
    }

    clearRoom(){
        this.setRoom(new EmptyRoom());
    }

    getNetworkId(){
        return this.clientid;
    }

    appendProperties(keys,values){
        var modified = this.properties.append(keys,values);
        if (modified.keys.length > 0){
            this.room.broadcastPeerProperties(this,modified.keys,modified.values);
        }
    }

    sendRejected(joinArgs,reason){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "Rejected",
            args: JSON.stringify({
                reason: reason,
                joinArgs: joinArgs
            })
        }));
    }

    sendSetRoom(){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "SetRoom",
            args: JSON.stringify({
                room: this.room.getRoomArgs(),
            })
        }));
    }

    sendDiscoveredRooms(args){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "Rooms",
            args: JSON.stringify(args)
        }));
    }

    sendPeerAdded(peer){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "PeerAdded",
            args: JSON.stringify({
                peer: peer.getPeerArgs()
            })
        }));
    }

    sendPeerRemoved(peer){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "PeerRemoved",
            args: JSON.stringify({
                uuid: peer.uuid
            })
        }));
    }

    sendRoomPropertiesAppended(keys,values){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "RoomPropertiesAppended",
            args: JSON.stringify({
                keys: keys,
                values: values
            })
        }));
    }

    sendPeerPropertiesAppended(peer,keys,values){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "PeerPropertiesAppended",
            args: JSON.stringify({
                uuid: peer.uuid,
                keys: keys,
                values: values
            })
        }));
    }

    sendBlob(uuid,blob){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "Blob",
            args: JSON.stringify({
                uuid: uuid,
                blob: blob
            })
        }));
    }

    sendPing(){
        this.send(Message.Create(this.getNetworkId(),
        {
            type: "Ping",
            args: JSON.stringify({
                sessionId: this.sessionId
            })
        }));
    }

    send(message){
        this.server.status.bytesOut += message.length;
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

    broadcastPeerProperties(peer,keys){}

    appendProperties(key,value){}

    processMessage(peer, message){}

    getPeersArgs(){
        return [];
    }

    getRoomArgs(){
        return {
            uuid: "",
            joincode: "",
            publish: false,
            name: "",
            keys: [],
            values: []
        }
    }
}

class Room{
    constructor(server){
        this.server = server;
        this.uuid = null;
        this.name = "(Unnamed Room)";
        this.publish = false;
        this.joincode = "";
        this.peers = [];
        this.properties = new PropertyDictionary();
        this.blobs = {};
        this.observers = [];
    }

    broadcastPeerProperties(peer,keys,values){
        this.peers.forEach(otherpeer => {
            if (otherpeer !== peer){
                otherpeer.sendPeerPropertiesAppended(peer,keys,values);
            }
        });
        this.observers.forEach(otherpeer =>{
            if (otherpeer !== peer){
                otherpeer.sendPeerPropertiesAppended(peer,keys,values);
            }
        });
    }

    appendProperties(keys,values){
        var modified = this.properties.append(keys,values);
        this.peers.forEach(peer => {
            peer.sendRoomPropertiesAppended(modified.keys,modified.values);
        });
    }

    addPeer(peer){
        this.peers.push(peer);
        peer.setRoom(this);
        for(var existing of this.peers){ // Tell the Peers about eachother
            if(existing !== peer){
                existing.sendPeerAdded(peer); // Tell the existing peer that the new Peer has joined
                peer.sendPeerAdded(existing); // And the new Peer about the existing one
            }
        };
        console.log(peer.uuid + " joined room " + this.name);
    }

    removePeer(peer){
        arrayRemove(this.peers, peer);
        peer.setRoom(new EmptyRoom()); // signal that the leave is complete
        for(var existing of this.peers){
            existing.sendPeerRemoved(peer); // Tell the remaining peers about the missing peer (no check here because the peer was already removed from the list)
            peer.sendPeerRemoved(existing);
        }
        console.log(peer.uuid + " left room " + this.name);
        this.checkRoom();
    }

    // Every time a peer or observer leaves, check if the room should still exist
    checkRoom(){
        if(this.peers.length <= 0){
            this.server.removeRoom(this);
        }
    }

    setBlob(uuid,blob){
        this.blobs[uuid] = blob;
    }

    getBlob(uuid){
        if(this.blobs.hasOwnProperty(uuid)){
            return this.blobs[uuid];
        }
        return "";
    }

    getRoomArgs(){
        return {
            uuid: this.uuid,
            joincode: this.joincode,
            publish: this.publish,
            name: this.name,
            keys: this.properties.keys(),
            values: this.properties.values()
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
        });
    }
}

// Be aware that while jsonschema can resolve forward declared references,
// initialisation is order dependent, and "alias" schemas must be defined after
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
        sceneid: {$ref: "/ubiq.messaging.networkid"},
        clientid: {$ref: "/ubiq.messaging.networkid"},
        keys: {type: "array", items: {type: "string"}},
        values: {type: "array", items: {type: "string"}},
    },
    required: ["uuid","sceneid","clientid","keys","values"]
});

Schema.add({
    id: "/ubiq.rooms.roominfo",
    type: "object",
    properties: {
        uuid: {type: "string"},
        joincode: {type: "string"},
        publish: {type: "boolean"},
        name: {type: "string"},
        keys: {type: "array", items: {type: "string"}},
        values: {type: "array", items: {type: "string"}},
    },
    required: ["uuid","joincode","publish","name","keys","values"]
});

Schema.add({
    id: "/ubiq.rooms.appendpeerpropertiesargs",
    type: "object",
    properties: {
        keys: {type: "array", items: {type: "string"}},
        values: {type: "array", items: {type: "string"}}
    },
    required: ["keys","values"]
});

Schema.add({
    id: "/ubiq.rooms.appendroompropertiesargs",
    type: "object",
    properties: {
        keys: {type: "array", items: {type: "string"}},
        values: {type: "array", items: {type: "string"}}
    },
    required: ["keys","values"]
});

Schema.add({
    id: "/ubiq.rooms.ping",
    type: "object",
    properties: {
        clientid: { $ref: "/ubiq.messaging.networkid"} // required because this needs a response
    },
    required: ["clientid"]
});

Schema.add({
    id: "/ubiq.rooms.discoverroomsargs",
    type: "object",
    properties: {
        clientid: { $ref: "/ubiq.messaging.networkid"}, // required because this needs a response
        joincode: {type: "string"}
    },
    required: ["clientid"]
});

Schema.add({
    id: "/ubiq.rooms.setblobargs",
    type: "object",
    properties: {
        uuid: { type: "string"},
        blob: { type: "string"}
    },
    required: ["uuid","blob"]
});

Schema.add({
    id: "/ubiq.rooms.getblobargs",
    type: "object",
    properties: {
        clientid: { $ref: "/ubiq.messaging.networkid"}, // required because this needs a response
        uuid: { type: "string"}
    },
    required: ["clientid","uuid"]
});

Schema.add({
    id: "/ubiq.rooms.updateroomargs",
    $ref: "/ubiq.rooms.roominfo"
});

Schema.add({
    id: "/ubiq.rooms.updatepeerargs",
    $ref: "/ubiq.rooms.peerinfo"
});

module.exports = {
    RoomServer,
    Room
}