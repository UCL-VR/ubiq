const { Message, NetworkId } = require("./messaging");

class NetworkContext{
    constructor(scene, object){
        this.object = object;   // The NetworkObject that this context belongs to
        this.scene = scene;     // The NetworkScene that this context belongs to
    }

    // Send a message to another Ubiq Networked Object. This method will intuit what to send based on the arguments.
    // The arguments can be,
    //  NetworkId
    //  Number
    //  Object
    //  Networked Object (object with an objectId and componentId properties)
    // The last object must always be the message to send, and this must be,
    //  JavaScript Object
    //  String
    //  Buffer.
    // You can call send in the following ways,
    //  this.context.send(this, message);
    //  this.context.send({objectId: objectId, componentId: componentId}, message);
    //  this.context.send(objectId, componentId, message);
    //  this.context.send(componentId, objectId, message);
    send(args){
        var message = arguments[arguments.length - 1]; // The message is always the last argument
        var objectId = this.object.objectId;
        var componentId = this.object.componentId;

        if(arguments.length == 0){
            throw "Send must have at least one argument"
        }
        else if(arguments.length == 1){
            // Nothing to do here, all the arguments are set up correctly.
            // Message.Create will intuit the correct encoding for the message on the wire
        }
        else{
            for(var i = 0; i < arguments.length-1; i++){
                var arg = arguments[i];
                if(typeof(arg) == "number"){
                    componentId = arg;
                }
                if(typeof(arg) == "object" && arg.hasOwnProperty("a") && arg.hasOwnProperty("b")){
                    objectId = new NetworkId(arg);
                }
                if(typeof(arg) == "networkid"){
                    objectId = arg;
                }
                if(typeof(arg) == "object" && arg.hasOwnProperty("objectId")){
                    objectId = arg.objectId;
                }
                if(typeof(arg) == "object" && arg.hasOwnProperty("componentId")){
                    componentId = arg.componentId;
                }
            }
        }

        this.scene.send(Message.Create(objectId, componentId, message));
    }
}

// A NetworkScene object provides the interface between a connection and the networked 
// objects in the application.

class NetworkScene{
    constructor(){
        this.objectId = NetworkId.Unique();
        this.objects = [];
        this.connections = []
    }

    // The Connection is expected to be a wrapped connection
    addConnection(connection){
        this.connections.push(connection);
        connection.onMessage.push(this.onMessage.bind(this));
        connection.onClose.push(this.onClose.bind(this, connection));
    }

    async onMessage(message){
        var object = this.objects.find(object => NetworkId.Compare(object.objectId, message.objectId) && (object.componentId == message.componentId));
        if(object !== undefined){
            object.processMessage(message);
        }
    }

    onClose(connection){
        var index = this.connections.indexOf(connection);
        if(index > -1){
            this.connections.slice(index, 1);
        }
    }

    send(buffer){
        this.connections.forEach(connection =>{
            connection.send(buffer);
        });
    }

    register(component){
        if(!component.hasOwnProperty("objectId")){
            console.error("Component does not have an objectId Property");
        }
        if(!component.hasOwnProperty("componentId")){
            console.error("Component does not have an objectId Property");
        }
        if(!component.processMessage){
            console.error("Component does not implement the processMessage method")
        }
        
        if(!this.objects.includes(component)){
            this.objects.push(component);
        }

        return new NetworkContext(this, component);
    }

    findComponent(name){
        return this.objects.find(object =>{
                return object.constructor.name == name;
            });
    }
}

module.exports = {
    NetworkScene,
    NetworkContext
}