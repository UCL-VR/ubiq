import { RoomClient } from 'components'
import WebSocket from 'ws'
import { NetworkScene, TcpConnectionWrapper, WebSocketConnectionWrapper } from 'ubiq'
import nconf from 'nconf'
import { createConnection } from 'net'

nconf.file('local', 'config/local.json')
nconf.file('test', 'config/test.json')
nconf.file('default', 'config/default.json')
const websocket = nconf.get('roomserver:wss')
const tcp = nconf.get('roomserver:tcp')

async function createTcpConnection (): Promise<TcpConnectionWrapper> {
    return await new Promise<TcpConnectionWrapper>((resolve) => {
        const s = createConnection(tcp.port, tcp.uri, () => {
            resolve(new TcpConnectionWrapper(s))
        })
    })
}

async function tcpPing (): Promise<void> {
    await new Promise<void>((resolve, reject) => {
        createTcpConnection().then((connection) => {
            const scene = new NetworkScene()
            scene.addConnection(connection)
            const roomclient = new RoomClient(scene)
            roomclient.addListener('OnPing', () => {
                connection.close()
                resolve()
            })
            roomclient.ping()
        }).catch(reject)
    })
}

describe('Connections', () => {
    test('Can establish WebSocket Connection', done => {
        // This test will only function if you have a valid local key/certificate
        // pair for the server.

        const ws = new WebSocket(`wss://${websocket.uri}:${websocket.port}`)
        ws.onerror = console.log
        const connection = new WebSocketConnectionWrapper(ws)
        const scene = new NetworkScene()
        scene.addConnection(connection)
        const roomclient = new RoomClient(scene)
        roomclient.addListener('OnPing', () => {
            connection.close()
            done()
        })
        roomclient.ping()
    })

    test('Can establish Tcp Connection', done => {
        tcpPing().then(done).catch(done)
    })

    test('Server can handle malformed header', done => {
        createTcpConnection().then((connection) => {
            connection.onClose.push(() => { // Expect server to close connection gracefully
                tcpPing().then(done).catch(done) // Check that the server is still present for other users
            })

            // Send an invalid header (negative length)
            const b = new Uint8Array(10)
            const view = new DataView(b.buffer)
            view.setInt32(0, -1543503868, true)
            connection.socket.write(b)
        }).catch(done)
    })

    test('Server can handle malformed packet - zero length', done => {
        createTcpConnection().then((connection) => {
            connection.onClose.push(() => { // Expect server to close connection gracefully
                tcpPing().then(done).catch(done) // Check that the server is still present for other users
            })

            // Send a packet with a length of zero
            const b = new Uint8Array(4)
            const view = new DataView(b.buffer)
            view.setInt32(0, 0, true)
            connection.socket.write(b)
        }).catch(done)
    })
})
