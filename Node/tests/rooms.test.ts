import { Room, RoomClient, RoomPeer } from "components";
import { NetworkScene, UbiqTcpConnection, Uuid, ConnectionWrapper } from "ubiq";
import { promise } from "zod";

// This set of unit tests is concerned with the RoomServer behaviour. They test
// the roomserver module, as well as the roomclient component, together.

// These tests will use an existing local server instance. This allows the server
// to be started in debug mode before the tests are run for interactive
// debugging.

const config = {
    uri: "localhost",
    port: 8009
}

// Create a set of helper functions for the tests
function createNewRoomClient() {
    const connection = UbiqTcpConnection(config.uri, config.port);
    const scene = new NetworkScene();
    scene.addConnection(connection);
    const roomclient = new RoomClient(scene);
    return roomclient;
}

function cleanupRoomClient(roomclient: RoomClient){
    roomclient.scene.connections.forEach((c: ConnectionWrapper) => c.close());
}

function cleanupRoomClients(clients: RoomClient[]){
    clients.forEach(roomclient =>{
        roomclient.scene.connections.forEach((c: ConnectionWrapper) => c.close());
    });
}

// Given an array of RoomClient instances, instruct each one to join a room by
// UUID and only return when all are successful.
function joinAllRoomClients(clients: RoomClient[], uuid: string): Promise<void[]>{
    let promise = Promise.all(clients.map(roomclient =>{
        return new Promise<void>(resolve =>{
             roomclient.addListener("OnJoinedRoom", (room: Room)=>{
                resolve();
             });
        }); 
    }));
    clients.forEach(roomclient =>{
        roomclient.join(uuid);
    })
    return promise;
}

describe("Room Server", ()=> {
    test("Server Reponds to Ping without Joining Room", done =>{
        const roomclient = createNewRoomClient();

        roomclient.addListener("OnPing", (p: string) => {
            try{
                expect(p).toBeDefined();
                done();
            }catch(error) {
                done(error);
            }
            finally{
                cleanupRoomClient(roomclient);
            }
        });

        roomclient.ping();
    });

    test("Create Room from UUID", done =>{
        const roomclient = createNewRoomClient();
        
        roomclient.addListener("OnJoinedRoom", (room: Room) =>{
            try{
                expect(room.uuid).toEqual(uuid);
                expect(room.publish).toBe(false);
                expect(room.name).toBe("My Room");
                expect(room.joincode).toBeDefined();
                done();
            }catch(error){
                done(error);
            }finally{
                cleanupRoomClient(roomclient);
            }
        });

        const uuid = Uuid.generate();
        roomclient.join(uuid);
    });

    test("Create Room from Name", done =>{
        const roomclient = createNewRoomClient();

        roomclient.addListener("OnJoinedRoom", (room: Room) =>{
            try{
                expect(room.uuid).toBeDefined();
                expect(room.publish).toBe(false);
                expect(room.name).toBe(roomName);
                expect(room.joincode).toBeDefined();
                done();
            }catch(error){
                done(error);
            }
            finally{
                cleanupRoomClient(roomclient);
            }    
        });

        const roomName = "Unit Test Room A";
        roomclient.join(roomName, false);
    });

    test("Join Existing Room via Join Code", done =>{
        // This tests joining a room via the Join Code, as well as checking if
        // the OnPeerAdded and OnPeerRemoved callbacks fire as expected.

        // This test works as follows: two clients are created. The first
        // creates a room, and in the OnJoinedRoom callback, instructs the
        // second to join the same room via the join code. The OnPeerAdded events
        // are also monitored. Events are wrapped in promises. When all expected
        // events successfully complete, the test terminates.

        const roomclientA = createNewRoomClient();
        const roomclientB = createNewRoomClient();

        const uuid = Uuid.generate();

        const roomclientAJoined = new Promise<void>((resolve) =>{
            roomclientA.addListener("OnJoinedRoom", (room: Room) =>{
                roomclientB.join(room.joincode);
                resolve();
            });
        });

        // The expect statements are wrapped in try-catch blocks, as they will
        // be executed in the stack of the roomclient message handler, not the
        // promise constructor, and so reject must be referenced explicitly.

        const roomclientAPeerAdded = new Promise<void>((resolve, reject) =>{
            roomclientA.addListener("OnPeerAdded", (peer: RoomPeer) =>{
                try{
                    expect(peer.client).toBeUndefined();
                    expect(peer.uuid).toBe(roomclientB.peer.uuid);
                    resolve();
               }catch(e){ reject(e); };
            });
        });

        const roomclientBJoined = new Promise<void>((resolve, reject) => {
            roomclientB.addListener("OnJoinedRoom", (room: Room) =>{
                try{
                    expect(room.uuid).toBe(uuid);
                    resolve();
                }catch(e){ reject(e); };
            });
        });

        const roomclientBPeerAdded = new Promise<void>((resolve, reject) =>{
            roomclientB.addListener("OnPeerAdded", (peer: RoomPeer) =>{
                try{
                    expect(peer.uuid).toBe(roomclientA.peer.uuid);
                    resolve();
                }catch(e){ reject(e); };
            })
        });

        Promise.all([
            roomclientAJoined, 
            roomclientAPeerAdded, 
            roomclientBJoined,
            roomclientBPeerAdded
        ]).then(()=>{
            done();
        }).catch((error)=>{
            done(error);
        }).finally(()=>{
            cleanupRoomClient(roomclientA);
            cleanupRoomClient(roomclientB);
        });

        roomclientA.join(uuid);
    });

    test("Exchange Room Property Between Multiple Peers", done =>{
        const roomclients: RoomClient[] = [];
        roomclients.push(createNewRoomClient());
        roomclients.push(createNewRoomClient());
        roomclients.push(createNewRoomClient());

        const uuid = Uuid.generate();
        const key1 = "ubiq.unittests.property1";
        const value1 = "R9Cy7IS8rDXjqPdYyuJ3";

        // When a room is updated, we expect all other members to recieve a
        // notification if they are already members at the time the property
        // is set.

        const onRoomUpdatedReceived = Promise.all(
            [roomclients[0], roomclients[2]].map(roomclient =>{
                return new Promise<void>(resolve => {
                    roomclient.addListener("OnRoomUpdated", ()=> {
                        resolve();
                    });
                });
            })
        );

        const lateJoinerHasProperty = onRoomUpdatedReceived.then(() => {

            // Check each of the existing rooms, now they have supposedly all 
            // received the message

            roomclients.forEach(roomclient =>{
                expect(roomclient.getRoomProperty(key1)).toBe(value1);
            });

            // When a client joins after a property is set, it will not receive
            // the OnRoomUpdated event, but it should still have the correct property

            return new Promise<void>((resolve, reject) => {
                const lateRoomClient = createNewRoomClient();
                roomclients.push(lateRoomClient);

                // This client should not receive OnRoomUpdated because it gets
                // the value when it joins

                lateRoomClient.addListener("OnRoomUpdated", ()=>{
                    reject(); 
                });

                // After the join, the room should have the value set correctly

                joinAllRoomClients([lateRoomClient], uuid).then(()=>{
                    expect(lateRoomClient.getRoomProperty(key1)).toBe(value1);
                    resolve();
                });
            });
        });
        
        // Termination criteria

        lateJoinerHasProperty.then(()=>{
            done();
        }).catch((error)=>{
            done(error);
        }).finally(()=>{
            cleanupRoomClients(roomclients);
        });

        // From now on we assume the joining functionality has all been tested.
        // This method will join all the roomclients and return a promise that
        // will resolve once all of the joins are successful.

        // Have one of the clients set a property to start off the events

        joinAllRoomClients(roomclients, uuid).then(()=>{
            roomclients[1].setRoomProperty(key1, value1);
        });
    });

    test("Exchange Peer Properties", done =>{
        

    });
});