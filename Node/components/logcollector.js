const { EventEmitter } = require('events')
const { Stream } = require('stream')
const { NetworkId } = require("ubiq")
const { Buffer } = require('buffer');

class LogCollectorMessage{
    constructor(message){
        var buffer = message.toBuffer();
        this.type = buffer[0];
        this.tag = buffer[1];
        this.data = buffer.slice(2);
    }

    static Create(type, content){
        if(typeof(content) == "object"){
            content = JSON.stringify(content);
        }
        if(typeof(content) == "string"){
            content = Buffer.from(content, 'utf8');
            var buffer = Buffer.alloc(content.length + 2);
            buffer[0] = type;
            buffer[1] = 0x2; // Assume experiment for now
            content.copy(buffer,2);
            return buffer;
        }
        throw "Unsupported content";
    }

    toString(){
        return new TextDecoder().decode(this.data);
    }

    fromJson(){
        return JSON.parse(this.toString());
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
        this.networkId = NetworkId.Create(scene.networkId, "LogCollector");
        this.broadcastId = new NetworkId("685a5b84-fec057d0");
        this.destinationId = NetworkId.Null;
        this.clock = 0;

        // When set, this LogCollector will always try to maintain its position as the Primary collector.
        this.lock = false;

        // The LogCollector Component Js implementation is based on Streams.

        // All incoming events from the network or local emitters go into the 
        // eventStream. This can then cache, or be piped to a local or remote destination.
        this._eventStream = new Stream.Readable({
            objectMode: true,
            read(){
            }
        });

        // A stream that forwards log events to the LogCollector specified by destinationId
        // The eventStream should be piped to this when destinationId points to another LogCollector
        // in the Peer Group.
        this._forwardingStream = new Stream.Writable(
            {
                objectMode: true,
                write: (msg,_,done) => {
                    this.scene.send(this.destinationId,msg);
                    done();
                }
            }
        );
        this._forwardingStream.collector = this;

        // The stream that generates the events to be passed outside the LogCollector.
        this._writingStream = new Stream.Writable(
            {
                objectMode: true,
                write: (msg,_,done) => {
                    var eventMessage = new LogCollectorMessage(msg);
                    this.emit("OnLogMessage",
                        eventMessage.tag,
                        eventMessage.fromJson()
                    );
                    this.count = this.count + 1;
                    done();
                },
                count: 0
            }
        );
        this._writingStream.collector = this;

        this.scene = scene;
        scene.register(this, this.networkId);
        scene.register(this, this.broadcastId);
        this.registerRoomClientEvents();
    }

    written(){
        return this._writingStream.count;
    }

    registerRoomClientEvents(){
        this.roomClient = this.scene.getComponent("RoomClient");
        if(this.roomClient == undefined){
            throw "RoomClient must be added to the scene before LogCollector";
        }
        this.roomClient.addListener("OnPeerAdded", function(){
            if(this.isPrimary()){
                this.startCollection();
            }
        }.bind(this));
    }

    isPrimary(){
        return NetworkId.Compare(this.destinationId, this.networkId);
    }

    // Sets this LogCollector as the Primary Collector, receiving all events from the Peer Group and writing them to the provided Stream.
    startCollection(){
        this.sendSnapshot(this.networkId);
    }

    // Unsets this LogCollector as the Primary Collector and stops writing to the stream.
    stopCollection(){
        if(this.isPrimary()){
            this.sendSnapshot(NetworkId.Null);
        }
    }

    // Locks this LogCollector as the Primary collector.
    lockCollection(){
        this.lock = true;
        this.startCollection();
    }

    sendSnapshot(destinationId){
        this.destinationId = destinationId;
        this.clock++;
        this.destinationChanged();
        for(const peer of this.roomClient.getPeers()){
            this.scene.send(this.broadcastId, LogCollectorMessage.Create(0x1, {clock: this.clock, state: destinationId}));
        };
    }

    processMessage(msg){
        var message = new LogCollectorMessage(msg);
        switch(message.type){
            case 0x1: //Command
                var cc = message.fromJson();
                if(this.lock){
                    this.clock = cc.clock; // In locked mode, any attempts to change the state externally are immediately counteracted
                }
                if(cc.clock > this.clock){
                    this.clock = cc.clock;
                    this.destinationId = cc.state;
                    this.destinationChanged();
                }else{
                    if(cc.clock == this.clock && this.isPrimary()){
                        this.clock += Math.floor(Math.random() * 10);
                        this.sendSnapshot(this.networkId);
                    }
                }
                break;
            case 0x2: //Event
                this._eventStream.push(msg);
                break;
            case 0x3: //Ping
                var ping = message.fromJson();
                if(NetworkId.Valid(ping.Responder)){
                    // The ping is a respones to our request (we don't send requests at the moment so there is nothing to do here...)
                }
                else{
                    // The ping is a request
                    if(this.isPrimary()){
                        ping.Responder = this.networkId;
                        ping.Written = this.written();
                        this.scene.send(ping.Source, LogCollectorMessage.Create(0x3, ping));
                    }else if(NetworkId.Valid(this.destinationId)){
                        this._eventStream.push(msg);
                    }else{
                        ping.Responder = this.networkId;
                        ping.Aborted = true;
                        this.scene.send(ping.Source, LogCollectorMessage.Create(0x3, ping));
                    }
                }
                break;
        }
    }

    destinationChanged(){
        this._eventStream.unpipe();
        if(NetworkId.Valid(this.destinationId)){
            if(this.isPrimary()){
                this._eventStream.pipe(this._writingStream);
            }else{
                this._eventStream.pipe(this._forwardingStream);
            }
        }
    }

    log(ev){
        this._eventStream.push(
            LogCollectorMessage.Create(0x2, ev)
        );
    }
}

module.exports = {
    LogCollector
}