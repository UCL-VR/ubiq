const { Message } = require('./messaging');
const WebSocket = require('ws');
const Tcp = require('net');
const { networkInterfaces } = require('os');

// All WrappedConnection types implement two callbacks,
// onMessage and onClose, and one function, Send.
// onMessage and send both provide and receive Ubiq Message
// types.

// Use the callbacks like so,
// connection.onMessage.push(this.myOnMessageCallback.bind(this));
// connection.onClose.push(this.myOnCloseCallback.bind(this));


class WrappedWebSocketServer{
    constructor(port){
        this.onConnection = [];
        this.port = port;
        var wss = new WebSocket.Server({ port: port });
        wss.on("connection", function(ws) {
            this.onConnection.map(callback => callback(new WebSocketConnectionWrapper(ws)));
        }.bind(this));
    }
}

class WebSocketConnectionWrapper{
    constructor(ws){
        this.onMessage = [] 
        this.onClose = []
        
        this.socket = ws;

        this.socket.on("message", function(data){
            this.onMessage.map(callback => callback(Message.Wrap(data)));
        }.bind(this));

        this.socket.on("close", function(event){
            this.onClose.map(callback => callback());
        }.bind(this));
    }

    send(message){
        this.socket.send(message.buffer);
    }

    endpoint(){
        return {
            address: this.socket._socket.remoteAddress,
            port: this.socket._socket.remotePort
        };
    }
}

class WrappedTcpServer{
    constructor(port){
        this.onConnection = [];
        this.port = port;
        var server = Tcp.createServer(); 
        server.on('connection', function(s){
            this.onConnection.map(callback => callback(new TcpConnectionWrapper(s)));
        }.bind(this));
        server.listen(port);
    }
}

class TcpConnectionWrapper{
    constructor(s){
        this.onMessage = []
        this.onClose = []
        this.socket = s;
        this.headersize = 4;
        this.header = Buffer.alloc(this.headersize);
        this.header.read = 0;
        this.data = null;
        
        this.socket.on("data", 
            this.onData.bind(this));
        
        this.socket.on("close", function(event){
            this.onClose.map(callback => callback());
        }.bind(this));

        this.socket.on("error", function(evet){
            this.onClose.map(callback => callback());
        }.bind(this));
    }

    onData(array){
        var fragment = Buffer.from(array.buffer, array.byteOffset, array.byteLength);
        var offset = 0;
        var available = fragment.length - offset;

        while(available > 0){ // we could have multiple messages packed into one fragment

            if(this.header.read < this.header.length){
                var remaining = this.header.length - this.header.read;
                var toread = Math.min(remaining, available);
                var read = fragment.copy(this.header, this.header.read, offset, offset + toread);
                this.header.read += read;
                offset += read;
                available = fragment.length - offset;

                // we have just received the complete header
                if(this.header.read == this.header.length)
                {
                    var length = this.header.readInt32LE(0);
                    this.data = Buffer.alloc(length + this.headersize);
                    this.data.read = this.header.copy(this.data, 0, 0, this.headersize); // Message Wrapper expects the header.
                }
            }

            if(this.data != null){
                if(this.data.read < this.data.length){
                    var remaining = this.data.length - this.data.read;
                    var toread = Math.min(remaining, available);
                    var read = fragment.copy(this.data, this.data.read, offset, offset + toread);
                    this.data.read += read;
                    offset += read;
                    available = fragment.length - offset;

                    // the message is complete
                    if(this.data.read == this.data.length){
                        this.onMessage.map(callback => callback(Message.Wrap(this.data)));
                        this.data = null;
                        this.header.read = 0;
                    }
                }
            }
        }
    }

    send(message){
        this.socket.write(message.buffer);
    }

    endpoint(){
        return {
            address: this.socket.remoteAddress,
            port: this.socket.remotePort
        }
    }
}

// Creates a new Ubiq Messaging Connection over TCP
function UbiqTcpConnection(uri,port){
    var client = new Tcp.Socket();
    client.connect(port, uri);
    return new TcpConnectionWrapper(client);
}

module.exports = {
    WebSocketConnectionWrapper,
    WrappedWebSocketServer,
    TcpConnectionWrapper,
    WrappedTcpServer,
    UbiqTcpConnection
}