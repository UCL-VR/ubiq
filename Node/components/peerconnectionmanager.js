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
        this.candidatesPaused = true;
        this.candidatesBuffer = [];
    }

    processMessage(m){
        m = m.toObject();
        switch(m.type){
            case 0: // Session Description
                this.emit("OnSignallingMessage", JSON.parse(m.args));
                break;
            case 1: // Ice Candidate
                let candidate = JSON.parse(m.args);
                if(this.candidatesPaused){
                    this.candidatesBuffer.push(candidate)
                }else{
                    this.emit("OnIceCandidate", candidate);
                }
                break;
        }
    }

    pauseCandidates(){
        this.candidatesPaused = true;
    }

    startCandidates(){
        this.candidatesPaused = false;
        this.candidatesBuffer.forEach(candidate =>{
            this.emit("OnIceCandidate", candidate);
        });
    }

    sendSignallingMessage(m){
        this.context.send({
            type: 0,
            args: JSON.stringify(m)
        });
    }

    sendOffer(offer){
        this.sendSignallingMessage(offer);
    }

    sendAnswer(answer){
        this.sendSignallingMessage(answer);
    }

    sendIceCandidate(candidate){
        this.context.send({
            type: 1,
            args: JSON.stringify(candidate)
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
        delete this.peers[peer.uuid];
        // Todo: remove actual PeerConnection Component
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