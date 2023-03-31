// This sample demonstrates how to use the PeerConnectionManager to establish
// an RTCPeerConnection with another Peer, and use it to exchange RTC data
// programmatically.

const { NetworkScene, UbiqTcpConnection } = require("ubiq");
const { RoomClient, PeerConnectionManager } = require("components");
const { RTCPeerConnection, RTCIceCandidate } = require('wrtc');
const { RTCAudioSink } = require('wrtc').nonstandard;
const fs = require('fs')

const config = JSON.parse(fs.readFileSync(__dirname + "/../config.json"));

// Create a connection to a Server
const connection = UbiqTcpConnection(config.tcp.uri, config.tcp.port);

// A NetworkScene
const scene = new NetworkScene();
scene.addConnection(connection);

// A RoomClient to join a Room
const roomclient = new RoomClient(scene);

roomclient.addListener("OnJoinedRoom", room => {
    console.log("Joined Room with Join Code " + room.joincode);
});

roomclient.addListener("OnPeerAdded", peer =>{
    console.log("New Peer " + peer.uuid + " joined Room");
});

// The Node wrtc package emulates the Browsers RTCPeerConnection API.
// A number of examples in the node-wrtc-examples repository show how data
// can be extracted or generated and transmitted with RTCPeerConnection
// transcievers.
// This next section creates an RTCPeerConnection for each PeerConnection
// Component, and connects up their events so the PeerConnection acts as the
// signalling system.

const peerconnectionmanager = new PeerConnectionManager(scene);
peerconnectionmanager.addListener("OnPeerConnection", async component =>{
    let pc = new RTCPeerConnection({
        sdpSemantics: 'unified-plan'
      });

    component.addListener("OnIceCandidate", async c =>{
        // (The SipSorcery implementation misses the candidate: prefix, so add
        // it here if necessary)
        if(c !== null && c.candidate !== '' && !c.candidate.startsWith("candidate:")){
            c.candidate = "candidate:" + c.candidate;
        }
        // Sending a null candidate signals that candidate exchange has ended,
        // however wrtc does not accept null here so the end is ignored.
        if(c !== null){
            pc.addIceCandidate(c);
        }
    });

    component.addListener("OnSignallingMessage", async m =>{
        if(m.type == "offer"){
            await pc.setRemoteDescription(m);
            let answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            component.sendAnswer(answer);
        }
        if(m.type == "answer"){
            await pc.setRemoteDescription(m);
        }
        component.startCandidates();
    });

    pc.addEventListener("message", m =>{
        component.sendSignallingMessage(m);
    });

    pc.addEventListener("icecandidate", e =>{
        component.sendIceCandidate(e.candidate);
    });

    pc.addEventListener("track", e =>{
        console.log("Added track " + e.track.kind);
    });

    pc.addEventListener("negotiationneeded", async e=>{
        if(!component.polite){
            let offer = await pc.createOffer();
            await pc.setLocalDescription(offer);
            component.sendOffer(offer);
            component.startCandidates();
        }
    });

    // Now the RTCPeerConnection has been connected to the PeerConnection
    // signalling system, we can use it as we like.
    
    // This creates a recv-only audio stream. We assume the other Peer will
    // create an audio stream for the microphone, as most of the samples do.

    // See the node-webtrc-examples, such as the PitchDetector, for more 
    // examples of what can be done.

    const transceiver = pc.addTransceiver('audio');
    const sink = new RTCAudioSink(transceiver.receiver.track);
});

roomclient.join(config.room);