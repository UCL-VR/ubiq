
const { RoomServer } = require("./rooms");
const { WrappedWebSocketServer, WrappedTcpServer } = require("./connections")

server = new RoomServer(); 
server.addServer(new WrappedTcpServer(8002));
server.addServer(new WrappedWebSocketServer(8003));