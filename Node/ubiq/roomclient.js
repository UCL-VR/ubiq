const { EventEmitter } = require("events");
const { NetworkId } = require("./messaging")
const { Uuid } = require("./uuid")
const { SerialisedDictionary } = require("./dictionary")

// Implements a RoomClient Network Component. This can be attached to a NetworkScene
// to have the NetworkScene join a Room.

class RoomClient extends EventEmitter{
    constructor(scene){
        super();

        // All NetworkObjects must have componentId and objectId properties set, before
        // calling register().
        // Since Js doesn't have an inherent scene-graph like Unity, how the objectIds are
        // resolved is up to the user.
        // Here, the RoomClient takes it from the NetworkScene.
        this.componentId = 1;
        this.objectId = scene.objectId;

        this.context = scene.register(this);

        this.peer = {
            uuid: Uuid.generate(),
            properties: {}
        }

        this.room = {
        }

        this.peers = new Map()
    }

    static serverObjectId = {
        objectId: new NetworkId(1),
        componentId: 1
    }

    getPeerInfo(){
        return {
            uuid: this.peer.uuid,
            networkId: this.objectId,
            properties: SerialisedDictionary.To(this.peer.properties)
        };
    }

    getRoomInfo(){
        return{
            uuid: this.room.uuid,
            joincode: this.room.joincode,
            publish: this.room.publish,
            name: this.room.name,
            properties: SerialisedDictionary.From(this.room.properties)
        }
    }

    getPeers(){
        return this.peers.values();
    }

    setRoomInfo(roominfo){
        var roomchanged = false;
        if(this.room.uuid != roominfo.uuid){
            roomchanged = true;
        }
        this.room.uuid = roominfo.uuid;
        this.room.joincode = roominfo.joincode;
        this.room.publish = roominfo.publish;
        this.room.name = roominfo.name;
        this.room.properties = SerialisedDictionary.From(roominfo.properties);
        return roomchanged;
    }

    sendServerMessage(type, args){
        this.context.send(RoomClient.serverObjectId, { type: type, args: JSON.stringify(args) });
    }

    // Joins a Room - pass either a Join Code or UUID to join an existing room, or a name and visibility
    // flag to create a new one.
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
        }else
        if(arguments.length == 1){ //Join an existing room by join code or UID
            var identifier = arguments[0];
            if(typeof(identifier) === "string" && identifier.length === 3){
                args.joincode = identifier;
            }else
            if(typeof(identifier) === "string" && Uuid.validate(identifier)){
                args.uuid = identifier;
            }else{
                throw (identifier + " is not a Join Code or a GUID");
            }
        }else
        if(arguments.length == 2){ // Create a new room with the specified parameters
            for(var i = 0; i < arguments.length; i++){
                var arg = arguments[i];
                if(typeof(arg) == "boolean"){
                    args.publish = arg;
                }
                if(typeof(arg) == "string"){
                    args.name = arg;
                }
            }
        }else
        if(arguments.length > 2){
            throw "Join must have 0, 1 or 2 arguments";
        }
        this.sendServerMessage("Join", args);
    }

    leave(){
        this.sendServerMessage("Leave", this.getPeerInfo());
    }

    updatePeer(){
        this.sendServerMessage("UpdatePeer", this.getPeerInfo());
    }

    updateRoom(){
        this.sendServerMessage("UpdateRoom", this.getRoomInfo());
    }

    ping(){
        var args = {
            id: this.objectId
        }
        this.sendServerMessage("Ping", args);
    }

    // part of the interface for a network component
    processMessage(message){
        message = message.toObject();
        var args = JSON.parse(message.args);
        switch(message.type){
            case "SetRoom":
                this.setRoomInfo(args.room);
                var newPeersMap = new Map();
                args.peers.forEach(peer =>{
                    if(peer.uuid == this.peer.uuid){
                        return;
                    }
                    newPeersMap.set(peer.uuid, {
                        uuid: peer.uuid,
                        networkId: peer.networkId,
                        properties: SerialisedDictionary.From(peer.properties)
                    })
                });
                this.peers.forEach((value,key)=>{
                    if(!newPeersMap.has(key)){
                        this.peers.delete(key);
                        this.emit("OnPeerRemoved", value);
                    }
                });
                newPeersMap.forEach((value,key)=>{
                    if(!this.peers.has(key)){
                        this.peers.set(key,value);
                        this.emit("OnPeerAdded", value);
                    }
                });            
                this.emit("OnJoinedRoom", this.room);
                this.emit("OnRoomUpdated", this.room);
                break;
            case "Rejected":
                this.emit("OnJoinedRoomFailed", args.reason);
                break;
            case "UpdateRoom":
                this.setRoomInfo(args);
                this.emit("OnRoomUpdated", this.room);
                break;
            case "Rooms":
                this.emit("OnRooms", args);
                break;
            case "UpdatePeer":
                if(args.uuid == this.peer.uuid){
                    return;
                }
                var isNewPeer = false;
                if(!this.peers.has(args.uuid)){
                    this.peers.set(args.uuid, {
                        uuid: args.uuid
                    })
                    isNewPeer = true;
                }
                var peer = this.peers.get(args.uuid);
                peer.networkId = args.networkId;
                peer.properties = SerialisedDictionary.From(args.properties);
                if(isNewPeer){
                    this.emit("OnPeerAdded", peer);
                }else{
                    this.emit("OnPeerUpdated", peer);
                }
                break;
            case "RemovedPeer":
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