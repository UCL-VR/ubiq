const { Message } = require('./messaging');
const WebSockets = require('ws');
const Tcp = require('net');
const { createServer } = require('https');
const path = require('path');
const fs = require('fs');
const { Buffer } = require('buffer');

// All WrappedConnection types implement two callbacks,
// onMessage and onClose, and one function, Send.
// onMessage and send both provide and receive Ubiq Message
// types.

// Use the callbacks like so,
// connection.onMessage.push(this.myOnMessageCallback.bind(this));
// connection.onClose.push(this.myOnCloseCallback.bind(this));


class WrappedSecureWebSocketServer{

    // Opens a new Secure WebSocket Server through an HTTPS instance.
    // The certificate and key should be provided as paths in the config.
    
    constructor(config){
        if(config == undefined){
            return;
        }

        this.onConnection = [];
        
        let certPath = path.resolve(config.cert);
        let keyPath = path.resolve(config.key);

        if(!fs.existsSync(certPath)){
            console.error(`Certificate at ${certPath} could not be found. WebSocket server will not be started.`);
            return;
        }
        if(!fs.existsSync(keyPath)){
            console.error(`Certificate at ${certPath} could not be found. WebSocket server will not be started.`);
            return;
        }

        // Use an https server to present the websocket 
        // (see: https://github.com/websockets/ws#usage-examples)
            
        const server = createServer({
            cert: fs.readFileSync(certPath),
            key: fs.readFileSync(keyPath)
        },
        (req, res) => {
            res.writeHead(200);
            res.end('Welcome to Ubiq! This is a Ubiq WebSocket Server. To use this endpoint, connect to it with a Ubiq Client.\n');
          }
        );
        const wss = new WebSockets.WebSocketServer({ server }); // Take care to use the WebSocketServer member, as the import of ws provides the WebSocket *client* type.
        this.port = config.port;
        server.listen(this.port);

        wss.on("connection", function(ws) {
            this.onConnection.map(callback => callback(new WebSocketConnectionWrapper(ws)));
        }.bind(this));

        this.status = "LISTENING";
    }
}

class WebSocketConnectionWrapper{
    constructor(ws){
        this.onMessage = [];
        this.onClose = [];
        this.state = ws.readyState;
        this.socket = ws;
        this.socket.binaryType = "arraybuffer"; // This has no effect in Node, but correctly configures the event type in the browser
        this.pending = [];

        this.socket.onmessage = function(event){
            let data = event.data;
            if(event.data instanceof ArrayBuffer){
                data = Buffer.from(data);
            }
            this.onMessage.map(callback => callback(Message.Wrap(data)));
        }.bind(this);

        this.socket.onclose = function(event){
            this.state = WebSocketConnectionWrapper.CLOSED;
            this.onClose.map(callback => callback());
        }.bind(this);

        this.socket.onopen = function(event){
            this.state = WebSocketConnectionWrapper.OPEN;
            this.pending.forEach(element => {
                this.socket.send(element);
            });
            this.pending = [];
        }.bind(this);
    }

    static CONNECTING = 0;
    static OPEN = 1;
    static CLOSED = 2;

    send(message){
        if(this.state == WebSocketConnectionWrapper.OPEN){
            this.socket.send(message.buffer);
        }else if(this.state == WebSocketConnectionWrapper.CONNECTING){
            this.pending.push(message.buffer);
        }
    }

    endpoint(){
        return {
            address: this.socket._socket.remoteAddress,
            port: this.socket._socket.remotePort
        };
    }
}

class WrappedTcpServer{
    constructor(config){
        if(config == undefined){
            return;
        }
        this.onConnection = [];
        this.port = config.port;
        var server = Tcp.createServer(); 
        server.on('connection', function(s){
            this.onConnection.map(callback => callback(new TcpConnectionWrapper(s)));
        }.bind(this));
        server.listen(this.port);
        this.status = "LISTENING";
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
        this.closed = false;
        
        this.socket.on("data", 
            this.onData.bind(this));
        
        this.socket.on("close", function(event){
            if(!this.closed){
                this.onClose.map(callback => callback());
                this.closed = true;
            }
        }.bind(this));

        this.socket.on("error", function(event){
            if(!this.closed){
                this.onClose.map(callback => callback());
                this.closed = true;
            }
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
    WrappedSecureWebSocketServer,
    TcpConnectionWrapper,
    WrappedTcpServer,
    UbiqTcpConnection
}