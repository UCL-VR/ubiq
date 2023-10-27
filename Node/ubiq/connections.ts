import { Message } from './messaging.js';
import WebSocket, { WebSocketServer } from 'ws';
import Tcp from 'net';
import https from 'https';
import path from 'path';
import fs from 'fs';

const createServer = https.createServer;

// All WrappedConnection types implement two callbacks,
// onMessage and onClose, and one function, Send.
// onMessage and Send both provide and receive Ubiq Message
// types.

// Use the callbacks like so,
// connection.onMessage.push(this.myOnMessageCallback.bind(this));
// connection.onClose.push(this.myOnCloseCallback.bind(this));

interface IWrappedSecureWebSocketServer {
    onConnection: any[];
    status: string;
    port: number;
}

interface WebSocketConfig {
    port: number;
}

interface SecureWebSocketConfig extends WebSocketConfig {
    cert: string;
    key: string;
}

export class WrappedSecureWebSocketServer implements IWrappedSecureWebSocketServer{

    // Opens a new Secure WebSocket Server through an HTTPS instance.
    // The certificate and key should be provided as paths in the config.
    
    onConnection: any[] = [];
    status: string = "";
    port: number = -1;
    constructor(config : SecureWebSocketConfig){
        if(config == undefined){
            return;
        }
        const self = this;
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
        const wss = new WebSocketServer({ server }); // Take care to use the WebSocketServer member, as the import of ws provides the WebSocket *client* type.
        this.port = config.port;
        server.listen(this.port);

        wss.on("connection", function(ws : WebSocket) {
            self.onConnection.map(callback => callback(new WebSocketConnectionWrapper(ws)));
        });

        this.status = "LISTENING";
    }
}

export interface ConnectionWrapper {
    onMessage: any[]
    onClose: any[]
    send(message : Message): void
    endpoint(): string | {address?: string, port?: number}
    close(): void
}

export class WebSocketConnectionWrapper implements ConnectionWrapper{

    onMessage: any[]
    onClose: any[]
    state: number
    socket: WebSocket
    pending: any[]
    constructor(ws: WebSocket){
        this.onMessage = [];
        this.onClose = [];
        this.state = ws.readyState;
        this.socket = ws;
        this.socket.binaryType = "arraybuffer"; // This has no effect in Node, but correctly configures the event type in the browser
        this.pending = [];

        const self = this;
        this.socket.on("message", function(event){
            let data : WebSocket.RawData = event;
            if(data instanceof ArrayBuffer){
                data = Buffer.from(data);
            }
            if (Array.isArray(data)) {
                data = Buffer.concat(data);
            }

            // We can safely assume that our data is of type Buffer now.
            self.onMessage.map(callback => callback(Message.Wrap(data as Buffer)));
        });

        this.socket.on('close', function(event){
            self.state = WebSocketConnectionWrapper.CLOSED;
            self.onClose.map(callback => callback());
        })

        this.socket.on('open', function(event : any){
            self.state = WebSocketConnectionWrapper.OPEN;
            self.pending.forEach(element => {
                self.socket.send(element);
            });
            self.pending = [];
        })
    }

    static CONNECTING = 0;
    static OPEN = 1;
    static CLOSED = 2;

    send(message : Message){
        if(this.state == WebSocketConnectionWrapper.OPEN){
            this.socket.send(message.buffer);
        }else if(this.state == WebSocketConnectionWrapper.CONNECTING){
            this.pending.push(message.buffer);
        }
    }

    endpoint(){
        return this.socket.url
    }

    close(){
        this.socket.close();
    }
}

export class WrappedTcpServer{
    onConnection: any[] = []
    port: number = -1
    status: string = ""
    constructor(config : WebSocketConfig){
        if(config == undefined){
            return;
        }
        const self = this;
        this.onConnection = [];
        this.port = config.port;
        var server = Tcp.createServer(); 
        server.on('connection', function(s){
            self.onConnection.map(callback => callback(new TcpConnectionWrapper(s)));
        });
        server.listen(this.port);
        this.status = "LISTENING";
    }
}

interface ITcpConnectionWrapper extends ConnectionWrapper{
    socket: any
    headersize: number
    header: Buffer
    data: any
    closed: boolean
}

export class TcpConnectionWrapper implements ITcpConnectionWrapper{
    onMessage: any[];
    onClose: any[];
    socket: Tcp.Socket;
    headersize: number;
    bufferRead: number;
    header: Buffer;
    data: any;
    closed: boolean;
    constructor(s: Tcp.Socket){
        this.onMessage = []
        this.onClose = []
        this.socket = s;
        this.headersize = 4;
        this.header = Buffer.alloc(this.headersize);
        this.bufferRead = 0;
        this.data = null;
        this.closed = false;
        const self = this;
        this.socket.on("data", 
            self.onData.bind(this));
        
        this.socket.on("close", function(event){
            if(!self.closed){
                self.onClose.map(callback => callback());
                self.closed = true;
            }
        });
        this.socket.on("error", function(event){
            if(!self.closed){
                self.onClose.map(callback => callback());
                self.closed = true;
            }
        });
    }

    onData(array: Buffer){
        var fragment = Buffer.from(array.buffer, array.byteOffset, array.byteLength);
        var offset = 0;
        var available = fragment.length - offset;

        while(available > 0){ // we could have multiple messages packed into one fragment

            if(this.bufferRead < this.header.length){
                var remaining = this.header.length - this.bufferRead;
                var toread = Math.min(remaining, available);
                var read = fragment.copy(this.header, this.bufferRead, offset, offset + toread);
                this.bufferRead += read;
                offset += read;
                available = fragment.length - offset;

                // we have just received the complete header
                if(this.bufferRead == this.header.length)
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
                        this.bufferRead = 0;
                    }
                }
            }
        }
    }

    send(message : Message){
        this.socket.write(message.buffer);
    }

    endpoint(){
        return {
            address: this.socket.remoteAddress,
            port: this.socket.remotePort
        }
    }

    close(){
        this.socket.end();
    }
}

// Creates a new Ubiq Messaging Connection over TCP
export function UbiqTcpConnection(uri: string,port: number){
    var client = new Tcp.Socket();
    client.connect(port, uri);
    return new TcpConnectionWrapper(client);
}
