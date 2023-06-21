import Ubiq from "/bundle.js"

// This creates a typical Browser WebSocket, with a wrapper that can 
// parse Ubiq messages.

// The config is downloaded before the module is dynamically imported
const config = window.ubiq.config;

const connection = new Ubiq.WebSocketConnectionWrapper(new WebSocket(`wss://${config.wss.uri}:${config.wss.port}`));
const scene = new Ubiq.NetworkScene();
scene.addConnection(connection);


import * as THREE from 'three'

const threejsscene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera( 75, window.innerWidth / window.innerHeight, 0.1, 1000 );

const renderer = new THREE.WebGLRenderer();
const container = document.getElementById("world");
renderer.setSize( container.offsetWidth, container.offsetHeight );
container.appendChild(renderer.domElement);

// The avatar object is the parent of the camera. The local camera
// transforms will be applied to the avatar's head in VR.
const avatar = new THREE.Group();
avatar.add(camera);
avatar.translateY(160);
avatar.rotateY(3.14);
threejsscene.add(avatar);

// Load the environment

import { FBXLoader } from 'three/addons/loaders/FBXLoader'

const loader = new FBXLoader();

loader.load("./Environment.fbx", function(object){
	threejsscene.add(object);
});

// Set up the first person camera

import { PointerLockControls } from 'three/addons/controls/PointerLockControls.js';

const playerController = new PointerLockControls(camera, container);

// -- The 3D scene is now configured --

// This section configures a simple Component for the avatar the browser controls.

class BrowserAvatar{
	constructor(scene){
		this.networkId = new Ubiq.NetworkId("b394b5c5-43e66da7")
		this.context = scene.register(this);
	}

	sendLookat(position, head, eyes){
		this.context.send({
			position,
			head:{
				x: head.x,
				y: head.y,
				z: head.z
			},
			eyes
		});
	}

	processMessage(m){

	}
}

// For this example, we have only one avatar controlled by the browser in a fixed
// position (with a fixed id).
// To support multiple avatars, the BrowserAvatar in Unity can be made a Prefab,
// and the NetworkSpawner used to create it.

const browserAvatar = new BrowserAvatar(scene);


// --- 
// This section sets up WebGazer to track the users gaze. The gaze is given via
// a callback and mapped to threejs coordinates before being transmitted to Unity.

// The majority of this section is to allow local calibration.

webgazer.ubiqConfig = {
	showPredictionPoints: true
};
const CalibrationPoints = {};
var PointCalibrate = 0;

// The WebGazer library performs a continous calibration based on mouse cursor
// movements and clicks.
// Therefore, the calibration process primary consists of prompting the user
// to make a number of well distributed interactions - there is no need to put
// the library into a special calibration mode.
// The functions below turn on and off calibration points. The code is based on
// the WebGazer calibration example.

function calPointClick(node) {
    const id = node.id;

    if (!CalibrationPoints[id]){ // initialises if not done
        CalibrationPoints[id]=0;
    }
    CalibrationPoints[id]++; // increments values

    if (CalibrationPoints[id]==5){ //only turn to yellow after 5 clicks
        node.style.setProperty('background-color', 'yellow');
        node.setAttribute('disabled', 'disabled');
        PointCalibrate++;
    }else if (CalibrationPoints[id]<5){
        //Gradually increase the opacity of calibration points when click to give some indication to user.
        var opacity = 0.2*CalibrationPoints[id]+0.2;
        node.style.setProperty('opacity', opacity);
    }

	if (PointCalibrate >= 9){ // last point is calibrated
        document.querySelectorAll('.Calibration').forEach((i) => {
            i.style.setProperty('display', 'none');
        });
		document.getElementById("instructions").textContent = "Calibration complete. You can re-calibrate at any time by pressing the Calibrate button.";
    }
}

function showCalibrationPoints(){
	document.querySelectorAll('.Calibration').forEach((i) => {
		i.removeAttribute("disabled");
		i.style.setProperty('background-color', 'red');
		i.style.removeProperty('display');
		i.style.setProperty('opacity', 0.2);
	})
	document.getElementById("instructions").textContent = "Look at each calibration point and click on it. Do this five times for each point. When each point is done, it will turn yellow.";
}

function hideCalibrationPoints(){
	document.querySelectorAll('.Calibration').forEach((i) => {
		i.style.setProperty('display', 'none');
	})
}

document.querySelectorAll('.Calibration').forEach((i) => {
	i.addEventListener('click', () => {
		calPointClick(i);
	})
})

document.getElementById("calibratebutton").onclick = () => {
	webgazer.clearData();
	showCalibrationPoints();
}

document.getElementById("gazedotbutton").onclick = () => {
	webgazer.ubiqConfig.showPredictionPoints = !webgazer.ubiqConfig.showPredictionPoints;
	webgazer.showPredictionPoints(webgazer.ubiqConfig.showPredictionPoints);
}

document.getElementById("videopreviewbutton").onclick = () => {
	webgazer.showVideoPreview(true);
}

webgazer.showVideoPreview(false);
hideCalibrationPoints();

webgazer.setGazeListener(function(data, elapsedTime) {
	if (data == null) {
		return;
	}
	// These lines put the eye positions in NDC
	const x = ((data.x / container.offsetWidth) - 0.5) * 2;
	const y = ((1 - (data.y / container.offsetHeight)) - 0.5) * 2;
	browserAvatar.sendLookat(
		camera.position, 
		camera.rotation,
		{
			x,
			y,
			z: camera.fov
		}
	);
}).begin();

// -- WebGazer is now configured...



// join the room
// The RoomClient is used to leave and join Rooms. Rooms define which other
// Peers are in the Peer Group.

const roomClient = new Ubiq.RoomClient(scene);

roomClient.addListener("OnJoinedRoom", room => {
	console.log("Joined Room with Join Code " + room.joincode);
});

// Manages the avatars. In this demo all avatars are the same mesh.

const avatarManager = new Ubiq.AvatarManager(scene);

const avatars = [];

avatarManager.addListener("OnAvatarCreated", avatar => {
	loader.load("./Avatar.fbx", function(object){
		var material = new THREE.MeshStandardMaterial( { color: 0xffffff, metalness: 0.9, roughness: 0.5, name: 'white' } );
		object.traverse(o =>{
			if(o.isMesh){
				o.material = material;
			}
		});
		threejsscene.add(object);
		object.threePointTrackedAvatar = new Ubiq.ThreePointTrackedAvatar(scene, avatar.networkId);
		object.scale.set(1,1,1);
		object.translateZ(100);
		object.translateY(80);
		avatars.push(object);
	});
});


roomClient.join(config.room);

// The pointer lock request must come from a user-event callback.
container.addEventListener("click", function(){
	if(!playerController.isLocked){
		playerController.lock();
	}
})




// Start rendering

function animate() {
	requestAnimationFrame( animate );
	avatars.forEach(a =>{
		a.position.set(a.threePointTrackedAvatar.head.position.x * -100,100,a.threePointTrackedAvatar.head.position.z*100);
	})
	renderer.render( threejsscene, camera );
}
animate();