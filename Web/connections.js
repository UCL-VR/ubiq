import { Message } from "/messaging.js"

// Exchanges messages with the UCL VR websocket counterpart
// On the native side, the amount of data needs to be known in advance for allocation, so each message is prefixed with the size
export class WebSocketConnection
{
    constructor(uri){
        this.socket = new WebSocket(uri);
        this.socket.binaryType = 'arraybuffer'; // tells the socket we want binary data as a blob rather than an arraybuffer
        this.socket.onmessage = async (event) => {
            this.onmessage.forEach(callback => callback(Message.Wrap(event.data)));
        }
        this.onmessage = new Array();
    }

    send(message){
        if(this.socket.readyState === 1){
            this.socket.send(message.buffer);
        }
        else{
            this.socket.onopen = function(event) {
                this.socket.send(message.buffer);
            }.bind(this);
        }
    }
}