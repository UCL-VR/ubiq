import crypto from 'crypto'
import { type Room, type RoomServer } from './roomserver'

interface IceServer {
    uri: string
    username: string
    password: string
    secret?: string
    timeoutSeconds: number
    refreshSeconds: number
    timeout?: NodeJS.Timeout
    onRefreshListener?: () => void
}

// Module which uses the ubiq RoomServer interface to provide STUN and TURN
// information to clients. Can generate short term authentication tokens
// for a TURN server if provided a secret. The secret is never shared.
export class IceServerProvider {
    roomServer: RoomServer
    iceServers: IceServer[]
    constructor (roomServer: RoomServer) {
        this.roomServer = roomServer
        this.iceServers = []

        this.roomServer.on('create', (room) => {
            for (const iceServer of this.iceServers) {
                link(room, iceServer.uri, iceServer.username, iceServer.password)
            }
        })
    }

    // Add an ice server to all open and new rooms

    // Uri is required - all other args optional

    // If a secret is given, short term credentials will be generated and shared
    // with clients. The credentials will last for timeoutSeconds and new
    // credentials will be re-generated every refreshSeconds.

    // If both a username AND password are given, these will be shared with
    // clients preferentially and the secret will not be used.
    addIceServer (uri: string, secret = '', timeoutSeconds = 0,
        refreshSeconds = 0, username = '', password = ''): void {
        console.log('IceServerProvider: Adding ice server with uri ' + uri)

        if (this.iceServers.some((iceServer) => { return iceServer.uri === uri })) {
            return
        }

        const iceServer: IceServer = {
            uri,
            username: '',
            password: '',
            secret: '',
            timeoutSeconds,
            refreshSeconds,
            timeout: undefined,
            onRefreshListener: () => {}
        }

        if (username.length > 0 && password.length > 0) {
            // Prefer username and password if given
            iceServer.username = username
            iceServer.password = password
        } else if (secret.length > 0 && timeoutSeconds > 0) {
            // No username and password, but secret given - generate short term credentials
            iceServer.secret = secret

            const cred = generateCredentials(secret, timeoutSeconds)
            iceServer.username = cred.username
            iceServer.password = cred.password

            iceServer.timeoutSeconds = timeoutSeconds
            iceServer.refreshSeconds = refreshSeconds

            if (iceServer.refreshSeconds <= 0) {
                iceServer.refreshSeconds = iceServer.timeoutSeconds * 0.8
            }

            // Setup refresh for short term credentials
            const roomServer = this.roomServer
            iceServer.onRefreshListener = function () {
                refresh(iceServer, roomServer)
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                iceServer.timeout = setTimeout(iceServer.onRefreshListener!, iceServer.refreshSeconds * 1000)
            }

            // Setup initial timeout
            iceServer.timeout = setTimeout(iceServer.onRefreshListener, refreshSeconds * 1000)
        }

        // Perform initial linking
        for (const room of this.roomServer.getRooms()) {
            link(room, iceServer.uri, iceServer.username, iceServer.password)
        }

        this.iceServers.push(iceServer)
    }

    removeIceServer (uri: string): void {
        let serverI = 0
        const present = this.iceServers.some((iceServer, i) => { serverI = i; return iceServer.uri === uri })

        if (present) {
            const iceServer = this.iceServers[serverI]
            clearTimeout(iceServer.timeout)

            for (const room of this.roomServer.getRooms()) {
                unlink(room, iceServer.uri)
            }

            this.iceServers.splice(serverI, 1)
        }
    }

    clearIceServers (): void {
        while (this.iceServers.length > 0) {
            this.removeIceServer(this.iceServers[this.iceServers.length - 1].uri)
        }
    }
}

// Used internally - not exported
// Generate fresh credentials for an ice server and link all rooms with new info
function refresh (iceServer: IceServer, roomServer: RoomServer): void {
    console.log('IceServerProvider: Refreshing credentials for ice server with uri ' + iceServer.uri)
    const cred = generateCredentials(iceServer.secret, iceServer.timeoutSeconds)
    iceServer.username = cred.username
    iceServer.password = cred.password

    for (const room of roomServer.getRooms()) {
        link(room, iceServer.uri, iceServer.username, iceServer.password)
    }
}

// Used internally - not exported
// Generate credentials for TURN rest api. See section 2.2 in:
// https://datatracker.ietf.org/doc/html/draft-uberti-behave-turn-rest-00
// Coturn expects UNIX timestamp for username and a SHA-1 HMAC for password
// Possible to include a username alongside the timestamp - we skip this
function generateCredentials (secret: any, timeoutSeconds: number): { username: string, password: string } {
    const unixTimestamp = (Date.now() / 1000 + timeoutSeconds).toString()
    return {
        username: unixTimestamp,
        password: generateSha1Hmac(secret, unixTimestamp)
    }
}

// Used internally - not exported
// eslint-disable-next-line @typescript-eslint/explicit-function-return-type
function generateSha1Hmac (secret: any, msg: crypto.BinaryLike | crypto.KeyObject) {
    const hmac = crypto.createHmac('sha1', secret).setEncoding('base64')
    hmac.write(msg)
    return hmac.end().read()
}

// Used internally - not exported
// Ensure room has ice server with matching uri and hmac
// If room has ice server with uri but different hmac, update hmac
// If changes would be made to room properties, push new room args
function link (room: Room, uri: string, username: string, password: string): void {
    console.log('IceServerProvider: Linking room ' + room.uuid + ' to ice server ' + uri + '[' + username + ':' + password + ']')

    let prop = room.properties.get('ice-servers')
    if (prop === '') {
        prop = '{"servers":[]}'
        room.properties.append(prop)
    }

    // Find ice-server object in property
    const iceServerProperty = JSON.parse(prop)
    const iceServers = iceServerProperty.servers as IceServer[]
    let serverI = 0
    const present = iceServers.some((iceServer, i) => { serverI = i; return iceServer.uri === uri })
    if (!present) {
        iceServers.push({
            uri: '',
            username: '',
            password: '',
            timeoutSeconds: 0,
            refreshSeconds: 0
        })
        serverI = iceServers.length - 1
    }

    const server = iceServers[serverI]
    if (server.uri !== uri || server.username !== username || server.password !== password) {
        server.uri = uri
        server.username = username
        server.password = password
        room.appendProperties('ice-servers', JSON.stringify(iceServerProperty))
    }
}

// Used internally - not exported
// Ensure room does not have an ice server with matching uri
// If changes would be made to room properties, push new room args
function unlink (room: Room, uri: string): void {
    console.log('IceServerProvider: Unlinking room ' + room.uuid + ' from ice server ' + uri)
    const args = room.getRoomArgs()

    // Find ice-servers property
    const propI = args.keys.indexOf('ice-servers')
    if (propI < 0) {
        return
    }

    // Remove all ice servers with matching uri
    const iceServers: IceServer[] = JSON.parse(args.values[propI])

    let modified = false
    while (true) {
        let serverI = 0
        const present = iceServers.some((iceServer, i) => { serverI = i; return iceServer.uri === uri })
        if (!present) {
            break
        }

        iceServers.splice(serverI, 1)
        modified = true
    }

    if (modified) {
        //FIXME:
        //room.updateRoom(args);
    }
}
