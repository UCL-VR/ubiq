const { program } = require('commander');
const { Conf } = require('./conf');

// Command line interface to set conf variables for the server
class Cli{
    static parse(argv=process.argv, conf=new Conf()){
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
                'STUN or TURN URI (RFC7064) with optional secrets');

        program.addHelpText('after', `
ICE Server Format:
    <stun|turn>:<hostname>:<port>[:secret[:timeout-seconds]]

ICE Server Examples:
    stun:example.com:3478
    turn:example.com:3478:secret:600`);

        program.parse(argv);
        var opts = program.opts();

        conf.port = opts.port;
        conf.webSocketPort = opts.webSocketPort;
        conf.sandboxPort = opts.sandboxPort;

        if (opts.iceServers !== undefined) {
            conf.iceServerUris = opts.iceServers;
        }

        return conf;
    }
}

module.exports = {
    Cli
}