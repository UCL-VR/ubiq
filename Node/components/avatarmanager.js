const { NetworkId } = require("ubiq")
const { EventEmitter } = require('events')

class ThreePointTrackedAvatar {
    constructor(scene, avatarId){
        this.networkId = NetworkId.Create(avatarId, "ThreePointTracked");
        this.context = scene.register(this);
    }

    readState(buffer){
        this.headPosition = {
            x: buffer.readFloatLE(0),
            y: buffer.readFloatLE(4),
            z: buffer.readFloatLE(8)
        }
        this.headRotation = {
            x: buffer.readFloatLE(12),
            y: buffer.readFloatLE(16),
            z: buffer.readFloatLE(20)
        }
        //Todo: rest of avatar state
    }

    processMessage(m){
        this.readState(m.message);
        console.log(this.headPosition);
    }
}

class AvatarManager extends EventEmitter{
    constructor(roomclient){
        super();
        roomclient.addListener("OnPeerAdded", peer =>{
            this.#createOrUpdateAvatar(peer);
        });
        roomclient.addListener("OnPeerUpdated", peer =>{
            this.#createOrUpdateAvatar(peer);
        });
        roomclient.addListener("OnPeerRemoved", peer =>{
            this.#destroyAvatar(peer);
        });
        this.avatars = new Map();
        this.prefix = "ubiq.avatars";
    }

    #createOrUpdateAvatar(peer){
        peer.properties.forEach((value,key) => {
            if(key.startsWith(this.prefix)){
                const networkId = key.slice(this.prefix.length + 1);
                const parameters = JSON.parse(value);
                if(!this.avatars.has(networkId)){
                    this.avatars.set(networkId, peer.uuid);
                    this.emit("OnAvatarCreated", { networkId, peer, parameters });
                }
            }
        });
    }

    #destroyAvatar(peer){
        let toDelete = [];
        this.avatars.forEach((value,key)=>{
            if(value.uuid == peer.uuid){
                toDelete.push(key);
            }
        });
        toDelete.forEach(id =>{
            this.avatars.delete(id);
            this.emit("OnAvatarDestroyed", id);
        });
    }
}

module.exports = {
    AvatarManager,
    ThreePointTrackedAvatar
}