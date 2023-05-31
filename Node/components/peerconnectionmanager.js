const { NetworkId } = require("ubiq")
const { EventEmitter } = require('events')

// The PeerConnection Component sends and receives WebRTC signalling messages
// using Ubiq, allowing two Peers to boostrap a WebRTC PeerConnection Object.
// The application should be the one to create the WebRTC PC. Once this is
// done, it can be hooked up to this class. This PC manages the negotiation,
// including telling the implementation when to create the offer.
// WebRTC is used by Ubiq mainly to support voice chat, but the PC can also
// be used to send low latency P2P messages via UDP, and video.
class PeerConnection extends EventEmitter{
    constructor(scene, networkId, uuid, polite){
        super();
        this.networkId = networkId;
        this.scene = scene;
        this.uuid = uuid;
        this.polite = polite;
        this.context = this.scene.register(this);
    }

    processMessage(m){
        m = m.toObject();

        // Convert Unity JsonUtility-friendly object into regular js object
        this.emit("OnSignallingMessage", {
            implementation: m.hasImplementation ? m.implementation : undefined,
            candidate: m.hasCandidate ? m.candidate : undefined,
            sdpMid: m.hasSdpMid ? m.sdpMid : undefined,
            sdpMLineIndex: m.hasSdpMLineIndex ? m.sdpMLineIndex : undefined,
            usernameFragment: m.hasUsernameFragment ? m.usernameFragment : undefined,
            type: m.hasType ? m.type : undefined,
            sdp: m.hasSdp ? m.sdp : undefined,
        });
    }

    sendIceCandidate(m){

        // A null iceCandidate means no further candidates, but support for
        // this is all over the place, so we just won't send it
        if (!m) {
            return;
        }

        // Convert regular js object into Unity JsonUtility-friendly object
        this.context.send({
            hasImplementation: false,
            candidate: m.candidate ? m.candidate : null,
            hasCandidate: m.candidate ? true : false,
            sdpMid: m.sdpMid ? m.sdpMid : null,
            hasSdpMid: m.sdpMid ? true : false,
            sdpMLineIndex: m.sdpMLineIndex ? m.sdpMLineIndex : null,
            hasSdpMLineIndex: m.sdpMLineIndex ? true : false,
            usernameFragment: m.usernameFragment ? m.usernameFragment : null,
            hasUsernameFragment: m.usernameFragment ? true : false,
            hasType: false,
            hasSdp: false,
        });
    }

    sendSdp(m){
        // Convert regular js object into Unity JsonUtility-friendly object
        this.context.send({
            hasImplementation: false,
            hasCandidate: false,
            hasSdpMid: false,
            hasSdpMLineIndex: false,
            hasUsernameFragment: false,
            hasType: m.type ? true : false,
            type: m.type ? m.type : null,
            hasSdp: m.sdp ? true : false,
            sdp: m.sdp ? m.sdp : null,
        });
    }
}

// The PeerConnectionManager is used to automatically establish PeerConnection
// Components with other Peers that have Peer Connection Managers.
// PeerConnection Components maintain WebRtc PeerConnections using Ubiq as
// the signalling layer.
class PeerConnectionManager extends EventEmitter{
    constructor(scene){
        super();
        this.serviceId = new NetworkId("c994-0768-d7b7-171c");
        this.networkId = NetworkId.Create(scene.networkId, this.serviceId);
        this.scene = scene;
        this.scene.register(this);
        this.roomclient = this.scene.getComponent("RoomClient");
        this.roomclient.addListener("OnPeerAdded", this.OnPeerAdded.bind(this));
        this.roomclient.addListener("OnPeerRemoved", this.OnPeerRemoved.bind(this));
        this.peers = {};
    }

    OnPeerAdded(peer){
        if(!this.peers.hasOwnProperty(peer.uuid)){
            if(this.roomclient.peer.uuid.localeCompare(peer.uuid) > 0){
                let pcid = NetworkId.Unique();
                this.createPeerConnection(pcid, peer.uuid, true);
                this.scene.send(
                    NetworkId.Create(peer.sceneid,this.serviceId),
                    {
                        type: "RequestPeerConnection",
                        networkId: pcid,
                        uuid: this.roomclient.peer.uuid
                    }
                )
            }
        }
    }

    OnPeerRemoved(peer){
        this.emit("OnPeerConnectionRemoved", this.peers[peer.uuid])
        delete this.peers[peer.uuid];
    }

    processMessage(m){
        m = m.toObject();
        switch(m.type){
            case "RequestPeerConnection":
                this.createPeerConnection(m.networkId, m.uuid, false);
        }
    }

    createPeerConnection(pcid, uuid, polite){
        this.peers[uuid] = new PeerConnection(this.scene, pcid, uuid, polite);
        this.emit("OnPeerConnection", this.peers[uuid]);
    }
}

module.exports = {
    PeerConnectionManager,
    PeerConnection
}