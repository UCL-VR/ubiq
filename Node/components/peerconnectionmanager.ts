import { NetworkId, type INetworkComponent, type NetworkContext, type NetworkScene, type Message } from 'ubiq'
import { EventEmitter } from 'events'
import { type RoomClient, type RoomPeer } from './roomclient'

// The PeerConnection Component sends and receives WebRTC signalling messages
// using Ubiq, allowing two Peers to boostrap a WebRTC PeerConnection Object.
// The application should be the one to create the WebRTC PC. Once this is
// done, it can be hooked up to this class. This PC manages the negotiation,
// including telling the implementation when to create the offer.
// WebRTC is used by Ubiq mainly to support voice chat, but the PC can also
// be used to send low latency P2P messages via UDP, and video.
export class PeerConnection extends EventEmitter implements INetworkComponent {
    networkId: NetworkId
    scene: NetworkScene
    uuid: string
    polite: boolean
    context: NetworkContext

    constructor (scene: NetworkScene, networkId: NetworkId, uuid: string, polite: boolean) {
        super()
        this.networkId = networkId
        this.scene = scene
        this.uuid = uuid
        this.polite = polite
        this.context = this.scene.register(this)
    }

    // These match the enumeration in the SignallingMessageHelper on the native side
    static Implementation = 0
    static IceCandidate = 1
    static Sdp = 2

    /* eslint-disable @typescript-eslint/strict-boolean-expressions */

    processMessage (message: Message): void {
        this.emit('OnSignallingMessage', message.toObject())
    }

    sendIceCandidate (m: any): void { // These types are determined by the webrtc API
        // A null iceCandidate means no further candidates, but support for
        // this is all over the place, so we just won't send it
        if (!m) {
            return
        }

        // Convert regular js object into Unity JsonUtility-friendly object
        this.context.send({
            cls: PeerConnection.IceCandidate,
            hasImplementation: false,
            candidate: m.candidate ? m.candidate : null,
            hasCandidate: !!m.candidate,
            sdpMid: m.sdpMid ? m.sdpMid : null,
            hasSdpMid: !!m.sdpMid,
            sdpMLineIndex: m.sdpMLineIndex ? m.sdpMLineIndex : null,
            hasSdpMLineIndex: !!m.sdpMLineIndex,
            usernameFragment: m.usernameFragment ? m.usernameFragment : null,
            hasUsernameFragment: !!m.usernameFragment,
            hasType: false,
            hasSdp: false
        })
    }

    sendSdp (m: any): void { // These types are determined by the webrtc API
        // Convert regular js object into Unity JsonUtility-friendly object
        this.context.send({
            cls: PeerConnection.Sdp,
            hasImplementation: false,
            hasCandidate: false,
            hasSdpMid: false,
            hasSdpMLineIndex: false,
            hasUsernameFragment: false,
            hasType: !!m.type,
            type: m.type ? m.type : null,
            hasSdp: !!m.sdp,
            sdp: m.sdp ? m.sdp : null
        })
    }
}

// The PeerConnectionManager is used to automatically establish PeerConnection
// Components with other Peers that have Peer Connection Managers.
// PeerConnection Components maintain WebRtc PeerConnections using Ubiq as
// the signalling layer.
export class PeerConnectionManager extends EventEmitter implements INetworkComponent {
    networkId: NetworkId
    serviceId: NetworkId
    scene: NetworkScene
    roomclient: RoomClient
    peers: any

    constructor (scene: NetworkScene) {
        super()
        this.serviceId = new NetworkId('c994-0768-d7b7-171c')
        this.networkId = NetworkId.Create(scene.networkId, this.serviceId)
        this.scene = scene
        this.scene.register(this)
        this.roomclient = this.scene.getComponent('RoomClient') as RoomClient
        this.roomclient.addListener('OnPeerAdded', this.OnPeerAdded.bind(this))
        this.roomclient.addListener('OnPeerRemoved', this.OnPeerRemoved.bind(this))
        this.peers = {}
    }

    OnPeerAdded (peer: RoomPeer): void {
        if (!this.peers.hasOwnProperty(peer.uuid)) {
            if (this.roomclient.peer.uuid.localeCompare(peer.uuid) > 0) {
                const pcid = NetworkId.Unique()
                this.createPeerConnection(pcid, peer.uuid, true)
                this.scene.send(
                    NetworkId.Create(peer.sceneid, this.serviceId),
                    {
                        type: 'RequestPeerConnection',
                        networkId: pcid,
                        uuid: this.roomclient.peer.uuid
                    }
                )
            }
        }
    }

    OnPeerRemoved (peer: RoomPeer): void {
        this.emit('OnPeerConnectionRemoved', this.peers[peer.uuid])
        // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
        delete this.peers[peer.uuid]
    }

    processMessage (message: Message): void {
        const m = message.toObject()
        switch (m.type) {
            case 'RequestPeerConnection':
                this.createPeerConnection(new NetworkId(m.networkId), m.uuid, false)
        }
    }

    createPeerConnection (pcid: NetworkId, uuid: string, polite: boolean): void {
        this.peers[uuid] = new PeerConnection(this.scene, pcid, uuid, polite)
        this.emit('OnPeerConnection', this.peers[uuid])
    }
}
