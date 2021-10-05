const { hrtime } = require('process')
const fs = require('fs');
const { file } = require('nconf');

class FileTransport{
    constructor(args){
        var now = Date()
        var filename = "";
        if(args.length > 0){
            filename += args[0] + "/"
        }else{
            filename = "./"
        }
        filename += "Server.";
        filename += "performance.json";
        this.stream = fs.createWriteStream(filename);
        this.stream.write("[");
        process.on('SIGTERM', ()=>{
            this.stream.write("]");
            this.stream.close();
        })
    }

    emit(event){
        this.stream.write(JSON.stringify(event) + ",\n");
    }
}

//This is the base class for all logging in the server. The server uses global 
//EventLogger instances to record various events. Different instances are for 
//different types of event.
class EventLogger{
    static begin(event){
        return {
            ticks: hrtime.bigint().toString(),
            event: event,
        };
    }

    // Creates an event where the arguments are the properties of the provided object. The name of the event is passed
    // separately.
    static buildStructedPropertiesEvent(event, object){
        var ev = this.begin(event);
        Object.getOwnPropertyNames(object).forEach(propertyName => {
            ev[propertyName] = object[propertyName];
        });
        return ev;
    }
}

// For standard observation and diagnostics. These events should be logged all the time.
class Info extends EventLogger{
    static log(message){
        console.log(message);
    }
}

class Performance extends EventLogger{
    static transport = null;

    static startLog(){
        this.transport = new FileTransport(arguments);
    }

    static logStructedEvent(event){
        this.transport.emit(event);
    }
    
    //The log entry point for Performance. When logging is disabled, this is set as an empty function.
    static log(event){
        var ev = EventLogger.buildStructuredEvent(arguments);
    }

    static logProperties(event, object){
        if(this.transport != null){
            this.logStructedEvent(EventLogger.buildStructedPropertiesEvent(event, object));
        }
    }
}

module.exports = {
    Info,
    Performance
}