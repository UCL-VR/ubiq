// Implements client components necessary to join and talk to a room. This file is the counterpart
// to the one in the Web directory, but take care that they are not identical, as usage differs
// between Node and the Web (e.g. in how exports are done).

// See the classes below for usage examples. Each instance sets its parent property, usually in its
// constructor. The arguments to the constructors are other Components or Objects that exist above
// it in the tree, and which have already been registered.
// NetworkScene sets the scene property of an instance automatically when it is registered, so
// the reference is passed through the tree as it is built and each class only needs to know
// about its direct ancestor.

const { Message, NetworkId, Uuid } = require("./ubiq");
const { RTCPeerConnection, RTCSessionDescription } = require('wrtc');
const { RTCAudioSink, RTCAudioSource } = require('wrtc').nonstandard;
const { TextEncoder } = require("util");
const { throws } = require("assert");
const { parse } = require("path");

class NetworkContext{
    constructor(scene, object, component){
        this.object = object;
        // this.component = component;
        this.scene = scene;
        this.encoder = new TextEncoder();
    }

    // send to the counterpart of this component
    send1(message){
        // this.scene.send(Message.Create(this.object.objectId, this.component.componentId, message));
        this.scene.send(Message.Create(this.object.objectId, message));
    }

    // send to the counterpart of this component on a specific gameobject
    send2(objectId, message){
        // this.scene.send(Message.Create(objectId, this.component.componentId, message));
        this.scene.send(Message.Create(objectId, message));
    }

    // send to an arbitrary, specific component instance
    send3(objectId, componentId, message){
        // this.scene.send(Message.Create(objectId, componentId, message));
        this.scene.send(Message.Create(objectId, message));
    }
}

class NetworkScene{
    constructor(connection){
        this.connection = connection; // the js implementation supports only one uplink
        this.connection.onMessage.push(this.onMessage.bind(this));
        this.objectId = 0;
        this.scene = this;
        this.objects = [];
    }

    async onMessage(message){
        var object = this.objects.find(object => NetworkId.Compare(object.objectId, message.objectId));
        if(object !== undefined){
            var component = object.components[message.componentId];
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
        if(component.hasOwnProperty("objectId")){
            object = component;
        }
        if(object == null){
            var parent = component;
            while(parent.hasOwnProperty("parent")){
                if(parent.hasOwnProperty("objectId")){
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
        object.components[component.componentId] = component;

        //ensure the network scene is aware of the object. there is no strong scene graph here like in unity...
        if(!this.objects.includes(object)){
            this.objects.push(object);
        }

        component.scene = this;
        return new NetworkContext(this, object, component);
    }
}


class RoomClient{
    constructor(parent){
        // this.componentId = 1; // fixed at design time
        this.parent = parent;
        this.context = parent.scene.register(this);
        this.context.object.objectId = NetworkId.Unique();
        this.peer = new Object();
        this.peer.uuid = Uuid.generate();
        this.peer.networkObject = this.context.object.objectId;
        this.peer.component = this.context.component.componentId;
        this.peer.properties = new Object();
        this.peer.properties.keys = [];
        this.peer.properties.values = [];
        this.peers = [];
        this.room = new Object();
        this.onJoinedRoom = [];
    }

    join(uuid){
        var args = new Object();
        args.uuid = uuid;
        args.peer = this.peer;
        // this.context.send3(1, 1, { type: "Join", args: JSON.stringify(args) });
        this.context.send3(1, { type: "Join", args: JSON.stringify(args) });
    }

    updatePeer(){
        this.room.peers.map(peer => {
            // this.context.send3(1, 1, {type: "UpdatePeer", args: JSON.stringify(this.peer)});
            this.context.send3(1, {type: "UpdatePeer", args: JSON.stringify(this.peer)});
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

class AvatarManager{
    constructor(roomclient){
        this.roomclient = roomclient;
    }

    addAvatar(){
        // the avatarmanager does not talk to the network
        this.roomclient.peer.properties.keys.push("avatar-params");
        this.roomclient.peer.properties.values.push(JSON.stringify({ sharedId: NetworkScene.generateUniqueId() }));
    }
}

// Web counterpart of the Peer Connection Manager. Creates WebRtcPeerConnection instances for other peers in the room.
class WebRtcPeerConnectionManager{
    constructor(roomclient){
        this.componentId = 3;
        this.parent = roomclient;
        this.context = this.parent.scene.register(this);
        this.roomclient = roomclient;
        this.roomclient.onJoinedRoom.push(this.onJoinedRoom.bind(this));
        this.onMakePeerConnection = function() { throw "Not Implemented Yet" };
        this.onPeerConnection = () => {};
    }

    onJoinedRoom(){
        this.roomclient.peers.map( peer => {
            if(peer.uuid != this.roomclient.peer.uuid){
                var pc = this.onMakePeerConnection();
                pc.objectId = NetworkScene.generateUniqueId();
                pc.isnegotiator = true;
                pc.annoucementswaiting = 1;

                this.context.send2(peer.networkObject, { type: "RequestPeerConnection", objectId: pc.objectId, uuid: this.roomclient.peer.uuid, x_webclient: true });

                console.log("Requesting PeerConnection " + pc.objectId);

                this.onPeerConnection(pc);
            }
        })
    }

    processMessage(message){
        message = message.toObject();
        switch(message.type){
            case "RequestPeerConnection":
                var pc = this.onMakePeerConnection();
                pc.objectId = message.objectId;

                console.log("Received PeerConnectionRequest " + pc.objectId);

                if(message.x_webclient == true){
                    break;
                }

                this.onPeerConnection(pc);
                break;
        }
    }
}

// This code is based on the latest Mozilla examples
// e.g. https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection/setRemoteDescription
// As a rule, only properties, events etc that are listed in the Mozilla documentation are used.

// An RTCPeerConnection wrapped as an Component allowing it to use the CVE as a signaling system
class WebRtcPeerConnection
{
    constructor(parent){
        this.parent = parent;
        this.componentId = 7;
        this.objectId = -1; // webrtcpeerconnection must be a networkobject
        this.context = parent.scene.register(this);
        this.pc = new RTCPeerConnection();
        this.onAudioTrack = [];
        this.onVideoTrack = [];

        this.pc.onicecandidate = ({candidate}) => {
            this.send("icecandidate", candidate);
        }

        this.pc.onnegotiationneeded = this.onnegotiationneeded.bind(this);

        this.pc.ondatachannel = (event) => {
            var ch = event.channel;
            event.channel.onopen = () =>
            {
                console.log("Data channel opened");
            }
            event.channel.onmessage = (message) =>
            {
                this.onDataChannelMessage(event.channel, message);
            }
        }

        this.pc.ontrack = (event) => {
            event.pc = this.pc;
            if(event.track.kind == "audio"){
                this.onAudioTrack.map(callback => callback(event));
            }
            if(event.track.kind == "video"){
                this.onVideoTrack.map(callback => callback(event));
            }
        };
    }

    async processMessage(message) {
        message = message.toObject();

        if(message.type == "description"){
            var description = JSON.parse(message.args);
            if(description.type == "offer"){
                const offer = new RTCSessionDescription(description); // the node webrtc is creating an object using a function so needs an explicit variable
                await this.pc.setRemoteDescription(offer);
                var answer = await this.pc.createAnswer();
                await this.pc.setLocalDescription(answer);
                this.send("description", this.pc.localDescription);
            }
            else{ // description.type == "answer"
                const answer = new RTCSessionDescription(description);
                await this.pc.setRemoteDescription(answer);
            }
        }

        if(message.type == "icecandidate"){
            var candidate = JSON.parse(message.args);
            if(candidate != null){
                this.pc.addIceCandidate(candidate);
            }
        }
    };

    async onnegotiationneeded(){
        var offer = await this.pc.createOffer();
        await this.pc.setLocalDescription(offer);
        this.send("description", offer);
    }

    send(type, object){
        this.context.send1({"type":type, "args":JSON.stringify(object)});
    }
}

// Listens to the remote audio tracks added to a WebRtcPeerConnection component and uses the OnData callback
// to measure statistics about their audio and send it back to the source.
class WebRtcPeerConnectionUvMeter{
    constructor(parent){
        this.parent = parent;
        this.context = parent.scene.register(this);
        this.componentId = 8;
        this.parent.onAudioTrack.push(this.onAudioTrack.bind(this));
        this.counter = 0;
    }

    onAudioTrack(event){
        this.sink = new RTCAudioSink(event.track);
        this.sink.ondata = this.onSinkData.bind(this);
    }

    onSinkData(data){
        var sum = 0;
        data.samples.forEach((sample, i) => {
            sum = sum + Math.abs(sample);
        })
        sum = sum / data.samples.length;
        this.volume = (sum / 15000);
        if(this.counter == 0){
            this.context.send1({"volume" : this.volume});
        }
        else{
            this.counter = this.counter + 1;
        }
        if(this.counter > 100){
            this.counter = 0;
        }
    }
}

class WebRtcPeerConnectionRemoteSineWaveSample{
    constructor(parent){
        this.parent = parent;
        this.componentId = 9;
        this.context = parent.scene.register(this);
        this.frequency = 100;
        this.sampleRate = 48000;
        this.time = 0;
        this.channelCount = 1;
        this.bitsPerSample = 16;
        this.maxValue = Math.pow(2, this.bitsPerSample) / 2 - 1;
        this.numberOfFrames = this.sampleRate / 100;
        this.secondsPerSample = 1 / this.sampleRate;
        this.source = new RTCAudioSource();
        this.samples = new Int16Array(this.channelCount * this.numberOfFrames);
        this.source = new RTCAudioSource();
        this.a = [1,1];
        this.data = {
            samples: this.samples,
            sampleRate: this.sampleRate,
            bitsPerSample: this.bitsPerSample,
            channelCount: this.channelCount,
            numberOfFrames: this.numberOfFrames
        }
        this.timer = null;
        this.track = null;
    }

    processMessage(message){
        var message = message.toObject();
        if(message.command == "frequency"){
            this.frequency = message.argument;
        }
        if(message.command == "toggle"){
            this.toggleSineWave();
        }
    }

    startSineWave(){
        if(this.track == null){
            this.track = this.source.createTrack();
            this.parent.pc.addTrack(this.track);
        }
        if(this.timer == null){
            this.time = 0;
            this.timer = setInterval(this.next.bind(this), 10);
        }
    }

    toggleSineWave(){
        if(this.timer == null){
            this.startSineWave();
        }else{
            clearInterval(this.timer);
            this.timer = null;
        }
    }

    next() {
        for (let i = 0; i < this.numberOfFrames; i++, this.time += this.secondsPerSample) {
          for (let j = 0; j < this.channelCount; j++) {
            this.samples[i * this.channelCount + j] = this.a[j] * Math.sin(2 * Math.PI * this.frequency * this.time) * this.maxValue;
          }
        }
        this.source.onData(this.data);
      }
}

module.exports = {
    NetworkScene,
    WebRtcPeerConnection,
    WebRtcPeerConnectionUvMeter,
    WebRtcPeerConnectionRemoteSineWaveSample
}