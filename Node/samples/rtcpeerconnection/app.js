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
// Implements https://w3c.github.io/webrtc-pc/#perfect-negotiation-example

const peerconnectionmanager = new PeerConnectionManager(scene);
peerconnectionmanager.addListener("OnPeerConnection", async component =>{
    let pc = new RTCPeerConnection({
        sdpSemantics: 'unified-plan'
    });

    component.makingOffer = false;
    component.ignoreOffer = false;
    component.isSettingRemoteAnswerPending = false;

    // Special handling for dotnet peers
    component.otherPeerId = undefined;

    pc.onicecandidate = ({candidate}) => component.sendIceCandidate(candidate);
    pc.ontrack = ({track, streams}) => console.log("Added track " + track.kind);
    pc.onnegotiationneeded = async () => {
        try {
            component.makingOffer = true;
            await pc.setLocalDescription();
            component.sendSdp(pc.localDescription);
        } catch (err) {
            console.error(err);
        } finally {
            component.makingOffer = false;
        }
    };

    component.addListener("OnSignallingMessage", async m => {

        // Special handling for dotnet peers
        if (component.otherPeerId === undefined) {
            component.otherPeerId = m.implementation ? m.implementation : null;
            if (component.otherPeerId == "dotnet") {
                // If just one of the two peers is dotnet, the
                // non-dotnet peer always takes on the role of polite
                // peer as the dotnet implementaton isn't smart enough
                // to handle rollback
                component.polite = true;
            }
        }

        let description = m.type ? {
            type: m.type,
            sdp: m.sdp
        } : undefined;

        let candidate = m.candidate ? {
            candidate: m.candidate,
            sdpMid: m.sdpMid,
            sdpMLineIndex: m.sdpMLineIndex,
            usernameFragment: m.usernameFragment
        } : undefined;

        try {
            if (description) {
              // An offer may come in while we are busy processing SRD(answer).
              // In this case, we will be in "stable" by the time the offer is processed
              // so it is safe to chain it on our Operations Chain now.
                const readyForOffer =
                    !component.makingOffer &&
                    (pc.signalingState == "stable" || component.isSettingRemoteAnswerPending);
                const offerCollision = description.type == "offer" && !readyForOffer;

                component.ignoreOffer = !component.polite && offerCollision;
                if (component.ignoreOffer) {
                    return;
                }
                component.isSettingRemoteAnswerPending = description.type == "answer";
                await pc.setRemoteDescription(description); // SRD rolls back as needed
                component.isSettingRemoteAnswerPending = false;
                if (description.type == "offer") {
                    await pc.setLocalDescription();
                    component.sendSdp(pc.localDescription);
                }
            } else if (candidate) {
                try {
                    await pc.addIceCandidate(candidate);
                } catch (err) {
                    if (!component.ignoreOffer) throw err; // Suppress ignored offer's candidates
                }
            }
        } catch (err) {
            console.error(err);
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