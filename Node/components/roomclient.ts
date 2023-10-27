import { EventEmitter } from "events";
import { NetworkId, Uuid, INetworkComponent, NetworkScene, Message, NetworkContext } from "ubiq";
import { PeerInfo, RoomInfo, AppendPeerPropertiesArgs, AppendRoomPropertiesArgs, JoinArgs, RoomServerMessage } from "modules/roomserver"

// Implements a RoomClient Network Component. This can be attached to a 
// NetworkScene to have the NetworkScene join a Room.

export class RoomPeer{
    uuid: string;
    sceneid: NetworkId;
    clientid: NetworkId;
    properties: Map<string,string>;
    client: RoomClient | undefined

    constructor(args: PeerInfo){
        this.uuid = args.uuid;
        this.sceneid = args.sceneid;
        this.clientid = args.clientid;
        this.properties = new Map();
        for(let i = 0; i < args.keys.length; i++){
            this.properties.set(args.keys[i], args.values[i]);
        };
    }

    getProperty(key: string){
        return this.properties.get(key);
    }

    setProperty(key: string, value: string){
        if(this.client === undefined){
            console.error("Properties may only be set on the local Peer");
            return;
        }
        this.client.setPeerProperty(key,value);
    }
}

export class Room {
    uuid: string;
    joincode: string;
    publish: boolean;
    name: string;
    properties: Map<string,string>;

    constructor(){
        this.uuid = "";
        this.joincode = "";
        this.publish = false;
        this.name = "Uninitialised Room Container";
        this.properties = new Map();
    }
}

export class RoomClient extends EventEmitter implements INetworkComponent{
    scene: NetworkScene;
    networkId: NetworkId;
    context: NetworkContext;
    room: Room;
    peer: RoomPeer;
    peers: Map<string, RoomPeer>;

    constructor(scene: NetworkScene){
        super();

        // All NetworkObjects must have the networkId property set, before
        // calling register().
        // For the RoomClient, the networkId is created based on the NetworkScene
        // Id (the 'service addressing' model,
        // https://ubiq.online/blog/latest-api-improvements/).

        this.scene = scene;
        this.networkId = NetworkId.Create(scene.networkId, "RoomClient");
        this.context = scene.register(this);

        this.peer = new RoomPeer({
            uuid: Uuid.generate(),
            sceneid: scene.networkId,
            clientid: this.networkId,
            keys: [],
            values: []
        });
        this.peer.client = this;

        this.room = new Room();

        this.peers = new Map()
    }

    static serverNetworkId =  new NetworkId(1);

    #getPeerInfo(){
        return {
            uuid: this.peer.uuid,
            sceneid: this.scene.networkId,
            clientid: this.networkId,
            keys: Array.from(this.peer.properties.keys()),
            values: Array.from(this.peer.properties.values())
        };
    }

    #setRoomInfo(roominfo: RoomInfo){
        this.room.uuid = roominfo.uuid;
        this.room.joincode = roominfo.joincode;
        this.room.publish = roominfo.publish;
        this.room.name = roominfo.name;
        this.room.properties = new Map();
        for(let i = 0; i < roominfo.keys.length; i++){
            this.room.properties.set(roominfo.keys[i], roominfo.values[i]);
        };
    }

    #sendServerMessage(type: string, args: Object){
        this.context.send(RoomClient.serverNetworkId, { type: type, args: JSON.stringify(args) } as RoomServerMessage);
    }

    #updatePeerProperties(uuid: string, updated: AppendPeerPropertiesArgs){
        var peer = this.peers.get(uuid);
        if(peer == undefined){
            return;
        }
        for(var i = 0; i < updated.keys.length; i++){
            peer.properties.set(updated.keys[i], updated.values[i]);
        }
    }

    #updateRoomProperties(updated: AppendRoomPropertiesArgs){
        for(var i = 0; i < updated.keys.length; i++){
            this.room.properties.set(updated.keys[i], updated.values[i]);
        }
    }

    #sendAppendPeerProperties(modified: AppendPeerPropertiesArgs){
        this.#sendServerMessage("AppendPeerProperties", modified); // The updated peer properties will be reflected back to update the local copy
    }

    #sendAppendRoomProperties(modified: AppendRoomPropertiesArgs){
        this.#sendServerMessage("AppendRoomProperties", modified); // The updated room properties will be reflected back to update the local copy
    }

    discoverRooms(){
        this.#sendServerMessage("DiscoverRooms", {clientid: this.networkId});
    }

    // Joins a Room - pass either a Join Code or UUID to join an existing room,
    // or a name and visibility flag to create a new one.
    join(...a: any[]){
        let args: JoinArgs = {
            joincode: "",
            name: "My Room",
            publish: false,
            peer: this.#getPeerInfo(),
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
        this.#sendServerMessage("Join", args);
    }

    ping(){
        this.#sendServerMessage("Ping", {clientid: this.networkId});
    }

    getPeers(){
        return this.peers.values();
    }

    getPeer(uuid: string){
        return this.peers.get(uuid);
    }

    setPeerProperty(key: string, value: string){
        this.#sendAppendPeerProperties({keys: [key], values: [value]});
    }

    setRoomProperty(key: string, value: string){
        this.#sendAppendRoomProperties({keys: [key], values: [value]});
    }

    getRoomProperty(key: string){
        return this.room.properties.get(key);
    }

    getRoomProperties(){
        return Object.fromEntries(this.room.properties);
    }

    // part of the interface for a network component
    processMessage(message: Message){
        let m = message.toObject();
        var args = JSON.parse(m.args);
        switch(m.type){
            case "SetRoom":
                this.#setRoomInfo(args.room);
                this.emit("OnJoinedRoom", this.room);
                break;
            case "Rejected":
                this.emit("OnJoinRejected", args.reason);
                break;
            case "Rooms":
                this.emit("OnRooms", args);
                break;
            case "RoomPropertiesAppended":
                this.#updateRoomProperties(args);
                this.emit("OnRoomUpdated", this.room);
                break;
            case "PeerPropertiesAppended":
                this.#updatePeerProperties(args.uuid, args);
                this.emit("OnPeerUpdated", this.peers.get(args.uuid));
                break;
            case "PeerAdded":
                    if(!this.peers.has(args.peer.uuid)){
                        let peer = new RoomPeer(args.peer);
                        this.peers.set(args.peer.uuid, peer);
                        this.emit("OnPeerAdded", peer);
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
                this.emit("OnPing", args.sessionId);
                break;
        }
    }
}