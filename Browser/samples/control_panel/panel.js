import { WebSocketConnectionWrapper, NetworkScene, NetworkId, RoomClient } from "/bundle.js"

// This creates a typical Browser WebSocket, with a wrapper that can 
// parse Ubiq messages.

// The config is downloaded before the module is dynamically imported
const config = window.ubiq.config;

const connection = new WebSocketConnectionWrapper(new WebSocket(`wss://${config.wss.uri}:${config.wss.port}`));

const scene = new NetworkScene();
scene.addConnection(connection);

// The RoomClient is used to leave and join Rooms. Rooms define which other
// Peers are in the Peer Group.

const roomClient = new RoomClient(scene);

roomClient.addListener("OnJoinedRoom", room => {
    console.log("Joined Room with Join Code " + room.joincode);
    document.getElementById("roomuuid").textContent = room.uuid;
    document.getElementById("roomjoincode").textContent = room.joincode;
});

const experimentNamespace = new NetworkId("9ea1be44-a29787fd");

class Door {
    constructor(scene, doorName){
        this.doorName = doorName;
        this.context = scene.register(this, NetworkId.Create(experimentNamespace, doorName));
        this.element = document.getElementById(doorName);
        this.element.onclick = () =>{
            this.context.send("Open");
        };
    }

    processMessage(m){
        switch(m.toString()){
            case "Opening":
                this.element.style = "background-color: yellow";
                break;
            case "Opened":
                this.element.style = "background-color: green";
                break;
            case "Notify":
                window.alert(this.doorName);
                break;
        }
    }
}

const door1 = new Door(scene, "Door1");
const door2 = new Door(scene, "Door2");
const door3 = new Door(scene, "Door3");

roomClient.join(config.room);
