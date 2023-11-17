import { WrappedSecureWebSocketServer, WrappedTcpServer } from "ubiq";
import { RoomServer, IceServerProvider, Status } from "modules";
import nconf from "nconf";

// nconf loads the configuration hierarchically - settings that load *first* 
// take priority. default.json contains most of the rarely changing 
// configuration properties, stored with the branch. Additional configuration 
// files - where present - add or override parameters, such as pre-shared 
// secrets, that should not be in source control.
process.argv.slice(2).forEach(element => {
    nconf.file(element,element);
});
nconf.file('local', 'config/local.json');
nconf.file('default', 'config/default.json');

const roomServer = new RoomServer();
roomServer.addServer(new WrappedTcpServer(nconf.get('roomserver:tcp')));
roomServer.addServer(new WrappedSecureWebSocketServer(nconf.get('roomserver:wss')));

const statusModule = new Status(roomServer, nconf.get('status'));

const iceServerProvider = new IceServerProvider(roomServer);
var iceServers = nconf.get('iceservers');
if (iceServers){
    for (const iceServer of iceServers){
        iceServerProvider.addIceServer(
            iceServer.uri,
            iceServer.secret,
            iceServer.timeoutSeconds,
            iceServer.refreshSeconds,
            iceServer.username,
            iceServer.password);
    }
}

// Set the type of room this Server should use. Make sure
// that the room type has been imported before we get here.

var roomTypeName = nconf.get("roomserver:roomType");
if(roomTypeName != undefined){
    roomServer.T = eval(roomTypeName);
}

process.on('SIGINT', function() {
    // Registering for SIGINT allows various modules to shutdown gracefully
    roomServer.exit(()=>{
        console.log("Shutdown");
        process.exit(0);
    });
 })