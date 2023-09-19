import { v4 as uuidv4, validate as validate, version as version } from 'uuid';

export class Uuid {
    static generate(){
        return uuidv4();
    }
    static validate(str : string){
        return validate(str);
    }
}