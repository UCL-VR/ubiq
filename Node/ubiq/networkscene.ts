import { type IConnectionWrapper } from './connections.js'
import { type LooseNetworkId, Message, NetworkId, type NetworkIdObject } from './messaging.js'
import { EventEmitter } from 'events'
import { type Buffer } from 'buffer' // This import is needed for rollup to polyfill Buffer

export interface INetworkContext {
    scene: any
    object: any
}

export interface INetworkComponent {
    networkId: NetworkId
    processMessage: (message: Message) => void
}

export class NetworkContext implements INetworkContext {
    scene: NetworkScene
    object: any
    networkId: NetworkId
    constructor (scene: NetworkScene, object: INetworkComponent, networkId: NetworkId) {
        this.object = object // The Networked Component that this context belongs to
        this.scene = scene // The NetworkScene that this context belongs to
        this.networkId = networkId // The NetworkId that is associated with the object by this entry
    }

    // Send a message to another Ubiq Component. This method will work out the
    // Network Id to send to based on the arguments.
    // The arguments can be a,
    //  NetworkId
    //  Number
    //  Object
    //  Networked Component (object with an networkId property)
    // The last argument must always be the message to send, and this must be a,
    //  JavaScript Object
    //  String
    //  Buffer.
    // You can call send in the following ways,
    //  this.context.send(this, message);
    //  this.context.send({networkId: id}, message);
    //  this.context.send(networkId, message);
    send (..._args: any): void {
        if (arguments.length === 0) {
            throw new Error('Send must have at least one argument')
        } else if (arguments.length === 1) {
            this.scene.send(this.networkId, arguments[0])
        } else {
            this.scene.send(arguments[0], arguments[1])
        }
    }
}

export class NetworkComponent implements INetworkComponent {
    networkId: NetworkId
    constructor () {
        this.networkId = NetworkId.Unique()
    }

    processMessage (_message: Message): void {
        throw new Error('Method not implemented.')
    }
}

interface INetworkSceneEntry {
    object: INetworkComponent
    networkId: NetworkId
}

// A NetworkScene object provides the interface between a Connection and the
// Networked Components in the application.
export class NetworkScene extends EventEmitter {
    connections: IConnectionWrapper[]
    entries: INetworkSceneEntry[]
    networkId: NetworkId
    constructor () {
        super()
        this.networkId = NetworkId.Unique()
        this.entries = []
        this.connections = []
    }

    // The Connection is expected to be a wrapped connection
    addConnection (connection: IConnectionWrapper): void {
        this.connections.push(connection)
        connection.onMessage.push(this.#onMessage.bind(this))
        connection.onClose.push(this.#onClose.bind(this, connection))
    }

    #onMessage (message: Message): void {
        this.entries.forEach(entry => {
            if (NetworkId.Compare(entry.networkId, message.networkId)) {
                // At this point we can s
                entry.object.processMessage(message)
            }
        })
        this.emit('OnMessage', message)
    }

    #onClose (connection: IConnectionWrapper): void {
        const index = this.connections.indexOf(connection)
        if (index > -1) {
            this.connections.slice(index, 1)
        }
    }

    send (looseNetworkId: LooseNetworkId, message: object | string | Buffer): void {
        let networkId: NetworkId | string = NetworkId.Unique()
        // Try to infer the Network Id format
        if (Object.getPrototypeOf(looseNetworkId).constructor.name === 'NetworkId') {
            networkId = looseNetworkId as NetworkId
        } else if (typeof (looseNetworkId) === 'number') {
            networkId = new NetworkId(looseNetworkId)
        } else if (typeof (looseNetworkId) === 'object' && looseNetworkId.hasOwnProperty('a') && looseNetworkId.hasOwnProperty('b')) {
            networkId = new NetworkId(looseNetworkId)
        } else if (typeof (looseNetworkId) === 'object' && looseNetworkId.hasOwnProperty('networkId')) {
            networkId = new NetworkId((looseNetworkId as NetworkIdObject).networkId)
        } else if (typeof (looseNetworkId) === 'string') {
            networkId = looseNetworkId
        }
        // Message.Create will determine the correct encoding of the message
        const buffer = Message.Create(networkId, message)

        this.connections.forEach(connection => {
            connection.send(buffer)
        })
    }

    // Registers a Networked Component so that it will recieve messages addressed
    // to its specific NetworkId via its processMessage method.
    // If a NetworkId is not specified, it is found from the networkId member.
    register (..._args: any): NetworkContext {
        const entry: INetworkSceneEntry = {
            object: arguments[0],
            networkId: NetworkId.Unique()
        }
        if (arguments.length === 2) {
            // The user is trying to register with a specific Id
            entry.networkId = arguments[1]
        } else if (arguments.length === 1) {
            // The user is trying to register with the 'networkId' member
            if (!entry.object.hasOwnProperty('networkId')) {
                throw new Error('Component does not have a networkId Property')
            }
            if (!(entry.object.networkId instanceof NetworkId)) {
                throw new Error("Component's networkId member must be of the type NetworkId")
            }
            entry.networkId = entry.object.networkId
        }

        // This check mainly exists for the browser, as users may write Js
        // ignoring the types when using the browser library.
        if (entry.object.processMessage === undefined) {
            throw new Error('Component does not have a processMessage method')
        }

        this.entries.push(entry)

        return new NetworkContext(this, entry.object, entry.networkId)
    }

    unregister (component: INetworkComponent): void {
        const i = this.entries.findIndex(entry => {
            return entry.object === component
        })
        if (i > -1) {
            this.entries.splice(i, 1)
        }
    }

    getComponent (name: string): INetworkComponent | undefined {
        return this.entries.find(entry => {
            return entry.object.constructor.name === name
        })?.object
    }
}
