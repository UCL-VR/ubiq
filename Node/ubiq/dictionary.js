class SerialisedDictionary{
    static From(dictionary){
        if(dictionary.keys.length > 0){
            return Object.assign(...dictionary.keys.map((k,i) => ({[k]: dictionary.values[i]})));
        }else{
            return {};
        }
        
    }

    static To(object){
        return { keys: Object.keys(object), values: Object.values(object) };
    }
}

module.exports = {
    SerialisedDictionary
}