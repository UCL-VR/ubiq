
const WebSocket = require('ws');
const { WebSocketConnectionWrapper } = require('./connections');
const { Message, NetworkId } = require("./messaging");
const { RTCPeerConnection } = require('wrtc');
const { NetworkScene, WebRtcPeerConnectionManager, WebRtcPeerConnection, WebRtcPeerConnectionUvMeter, WebRtcPeerConnectionRemoteSineWaveSample } = require('./lib');

// The Sandbox is a special server that creates a unique "room" for every client, used for testing
// individual features

class SandboxServer{
    constructor(port){
        var socket = new WebSocket.Server({ port: port });
        socket.on("connection", this.onConnection.bind(this));
        console.log("Started Sandbox on " + port);
    }

    onConnection(ws){
        new AudioSystemTestSandbox(new WebSocketConnectionWrapper(ws));
    }
}
class AudioSystemTestSandbox{
    constructor(connection){
        var scene = new NetworkScene(connection);
        var pc = new WebRtcPeerConnection(scene);
        pc.objectId = new NetworkId(1);
        pc.onAudioTrack.push((event) =>
        {
            event.pc.addTrack(event.track);
        })
        var uvMeter = new WebRtcPeerConnectionUvMeter(pc);
        var audiosource = new WebRtcPeerConnectionRemoteSineWaveSample(pc);
    }
}

module.exports ={
    SandboxServer
}