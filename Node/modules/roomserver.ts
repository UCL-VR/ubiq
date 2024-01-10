import { Message, NetworkId, Uuid, type IConnectionWrapper, type IServerWrapper } from 'ubiq'
import { EventEmitter } from 'events'
import { type ValidationError } from 'jsonschema'
import { z } from 'zod'

const RoomServerReservedId = 1

// The following Zod objects define the schema for the RoomSever messages on
// the wire.

const RoomServerMessage = z.object({
    type: z.string(),
    args: z.string()
})

const RoomInfo = z.object({
    uuid: z.string(),
    joincode: z.string(),
    publish: z.boolean(),
    name: z.string(),
    keys: z.array(z.string()),
    values: z.array(z.string())
})

const PeerInfo = z.object({
    uuid: z.string(),
    sceneid: NetworkId.Schema,
    clientid: NetworkId.Schema,
    keys: z.array(z.string()),
    values: z.array(z.string())
})

const JoinArgs = z.object({
    joincode: z.string().optional(),
    uuid: z.string().optional(),
    name: z.string().optional(),
    publish: z.boolean(),
    peer: PeerInfo
})

const PingArgs = z.object({
    clientid: NetworkId.Schema
})

const AppendPeerPropertiesArgs = z.object({
    keys: z.array(z.string()),
    values: z.array(z.string())
})

const AppendRoomPropertiesArgs = z.object({
    keys: z.array(z.string()),
    values: z.array(z.string())
})

const DiscoverRoomArgs = z.object({
    clientid: NetworkId.Schema,
    joincode: z.string()
})

const SetBlobArgs = z.object({
    uuid: z.string(),
    blob: z.string()
})

const GetBlobArgs = z.object({
    clientid: NetworkId.Schema,
    uuid: z.string()
})

// A number of these message types are exported, as they are also used by Js
// RoomClient implementations.

/* eslint-disable @typescript-eslint/no-redeclare */ // The type and object universe are separate in typescript. This is the intended use of zod.

export type RoomInfo = z.infer<typeof RoomInfo>
export type RoomServerMessage = z.infer<typeof RoomServerMessage>
export type PeerInfo = z.infer<typeof PeerInfo>
export type JoinArgs = z.infer<typeof JoinArgs>
export type AppendPeerPropertiesArgs = z.infer<typeof AppendPeerPropertiesArgs>
export type AppendRoomPropertiesArgs = z.infer<typeof AppendRoomPropertiesArgs>

// Next we define a set of convenience functions and classes

// https://stackoverflow.com/questions/1349404/generate-random-string-characters-in-javascript
// Proof of concept - not crypto secure
function JoinCode (): string {
    let result = ''
    const characters = 'abcdefghijklmnopqrstuvwxyz0123456789'
    const charactersLength = characters.length
    for (let i = 0; i < 3; i++) {
        result += characters.charAt(Math.floor(Math.random() * charactersLength))
    }
    return result
}

function arrayRemove<T> (array: T[], element: T): void {
    const index = array.indexOf(element)
    if (index > -1) {
        array.splice(index, 1)
    }
}

interface DictionaryResponse {
    keys: string[]
    values: any[]
}

class PropertyDictionary {
    dict: Record<string, any>
    constructor () {
        this.dict = {}
    }

    append (keys: string | string[], values?: any | any[]): DictionaryResponse {
        const response: DictionaryResponse = {
            keys: [],
            values: []
        }

        if (keys === undefined || values === undefined) {
            return response
        }

        const set = function (key: string, value: any, dict: Record<string, any>): boolean {
            if (value === '') {
                // Attempting to remove
                if (dict.hasOwnProperty(key)) {
                    // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
                    delete dict[key]
                    return true
                }

                return false
            }

            if (dict.hasOwnProperty(key) && dict[key] === value) {
                return false
            }

            dict[key] = value
            return true
        }

        if ((typeof keys === 'string'/* || keys instanceof String */) &&
            (typeof values === 'string' /* || values instanceof String */)) {
            if (set(keys, values, this.dict)) {
                response.keys = [keys]
                response.values = [values]
            }
            return response
        }

        if (!Array.isArray(keys) || !Array.isArray(values)) {
            return response
        }

        // Set for uniqueness - if modified multiple times, last value is used
        const modified = new Set<string>()
        const dict = this.dict
        keys.forEach(function (key, i) {
            if (values.length <= i) {
                return
            }

            const value = values[i]
            if (set(key, value, dict)) {
                modified.add(key)
            }
        })

        response.keys = Array.from(modified)
        response.values = response.keys.map((key) => this.get(key))
        return response
    }

    set (keys: string | string[], values: any | any[]): void {
        this.dict = {}
        this.append(keys, values)
    }

    get (key: string): any | string {
        if (this.dict.hasOwnProperty(key)) {
            return this.dict[key]
        }
        return ''
    }

    keys (): string[] {
        return Object.keys(this.dict)
    }

    values (): any[] {
        return Object.values(this.dict)
    }
}

class RoomDatabase {
    byUuid: Record<string, Room>
    byJoincode: Record<string, Room>
    constructor () {
        this.byUuid = {}
        this.byJoincode = {}
    }

    // Return all room objects in the database
    all (): Room[] {
        return Object.values(this.byUuid)
    }

    // Add room to the database
    add (room: Room): void {
        this.byUuid[room.uuid] = room
        this.byJoincode[room.joincode] = room
    }

    // Remove room from the database by uuid
    remove (uuid: string): void {
        // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
        delete this.byJoincode[this.byUuid[uuid].joincode]
        // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
        delete this.byUuid[uuid]
    }

    // Return room object with given uuid, or null if not present
    uuid (uuid: string): Room | null {
        if (this.byUuid.hasOwnProperty(uuid)) {
            return this.byUuid[uuid]
        }
        return null
    }

    // Return room object with given joincode, or null if not present
    joincode (joincode: string): Room | null {
        if (this.byJoincode.hasOwnProperty(joincode)) {
            return this.byJoincode[joincode]
        }
        return null
    }
}

export class Statistics {
    connections: number
    rooms: number
    roomscreated: number
    messages: number
    bytesIn: number
    bytesOut: number
    time: any

    constructor () {
        this.connections = 0
        this.rooms = 0
        this.roomscreated = 0
        this.messages = 0
        this.bytesIn = 0
        this.bytesOut = 0
        this.time = 0
    }
}

// This is the primary server for rendezvous and bootstrapping. It accepts
// websocket and net connections, (immediately handing them over to RoomPeer
// instances) and performs book-keeping for finding and joining rooms.

export class RoomServer extends EventEmitter {
    roomDatabase: RoomDatabase
    networkId: NetworkId
    servers: IServerWrapper[]
    stats: Statistics
    T: typeof Room
    constructor () {
        super()
        this.roomDatabase = new RoomDatabase()
        this.networkId = new NetworkId(RoomServerReservedId)
        this.stats = new Statistics()
        this.servers = []
        this.T = Room
    }

    addServer (server: IServerWrapper): void {
        if (server.status === 'LISTENING') {
            console.log('Added RoomServer port ' + server.port)
            this.servers.push(server)
            server.onConnection.push(this.onConnection.bind(this))
        }
    }

    onConnection (wrapped: IConnectionWrapper): void {
        console.log('RoomServer: Client Connection from ' + wrapped.endpoint().address + ':' + wrapped.endpoint().port)
        // eslint-disable-next-line no-new
        new RoomPeer(this, wrapped)
    }

    // Expects args from schema ubiq.rooms.joinargs
    join (peer: RoomPeer, args: JoinArgs): void {
        let room = null
        if ((args.uuid != null) && args.uuid !== '') {
            // Room join request by uuid
            if (!Uuid.validate(args.uuid)) {
                console.log(peer.uuid + ' attempted to join room with uuid ' + args.uuid + ' but the we were expecting an RFC4122 v4 uuid.')
                peer.sendRejected(args, 'Could not join room with uuid ' + args.uuid + '. We require an RFC4122 v4 uuid.')
                return
            }

            // Not a problem if no such room exists - we'll create one
            room = this.roomDatabase.uuid(args.uuid)
        } else if ((args.joincode != null) && args.joincode !== '') {
            // Room join request by joincode
            room = this.roomDatabase.joincode(args.joincode)

            if (room === null) {
                console.log(peer.uuid + ' attempted to join room with code ' + args.joincode + ' but no such room exists')
                peer.sendRejected(args, 'Could not join room with code ' + args.joincode + '. No such room exists.')
                return
            }
        }

        if (room !== null && peer.room.uuid === room.uuid) {
            console.log(peer.uuid + ' attempted to join room with code ' + args.joincode + ' but peer is already in room')
            return
        }

        if (room === null) {
            // Otherwise new room requested
            let uuid = ''
            if ((args.uuid != null) && args.uuid !== '') {
                // Use specified uuid
                // we're sure it's correctly formatted and isn't already in db
                uuid = args.uuid
            } else {
                // Create new uuid if none specified
                while (true) {
                    uuid = Uuid.generate()
                    if (this.roomDatabase.uuid(uuid) === null) {
                        break
                    }
                }
            }
            let joincode = ''
            while (true) {
                joincode = JoinCode()
                if (this.roomDatabase.joincode(joincode) === null) {
                    break
                }
            }
            let publish = false
            if (args.publish) {
                publish = args.publish
            }
            let name = uuid
            if ((args.name != null) && args.name.length !== 0) {
                name = args.name
            }
            room = new this.T(this)
            room.uuid = uuid
            room.joincode = joincode
            room.publish = publish
            room.name = name
            this.roomDatabase.add(room)
            this.stats.rooms++
            this.stats.roomscreated++
            this.emit('create', room)

            console.log(room.uuid + ' created with joincode ' + joincode)
        }

        if (peer.room.uuid != null) {
            peer.room.removePeer(peer)
        }
        room.addPeer(peer)
    }

    findOrCreateRoom (args: any): Room {
        let room = this.roomDatabase.uuid(args.uuid)
        if (room === null) {
            let joincode = ''
            while (true) {
                joincode = JoinCode()
                if (this.roomDatabase.joincode(joincode) === null) {
                    break
                }
            }
            const publish = false
            const name = args.uuid
            const uuid = args.uuid
            room = new this.T(this)
            room.uuid = uuid
            room.joincode = joincode
            room.publish = publish
            room.name = name
            this.roomDatabase.add(room)
            this.emit('create', room)

            console.log(room.uuid + ' created with joincode ' + joincode)
        }
        return room
    }

    getRooms (): Room[] {
        return this.roomDatabase.all()
    }

    getRoom (uuid: string): Room | undefined {
        return this.roomDatabase.byUuid[uuid]
    }

    // Return requested rooms for publishable rooms
    // Optionally uses joincode to filter, in which case room need not be publishable
    // Expects args from schema ubiq.rooms.discoverroomargs
    discoverRooms (args: { joincode?: string }): Room[] {
        if ((args.joincode != null) && args.joincode !== '') {
            return this.roomDatabase.all().filter(r => r.joincode === args.joincode)
        } else {
            return this.roomDatabase.all().filter(r => r.publish)
        }
    }

    removeRoom (room: Room): void {
        this.emit('destroy', room)
        this.roomDatabase.remove(room.uuid)
        this.stats.rooms--
        console.log('RoomServer: Deleting empty room ' + room.uuid)
    }

    getStats (): Statistics {
        this.stats.time = (Date.now() * 10000) + 621355968000000000 // This snippet converts Js ticks to .NET ticks making them directly comparable with Ubiq's logging timestamps
        return this.stats
    }

    // eslint-disable-next-line @typescript-eslint/no-invalid-void-type
    async exit (): Promise<void[]> {
        // eslint-disable-next-line @typescript-eslint/return-await
        return Promise.all(
            // eslint-disable-next-line @typescript-eslint/promise-function-async
            this.servers.map(server => server.close())
        )
    }
}

// The RoomPeer class manages a Connection to a RoomClient. This class interacts
// with the connection, formatting and parsing messages and calling the
// appropriate methods on RoomServer and others.

class RoomPeer {
    server: RoomServer
    connection: IConnectionWrapper
    room: Room
    networkSceneId: NetworkId
    roomClientId: NetworkId
    uuid: string
    properties: PropertyDictionary
    sessionId: string
    constructor (server: RoomServer, connection: IConnectionWrapper) {
        this.server = server
        this.server.stats.connections += 1
        this.connection = connection
        this.room = new EmptyRoom()
        this.networkSceneId = new NetworkId({
            a: Math.floor(Math.random() * 2147483648),
            b: Math.floor(Math.random() * 2147483648)
        })
        this.roomClientId = new NetworkId(0)
        this.uuid = ''
        this.properties = new PropertyDictionary()
        this.connection.onMessage.push(this.onMessage.bind(this))
        this.connection.onClose.push(this.onClose.bind(this))
        this.sessionId = Uuid.generate()
    }

    onMessage (message: Message): void {
        this.server.stats.messages += 1
        this.server.stats.bytesIn += message.length
        if (NetworkId.Compare(message.networkId, this.server.networkId)) {
            try {
                const object = RoomServerMessage.parse(message.toObject())
                switch (object.type) {
                    case 'Join':
                        {
                            const args = JoinArgs.parse(JSON.parse(object.args))
                            this.networkSceneId = args.peer.sceneid // Join message always includes peer uuid and object id
                            this.roomClientId = args.peer.clientid
                            this.uuid = args.peer.uuid
                            this.properties.append(args.peer.keys, args.peer.values)
                            this.server.join(this, args)
                        }
                        break
                    case 'AppendPeerProperties':
                        {
                            const args = AppendPeerPropertiesArgs.parse(JSON.parse(object.args))
                            this.appendProperties(args.keys, args.values)
                        }
                        break
                    case 'AppendRoomProperties':
                        {
                            const args = AppendRoomPropertiesArgs.parse(JSON.parse(object.args))
                            this.room.appendProperties(args.keys, args.values)
                        }
                        break
                    case 'DiscoverRooms':
                        {
                            const args = DiscoverRoomArgs.parse(JSON.parse(object.args))
                            this.roomClientId = args.clientid // This message may be received before Join
                            this.sendDiscoveredRooms({
                                rooms: this.server.discoverRooms(args).map(r => r.getRoomArgs()),
                                version: '0.0.4', // This mechanism has been deprecated and will no longer change
                                request: args
                            })
                        }
                        break
                    case 'SetBlob':
                        {
                            const args = SetBlobArgs.parse(JSON.parse(object.args))
                            this.room.setBlob(args.uuid, args.blob)
                        }
                        break
                    case 'GetBlob':
                        {
                            const args = GetBlobArgs.parse(JSON.parse(object.args))
                            this.roomClientId = args.clientid // This message may be received before Join
                            this.sendBlob(args.uuid, this.room.getBlob(args.uuid))
                        }
                        break
                    case 'Ping':
                        {
                            const args = PingArgs.parse(JSON.parse(object.args))
                            this.roomClientId = args.clientid // This message may be received before Join
                            this.sendPing()
                        }
                        break
                    default:
                        console.warn(`Received unknown server message ${object.type}`)
                };
            } catch (e) {
                if (e instanceof z.ZodError) {
                    console.log(`Peer ${this.uuid}: Error in message - ${JSON.stringify(e.issues)}`)
                } else {
                    console.log(`Peer ${this.uuid}: Uknown error in server message`)
                }
            }
        } else {
            this.room.processMessage(this, message) // Message is intended for other peer(s) - this method forwards it to the other members
        }
    }

    onValidationFailure (error: { validation: { errors: ValidationError[] }, json: any }): void {
        error.validation.errors.forEach(error => {
            // eslint-disable-next-line @typescript-eslint/no-base-to-string, @typescript-eslint/restrict-template-expressions
            console.error(`Validation error in ${error.schema}: ${error.message}`)
        })
        console.error('Message Json: ' + JSON.stringify(error.json))
    }

    getPeerArgs (): PeerInfo {
        return {
            uuid: this.uuid,
            sceneid: this.networkSceneId,
            clientid: this.roomClientId,
            keys: this.properties.keys(),
            values: this.properties.values()
        }
    }

    onClose (): void {
        this.room.removePeer(this)
        this.server.stats.connections -= 1
    }

    setRoom (room: Room): void {
        this.room = room
        this.sendSetRoom()
    }

    clearRoom (): void {
        this.setRoom(new EmptyRoom())
    }

    getNetworkId (): NetworkId {
        return this.roomClientId
    }

    appendProperties (keys: string | string[], values: any | any[]): void {
        const modified = this.properties.append(keys, values)
        if (modified.keys.length > 0) {
            this.room.broadcastPeerProperties(this, modified.keys, modified.values)
        }
    }

    sendRejected (joinArgs: JoinArgs, reason: string): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'Rejected',
                args: JSON.stringify({
                    reason,
                    joinArgs
                })
            }))
    }

    sendSetRoom (): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'SetRoom',
                args: JSON.stringify({
                    room: this.room.getRoomArgs()
                })
            }))
    }

    sendDiscoveredRooms (args: any): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'Rooms',
                args: JSON.stringify(args)
            }))
    }

    sendPeerAdded (peer: RoomPeer): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'PeerAdded',
                args: JSON.stringify({
                    peer: peer.getPeerArgs()
                })
            }))
    }

    sendPeerRemoved (peer: RoomPeer): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'PeerRemoved',
                args: JSON.stringify({
                    uuid: peer.uuid
                })
            }))
    }

    sendRoomPropertiesAppended (keys: string[], values: any[]): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'RoomPropertiesAppended',
                args: JSON.stringify({
                    keys,
                    values
                })
            }))
    }

    sendPeerPropertiesAppended (peer: RoomPeer, keys: string[], values: any[]): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'PeerPropertiesAppended',
                args: JSON.stringify({
                    uuid: peer.uuid,
                    keys,
                    values
                })
            }))
    }

    sendBlob (uuid: string, blob: string): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'Blob',
                args: JSON.stringify({
                    uuid,
                    blob
                })
            }))
    }

    sendPing (): void {
        this.send(Message.Create(this.getNetworkId(),
            {
                type: 'Ping',
                args: JSON.stringify({
                    sessionId: this.sessionId
                })
            }))
    }

    send (message: Message): void {
        this.server.stats.bytesOut += message.length
        this.connection.send(message)
    }
}

export class Room {
    server: any
    uuid: string
    name: string
    publish: boolean
    joincode: string
    peers: RoomPeer[]
    properties: PropertyDictionary
    blobs: Record<string, string>
    constructor (server: any) {
        this.server = server
        this.uuid = ''
        this.name = '(Unnamed Room)'
        this.publish = false
        this.joincode = ''
        this.peers = []
        this.properties = new PropertyDictionary()
        this.blobs = {}
    }

    broadcastPeerProperties (peer: RoomPeer, keys: string[], values: any[]): void {
        this.peers.forEach(otherpeer => {
            if (otherpeer !== peer) {
                otherpeer.sendPeerPropertiesAppended(peer, keys, values)
            }
        })
    }

    appendProperties (keys: string | string[], values: any): void {
        const modified = this.properties.append(keys, values)
        this.peers.forEach(peer => {
            peer.sendRoomPropertiesAppended(modified.keys, modified.values)
        })
    }

    addPeer (peer: RoomPeer): void {
        this.peers.push(peer)
        peer.setRoom(this)
        for (const existing of this.peers) { // Tell the Peers about eachother
            if (existing !== peer) {
                existing.sendPeerAdded(peer) // Tell the existing peer that the new Peer has joined
                peer.sendPeerAdded(existing) // And the new Peer about the existing one
            }
        };
        console.log(peer.uuid + ' joined room ' + this.name)
    }

    removePeer (peer: RoomPeer): void {
        arrayRemove(this.peers, peer)
        peer.setRoom(new EmptyRoom()) // signal that the leave is complete
        for (const existing of this.peers) {
            existing.sendPeerRemoved(peer) // Tell the remaining peers about the missing peer (no check here because the peer was already removed from the list)
            peer.sendPeerRemoved(existing)
        }
        console.log(peer.uuid + ' left room ' + this.name)
        this.checkRoom()
    }

    // Every time a peer or observer leaves, check if the room should still exist
    checkRoom (): void {
        if (this.peers.length <= 0) {
            this.server.removeRoom(this)
        }
    }

    setBlob (uuid: string, blob: string): void {
        this.blobs[uuid] = blob
    }

    getBlob (uuid: string): string {
        if (this.blobs.hasOwnProperty(uuid)) {
            return this.blobs[uuid]
        }
        return ''
    }

    getRoomArgs (): RoomInfo {
        return {
            uuid: this.uuid,
            joincode: this.joincode,
            publish: this.publish,
            name: this.name,
            keys: this.properties.keys(),
            values: this.properties.values()
        }
    }

    getPeersArgs (): PeerInfo[] {
        return this.peers.map(c => c.getPeerArgs())
    }

    processMessage (source: RoomPeer, message: Message): void {
        this.peers.forEach(peer => {
            if (peer !== source) {
                peer.send(message)
            }
        })
    }
}

// When peers are not in a room, their room member is set to an instance of
// EmptyRoom, which contains  callbacks and basic information to signal that
// they are not members of any room.

class EmptyRoom extends Room {
    uuid: string
    constructor () {
        super(null)
        this.uuid = ''
    }

    removePeer (peer: RoomPeer): void { }

    addPeer (peer: RoomPeer): void { }

    broadcastPeerProperties (peer: RoomPeer, keys: string[], values: any[]): void { }

    appendProperties (keys: string | string[], values: any): void { }

    processMessage (source: RoomPeer, message: Message): void { }

    getPeersArgs (): PeerInfo[] {
        return []
    }

    getRoomArgs (): RoomInfo {
        return {
            uuid: this.uuid,
            joincode: '',
            publish: false,
            name: '',
            keys: [],
            values: []
        }
    }
}
