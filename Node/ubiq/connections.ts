import { Message } from './messaging.js'
import { Buffer } from 'buffer' // This import is needed for rollup to polyfill Buffer
import { type WebSocket, type MessageEvent, WebSocketServer } from 'ws'
import Tcp from 'net'
import https from 'https'
import path from 'path'
import fs from 'fs'

const createServer = https.createServer

// All WrappedConnection types implement two callbacks,
// onMessage and onClose, and one function, Send.
// onMessage and Send both provide and receive Ubiq Message
// types.

// Use the callbacks like so,
// connection.onMessage.push(this.myOnMessageCallback.bind(this));
// connection.onClose.push(this.myOnCloseCallback.bind(this));

export interface IServerWrapper {
    onConnection: any[]
    status: string
    port: number
    close: () => Promise<void>
}

export interface Endpoint {
    address: string
    port: number
}

interface SocketConfig {
    port: number
}

interface SecureWebSocketConfig extends SocketConfig {
    cert: string
    key: string
}

export class WrappedSecureWebSocketServer implements IServerWrapper {
    // Opens a new Secure WebSocket Server through an HTTPS instance.
    // The certificate and key should be provided as paths in the config.

    onConnection: any[] = []
    status: string = ''
    port: number = -1
    server: https.Server | undefined
    constructor (config: SecureWebSocketConfig) {
        if (config === undefined) {
            console.error('SecureWebSocketConfig must be provided.')
            return
        }
        // eslint-disable-next-line @typescript-eslint/no-this-alias
        const self = this
        this.onConnection = []

        const certPath = path.resolve(config.cert)
        const keyPath = path.resolve(config.key)

        if (!fs.existsSync(certPath)) {
            console.error(`Certificate at ${certPath} could not be found. WebSocket server will not be started.`)
            return
        }
        if (!fs.existsSync(keyPath)) {
            console.error(`Certificate at ${certPath} could not be found. WebSocket server will not be started.`)
            return
        }

        // Use an https server to present the websocket
        // (see: https://github.com/websockets/ws#usage-examples)

        const server = createServer({
            cert: fs.readFileSync(certPath),
            key: fs.readFileSync(keyPath)
        },
        (req, res) => {
            res.writeHead(200)
            res.end('Welcome to Ubiq! This is a Ubiq WebSocket Server. To use this endpoint, connect to it with a Ubiq Client.\n')
        }
        )
        const wss = new WebSocketServer({ server }) // Take care to use the WebSocketServer member, as the import of ws provides the WebSocket *client* type.
        this.port = config.port
        server.listen(this.port)

        wss.on('connection', function (ws: WebSocket) {
            self.onConnection.map(callback => callback(new WebSocketConnectionWrapper(ws)))
        })

        this.server = server
        this.status = 'LISTENING'
    }

    async close (): Promise<void> {
        await new Promise<void>((resolve) => {
            if (this.server != null) {
                this.server.closeAllConnections()
                this.server.close((error) => {
                    if (error != null) {
                        console.error(error) // Not much else we can do since the server is going away!
                    }
                    resolve()
                })
            } else {
                resolve()
            }
        })
    }
}

export interface IConnectionWrapper {
    onMessage: any[]
    onClose: any[]
    send: (message: Message) => void
    endpoint: () => Endpoint
    close: () => void
}

export class WebSocketConnectionWrapper implements IConnectionWrapper {
    onMessage: any[]
    onClose: any[]
    state: number
    socket: WebSocket
    pending: any[]
    constructor (ws: WebSocket) {
        this.onMessage = []
        this.onClose = []
        this.state = ws.readyState
        this.socket = ws
        this.socket.binaryType = 'arraybuffer' // This has no effect in Node, but correctly configures the event type in the browser
        this.pending = []

        // eslint-disable-next-line @typescript-eslint/no-this-alias
        const self = this
        this.socket.onmessage = function (event: MessageEvent) {
            const data = event.data
            if (data instanceof ArrayBuffer) {
                self.onMessage.map(callback => callback(Message.Wrap(Buffer.from(data))))
            } else if (data instanceof Buffer && Array.isArray(data)) {
                self.onMessage.map(callback => callback(Message.Wrap(Buffer.concat(data))))
            } else if (data instanceof Buffer) {
                self.onMessage.map(callback => callback(Message.Wrap(data)))
            }
        }

        this.socket.onclose = function (event) {
            self.state = WebSocketConnectionWrapper.CLOSED
            self.onClose.map(callback => callback())
        }

        this.socket.onopen = function (event: any) {
            self.state = WebSocketConnectionWrapper.OPEN
            self.pending.forEach(element => {
                self.socket.send(element)
            })
            self.pending = []
        }
    }

    static CONNECTING = 0
    static OPEN = 1
    static CLOSED = 2

    send (message: Message): void {
        if (this.state === WebSocketConnectionWrapper.OPEN) {
            this.socket.send(message.buffer)
        } else if (this.state === WebSocketConnectionWrapper.CONNECTING) {
            this.pending.push(message.buffer)
        }
    }

    endpoint (): Endpoint {
        const socket = (this.socket as any)
        return {
            address: socket._socket.remoteAddress, // This only works in Node
            port: socket._socket.remotePort
        }
    }

    close (): void {
        this.socket.close()
    }
}

export class WrappedTcpServer implements IServerWrapper {
    onConnection: any[] = []
    port: number = -1
    status: string = ''
    server: Tcp.Server
    constructor (config: SocketConfig) {
        if (config === undefined) {
            console.error('SocketConfig must be defined')
        }
        // eslint-disable-next-line @typescript-eslint/no-this-alias
        const self = this
        this.onConnection = []
        this.port = config.port
        const server = Tcp.createServer()
        server.on('connection', function (s) {
            self.onConnection.map(callback => callback(new TcpConnectionWrapper(s)))
        })
        server.listen(this.port)
        this.status = 'LISTENING'
        this.server = server
    }

    async close (): Promise<void> {
        await new Promise<void>((resolve) => {
            this.server.close((error) => {
                if (error != null) {
                    console.error(error) // Not much else we can do since the server is going away!
                }
                resolve()
            })
        })
    }
}

export class TcpConnectionWrapper implements IConnectionWrapper {
    onMessage: any[]
    onClose: any[]
    socket: Tcp.Socket
    headersize: number
    bufferRead: number
    header: Buffer
    data: any
    closed: boolean
    constructor (s: Tcp.Socket) {
        this.onMessage = []
        this.onClose = []
        this.socket = s
        this.headersize = 4
        this.header = Buffer.alloc(this.headersize)
        this.bufferRead = 0
        this.data = null
        this.closed = false
        // eslint-disable-next-line @typescript-eslint/no-this-alias
        const self = this

        this.socket.on('data', self.onData.bind(this))

        this.socket.on('close', function (event) {
            if (!self.closed) {
                self.onClose.map(callback => callback())
                self.closed = true
            }
        })
        this.socket.on('error', function (event) {
            if (!self.closed) {
                self.onClose.map(callback => callback())
                self.closed = true
            }
        })
    }

    onData (array: Buffer): void {
        if (this.closed) {
            return // We shouldn't be receiving anything if the connection is closed, but if for whatever reason we do, don't try to process them
        }

        const fragment = Buffer.from(array.buffer, array.byteOffset, array.byteLength)
        let offset = 0
        let available = fragment.length - offset

        while (available > 0) { // we could have multiple messages packed into one fragment
            if (this.bufferRead < this.header.length) {
                const remaining = this.header.length - this.bufferRead
                const toread = Math.min(remaining, available)
                const read = fragment.copy(this.header, this.bufferRead, offset, offset + toread)
                this.bufferRead += read
                offset += read
                available = fragment.length - offset

                // we have just received the complete header
                if (this.bufferRead === this.header.length) {
                    const length = this.header.readInt32LE(0)
                    // Sanity check the length.
                    // If a packet greater than 100 Mb is attempted it is probably not a valid client connection.
                    // Since we call Message.Wrap in this method, the size must be at least the minimum Message
                    // size (12) to be a valid message.
                    if (length < 12 || length > 100 * 1024 * 1024) {
                        console.log(`Received message with expected length of ${length}. Force closing connection.`)
                        this.onClose.map(callback => callback())
                        this.close()
                        this.closed = true
                        return
                    }
                    this.data = Buffer.alloc(length + this.headersize)
                    this.data.read = this.header.copy(this.data, 0, 0, this.headersize) // Message Wrapper expects the header.
                }
            }

            if (this.data != null) {
                if (this.data.read < this.data.length) {
                    const remaining = this.data.length - this.data.read
                    const toread = Math.min(remaining, available)
                    const read = fragment.copy(this.data, this.data.read, offset, offset + toread)
                    this.data.read += read
                    offset += read
                    available = fragment.length - offset

                    // the message is complete
                    if (this.data.read === this.data.length) {
                        this.onMessage.map(callback => callback(Message.Wrap(this.data)))
                        this.data = null
                        this.bufferRead = 0
                    }
                }
            }
        }
    }

    send (message: Message): void {
        this.socket.write(message.buffer)
    }

    endpoint (): Endpoint {
        return {
            address: this.socket.remoteAddress ?? 'uknown',
            port: this.socket.remotePort ?? -1
        }
    }

    close (): void {
        this.socket.end()
    }
}

// Creates a new Ubiq Messaging Connection over TCP
export function UbiqTcpConnection (uri: string, port: number): TcpConnectionWrapper {
    const client = new Tcp.Socket()
    client.connect(port, uri)
    return new TcpConnectionWrapper(client)
}
