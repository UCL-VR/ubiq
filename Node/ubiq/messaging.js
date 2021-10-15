const { exception } = require('console');
const { TextDecoder } = require('util');
const { Schema } = require('./schema');

const MESSAGE_HEADER_SIZE = 10;

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
            data = data.replace("-","");
            a = data.substring(0, 8);
            b = data.substring(8, 8);
            a = parseInt(a, 16);
            b = parseInt(b, 16);
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
        throw exception();
    }

    static Compare(x, y){
        return(x.a == y.a && x.b == y.b);
    }

    static WriteBuffer(networkId, buffer, offset){
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
        msg.componentId = data.readUInt16LE(12)
        msg.message = data.slice(14);
        return msg;
    }

    static Create(objectId, componentId, message){
        var msg = new Message();

        if(typeof(message) == 'object'){
            message = JSON.stringify(message);
        }
        if(typeof(message) == 'string'){
            message = Buffer.from(message, 'utf8');
        }

        var length = message.length + MESSAGE_HEADER_SIZE;
        var buffer = Buffer.alloc(length + 4);

        buffer.writeInt32LE(length, 0);
        buffer.writeNetworkId(objectId, 4);
        buffer.writeInt32LE(componentId, 12);
        message.copy(buffer, 14);

        var msg = new Message();
        msg.buffer = buffer;
        msg.length = length;
        msg.componentId = componentId;
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
}

module.exports = {
    Message,
    NetworkId
}