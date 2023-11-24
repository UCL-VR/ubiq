import { Uuid } from 'ubiq'

// This module hooks into a room, so we use some convenience methods from the
// rooms tests

import { cleanupRoomClient, createNewRoomClient } from './rooms.test'

describe('Ice Module Tests', () => {
    test('Ice Credentials supplied with room', (done) => {
        const rc = createNewRoomClient()
        const uuid = Uuid.generate()

        const completed = new Promise<void>(resolve => {
            rc.addListener('OnJoinedRoom', (room) => {
                expect(room.properties.has('ice-servers')).toBeTruthy()
                const servers = JSON.parse(room.properties.get('ice-servers'))
                expect(servers).toHaveProperty('servers')
                expect(servers.servers.length).toBeGreaterThan(0)
                resolve()
            })
        })

        completed.then(done).catch((error) => done(error)).finally(() => { cleanupRoomClient(rc) })

        rc.join(uuid)
    })
})
