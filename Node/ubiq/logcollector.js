const { Stream, EventEmitter } = require('stream');
const { NetworkId } = require("../ubiq")

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

class LogStream extends Stream.Readable{
    constructor(){
        super()
        this.paused = true;
    }

    // _read is called to indicate the sink is ready to receive. 
    // Beware the logging system does not support backpressure - ensure there is enough 
    // capacity downstream to receive all expected log events or out of memory issues
    // may occur.
    // The LogCollector won't write anything until startCollection has been called.
    _read(size){
        this.paused = false;
    }

    push(chunk){
        if(!this.paused){
            super.push(chunk);
        }
    }

    resume(){
        this.paused = false;
        super.resume();
    }
}

// The LogCollector can be attached to a NetworkScene with a RoomClient to receive logs
// from the LogMangers in a Room.
// Call startCollection() to begin receiving. There must only be one LogCollector in
// the Room.
// The log events are output via the userEventStream and applicationEventStream Readables. 
// Register for the "data" event, or pipe these to other streams to receive log events.
// Until this is done, or resume() is called, the streams will be paused. In paused mode
// the streams will discard any events, so make sure to connect the streams to the sink
// before calling startCollection().
class LogCollector extends EventEmitter{
    constructor(scene){
        super()
        this.objectId = new NetworkId("fc26-78b8-4498-9953");
        this.componentId = 0;
        this.context = scene.register(this);
        this.collecting = false;
        this.userEventStream = new LogStream();
        this.applicationEventStream = new LogStream();
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
            if(this.collecting){
                if(message.tag == 0x1){
                    this.applicationEventStream.push(message.data);
                }
                if(message.tag == 0x2){
                    this.userEventStream.push(message.data);
                }
            }
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