
const { RoomServer } = require("./rooms");
const { SandboxServer } = require("./sandbox");
const { WrappedWebSocketServer, WrappedTcpServer } = require("./connections")
const { Cli } = require("./cli");

conf = Cli.parse();

roomServer = new RoomServer();
roomServer.addServer(new WrappedTcpServer(conf.port));
roomServer.addServer(new WrappedWebSocketServer(conf.webSocketPort));

sandbox = new SandboxServer(conf.sandboxPort);