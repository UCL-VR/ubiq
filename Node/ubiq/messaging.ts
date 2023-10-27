import perf_hooks from "perf_hooks";
import { z } from 'zod';

const performance = perf_hooks.performance;

const MESSAGE_HEADER_SIZE = 8;

export const NetworkIdSchema = z.object({
    a: z.number().int(),
    b: z.number().int()
})

export interface INetworkId extends z.infer<typeof NetworkIdSchema>{
}

export type NetworkIdObject = Record<string, any> & {
    networkId: LooseNetworkId
}

export type LooseNetworkId = string | number | Buffer | ArrayBufferView | NetworkId | NetworkIdObject

export class NetworkId{
    a: number;
    b: number;
    constructor(data : LooseNetworkId){
        if(typeof(data) == 'string'){
            data = data.replace(/-/g,'');
            this.a = parseInt(data.substring(0, 8), 16);
            this.b = parseInt(data.substring(8, 16), 16);
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
        if(ArrayBuffer.isView(data)){
            var view = new Uint32Array(data.buffer);
            this.a = view[0];
            this.b = view[1];
            return;
        }
        if(typeof(data) == 'object' && data.hasOwnProperty("a") && typeof(data.a) == 'number' && data.hasOwnProperty("b") && typeof(data.b) == 'number'){
            this.a = data.a;
            this.b = data.b;
            return;
        }
        throw `Cannot construct NetworkId from ${data}`;
    }

    toString(){
        return `${this.a.toString(16)}-${this.b.toString(16)}`;
    }

    static Schema = z.object({
        a: z.number().int(),
        b: z.number().int()
    });

    static Null = {a: 0, b: 0};

    static Compare(x : NetworkId, y: NetworkId) {
        return (x.a == y.a && x.b == y.b);
    }

    static WriteBuffer(networkId : NetworkId, buffer : Buffer, offset : number) {
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

    static Valid(x : NetworkId){
        return x.a != 0 && x.b != 0;
    }

    static Create(namespace: LooseNetworkId, service : string | number | NetworkId){
        var id = new NetworkId(namespace);
        var data = new Uint32Array(2);
        data[0] = id.a;
        data[1] = id.b;
        if(typeof(service) === 'number'){
            data[0] = Math.imul(data[0], service);
            data[1] = Math.imul(data[1], service);
            return new NetworkId(data);
        }else if(service instanceof NetworkId){
            data[0] = Math.imul(data[0], service.b);
            data[1] = Math.imul(data[1], service.a);
            return new NetworkId(data);
        }else if(typeof(service) === 'string'){
            var bytes = Buffer.from(service,'utf8');
            for(var i = 0; i < bytes.length; i++){
                if(i % 2 != 0){
                    data[0] = Math.imul(data[0], bytes[i]);
                }else{
                    data[1] = Math.imul(data[1], bytes[i]);
                }
            }
            return new NetworkId(data);
        }
        throw `Cannot construct namespaced NetworkId from ${namespace} and ${service}`;
    }
}

interface IMessage {
    buffer: Buffer
    length: number
    networkId: NetworkId
    message: Buffer
}

export class Message implements IMessage{
    buffer: Buffer = Buffer.from("");
    length: number = -1;
    networkId: NetworkId = new NetworkId(-1);
    message: Buffer = Buffer.from("");
    constructor(){
    }

    static Wrap(data : Buffer){
        var msg = new Message();
        msg.buffer = data;
        msg.length = data.readInt32LE(0)
        msg.networkId = new NetworkId(data.slice(4));
        msg.message = data.slice(12);
        return msg;
    }

    static Create(networkId : NetworkId | string, message : string | object | Buffer){
        var msg = new Message();
        let convertedMessage : Buffer = Buffer.from("");
        if(!Buffer.isBuffer(message)){
            if(typeof(message) == 'object'){
                convertedMessage = Buffer.from(JSON.stringify(message), 'utf8');
            }
            if(typeof(message) == 'string'){
                convertedMessage = Buffer.from(message, 'utf8');
            }
        }
        else {
            convertedMessage = message;
        }
        var length = convertedMessage.length + MESSAGE_HEADER_SIZE;
        var buffer = Buffer.alloc(length + 4);

        if(typeof(networkId) == "string"){
            networkId = new NetworkId(networkId);
        }

        buffer.writeInt32LE(length, 0);
        NetworkId.WriteBuffer(networkId, buffer, 4);
        convertedMessage.copy(buffer, 12);

        var msg = new Message();
        msg.buffer = buffer;
        msg.length = length;
        msg.networkId = networkId;
        msg.message = convertedMessage;

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
