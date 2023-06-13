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

const log = new Ubiq.LogCollector(scene);

var experimentInstance = Ubiq.NetworkId.Unique().toString();

class QuestionnaireController{
	constructor(scene){
		this.networkId = new Ubiq.NetworkId("4caddaa6-5627a5fa");
		this.context = scene.register(this);
	}

	processMessage(m){
		console.log(m.toString());
		switch(m.toString()){
			case "DoQuestionnaire":
				this.showQuestionnaire();
				break;
			default:
				experimentInstance = m.toString();
		}
	}

	questionnaireDone(){
		this.context.send("QuestionnaireComplete");
	}

	showQuestionnaire(){
		document.querySelectorAll(".instruction").forEach(x => x.style.display = 'none');
		document.querySelectorAll(".question").forEach(x => x.style.display = 'grid');
	}

	showInstructions(){
		document.querySelectorAll(".instruction").forEach(x => x.style.display = 'grid');
		document.querySelectorAll(".question").forEach(x => x.style.display = 'none');
	}
}

const questionnaireController = new QuestionnaireController(scene);


document.getElementById("submitbutton").addEventListener("click",()=>{
	log.log({
		peer: experimentInstance,
		results: {
			cylinder1: document.getElementById("slider1").value,
			cylinder2: document.getElementById("slider2").value,
			cylinder3: document.getElementById("slider3").value,
			report: document.getElementById("question2response").value
		}
	});
	questionnaireController.questionnaireDone();
	questionnaireController.showInstructions();
	console.log("Questionnaire submitted");
});

questionnaireController.showInstructions();

roomClient.join(config.room);
