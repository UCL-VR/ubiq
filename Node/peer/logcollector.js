const { NetworkId, NetworkScene, Uuid } = require("../ubiq")

class LogCollectorMessage{
    constructor(message){
        var buffer = message.toBuffer();
        this.header = buffer[0];
        this.tag = buffer[1];
        this.data = buffer.slice(2);
    }

    static Create(content){
        if(typeof(content) == "string"){
            content = Buffer.from(content, 'utf8');
            var buffer = Buffer.alloc(content.length + 2);
            buffer[0] = 0x2;
            buffer[1] = 0x0;
            content.copy(buffer,2);
            return buffer;
        }
        throw "Unsupported content";
    }

    toString(){
        return new TextDecoder().decode(this.data);
    }
}

class LogCollector{
    constructor(scene){
        this.objectId = new NetworkId("fc26-78b8-4498-9953");
        this.componentId = 0;
        this.context = scene.register(this);
        this.collecting = false;
        this.registerRoomClients();
    }

    static managerIds = {
        objectId: new NetworkId("92e9-e831-8281-2761"),
        componentId: 0
    }

    registerRoomClients(){
        var roomclient = this.context.scene.findComponent("RoomClient");
        if(roomclient == undefined){
            throw "RoomClient must be added to the scene before LogCollector";
        }
        roomclient.addListener("OnPeerAdded", function(){
            if(this.collecting){
                this.startCollection();
            }
        }.bind(this));
    }

    startCollection(){
        this.collecting = true;
        this.context.send(LogCollector.managerIds, LogCollectorMessage.Create("StartTransmitting"));
    }

    stopCollection(){
        this.context.send(LogCollector.managerIds, LogCollectorMessage.Create("StopTransmitting"));
    }

    processMessage(msg){
        var message = new LogCollectorMessage(msg);
        if(message.header == 0x1){

        }else if(message.header == 0x2){
            throw "Unsupported LogCollector String Message " + message.toString();
        }else{
            throw "Unsupported LogCollector Message Type " + message.header
        }
    }
}

module.exports = {
    LogCollector
}