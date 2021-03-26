
const { RoomServer } = require("./rooms");
const { SandboxServer } = require("./sandbox");
const { WrappedWebSocketServer, WrappedTcpServer } = require("./connections")

server = new RoomServer(); 
server.addServer(new WrappedTcpServer(8004));
server.addServer(new WrappedWebSocketServer(8005));

sandbox = new SandboxServer(8006); // remember to set the port appropriately for the branch