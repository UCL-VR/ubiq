import Ubiq from "/bundle.js"

// This creates a typical Browser WebSocket, with a wrapper that can 
// parse Ubiq messages.

// The config is downloaded before the module is dynamically imported
const config = window.ubiq.config;

const connection = new Ubiq.WebSocketConnectionWrapper(new WebSocket(`wss://${config.wss.uri}:${config.wss.port}`));

const scene = new Ubiq.NetworkScene();
scene.addConnection(connection);

// The RoomClient is used to leave and join Rooms. Rooms define which other
// Peers are in the Peer Group.

const roomClient = new Ubiq.RoomClient(scene);

roomClient.addListener("OnJoinedRoom", room => {
    console.log("Joined Room with Join Code " + room.joincode);
    document.getElementById("roomuuid").textContent = room.uuid;
    document.getElementById("roomjoincode").textContent = room.joincode;
});

// This section binds the Ubiq PeerConnection Component to the actual 
// RTCPeerConnection instance created by the browser. This is done by connecting
// the complementary APIs of the two types. Once this is done, the browser code 
// can interact with the RTCPeerConnection - adding outgoing tracks, listening
// for incoming ones - as if were any other.

const peerConnectionManager = new Ubiq.PeerConnectionManager(scene);

peerConnectionManager.addListener("OnPeerConnectionRemoved", component =>{
    for(let element of component.elements){
        element.remove();
    }
});

peerConnectionManager.addListener("OnPeerConnection", async component =>{
    let pc = new RTCPeerConnection({
        sdpSemantics: 'unified-plan',
    });

    component.elements = [];

    component.makingOffer = false;
    component.ignoreOffer = false;
    component.isSettingRemoteAnswerPending = false;

    // Special handling for dotnet peers
    component.otherPeerId = undefined;

    pc.onicecandidate = ({candidate}) => component.sendIceCandidate(candidate);

    pc.onnegotiationneeded = async () => {
        try {
            component.makingOffer = true;
            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);
            component.sendSdp(offer);
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
                    const answer = await pc.createAnswer();
                    await pc.setLocalDescription(answer);
                    component.sendSdp(answer);
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

    pc.ontrack = ({track, streams}) => {
        switch(track.kind){
            case 'audio':
                const audioplayer = document.getElementById("audioplayer");
                audioplayer.srcObject = new MediaStream([track]);
                break;
            case 'video':
                const videoplayer = document.getElementById("videoplayer");
                videoplayer.srcObject = new MediaStream([track]);
                break;
        }
    }

    pc.onconnectionstatechange = e => {
        if(e.target.connectionState == "disconnected"){
            for(let element of component.elements){
                element.remove();
            }
        }
    };
});

class StreamingCameraControls {
    constructor(scene, videoplayer){
        this.networkId = new Ubiq.NetworkId("e09e9c92-30b82547");
        this.context = scene.register(this);
        videoplayer.addEventListener("mousemove",(ev)=>{
            if(ev.buttons==1){
             this.context.send({
                x: ev.clientX,
                y: ev.clientY
             });
            }
        });
    }

    processMessage(m){
    }
}

const controls = new StreamingCameraControls(scene, document.getElementById("videoplayer"));

roomClient.join(config.room);
