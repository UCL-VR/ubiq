import { NetworkScene, UbiqTcpConnection, Uuid, WrappedTcpServer } from 'ubiq'

// This module hooks into a room, so we use some convenience methods from the
// rooms tests

import { cleanupRoomClient, createNewRoomClient } from './rooms.test'
import { IceServerProvider, RoomServer } from 'modules'
import { RoomClient } from 'components'
import { type AddressInfo } from 'net'

describe('Ice Module Tests', () => {
    test('Ice Credentials supplied with room', (done) => {
        const rc = createNewRoomClient()
        const uuid = Uuid.generate()

        const completed = new Promise<void>(resolve => {
            rc.addListener('OnJoinedRoom', (room) => {
                expect(room.properties.has('ice-servers')).toBeTruthy()
                const servers = JSON.parse(room.properties.get('ice-servers'))
                expect(servers).toHaveProperty('servers')
                expect(servers.servers.length).toBeGreaterThan(0) // (This test does assume the server we are connecting to has at least one ice server, such as Google's STUN)
                resolve()
            })
        })

        completed.then(done).catch((error) => done(error)).finally(() => { cleanupRoomClient(rc) })

        rc.join(uuid)
    })

    test('Ice Credentials update on expiration', (done) => {
        // This particular test runs its own server

        const serverConnection = new WrappedTcpServer({
            port: 0 // Specifying 0 binds the server to a random free port provided by the OS
        })

        const port = (serverConnection.server.address() as AddressInfo).port

        const roomServer = new RoomServer()
        roomServer.addServer(serverConnection)

        const iceServerProvider = new IceServerProvider(roomServer)

        iceServerProvider.addIceServer(
            'stun.none.cs.ucl.ac.uk',
            'mysecret',
            1.5,
            1,
            '',
            ''
        )

        const connection = UbiqTcpConnection('localhost', port)
        const scene = new NetworkScene()
        scene.addConnection(connection)
        const rc = new RoomClient(scene)

        const uuid = Uuid.generate()

        let numUpdates = 0
        let previousPassword = ''

        const completed = new Promise<void>(resolve => {
            rc.addListener('OnRoomUpdated', (room) => {
                expect(room.properties.has('ice-servers')).toBeTruthy()
                const serversProperty = JSON.parse(room.properties.get('ice-servers'))
                const servers = serversProperty.servers
                if (servers.length > 0) { // (During clean-up, all the servers will be removed leaving this array empty)
                    expect(servers[0].password).not.toEqual(previousPassword)
                    previousPassword = servers[0].password // The password is recomputed each timeout
                    numUpdates++
                    if (numUpdates === 2) {
                        resolve()
                    }
                }
            })
        })

        const serverFinished = new Promise<void>(resolve => {
            roomServer.addListener('destroy', (room) => {
                resolve()
            })
        })

        completed
            .then(() => { iceServerProvider.clearIceServers() })
            .then(() => { cleanupRoomClient(rc) })
            .then(async () => { await serverFinished })
            .then(async () => { await roomServer.exit() })
            .then(done)
            .catch((error) => done(error))

        rc.join(uuid)
    })
})
