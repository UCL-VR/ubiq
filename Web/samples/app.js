import Ubiq from "/bundle.js"

const connection = new Ubiq.WebSocketConnectionWrapper(new WebSocket("wss://192.168.1.2:8010"));

const scene = new Ubiq.NetworkScene();
scene.addConnection(connection);

const roomclient = new Ubiq.RoomClient(scene);

roomclient.addListener("OnJoinedRoom", room => {
    console.log("Joined Room with Join Code " + room.joincode);
});

roomclient.addListener("OnPeerAdded", peer => {
    console.log("New Peer: " + peer.uuid);
});

roomclient.addListener("OnPeerAdded", peer => {
    const peerDiv = document.createElement("div");
    peerDiv.setAttribute("id", peer.uuid);
    document.getElementById("peers").appendChild(peerDiv);
    updatePeerProperties(peer);
});

roomclient.addListener("OnPeerUpdated", peer => {
    updatePeerProperties(peer);
});

roomclient.addListener("OnJoinedRoom", room => {
    updateRoomProperties(room.properties);
});

roomclient.addListener("OnRoomUpdated", room => {
    updateRoomProperties(room.properties);
});



function updateRoomProperties(map) {
    const container = document.getElementById("roomdictionary");
    container.innerHTML = "";
    map.forEach((value,key) => {
        const entry = document.createElement("div");
        const disp = document.createTextNode(key + " " + value);
        entry.appendChild(disp);
        container.appendChild(entry);
    });
};

function updatePeerProperties(peer){
    const container = document.getElementById(peer.uuid);
    container.innerHTML = "";
    peer.properties.forEach((value,key) => {
        const entry = document.createElement("div");
        const disp = document.createTextNode(key + " " + value);
        entry.appendChild(disp);
        container.appendChild(entry);
    });
}



class WebSampleComponent{
    constructor(scene){
        this.networkId = new Ubiq.NetworkId("d184c9e2-d4e096fd");
        this.context = scene.register(this);
    }

    processMessage(m){
        console.log(m);
    }

    send(){
        this.context.send("Hello World!");
    }
}

const component = new WebSampleComponent(scene);

document.getElementById("sendbutton").onclick = () =>{
    component.send();
};



const peerconnectionmanager = new Ubiq.PeerConnectionManager(scene);

peerconnectionmanager.addListener("OnPeerConnection", async component =>{
    let pc = new RTCPeerConnection({
        sdpSemantics: 'unified-plan',
      });

    // https://developer.mozilla.org/en-US/docs/Web/API/WebRTC_API/Connectivity
    
    // Set up the listeners first

    component.addListener("OnIceCandidate", async c =>{
        // (The current version of wrtc requires the candidate to be prefixed
        // as below.)
        // Todo: which is out of spec, wrtc or sipsorcery?
        if(c !== null && c.candidate !== ''){
            c.candidate = "candidate:" + c.candidate;
        }
        pc.addIceCandidate(c);
        console.log("Received ice candidate");
    });

    component.addListener("OnSignallingMessage", async m =>{
        if(m.type == "offer"){
            await pc.setRemoteDescription(m);
            let answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            component.sendAnswer(answer);
            component.startCandidates();
        }
        if(m.type == "answer"){
            await pc.setRemoteDescription(m);
            component.startCandidates();
        }
        //Todo: rollback functionality
    });

    pc.addEventListener("message", m =>{
        component.sendSignallingMessage(m);
    });

    pc.addEventListener("icecandidate", e =>{
        component.sendIceCandidate(e.candidate);
        console.log("sending ice candidate");
    });

    pc.addEventListener("track", e =>{
        document.getElementById("audioelement").srcObject = e.streams[0];
    });

    pc.addEventListener("negotiationneeded", async e=>{
        let offer = await pc.createOffer();
        await pc.setLocalDescription(offer);
        component.sendOffer(offer);
        component.startCandidates();
    });

    // Create the channels, if we are the polite peer

    if(!component.polite){
        // Create the audio sources and sinks
        let dc = pc.createDataChannel("myChannel");
    }
});




const roomGuid = "6765c52b-3ad6-4fb0-9030-2c9a05dc4732";

roomclient.join(roomGuid);

/*

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
*/
export function hello(){
    alert("Hello World");
}