const { exception, assert } = require('console');
const { TextDecoder } = require('util');
const { Schema } = require('./schema');
const { performance } = require('perf_hooks');
const { type } = require('os');

const MESSAGE_HEADER_SIZE = 8;

Schema.add({
    id: '/ubiq.messaging.networkid',
    type: "object",
    properties: {
        a: {type: "integer"},
        b: {type: "integer"}
    },
    required: ["a","b"]
});

class NetworkId{
    constructor(data){
        if(typeof(data) == 'string'){
            data = data.replace(/-/g,'');
            this.a = data.substring(0, 8);
            this.b = data.substring(8, 16);
            this.a = parseInt(this.a, 16);
            this.b = parseInt(this.b, 16);
            return;
        }
        if(typeof(data) == 'number'){
            this.a = 0;
            this.b = data;
            return;
        }
        if(Buffer.isBuffer(data)){
            this.a = data.readUInt32LE(0);
            this.b = data.readUInt32LE(4);
            return;
        }
        if(typeof(data) == 'object' && data.hasOwnProperty("a") && typeof(data.a) == 'number' && data.hasOwnProperty("b") && typeof(data.b) == 'number'){
            this.a = data.a;
            this.b = data.b;
            return;
        }
        throw exception();
    }

    static Compare(x, y){
        assert(x !== undefined && y !== undefined);
        return(x.a == y.a && x.b == y.b);
    }

    static WriteBuffer(networkId, buffer, offset){
        if(networkId == undefined){
            console.error("Undefined networkId when writing " + (new Error().stack));
            return;
        }
        buffer.writeUInt32LE(networkId.a, offset + 0);
        buffer.writeUInt32LE(networkId.b, offset + 4);
    }

    static Unique(){
        var d = new Date().getTime();//Timestamp
        var d2 = (performance && performance.now && (performance.now()*1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
        var id = 'xxxx-xxxx-xxxx-xxxx'.replace(/[xy]/g, function(c) {
            var r = Math.random() * 16;//random number between 0 and 16
            r = (d2 + r)%16 | 0;
            d2 = Math.floor(d2/16);
            return r.toString(16);
        });
        return new NetworkId(id);
    }

    static Valid(x){
        return x.a != 0 && x.b != 0;
    }

    static Create(namespace, service){
        var bytes = Buffer.from(service,'utf8');
        var order = false;
        var id = new NetworkId(namespace);
        for(var i = 0; i < bytes._length_; i++){
            if(i % 2 == 0){
                id.a = id.a * bytes[i] + id.b;
            }else{
                id.b = id.b * bytes[i] + id.a;
            }
        }
        return id;
    }

    static Null = new NetworkId(0);
}

Buffer.prototype.writeNetworkId = function(networkId, offset){
    NetworkId.WriteBuffer(networkId, this, offset);
}

Buffer.prototype.readNetworkId = function(offset){
    return new NetworkId(this.slice(offset));
}

class Message{
    constructor(){
    }

    static Wrap(data){
        var msg = new Message();
        msg.buffer = data;
        msg.length = data.readInt32LE(0)
        msg.objectId = new NetworkId(data.slice(4));
        msg.message = data.slice(12);
        return msg;
    }

    static Create(objectId, message){
        var msg = new Message();
        
        if(!Buffer.isBuffer(message)){
            if(typeof(message) == 'object'){
                message = JSON.stringify(message);
            }
            if(typeof(message) == 'string'){
                message = Buffer.from(message, 'utf8');
            }
        }

        var length = message.length + MESSAGE_HEADER_SIZE;
        var buffer = Buffer.alloc(length + 4);

        if(typeof(objectId) == "string"){
            objectId = new NetworkId(objectId);
        }

        buffer.writeInt32LE(length, 0);
        buffer.writeNetworkId(objectId, 4);
        message.copy(buffer, 12);

        var msg = new Message();
        msg.buffer = buffer;
        msg.length = length;
        msg.objectId = objectId;
        msg.message = message;

        return msg;
    }

    toString(){
        return new TextDecoder().decode(this.message);
    }

    toObject(){
        return JSON.parse(this.toString());
    }
    
    toBuffer(){
        return this.message;
    }
}

module.exports = {
    Message,
    NetworkId
}