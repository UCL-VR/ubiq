const WebSocket = require('ws');
const Tcp = require('net');

class SingleWebsocketRelay{
    constructor(port){
        var wss = new WebSocket.Server({ port: port });
        wss.on('connection', function connection(ws) {
            console.log("Client Connected");
            ws.on('message', function incoming(data) {
                wss.clients.forEach(function each(client) {
                if (client !== ws && client.readyState === WebSocket.OPEN) {
                    client.send(data);
                    console.log("Exchanged");
                }
                });
            });
        });
    }
}

class SingleTcpRelay {
    constructor(port) {
        var wss = Tcp.createServer();
        wss.clients = [];
        wss.on('connection', function connection(ws) {
            console.log("Client Connected");
            wss.clients.push(ws);
            ws.on('data', function incoming(data) {
                wss.clients.forEach(function each(client) {
                    if (client !== ws && client.readyState === 'open') {
                        client.write(data);
                        console.log("Exchanged");
                    }
                });
            });
        });
        wss.listen(port);
    }
}

module.exports = {
    SingleTcpRelay,
    SingleWebsocketRelay
}