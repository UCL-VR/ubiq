// Configuration variables for the server
// Seperated out to allow for construction through code, command line or file
class Conf{
    constructor(){
        this.port = 0;
        this.webSocketPort = 0;
        this.sandboxPort = 0;
        this.iceServerUris = [];
    }
}

module.exports = {
    Conf
}