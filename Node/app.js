
const { RoomServer } = require("./rooms");
const { SandboxServer } = require("./sandbox");
const { WrappedWebSocketServer, WrappedTcpServer } = require("./connections")
const { Cli } = require("./cli");
const { IceServerProvider } = require("./ice");
const { ConfValidator } = require("./conf");

var conf = Cli.parse();

var validator = new ConfValidator(conf);
if (!validator.result.valid) {
    console.log(validator.result.errors);
    return;
}

roomServer = new RoomServer();
roomServer.addServer(new WrappedTcpServer(conf.port));
roomServer.addServer(new WrappedWebSocketServer(conf.webSocketPort));

iceServerProvider = new IceServerProvider(roomServer);
for (const iceServer of conf.iceServers){
    iceServerProvider.addIceServer(
        iceServer.uri,
        iceServer.secret,
        iceServer.timeoutSeconds,
        iceServer.refreshSeconds,
        iceServer.username,
        iceServer.password);
}

sandbox = new SandboxServer(conf.sandboxPort);