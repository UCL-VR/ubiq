class SpecialRoomServer{
    constructor(){
        this.server = new WebSocket.Server({ port: 8082 });
        this.server.on("connection", this.onConnection.bind(this));
        this.rooms = {};
    }

    onConnection(ws){
        new RoomClientConnection(new WebSocketConnectionWrapper(ws), new SpecialRoom()); // SpecialRoom acts as both the room and the server
    }
}

// The demonstration room is a special-case implementation for the demo scene. A new instance is created for each client.
class SpecialRoom{
    constructor(){
        this.uuid = "Demonstration Room Uuid";
        this.name = "Demonstration Room";
        this.clients = [];

        // the scene is an implicit client
        var scene = new Scene();
        scene.id = 100;
        scene.addComponent(new AvatarManager());
        scene.addComponent(new PeerConnectionManager());

        // manually set up the peer information to send to the real client
        var client = new RoomClientConnection(new SceneConnectionWrapper(scene), this);
        client.peer = new Object();
        client.peer.networkObject = scene.id;
        client.peer.component = 1;
        client.peer.uuid = "Demonstration Scene Remote Peer";

        // finally register the fake peer, so that it will be picked up in getRoomArgs() when a peer joins for real
        this.addPeer(client);

        /*
        for(const [key,object] of Object.entries(scene.objects)){
            for(const [key,component] of Object.entries(object.components)){
                if(typeof component.onJoinedRoom == "function"){
                    component.onJoinedRoom();
                }
            }
        }
        */
    }

    join(client, args){
        this.addPeer(client);
    }

    addPeer(client)
    {
        this.clients.push(client)
        client.setRoom(this);
    }

    getRoomArgs()
    {
        return {
            uuid: this.uuid,
            name : this.name,
            peers : this.clients.map(c => c.peer),
            properties : []
        };
    }

    processMessage(source, message){
        this.clients.forEach(client =>{
            if(client != source){
                client.send(message);
            }
        })
    }
}

class DemonstrationDummyPeer{
    constructor(){

    }

    connect(){

    }
}

// Wraps a Scene and emulates a connection *to* it for the benefit of other objects
class SceneConnectionWrapper{
    constructor(scene){
        this.scene = scene;
        this.onMessage = function(message){
        }
        this.scene.send = function(message){
            this.onMessage(message);
        }.bind(this);
    }

    send(message){
        this.scene.processMessage(message);
    }
}

class RoomClient{
    constructor(){
        this.id = 1;
    }

    processMessage(message){

    }
}

class AvatarManager{
    constructor(){
        this.id = 2;
    }

    processMessage(message){

    }

    onJoinedRoom(){

    }
}

class PeerConnectionManager{
    constructor(){
        this.id = 3;
    }

    processMessage(message){

    }
}