const { program } = require('commander');

// Command line interface to set conf variables for the server
class Cli{
    static parse(argv=process.argv){
        program
            .option(
                '-p, --port <port-number>',
                'primary TCP listener port',
                8000)
            .option(
                '-w, --web-socket-port <port-number>',
                'websocket listener port',
                8001)
            .option(
                '-s, --sandbox-port <port-number>',
                'sandbox listener port',
                8002)
            .option(
                '-i, --ice-servers <uri...>',
                'STUN (RFC7064) or TURN (RFC7065) URI with optional params');

        program.addHelpText('after', `
ICE-Server Usage: <uri>[!<param>=<value>...]

ICE-Server Options:
  !secret       alphanumeric value shared with turn server for time-limited credentials
  !timeout      lifetime of time-limited credentials in seconds
  !refresh      time to generate new time-limited credentials in seconds (default: 0.8*timeout)
  !username     user for long-term credential mechanism
  !password     pass for long-term credential mechanism

ICE-Server Examples:
  stun:example.com:3478
  turn:example.com:3478!secret=1234!timeout=600`);

        program.parse(argv);
        var opts = program.opts();
        var iceServers = [];

        if (opts.iceServers === undefined) {
            opts.iceServers = [];
        }

        // Build ice server list by stripping out ubiq specific params
        for (const extUri of opts.iceServers){
            var get = extUriString => {
                var extUriComponents = extUriString.split("!");

                var iceServer = {
                    uri: extUriComponents[0],
                    secret: "",
                    timeoutSeconds: 0,
                    refreshSeconds: 0,
                    username: "",
                    password: ""
                };
                for (const extUriComponent of extUriComponents){
                    if (extUriComponent.startsWith("secret")){
                        var pair = extUriComponent.split("=");
                        if (pair.length > 1){
                            iceServer.secret = pair[1];
                        }
                    } else if (extUriComponent.startsWith("timeout")){
                        var pair = extUriComponent.split("=");
                        if (pair.length > 1){
                            iceServer.timeoutSeconds = pair[1];
                        }
                    } else if (extUriComponent.startsWith("refresh")){
                        var pair = extUriComponent.split("=");
                        if (pair.length > 1){
                            iceServer.refreshSeconds = pair[1];
                        }
                    } else if (extUriComponent.startsWith("username")){
                        var pair = extUriComponent.split("=");
                        if (pair.length > 1){
                            iceServer.username = pair[1];
                        }
                    } else if (extUriComponent.startsWith("password")){
                        var pair = extUriComponent.split("=");
                        if (pair.length > 1){
                            iceServer.password = pair[1];
                        }
                    }
                }
                return iceServer;
            }

            var iceServer = get(extUri);
            if (iceServer === null){
                console.log("Could not parse ICE Server: " + extUri + ". Skipping...");
            } else {
                iceServers.push(iceServer);
            }
        }

        return {
            port: opts.port,
            webSocketPort: opts.webSocketPort,
            sandboxPort: opts.sandboxPort,
            iceServers: iceServers
        };
    }
}

module.exports = {
    Cli
}