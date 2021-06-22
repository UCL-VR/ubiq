const { Validator } = require('jsonschema');

// The Schema class provides validation for server messages. It is common to multiple compoenents as Ubiq messages may be interdependent.
class Schema {
    static add(schema){
        if(Schema.validator == null){
            Schema.validator = new Validator();
        }
        // A schema that is just a ref is an alias - this isnt supported on the json schema validator at the moment, so implement it manually
        if('$ref' in schema){
            Schema.validator.schemas[schema.id] = Schema.validator.schemas[schema.$ref];
        }else{
            Schema.validator.addSchema(schema,schema.id);
        }
    }

    // Validate a Json message using a registered schema ID, passed as a string.
    static validate(json, schema, failure){
        try{
            var argsResult = Schema.validator.validate(
                json,
                {
                    "type": "object",
                    "$ref": schema
                });
            if (argsResult.valid) {
                return true;
            } else {
                throw argsResult;
            }
        }
        catch(schemaError){
            var error = {
                json: json,
                schema: schema,
                validation: schemaError
            };
            return failure(error);
        }
    }
}

module.exports = {
    Schema
}