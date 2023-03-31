const { NetworkId } = require("ubiq")
const { EventEmitter } = require('events')

// The ThreePointTrackedAvatar Component sends typical XR tracked poses (head
// and hands) as floating point arrays.

class Pose{
    constructor(){
        this.position = {
            x: 0,
            y: 0,
            z: 0
        },
        this.rotation = {
            x: 0,
            y: 0,
            z: 0,
            w: 0
        }
    }
}

class HandPose extends Pose {
    constructor(){
        super()
        this.grip = 0;
    }
}
class ThreePointTrackedAvatar {
    constructor(scene, avatarId){
        this.networkId = NetworkId.Create(avatarId, "ThreePointTracked");
        this.context = scene.register(this);
        this.head = new Pose();
        this.left = new HandPose();
        this.right = new HandPose();
    }

    readState(buffer){
        this.head.position.x  = buffer.readFloatLE(0);
        this.head.position.y  = buffer.readFloatLE(4);
        this.head.position.z  = buffer.readFloatLE(8);
        this.head.rotation.x = buffer.readFloatLE(12);
        this.head.rotation.y = buffer.readFloatLE(16);
        this.head.rotation.z = buffer.readFloatLE(20);
        this.head.rotation.w = buffer.readFloatLE(24);

        this.left.position.x  = buffer.readFloatLE(28);
        this.left.position.y  = buffer.readFloatLE(32);
        this.left.position.z  = buffer.readFloatLE(36);
        this.left.rotation.x = buffer.readFloatLE(40);
        this.left.rotation.y = buffer.readFloatLE(44);
        this.left.rotation.z = buffer.readFloatLE(48);
        this.left.rotation.w = buffer.readFloatLE(52);

        this.right.position.x  = buffer.readFloatLE(56);
        this.right.position.y  = buffer.readFloatLE(60);
        this.right.position.z  = buffer.readFloatLE(64);
        this.right.rotation.x = buffer.readFloatLE(68);
        this.right.rotation.y = buffer.readFloatLE(72);
        this.right.rotation.z = buffer.readFloatLE(76);
        this.right.rotation.w = buffer.readFloatLE(80);
    }

    processMessage(m){
        this.readState(m.message);
    }
}

// The AvatarManager detects when Peers have created an Avatar to embody, and
// emit the OnAvatarCreated and OnAvatarDestroyed messages accordingly.
// This class does not actually create an Avatar object, as there is no 
// JavaScript equivalent of the Avatar Catalogue.
// If JavaScript clients can predict or interpret the events however, they can
// use them to create and register new Network Components to represent the
// avatars.
// For example, an unmodified Start Here scene will always create an Avatar
// with a ThreePointTracked Component attached. Above, an example counterpart
// class is included, which can be instantiated to recieve pose updates, using
// the networkId provided in the OnAvatarCreated event, like so:
//
//     avatarmanager.addListener("OnAvatarCreated", avatar => {
//        const tpta = new ThreePointTrackedAvatar(scene, avatar.networkId);
//     });
//
class AvatarManager extends EventEmitter{
    constructor(scene){
        super();
        const roomclient = scene.getComponent("RoomClient");
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