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

class ImuComponent {
	constructor(scene){
		this.networkId = new Ubiq.NetworkId("5221c34a-94fec9b5");
		this.context = scene.register(this);
	}

	sendFrame(f){
		this.context.send(f);
	}

	processMessage(m){
	}
}

const imuComponent = new ImuComponent(scene);

// This section establishes a connection to the Bluetooth device

// See Gergana Young @Medium for a succint example of connecting to a BLE 
// device in the Browser.
// https://medium.com/@gerybbg/web-bluetooth-by-example-6d200fa9a3ed

// These UUIDs are user defined and should match those the device is configured
// with. Under BLE, you can create as many services as desired.

// There are two ways to update a characteristic under BLE - the value can be 
// written or set.

const primaryServiceUuid = '19b10000-e8f2-537e-4f6c-d104768a1214';
const receiveCharUuid = '19b10001-e8f2-537e-4f6c-d104768a1214';

const connectButton = document.getElementById("connectbutton");
const valueDiv = document.getElementById("characteristicvalue")

let device, sendCharacteristic, receiveCharacteristic;
connectButton.onclick = async () => {
  device = await navigator.bluetooth
			 .requestDevice({
				filters: [{
				  services: [primaryServiceUuid]
				}]
			 });
  const server = await device.gatt.connect();
  const service = await server.getPrimaryService(primaryServiceUuid);
  receiveCharacteristic = await service.getCharacteristic(receiveCharUuid);
  
  receiveCharacteristic.addEventListener('characteristicvaluechanged', ev => {
		// The value will be received as a DataView of an ArrayBuffer
		const frame = {
			acceleration: {
				x: ev.target.value.getFloat32(0,true),
				y: ev.target.value.getFloat32(4,true),
				z: ev.target.value.getFloat32(8,true)
			},
			rotation: {
				x: ev.target.value.getFloat32(12,true),
				y: ev.target.value.getFloat32(16,true),
				z: ev.target.value.getFloat32(20,true)
			}
		}
		valueDiv.innerText = JSON.stringify(frame);
		imuComponent.sendFrame(frame);
	});

	receiveCharacteristic.startNotifications();
};

roomClient.join(config.room);
