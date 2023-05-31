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
});

roomClient.addListener("OnPeerAdded", peer => {
    console.log("New Peer: " + peer.uuid);
});

roomClient.addListener("OnPeerAdded", peer => {
    updatePeerProperties(peer);
});

roomClient.addListener("OnPeerRemoved", peer =>{
    removePeerProperties(peer);
});

roomClient.addListener("OnPeerUpdated", peer => {
    updatePeerProperties(peer);
});

roomClient.addListener("OnJoinedRoom", room => {
    updateRoomProperties(room.properties);
});

roomClient.addListener("OnRoomUpdated", room => {
    updateRoomProperties(room.properties);
});

// Creates a new DOM node to represent a Peer. This includes a header and a
// dictionary for its properties.
function updatePeerProperties(peer) {
    let entry = document.getElementById(peer.uuid);
    if(entry === null){
        entry = document.createElement("div");
        entry.classList.add("block");
        entry.id = peer.uuid;

        const header = document.createElement("div");
        header.classList.add("header-1");
        header.innerText = `Peer ${peer.uuid}`;
        entry.appendChild(header);

        const dict = document.createElement("div");
        dict.classList.add("dictionary-list");
        entry.appendChild(dict);

        const container = document.getElementById("peers");
        container.appendChild(entry);
    }
    updateDictionaryElement(entry.children[1], peer.properties);
}

function removePeerProperties(peer) {
    let entry = document.getElementById(peer.uuid);
    if(entry !== null){
        entry.remove();
    }
}

// Updates the dictionary element that shows the properties of the room.
function updateRoomProperties(map) {
    updateDictionaryElement(document.getElementById("room"), map);
}

// Clears the contents of a node with with "dictionary-list" class applied and
// replaces it with the contents of the Map map. The new content will be a set
// of key and value elements containing the keys and values of each entry in
// map. The elements are laid out sequentially.
function updateDictionaryElement(container, map) {
    container.innerHTML = "";
    let row = 1;
    map.forEach((value,key) => {
        const keyNode = document.createElement("div");
        keyNode.className = "list-key";
        keyNode.innerHTML = key;
        keyNode.style = `grid-row: ${row} column 1`;

        const valueNode = document.createElement("div");
        valueNode.className = "list-value";
        valueNode.style = `grid-row: ${row} column 2`;

        row++;

        try{
            createObjectTree(valueNode, JSON.parse(value));
        }catch(e){
            valueNode.innerHTML = value;
        }

        container.appendChild(keyNode);
        container.appendChild(valueNode);
    });
};

// This function creates a set of elements below parentNode for all members of
// obj. If one of the members is itself an object, then this function calls
// itself in order to create a set of inset child elements for that object,
// and so on until the bottom of the graph is reached.
function createObjectTree(parentNode, obj, level = 0){
    for(let key in obj){
        let value = obj[key];

        const pairNode = document.createElement("div");
        pairNode.innerHTML = key + ": " + value;

        if(level > 0){
            pairNode.classList.add("nested");
        }

        parentNode.appendChild(pairNode);

        // If the value type is an object, create a child node to display that
        // object's members

        if(typeof(value) === "object"){
            pairNode.classList.add("caret");
            pairNode.classList.toggle("caret-down");

            pairNode.addEventListener("click", event =>{
                let children = pairNode.children;
                for(var i = 0; i < children.length; i++){
                    children[i].classList.toggle("hidden");
                }
                pairNode.classList.toggle("caret-down");
                event.stopPropagation();
            })

            createObjectTree(pairNode, value, level + 1);
        }
    }
}

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
    // pc.ontrack = ({track, streams}) => console.log("Added track " + track.kind);
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

    pc.ontrack = ({track, streams}) => {
        switch(track.kind){
            case 'audio':
                const prototpye = document.getElementById("audio-prototype");
                const audioplayer = prototpye.cloneNode(true);
                audioplayer.srcObject = new MediaStream([track]);
                audioplayer.classList.toggle("hidden");
                document.getElementById("audioelements").appendChild(audioplayer);
                component.elements.push(audioplayer);
        }
    }

    pc.onconnectionstatechange = e => {
        if(e.target.connectionState == "disconnected"){
            for(let element of component.elements){
                element.remove();
            }
        }
    };

    // Add our local microphone. Adding a track will create a transciever, which
    // will create both a sender and receiver, the receiver we can attach to
    // the local audio player.
    const microphoneStream = await navigator.mediaDevices.getUserMedia({
        audio: true
    });
    for(let track of microphoneStream.getAudioTracks()){
        pc.addTrack(track);
    }
});


class FlatSphere{
    constructor(threePointTrackedAvatar){
        this.avatar = threePointTrackedAvatar;
    }

    draw(ctx){
        ctx.beginPath();
        let size = Math.max(2 - Math.abs(this.avatar.head.position.y - 1.6),0) * 0.5 * ctx._scale;
        ctx.arc(
            this.avatar.head.position.x * ctx._scale,
            this.avatar.head.position.z * ctx._scale,
            size,0,2*Math.PI);
        ctx.stroke();
    }
}


class FlatWorld{
    constructor(canvas){
        this.canvas = canvas;
        this.ctx = canvas.getContext("2d");
        this.offsetx = this.canvas.width / 2;
        this.offsety = this.canvas.height / 2;
        this.ctx.translate(this.offsetx, this.offsety);
        this.ctx._scale = 10;
        this.avatars = [];
        requestAnimationFrame(this.animate.bind(this));
    }

    addAvatar(avatar){
        this.avatars.push(avatar);
    }

    animate(){
        requestAnimationFrame(this.animate.bind(this));
        this.ctx.clearRect(-this.offsetx, -this.offsety, this.canvas.width, this.canvas.height);
        this.avatars.forEach(avatar => {
            avatar.draw(this.ctx);
        });
    }
}

const world = new FlatWorld(document.getElementById("flatlandcanvas"));

const avatarManager = new Ubiq.AvatarManager(scene);

avatarManager.addListener("OnAvatarCreated", avatar => {
    world.addAvatar(new FlatSphere(new Ubiq.ThreePointTrackedAvatar(scene, avatar.networkId)));
});

function processBuffer(format, m){
    var result = "";
    var pointer = m.message.byteOffset;
    var end = pointer + m.message.byteLength;
    var buffer = m.buffer;
    var tokens = format.split(/\s+/);
    var token = 0;
    while(pointer < end){
        switch(tokens[token]){
            case "float":
                if(end - pointer < 4){
                    pointer = end;
                    break
                };
                result += buffer.readFloatLE(pointer);
                pointer += 4;
                break;
            case "int":
                if(end - pointer < 4){
                    pointer = end;
                    break
                };
                result += buffer.readIntLE(pointer);
                pointer += 4;
                break;
            case "string":
                return result + new TextDecoder("utf-8").decode(new Uint8Array(buffer.buffer, pointer));
            default:
                return "Unknown Format";
        }
        if(token < tokens.length - 1){
            token++;
        }
        result += "\n";
    }
    return result;
}

// The WebComponent allows users to listen and send to arbitrary Networked
// Components.

class WebComponent{
    constructor(scene, node, headernode, sendnode, inputnode, messagesnode, formatnode, closenode){
        this.messagesnode = messagesnode;
        this.formatnode = formatnode;
        this.node = node;
        sendnode.addEventListener("click", (e)=>{
            e.stopPropagation();
            this.context.send(inputnode.value);
        });
        closenode.addEventListener("click", (e)=>{
            e.stopPropagation();
            this.node.remove();

        });
        headernode.addEventListener("paste",(e)=>{
            e.preventDefault();
            var clipboarddata =  window.event.clipboardData.getData('text/plain');
            e.target.textContent = clipboarddata;
            e.target.dispatchEvent(new InputEvent("input"));
        })
        headernode.addEventListener("input", (e)=>{
            e.stopPropagation();
            try{
                let id = new Ubiq.NetworkId(e.target.textContent);
                if(this.networkId === undefined || !Ubiq.NetworkId.Compare(this.networkId,id)){
                    this.networkId = id;
                    scene.unregister(this);
                    this.context = scene.register(this);
                    e.target.classList.add("listener-bold");
                }
            }
            catch
            {
                e.target.classList.remove("listener-bold");
            }
        });
    }

    processMessage(m){
        // Use the format field to determine how to parse the message
        this.messagesnode.innerText = processBuffer(
            this.formatnode.innerText.toLowerCase(),
            m
        );
    }
}

document.getElementById("createlistenerbutton").onclick = () =>{
    const prototype = document.getElementById("listener-prototype");
    const node = prototype.cloneNode(true);
    node.style.display = "grid";

    const headernode = node.querySelector(".listener-header");
    const messagesnode = node.querySelector(".listener-messages");
    const inputnode = node.querySelector(".listener-input");
    const sendnode = node.querySelector(".listener-send");
    const closenode = node.querySelector(".listener-close");
    const formatnode = node.querySelector(".listener-format");

    const listeners = document.getElementById("listeners");
    listeners.append(node);

    const listener = new WebComponent(scene,
        node,
        headernode,
        sendnode,
        inputnode,
        messagesnode,
        formatnode,
        closenode
    );
};

// The ComponentStatistics object listens to all OnMessage events from the
// NetworkScene, regardless of who they are for, and displays the distribution
// of messages between the different Ids.
class ComponentStatistics{
    constructor(scene){
        this.ids = {}; // use an object instead of a Map here, so we can update individual fields directly
        scene.addListener("OnMessage", m =>{
            const networkId = m.networkId.toString();
            if(!this.ids.hasOwnProperty(networkId)){
                this.ids[networkId] = {
                    count: 0
                };
            }
            this.ids[networkId].count++;
        });
    }

    update(node){
        for(var id in this.ids){
            let row = document.getElementById(id);
            if(row == null){
                row = document.createElement("div");
                row.id = id;
                row.classList.add("address-grid-entry");

                const name = document.createElement("div");
                name.innerHTML = id;
                row.appendChild(name);

                const count = document.createElement("div");
                row.appendChild(count);

                node.appendChild(row);
            }
            row.childNodes[1].innerHTML = this.ids[id].count;
        }
    }
}

const stats = new ComponentStatistics(scene);
const statisticsGrid = document.getElementById("messagestatistics");

function f(){
    stats.update(statisticsGrid);
    window.requestAnimationFrame(f);
}
window.requestAnimationFrame(f);

roomClient.join(config.room);

export function hello(){
    alert("Hello World");
}