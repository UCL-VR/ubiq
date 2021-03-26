const socket = new WebSocketConnection();
const context = new AudioContext();

const pc1 = new RtcPeerConnectionComponent(new SceneGraphBusNode(socket, 1), 1);

// Extend the peer connection component by defining more methods. These will be called by both the DOM and the Component itself.

// Note how these additions are anonymous functions, not lambdas, which allows them to access the this member

RtcPeerConnectionComponent.prototype.playAudioFile = async function () {
    //Streams an MP3 to a peer via WebRTC. Uses the Web Audio MediaStreamAudioSourceNode and MediaStreamAudioDestinationNode types to get an Audio element
    //as a media stream that can be provided to WebRTC as a track.
    //The Audio element has an audio file set to play on it.

    var player = document.getElementById("audioplayer");
    var source = context.createMediaElementSource(player);
    
    //source.connect(context.destination); // uncomment to play out of the speakers

    // destination is a node that can be added to the end of a graph, containing a media stream member that represents the output and can be forwarded to WebRTC
    
    var destination = context.createMediaStreamDestination();
    source.connect(destination);
    this.pc.addTrack(destination.stream.getAudioTracks()[0]); // By definition the destination stream has only one audio track.

    player.play();
    context.resume();
}

RtcPeerConnectionComponent.prototype.callLocalAudio  = async function () {
    var constraints = {
        audio: true,
        video: false,
        sampleRate: 44800
    }
    const localstream = await navigator.mediaDevices.getUserMedia(constraints);
    for (const track of localstream.getTracks()) {
        pc.addTrack(track, localstream);
    }
}

RtcPeerConnectionComponent.prototype.sendChatText = async function() {
    this.ch.send(new TextEncoder("utf-8").encode(document.getElementById("messagebox").value));
}

// Creates a data channel to send avatar updates as Json objects. The VirtualWorld is created automatically on startup.
// It is assumed the counterpart can match based on the Data Channel label.
RtcPeerConnectionComponent.prototype.callVirtualAvatar = async function() {
    this.avatarDataChannel = this.pc.createDataChannel("avatarTransforms");
    
    // helper method added directly to the dataChannelObject
    this.avatarDataChannel.encoder = new TextEncoder("utf-8");
    this.avatarDataChannel.sendJson = function(message){
        if(this.readystate = 'open'){
            this.send(this.encoder.encode(JSON.stringify(message)));
        }
    }
    window.setInterval(
        function(){
            this.avatarDataChannel.sendJson({ x: this.world.avatar.x, y: this.world.avatar.y });
        }.bind(this),
        30
    )

    this.avatarDataChannel.decoder = new TextDecoder("utf-8");
    this.avatarDataChannel.onmessage = function(message){
        this.world.onavatar(JSON.parse(this.avatarDataChannel.decoder.decode(message.data)));
    }.bind(this)
}

RtcPeerConnectionComponent.prototype.onDataChannelMessage = async function (event) {
    document.getElementById("conversation").innerText += new TextDecoder().decode(event.message.data) + "\n";
}

RtcPeerConnectionComponent.prototype.onAudioTrack = async function (track) {
    document.getElementById("audioelement").srcObject= new MediaStream([track]);
}

RtcPeerConnectionComponent.prototype.onVideoTrack = async function (track) {
    document.getElementById("videoelement").srcObject = new MediaStream([track]);
}

class VirtualWorldAvatar{
    constructor(){
        this.x = 0;
        this.y = 0;
    }

    draw(ctx){
        ctx.fillRect(this.x,this.y,10,10);
    }

    move(x,y){
        this.x = this.x + x;
        this.y = this.y + y;
    }
}


class VirtualWorld{
    constructor(canvas){
        this.canvas = canvas;
        this.ctx = canvas.getContext("2d");
        this.offsetx = this.canvas.width / 2;
        this.offsety = this.canvas.height / 2;
        this.ctx.translate(this.offsetx, this.offsety);
        this.avatar = new VirtualWorldAvatar();
        this.remote = new VirtualWorldAvatar();
        this.canvas.onmousemove = this.onmousemove.bind(this);
    }

    onavatar(avatartransform){
        this.remote.x = avatartransform.x;
        this.remote.y = avatartransform.y;
    }

    onmousemove(evt){
        var rect = this.canvas.getBoundingClientRect();
        var x = evt.clientX - rect.left - this.offsetx;
        var y = evt.clientY - rect.top - this.offsety;
        var dx = x - this.avatar.x;
        var dy = y - this.avatar.y;
        this.avatar.move(dx * 0.1, dy * 0.1);
    }

    animate(){
        requestAnimationFrame(this.animate.bind(this));
        this.ctx.clearRect(-this.offsetx, -this.offsety, this.canvas.width, this.canvas.height);
        this.avatar.draw(this.ctx);
        this.remote.draw(this.ctx);
    }
}

RtcPeerConnectionComponent.prototype.onLoaded = async function () {
    this.world = new VirtualWorld(document.getElementById("avatarcanvas"));
    requestAnimationFrame(this.world.animate.bind(this.world));
}