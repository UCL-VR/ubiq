import { EventEmitter } from 'events'
import { Stream, type Readable, type Writable } from 'stream'
import { NetworkId, type INetworkComponent, type Message, type NetworkScene } from 'ubiq'
import { type RoomClient } from './roomclient'
import { Buffer } from 'buffer' // This import is needed for rollup to polyfill Buffer

class LogCollectorMessage {
    type: number
    tag: number
    data: Buffer

    constructor (message: Message) {
        const buffer = message.toBuffer()
        this.type = buffer[0]
        this.tag = buffer[1]
        this.data = buffer.slice(2)
    }

    static Create (type: number, content: object | string | Buffer): Buffer {
        if (typeof (content) === 'object') {
            content = JSON.stringify(content)
        }
        if (typeof (content) === 'string') {
            const contentBuffer = Buffer.from(content, 'utf8')
            const messageBuffer = Buffer.alloc(contentBuffer.length + 2)
            messageBuffer[0] = type
            messageBuffer[1] = 0x0
            contentBuffer.copy(messageBuffer, 2)
            return messageBuffer
        }
        throw new Error('Unsupported content')
    }

    toString (): string {
        return new TextDecoder().decode(this.data)
    }

    fromJson (): object {
        return JSON.parse(this.toString())
    }
}

interface CC {
    clock: number
    state: NetworkId
}

interface PingRequest { // This is called the PingMessage in C# (that should be changed)
    Source: NetworkId
    Responder: NetworkId
    Token: string
    Written: number
    Aborted: boolean
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
export class LogCollector extends EventEmitter implements INetworkComponent {
    scene: NetworkScene
    networkId: NetworkId
    broadcastId: NetworkId
    destinationId: NetworkId
    clock: number
    lock: boolean
    count: number
    _eventStream: Readable
    _forwardingStream: Writable
    _writingStream: Writable
    roomClient: RoomClient | undefined

    constructor (scene: NetworkScene) {
        super()
        this.scene = scene
        this.networkId = NetworkId.Create(scene.networkId, 'LogCollector')
        this.broadcastId = new NetworkId('685a5b84-fec057d0')
        this.destinationId = NetworkId.Null
        this.clock = 0
        this.count = 0

        // When set, this LogCollector will always try to maintain its position as the Primary collector.
        this.lock = false

        // The LogCollector Component Js implementation is based on Streams.

        // All incoming events from the network or local emitters go into the
        // eventStream. This can then cache, or be piped to a local or remote destination.
        this._eventStream = new Stream.Readable({
            objectMode: true,
            read () {
            }
        })

        // eslint-disable-next-line @typescript-eslint/no-this-alias
        const collector = this

        // A stream that forwards log events to the LogCollector specified by destinationId
        // The eventStream should be piped to this when destinationId points to another LogCollector
        // in the Peer Group.
        this._forwardingStream = new Stream.Writable(
            {
                objectMode: true,
                write: (msg, _, done) => {
                    collector.scene.send(collector.destinationId, msg)
                    done()
                }
            }
        )

        // The stream that generates the events to be passed outside the LogCollector.
        this._writingStream = new Stream.Writable(
            {
                objectMode: true,
                write: (msg, _, done) => {
                    const eventMessage = new LogCollectorMessage(msg)
                    this.emit('OnLogMessage',
                        eventMessage.tag,
                        eventMessage.fromJson()
                    )
                    collector.count = collector.count + 1
                    done()
                }
            }
        )

        scene.register(this, this.networkId)
        scene.register(this, this.broadcastId)
        this.registerRoomClientEvents()
    }

    written (): number {
        return this.count
    }

    registerRoomClientEvents (): void {
        this.roomClient = this.scene.getComponent('RoomClient') as RoomClient
        if (this.roomClient === undefined) {
            throw new Error('RoomClient must be added to the scene before LogCollector')
        }
        this.roomClient.addListener('OnPeerAdded', () => {
            if (this.isPrimary()) {
                this.startCollection()
            }
        })
    }

    isPrimary (): boolean {
        return NetworkId.Compare(this.destinationId, this.networkId)
    }

    // Sets this LogCollector as the Primary Collector, receiving all events from the Peer Group and writing them to the provided Stream.
    startCollection (): void {
        this.sendSnapshot(this.networkId)
    }

    // Unsets this LogCollector as the Primary Collector and stops writing to the stream.
    stopCollection (): void {
        if (this.isPrimary()) {
            this.sendSnapshot(NetworkId.Null)
        }
    }

    // Locks this LogCollector as the Primary collector.
    lockCollection (): void {
        this.lock = true
        this.startCollection()
    }

    sendSnapshot (destinationId: NetworkId): void {
        this.destinationId = destinationId
        this.clock++
        this.destinationChanged()
        if (this.roomClient !== undefined) {
            // eslint-disable-next-line @typescript-eslint/no-unused-vars
            for (const _peer of this.roomClient.getPeers()) {
                this.scene.send(this.broadcastId, LogCollectorMessage.Create(0x1, { clock: this.clock, state: destinationId }))
            };
        }
    }

    processMessage (msg: Message): void {
        const message = new LogCollectorMessage(msg)
        switch (message.type) {
            case 0x1: // Command
                {
                    const cc = message.fromJson() as CC
                    if (this.lock) {
                        this.clock = cc.clock // In locked mode, any attempts to change the state externally are immediately counteracted
                    }
                    if (cc.clock > this.clock) {
                        this.clock = cc.clock
                        this.destinationId = cc.state
                        this.destinationChanged()
                    } else {
                        if (cc.clock === this.clock && this.isPrimary()) {
                            this.clock += Math.floor(Math.random() * 10)
                            this.sendSnapshot(this.networkId)
                        }
                    }
                }
                break
            case 0x2: // Event
                this._eventStream.push(msg)
                break
            case 0x3: // Ping
                {
                    const ping = message.fromJson() as PingRequest
                    if (NetworkId.Valid(ping.Responder)) {
                        // The ping is a respones to our request (we don't send requests at the moment so there is nothing to do here...)
                    } else {
                        // The ping is a request
                        if (this.isPrimary()) {
                            ping.Responder = this.networkId
                            ping.Written = this.written()
                            this.scene.send(ping.Source, LogCollectorMessage.Create(0x3, ping))
                        } else if (NetworkId.Valid(this.destinationId)) {
                            this._eventStream.push(msg)
                        } else {
                            ping.Responder = this.networkId
                            ping.Aborted = true
                            this.scene.send(ping.Source, LogCollectorMessage.Create(0x3, ping))
                        }
                    }
                }
                break
        }
    }

    destinationChanged (): void {
        this._eventStream.unpipe()
        if (NetworkId.Valid(this.destinationId)) {
            if (this.isPrimary()) {
                this._eventStream.pipe(this._writingStream)
            } else {
                this._eventStream.pipe(this._forwardingStream)
            }
        }
    }
}
