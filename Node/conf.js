const { Validator } = require('jsonschema');

// Configuration variables for the server
// Seperated out to allow for construction through code, command line or file
class ConfValidator{
    constructor(conf){
        this.validator = new Validator();
        this.schema = {
            "type": "object",
            "properties": {
                "port":             { "type": "number" },
                "webSocketPort":    { "type": "number" },
                "sandboxPort":      { "type": "number" },
                "ice-servers":      {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "uri":              { "type": "string" },
                            "secret":           { "type": "string" },
                            "timeoutSeconds":   { "type": "number" },
                            "refreshSeconds":   { "type": "number" },
                            "username":         { "type": "string" },
                            "password":         { "type": "string" }
                        },
                        "required": [
                            "uri",
                            "secret",
                            "timeoutSeconds",
                            "refreshSeconds",
                            "username",
                            "password"
                        ]
                    }
                }
            },
            "required": [
                "port",
                "webSocketPort",
                "sandboxPort",
                "iceServers"
            ]
        }

        this.result = this.validator.validate(conf,this.schema);
    }
}

module.exports = {
    ConfValidator
}