import { Message } from "/messaging.js"
import { uuid } from "/uuid.js"

class NetworkContext{
    constructor(scene, object, component){
        this.object = object;
        this.component = component;
        this.scene = scene;
        this.encoder = new TextEncoder();
    }

    // send to the counterpart of this component
    send1(message){
        this.scene.send(Message.Create(this.object.objectid, this.component.componentid, message));
    }

    // send to the counterpart of this component on a specific gameobject
    send2(objectid, message){
        this.scene.send(Message.Create(objectid, this.component.componentid, message));
    }

    // send to an arbitrary, specific component instance
    send3(objectid, componentid, message){
        this.scene.send(Message.Create(objectid, componentid, message));
    }

    getNewObjectId(){
        return Math.ceil(Math.random() * 2147483647);
    }
}

export class NetworkScene{
    constructor(connection){
        this.connection = connection; // the web implementation supports only one uplink
        this.connection.onmessage.push(this.onMessage.bind(this));
        this.objectid = 0;
        this.scene = this;
        this.objects = [];
    }

    async onMessage(message){
        var object = this.objects.find(object => object.objectid == message.objectid);
        if(object !== undefined){
            var component = object.components[message.componentid];
            if(component !== undefined){
                component.processMessage(message);
            }
        }
    }

    send(buffer){
        this.connection.send(buffer);
    }

    register(component){
        var object = null;
        if(component.hasOwnProperty("objectid")){
            object = component;
        }
        if(object == null){
            var parent = component;
            while(parent.hasOwnProperty("parent")){
                if(parent.hasOwnProperty("objectid")){
                    object = parent;
                    break;
                }
                parent = parent.parent;
            }
        }
        if(object == null){
            object = this;
        }
        
        if(!object.hasOwnProperty("components")){
            object.components = {};
        }
        object.components[component.componentid] = component;

        //ensure the network scene is aware of the object. there is no strong scene graph here like in unity...
        if(!this.objects.includes(object)){
            this.objects.push(object);
        }

        component.scene = this;
        return new NetworkContext(this, object, component);
    }

    static GetNewObjectId(){
        return Math.ceil(Math.random() * 2147483647);
    }
}


export class RoomClient{
    constructor(parent){
        this.componentid = 1; // fixed at design time
        this.parent = parent;
        this.context = parent.scene.register(this);
        this.context.object.objectid = this.context.getNewObjectId();
        this.peer = new Object();
        this.peer.guid = uuid();
        this.peer.networkObject = this.context.object.objectid;
        this.peer.component = this.context.component.componentid;
        this.peer.properties = new Object();
        this.peer.properties.keys = [];
        this.peer.properties.values = [];
        this.peers = [];
        this.room = new Object();
        this.onJoinedRoom = [];
    }

    join(guid){
        var args = new Object();
        args.guid = guid;
        args.peer = this.peer;
        this.context.send3(1, 1, { type: "Join", args: JSON.stringify(args) });
    }

    updatePeer(){
        this.room.peers.map(peer => {
            this.context.send3(1, 1, {type: "Peer", args: JSON.stringify(this.peer)});
        })
    }

    // part of the interface for a network component
    processMessage(message){
        message = message.toObject();
        switch(message.type){
            case "Accepted":
                var args = JSON.parse(message.args);
                this.room = args.room;
                this.peers = args.peers;
                this.onJoinedRoom.map(callback => callback());
                break;
        }
    }
}

export class AvatarManager{
    constructor(roomclient){
        this.roomclient = roomclient;
    }

    addAvatar(){
        // the avatarmanager does not talk to the network
        this.roomclient.peer.properties.keys.push("avatar-params");
        this.roomclient.peer.properties.values.push(JSON.stringify({ sharedId: NetworkScene.GetNewObjectId() }));
    }
}

// Web counterpart of the Peer Connection Manager. Creates WebRtcPeerConnection instances for other peers in the room.
export class WebRtcPeerConnectionManager{
    constructor(roomclient){
        this.componentid = 3;
        this.parent = roomclient;
        this.context = this.parent.scene.register(this);
        this.roomclient = roomclient;
        this.roomclient.onJoinedRoom.push(this.onJoinedRoom.bind(this));
        this.onMakePeerConnection = function() { throw "Not Implemented Yet" };
    }

    onJoinedRoom(){
        this.roomclient.peers.map( peer => {
            if(peer.guid != this.roomclient.peer.guid){
                var pc = this.onMakePeerConnection();
                pc.objectid = NetworkScene.GetNewObjectId();
                pc.isnegotiator = true;
                pc.annoucementswaiting = 1;

                this.context.send2(peer.networkObject, { type: "RequestPeerConnection", objectid: pc.objectid, guid: this.roomclient.peer.guid, x_webclient: true });
                
                console.log("Requesting PeerConnection " + pc.objectid);
                
                this.onPeerConnection?.(pc);
            }
        })
    }

    processMessage(message){
        message = message.toObject();
        switch(message.type){
            case "RequestPeerConnection":
                var pc = this.onMakePeerConnection();
                pc.objectid = message.objectid;
                
                console.log("Received PeerConnectionRequest " + pc.objectid);
                
                if(message.x_webclient == true){
                    break;
                }

                this.onPeerConnection?.(pc);
                break;
        }
    }
}

// This code is based on the latest Mozilla examples 
//e.g. https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection/setRemoteDescription
// As a rule, only properties, events etc that are listed in the Mozilla documentation are used.

// An RTCPeerConnection wrapped as an Entity allowing it to easily exchange messages with the scene graph bus
export class WebRtcPeerConnection
{
    constructor(parent){
        this.parent = parent;
        this.componentid = 7;
        this.objectid = -1; // webrtcpeerconnection must be a networkobject
        this.context = parent.scene.register(this);
        this.annoucementswaiting = 0;
        this.isnegotiator = false;
        this.isnegotiating = false;
        this.onannoucecallback = null;

        this.processMessage = async (message) => {
            message = message.toObject();
            
            if(message.type == "announce"){
                this.annoucementswaiting--;
                this.onannoucecallback?.();
            }

            if (message.type == "role")
            {
                var args = JSON.parse(message.args);
                if (args.type == "request")
                {
                    if(!this.isnegotiating){
                        this.isnegotiator = false;
                        this.send("role", { type: "response" });
                    } // otherwise ignore the request and rely on the client's negotiationneeded event being fired again when the state goes stable
                }
                if(args.type == "response")
                {
                    this.isnegotiator = true;
                    this.renegotiate(); // the request is only submitted in response to a renegotiationneeded event
                }
            }

            if(message.type == "description"){
                var description = JSON.parse(message.args);
                if(description.type == "offer"){
                    await this.pc.setRemoteDescription(new RTCSessionDescription(description));
                    var answer = await this.pc.createAnswer();
                    await this.pc.setLocalDescription(answer);
                    this.send("description", this.pc.localDescription);
                }
                else{ // description.type == "answer"
                    await this.pc.setRemoteDescription(new RTCSessionDescription(description));
                }
            }
            
            if(message.type == "icecandidate"){
                var candidate = JSON.parse(message.args);
                if(candidate != null){
                    this.pc.addIceCandidate(candidate);
                }
            }
        };

        this.pc = new RTCPeerConnection();
        
        this.pc.onicecandidate = ({candidate}) => {
            this.send("icecandidate", candidate);
        }

        this.pc.onnegotiationneeded = this.renegotiateonannounce.bind(this);

        this.pc.ondatachannel = (event) => {
            var ch = event.channel;
            event.channel.onopen = () =>
            {
                console.log("Data channel opened");
                this.onDataChannel?.(event.channel);
            }
            event.channel.onmessage = (message) =>
            {
                this.onDataChannelMessage?.(event.channel, message);
            }
        }

        this.pc.ontrack = (event) => {
            if(event.track.kind == "audio")
            {
                this.onAudioTrack?.(event.track);
            }
            if(event.track.kind == "video")
            {
                this.onVideoTrack?.(event.track);
            }
        };

        this.pc.onsignalingstatechange = (event) =>{
            if(this.pc.signalingState === "stable"){
                this.isnegotiating = false;
            }
        }

        this.send("announce", null);
    }

    async renegotiateonannounce(){
        if(this.annoucementswaiting > 0){
            this.onannoucecallback = this.renegotiateonannounce.bind(this);
        }else{
            this.onannoucecallback = null;
            this.renegotiate();
        }
    }

    async renegotiate(){
        if(this.isnegotiator){
            this.isnegotiating = true;
            var offer = await this.pc.createOffer();
            await this.pc.setLocalDescription(offer);
            this.send("description", offer);
        }else{
            this.send("role", {type: "request"} );
        }
    }

    send(type, object){
        this.context.send1({"type":type, "args":JSON.stringify(object)});
    }

}


