import { WebSocketConnection } from "/connections.js"
import { NetworkScene } from "/lib.js"
import { RoomClient } from "/lib.js"
import { AvatarManager } from "/lib.js"
import { WebRtcPeerConnectionManager, WebRtcPeerConnection } from "/lib.js"

const context = new AudioContext();

const player = document.getElementById("audioplayer");
const source = context.createMediaElementSource(player);

// Note how these additions are anonymous functions, not lambdas, which allows them to access the this member

WebRtcPeerConnection.prototype.playAudioFile = async function () {
    //Streams an MP3 to a peer via WebRTC. Uses the Web Audio MediaStreamAudioSourceNode and MediaStreamAudioDestinationNode types to get an Audio element
    //as a media stream that can be provided to WebRTC as a track.
    //The Audio element has an audio file set to play on it.

   // var player = document.getElementById("audioplayer"); // define these outside here so we can reuse the element
   // var source = context.createMediaElementSource(player);
    
    //source.connect(context.destination); // uncomment to play out of the speakers

    // destination is a node that can be added to the end of a graph, containing a media stream member that represents the output and can be forwarded to WebRTC
    
    var destination = context.createMediaStreamDestination();
    source.connect(destination);
    this.pc.addTrack(destination.stream.getAudioTracks()[0]); // By definition the destination stream has only one audio track.

    player.play();
    context.resume();
}

class StressTestPeer{
    constructor(){
        const connection = new WebSocketConnection("ws://localhost:8081");
        const scene = new NetworkScene(connection);
        const client = new RoomClient(scene);
        
        const avatarManager = new AvatarManager(client);
        avatarManager.addAvatar(); // trick the avatar manager into creating a bunch of avatars

        const peerConnectionManager = new WebRtcPeerConnectionManager(client);
        peerConnectionManager.onMakePeerConnection = () => new WebRtcPeerConnection(scene);
        peerConnectionManager.onPeerConnection = (pc) => pc.playAudioFile();
        
        client.join("Hello World");
    }
}

var clients = [];

function addClient(){
    clients.push(new StressTestPeer());
}

function go(){
    for(var i = 0; i < 1; i++){
        addClient();
    }
}

document.getElementById("playbutton").onclick = () => go();