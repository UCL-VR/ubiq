const { Message, NetworkId } = require("./messaging");
const { EventEmitter } = require('events');

class NetworkContext{
    constructor(scene, object, networkId){
        this.object = object;   // The Networked Component that this context belongs to
        this.scene = scene;     // The NetworkScene that this context belongs to
        this.networkId = networkId; // The NetworkId that this object is registered under
    }

    // Send a message to another Ubiq Component. This method will work out the
    // Network Id to send to based on the arguments.
    // The arguments can be a,
    //  NetworkId
    //  Number
    //  Object
    //  Networked Component (object with an networkId property)
    // The last argument must always be the message to send, and this must be a,
    //  JavaScript Object
    //  String
    //  Buffer.
    // You can call send in the following ways,
    //  this.context.send(this, message);
    //  this.context.send({networkId: id}, message);
    //  this.context.send(networkId, message);
    send(args){
        if(arguments.length == 0){
            throw "Send must have at least one argument"
        }else if(arguments.length == 1){
            this.scene.send(this.networkId, arguments[0]);
        }else{
            this.scene.send(arguments[0], arguments[1]);
        }
    }
}

// A NetworkScene object provides the interface between a Connection and the 
// Networked Components in the application.
class NetworkScene extends EventEmitter{
    constructor(){
        super();
        this.networkId = NetworkId.Unique();
        this.entries = [];
        this.connections = []
        
    }

    // The Connection is expected to be a wrapped connection
    addConnection(connection){
        this.connections.push(connection);
        connection.onMessage.push(this.#onMessage.bind(this));
        connection.onClose.push(this.#onClose.bind(this, connection));
    }

    async #onMessage(message){
        this.entries.forEach(entry => {
            if(NetworkId.Compare(entry.networkId, message.networkId)){
                entry.object.processMessage(message);
            }
        });
        this.emit("OnMessage", message);
    }

    #onClose(connection){
        var index = this.connections.indexOf(connection);
        if(index > -1){
            this.connections.slice(index, 1);
        }
    }

    send(networkId, message){
        // Try to infer the Network Id format
        if(Object.getPrototypeOf(networkId).constructor.name == "NetworkId"){
            // Nothing to do
        }else if(typeof(networkId) == "number"){
            networkId = new NetworkId(networkId);
        }else if(typeof(networkId) == "object" && networkId.hasOwnProperty("a") && networkId.hasOwnProperty("b")){
            networkId = new NetworkId(networkId);
        }else if(typeof(networkId) == "object" && networkId.hasOwnProperty("networkId")){
            networkId = new NetworkId(networkId.networkId);
        }

        // Message.Create will determine the correct encoding of the message
        var buffer = Message.Create(networkId, message);

        this.connections.forEach(connection =>{
            connection.send(buffer);
        });
    }

    // Registers a Networked Component so that it will recieve messages addressed
    // to its specific NetworkId via its processMessage method.
    // If a NetworkId is not specified, it is found from the networkId member.
    register(args){
        let entry = {
            object: arguments[0]
        };
        if(arguments.length == 2){
            // The user is trying to register with a specific Id
            entry.networkId = arguments[1];
        }else if(arguments.length == 1){
            // The user is trying to register with the 'networkId' member
            if(!entry.object.hasOwnProperty("networkId")){
                console.error("Component does not have a networkId Property");
                return;
            }
            entry.networkId = entry.object.networkId;   
        }

        if(!entry.object.processMessage){
            console.error("Component does not have a processMessage method");
            return;
        }
        
        this.entries.push(entry);

        return new NetworkContext(this, entry.object, entry.networkId);
    }

    unregister(component){
        const i = this.entries.findIndex(entry =>{
            return entry.object === component;
        });
        if(i > -1){
            this.entries.splice(i, 1);
        }
    }

    getComponent(name){
        return this.entries.find(entry =>{
                return entry.object.constructor.name == name;
            }).object;
    }
}

module.exports = {
    NetworkScene,
    NetworkContext
}