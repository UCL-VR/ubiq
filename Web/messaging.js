export class Message{
    constructor(){
    }

    static Wrap(data){
        var msg = new Message();
        msg.buffer = data;
        var view = new DataView(msg.buffer);
        msg.length = view.getInt32(0, true);
        msg.objectid = view.getInt32(4, true);
        msg.componentid = view.getInt32(8, true);
        msg.message = data.slice(12);
        return msg;
    }

    static Create(object, component, message){
        var msg = new Message();

        if(typeof(message) == 'object'){
            message = JSON.stringify(message);
        }
        if(typeof(message) == 'string'){
            message = new TextEncoder().encode(message);
        }

        var length = message.length + 8;
        var buffer = new ArrayBuffer(length + 4);

        var view = new DataView(buffer);
        view.setInt32(0, length, true);
        view.setInt32(4, object, true);
        view.setInt32(8, component, true);
        new Uint8Array(buffer).set(message, 12);

        var msg = new Message();
        msg.buffer = buffer;
        msg.length = length;
        msg.componentid = component;
        msg.objectid = object;
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