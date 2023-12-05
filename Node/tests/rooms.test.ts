import { RoomClient, type Room, type RoomPeer } from 'components'
import { NetworkScene, UbiqTcpConnection, Uuid, type IConnectionWrapper } from 'ubiq'
import nconf from 'nconf'

// This set of unit tests is concerned with the RoomServer behaviour. They test
// the roomserver module, as well as the roomclient component, together.

// These tests will use an existing local server instance. This allows the server
// to be started in debug mode before the tests are run for interactive
// debugging.

// The test.json config can be used to set the server address to test. By
// default, this only sets the uri (localhost), and the ports are set by
// default.json. Like app.ts, local.json supercededs everything.

nconf.file('local', 'config/local.json')
nconf.file('test', 'config/test.json')
nconf.file('default', 'config/default.json')
const config = nconf.get('roomserver:tcp')

// Create a set of helper functions for the tests
export function createNewRoomClient (): RoomClient {
    const connection = UbiqTcpConnection(config.uri, config.port)
    const scene = new NetworkScene()
    scene.addConnection(connection)
    const roomclient = new RoomClient(scene)
    return roomclient
}

export function cleanupRoomClient (roomclient: RoomClient): void {
    roomclient.scene.connections.forEach((c: IConnectionWrapper) => { c.close() })
}

export function cleanupRoomClients (clients: RoomClient[]): void {
    clients.forEach(roomclient => {
        roomclient.scene.connections.forEach((c: IConnectionWrapper) => { c.close() })
    })
}

// Given an array of RoomClient instances, instruct each one to join a room by
// UUID and only return when all are successful.
async function joinAllRoomClients (clients: RoomClient[], uuid: string): Promise<void> {
    const promise = Promise.all(clients.map(async roomclient => {
        await new Promise<void>(resolve => {
            roomclient.addListener('OnJoinedRoom', (room: Room) => {
                resolve()
            })
        })
    }))
    clients.forEach(roomclient => {
        roomclient.join(uuid)
    })
    await promise
}

// Given a specific RoomClient, resolve when that RoomClient has been notified
// of a specific peer joining the room (may return immediately if it already has).
async function waitForSpecificPeer (client: RoomClient, other: string): Promise<void> {
    await new Promise<void>((resolve) => {
        if (client.getPeer(other) !== undefined) {
            resolve() // Already has the other peer
        } else {
            client.addListener('OnPeerAdded', peer => {
                if (peer.uuid === other) {
                    resolve()
                }
            })
        }
    })
}

describe('Room Server', () => {
    test('Server Reponds to Ping without Joining Room', done => {
        const roomclient = createNewRoomClient()

        roomclient.addListener('OnPing', (p: string) => {
            try {
                expect(p).toBeDefined()
                done()
            } catch (error) {
                done(error)
            } finally {
                cleanupRoomClient(roomclient)
            }
        })

        roomclient.ping()
    })

    test('Create Room from UUID', done => {
        const roomclient = createNewRoomClient()

        roomclient.addListener('OnJoinedRoom', (room: Room) => {
            try {
                expect(room.uuid).toEqual(uuid)
                expect(room.publish).toBe(false)
                expect(room.name).toBe('My Room')
                expect(room.joincode).toBeDefined()
                done()
            } catch (error) {
                done(error)
            } finally {
                cleanupRoomClient(roomclient)
            }
        })

        const uuid = Uuid.generate()
        roomclient.join(uuid)
    })

    test('Create Room from Name', done => {
        const roomclient = createNewRoomClient()

        roomclient.addListener('OnJoinedRoom', (room: Room) => {
            try {
                expect(room.uuid).toBeDefined()
                expect(room.publish).toBe(false)
                expect(room.name).toBe(roomName)
                expect(room.joincode).toBeDefined()
                done()
            } catch (error) {
                done(error)
            } finally {
                cleanupRoomClient(roomclient)
            }
        })

        const roomName = 'Unit Test Room A'
        roomclient.join(roomName, false)
    })

    test('Join Existing Room via Join Code', done => {
        // This tests joining a room via the Join Code, as well as checking if
        // the OnPeerAdded and OnPeerRemoved callbacks fire as expected.

        // This test works as follows: two clients are created. The first
        // creates a room, and in the OnJoinedRoom callback, instructs the
        // second to join the same room via the join code. The OnPeerAdded events
        // are also monitored. Events are wrapped in promises. When all expected
        // events successfully complete, the test terminates.

        const roomclientA = createNewRoomClient()
        const roomclientB = createNewRoomClient()

        const uuid = Uuid.generate()

        const roomclientAJoined = new Promise<void>((resolve) => {
            roomclientA.addListener('OnJoinedRoom', (room: Room) => {
                roomclientB.join(room.joincode)
                resolve()
            })
        })

        // The expect statements are wrapped in try-catch blocks, as they will
        // be executed in the stack of the roomclient message handler, not the
        // promise constructor, and so reject must be referenced explicitly.

        const roomclientAPeerAdded = new Promise<void>((resolve, reject) => {
            roomclientA.addListener('OnPeerAdded', (peer: RoomPeer) => {
                try {
                    expect(peer.uuid).toBe(roomclientB.peer.uuid)
                    resolve()
                } catch (e) { reject(e) };
            })
        })

        const roomclientBJoined = new Promise<void>((resolve, reject) => {
            roomclientB.addListener('OnJoinedRoom', (room: Room) => {
                try {
                    expect(room.uuid).toBe(uuid)
                    resolve()
                } catch (e) { reject(e) };
            })
        })

        const roomclientBPeerAdded = new Promise<void>((resolve, reject) => {
            roomclientB.addListener('OnPeerAdded', (peer: RoomPeer) => {
                try {
                    expect(peer.uuid).toBe(roomclientA.peer.uuid)
                    resolve()
                } catch (e) { reject(e) };
            })
        })

        Promise.all([
            roomclientAJoined,
            roomclientAPeerAdded,
            roomclientBJoined,
            roomclientBPeerAdded
        ]).then(() => {
            done()
        }).catch((error) => {
            done(error)
        }).finally(() => {
            cleanupRoomClient(roomclientA)
            cleanupRoomClient(roomclientB)
        })

        roomclientA.join(uuid)
    })

    test('Exchange Room Property Between Multiple Peers', done => {
        const roomclients: RoomClient[] = []
        roomclients.push(createNewRoomClient())
        roomclients.push(createNewRoomClient())
        roomclients.push(createNewRoomClient())

        const uuid = Uuid.generate()
        const key1 = 'ubiq.unittests.property1'
        const value1 = 'R9Cy7IS8rDXjqPdYyuJ3'

        // When a room is updated, we expect all other members to recieve a
        // notification if they are already members at the time the property
        // is set.

        // roomclients[1] is going to set the property. Setting a Room property
        // is a 'request' - there is no guarantee so all clients, including the
        // one sending the update, need to wait until it is acknowledged.

        const onRoomUpdatedReceived = Promise.all(
            roomclients.map(async roomclient => {
                await new Promise<void>(resolve => {
                    roomclient.addListener('OnRoomUpdated', () => {
                        resolve()
                    })
                })
            })
        )

        const lateJoinerHasProperty = onRoomUpdatedReceived.then(async () => {
            // Check each of the existing rooms, now they have supposedly all
            // received the message

            roomclients.forEach(roomclient => {
                expect(roomclient.getRoomProperty(key1)).toBe(value1)
            })

            // When a client joins after a property is set, it will not receive
            // the OnRoomUpdated event, but it should still have the correct property

            await new Promise<void>((resolve, reject) => {
                const lateRoomClient = createNewRoomClient()
                roomclients.push(lateRoomClient)

                // This client should not receive OnRoomUpdated because it gets
                // the value when it joins

                lateRoomClient.addListener('OnRoomUpdated', () => {
                    reject(new Error('Not expecting OnRoomUpdated'))
                })

                // After the join, the room should have the value set correctly

                joinAllRoomClients([lateRoomClient], uuid).then(() => {
                    expect(lateRoomClient.getRoomProperty(key1)).toBe(value1)
                    resolve()
                }).catch(error => {
                    reject(error)
                })
            })
        })

        // Termination criteria

        lateJoinerHasProperty.then(() => {
            done()
        }).catch((error) => {
            done(error)
        }).finally(() => {
            cleanupRoomClients(roomclients)
        })

        // From now on we assume the joining functionality has all been tested.
        // This method will join all the roomclients and return a promise that
        // will resolve once all of the joins are successful.

        // Have one of the clients set a property to start off the events

        void joinAllRoomClients(roomclients, uuid).then(() => {
            roomclients[1].setRoomProperty(key1, value1)
        })
    })

    test('Exchange Peer Properties', done => {
        const roomclients: RoomClient[] = []
        roomclients.push(createNewRoomClient())
        roomclients.push(createNewRoomClient())

        const uuid = Uuid.generate()
        const key = 'ubiq.unittests.property1'

        const values: string[] = []
        values.push('Mw4oumCWHIaM2gwC7WyJ')
        values.push('klPteFvLrmR6MOhgn1TM')

        // Peer properties may only be set by the local peer. When they are set,
        // notifications are sent to existing peers. New peers are initialised
        // with the correct properties.

        // (A note about this particular test - the current way the peer
        // properties work is that they are sent to the server 'round robin' when
        // updating the local peer. This is an implementation detail though and
        // is not tested explicitly - only that the order of the updates is
        // correct.)

        // Peer 1 sets a property before joining

        roomclients[0].peer.setProperty(key, values[0])

        // Then peer 1 joins the room

        const peer1Joined = joinAllRoomClients([roomclients[0]], uuid)

        // Then peer 2 joins the room, notably after peer 1 has finished its
        // negotiation. Peer two won't necessarily know of Peer 2 yet, because
        // that comes in a different message

        void peer1Joined.then(() => {
            void joinAllRoomClients([roomclients[1]], uuid)
        })

        // Wait until the peers heear about eachother.

        const peersKnowAboutEachother = Promise.all([
            waitForSpecificPeer(roomclients[0], roomclients[1].peer.uuid),
            waitForSpecificPeer(roomclients[1], roomclients[0].peer.uuid)
        ])

        // Now, two peers have joined, one of which has a value in the key...

        const afterTestsRun = peersKnowAboutEachother.then(async () => {
            // Peer 2 should be able to see Peer 1's property
            expect(roomclients[1].getPeer(roomclients[0].peer.uuid)?.getProperty(key)).toBe(values[0])

            // Peer 1 should get undefined when trying to access the same (or
            // any) key on on peer 2
            expect(roomclients[0].getPeer(roomclients[1].peer.uuid)?.getProperty(key)).toBeUndefined()

            // Any future changes to peer 2 though should raise the OnPeerUpdated
            // event
            const peer1GetsPeerUpdatedMessage = new Promise<void>((resolve) => {
                roomclients[0].addListener('OnPeerUpdated', peer => {
                    if (peer.uuid === roomclients[1].peer.uuid) {
                        expect(peer.getProperty(key)).toBe(values[1])
                        resolve()
                    }
                })
            })

            // Now set the property on peer 2 and see if the above event is called
            roomclients[1].peer.setProperty(key, values[1])

            // We expect that peer 1 will recieve a notification
            await peer1GetsPeerUpdatedMessage
        })

        afterTestsRun.then(() => {
            done()
        }).catch((error) => {
            done(error)
        }).finally(() => {
            cleanupRoomClients(roomclients)
        })
    })
})
