import { RoomClient } from "components";
import WebSocket from 'ws';
import { NetworkScene, WebSocketConnectionWrapper } from "ubiq";

const websocket = {
    uri: "localhost",
    port: 8010
}

describe("Connections", ()=>{
    test("Can establish WebSocket Connection", done=>{

        // This test will only function if you have a valid local key/certificate
        // pair for the server.

        const ws = new WebSocket(`wss://${websocket.uri}:${websocket.port}`);
        ws.onerror = console.log;
        const connection = new WebSocketConnectionWrapper(ws);
        const scene = new NetworkScene();
        scene.addConnection(connection);
        const roomclient = new RoomClient(scene);
        roomclient.addListener("OnPing", ()=>{
            connection.close();
            done();
        });
        roomclient.ping();
    });

});