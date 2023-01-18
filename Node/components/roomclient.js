const { EventEmitter } = require("events");
const { NetworkId } = require("ubiq/messaging")
const { Uuid } = require("ubiq/uuid")
const { SerialisedDictionary } = require("ubiq/dictionary")

// Implements a RoomClient Network Component. This can be attached to a 
// NetworkScene to have the NetworkScene join a Room.

class RoomClient extends EventEmitter{
    constructor(scene){
        super();

        // All NetworkObjects must have the networkId property set, before
        // calling register().
        // For the RoomClient, the networkId is created based on the NetworkScene
        // Id (the 'service addressing' model,
        // https://ubiq.online/blog/latest-api-improvements/).

        this.networkId = NetworkId.Create(scene.networkId, "RoomClient");
        this.scene = scene;
        this.context = scene.register(this);

        this.peer = {
            uuid: Uuid.generate(),
            keys: [],
            values: []
        }

        this.room = {
        }

        this.peers = new Map()
    }

    static serverNetworkId =  new NetworkId(1);

    getPeerInfo(){
        return {
            uuid: this.peer.uuid,
            sceneid: this.scene.networkId,
            clientid: this.networkId,
            keys: this.peer.keys,
            values: this.peer.values
        };
    }

    getRoomInfo(){
        return{
            uuid: this.room.uuid,
            joincode: this.room.joincode,
            publish: this.room.publish,
            name: this.room.name,
            keys: this.room.keys,
            values: this.room.values
        }
    }

    getPeers(){
        return this.peers.values();
    }

    setRoomInfo(roominfo){
        this.room.uuid = roominfo.uuid;
        this.room.joincode = roominfo.joincode;
        this.room.publish = roominfo.publish;
        this.room.name = roominfo.name;
        this.room.keys = roominfo.keys
        this.room.values = roominfo.values
    }

    sendServerMessage(type, args){
        this.context.send(RoomClient.serverNetworkId, { type: type, args: JSON.stringify(args) });
    }

    // Joins a Room - pass either a Join Code or UUID to join an existing room,
    // or a name and visibility flag to create a new one.
    join(){
        var args = {
            joincode: "",
            name: "My Room",
            publish: false,
            peer: this.getPeerInfo()
        };
        if(arguments.length == 0){ // Create a new room (with default parameters)
            args.name = "My Room";
            args.publish = false;
        }else if(arguments.length == 1){ //Join an existing room by join code or UID
            var identifier = arguments[0];
            if(typeof(identifier) === "string" && identifier.length === 3){
                args.joincode = identifier;
            }else
            if(typeof(identifier) === "string" && Uuid.validate(identifier)){
                args.uuid = identifier;
            }else{
                throw (identifier + " is not a Join Code or a GUID");
            }
        }else if(arguments.length == 2){ // Create a new room with the specified parameters
            for(var i = 0; i < arguments.length; i++){
                var arg = arguments[i];
                if(typeof(arg) == "boolean"){
                    args.publish = arg;
                }
                if(typeof(arg) == "string"){
                    args.name = arg;
                }
            }
        }else if(arguments.length > 2){
            throw "Join must have 0, 1 or 2 arguments";
        }
        this.sendServerMessage("Join", args);
    }

    updatePeerProperties(modified){

    }

    updateRoomProperties(modified){

    }

    sendAppendPeerProperties(modified){
        this.sendServerMessage("AppendPeerProperties", modified);
    }

    sendAppendRoomProperties(modified){
        this.sendServerMessage("AppendRoomProperties", modified);
    }

    discoverRooms(){
        this.sendServerMessage("DiscoverRooms", {clientid: this.networkId});
    }

    ping(){
        this.sendServerMessage("Ping", {clientid: this.networkId});
    }

    // part of the interface for a network component
    processMessage(message){
        message = message.toObject();
        var args = JSON.parse(message.args);
        switch(message.type){
            case "SetRoom":
                this.setRoomInfo(args.room);
                this.emit("OnJoinedRoom", this.room);
                break;
            case "Rejected":
                this.emit("OnJoinRejected", args.reason);
                break;
            case "Rooms":
                this.emit("OnRooms", args);
                break;
            case "RoomPropertiesAppended":
                this.updateRoomProperties(args);
                this.emit("OnRoomUpdated", this.room);
                break;
            case "PeerPropertiesAppended":
                this.updatePeerProperties(args);
                this.emit("OnPeerUpdated", this.room);
                break;
            case "PeerAdded":
                    if(!this.peers.has(args.peer.uuid)){
                        this.peers.set(args.peer.uuid, args.peer);
                        this.emit("OnPeerAdded", args.peer);
                    }               
                    break;
            case "PeerRemoved":
                if(this.peers.has(args.uuid)){
                    var peer = this.peers.get(args.uuid);
                    this.peers.delete(args.uuid);
                    this.emit("OnPeerRemoved", peer);
                }               
                break;
            case "Ping":
                this.emit("OnPing", args.id);
                break;
        }
    }
}

module.exports = {
    RoomClient
}