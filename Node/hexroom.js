const { Room } = require("./rooms")

function arrayRemove(array,element){
    const index = array.indexOf(element);
    if (index > -1) {
        array.splice(index, 1);
    }
}

function checkPeer(peer){
    if(!peer.hasOwnProperty("observed")){
        peer.observed = [];
    }
}

class HexRoom extends Room{
    constructor(server){
        super(server);
        this.observers = []; // This room can be observed by Peers that are not members of it
    }

    processMessage(source, message){
        super.processMessage(source, message);
        this.observers.forEach(peer =>{ // Observers receive messages emanating from this room, but emit messages from their own room
            if(peer != source){
                peer.send(message);
            }
        });
    }

    // Forwarded to this room when the Peer sends a message to the server with 
    // an unknown Type
    processRoomMessage(peer, message){
        switch(message.type){
            case "SetObserved":
                this.changeObserved(peer, message.args.rooms); // Name collision with the internal api
                break;
        }
    }

    changeObserved(peer, rooms){

        // This method makes the changes in observership. It does this by 
        // finding rooms to add or remove the peer from based on the peers
        // observed list, then finds those rooms on the server (which should all
        // be of this type) and calls the addObserver and removeObserver methods, 
        // which update the room instances, and peers.

        var unobserved = peer.observed.filter(existing => !rooms.includes(existing.uuid)).map(x => x.uuid);

        for(var room of unobserved){
            room = this.server.findOrCreateRoom({uuid: room});
            room.removeObserver(peer);
        }

        var observed = rooms;

        for(var room of observed){
            room = this.server.findOrCreateRoom({uuid: room});
            room.addObserver(peer);
        }
    }

    addPeer(peer){
        // If the peer is an observer, then upgrade the peer in place...
        if(this.observers.includes(peer)){
            arrayRemove(this.observers, peer);

            checkPeer(peer)
            arrayRemove(peer.observed, this);
        }

        super.addPeer(peer);

        this.observers.forEach(existing =>{
            existing.sendPeerAdded(peer); // Tell existing observers about the new Peer
        })
    }

    removePeer(peer){
        super.removePeer(peer);
        for(var existing of this.observers) {
            existing.sendPeerRemoved(peer); // Tell the observers about the missing peer
        };
    }

    addObserver(peer){
        if(!this.observers.includes(peer)){
            
            // Add this peer to the observers list
            this.observers.push(peer);

            // And add this room the peer's observed list
            checkPeer(peer);
            peer.observed.push(this);

            this.peers.forEach(member => {
                peer.sendPeerAdded(member); // Tell the new observer about the existing members
            })
            console.log(peer.uuid + " observed room " + this.uuid);
        }
    }

    removeObserver(peer){
        if(this.observers.includes(peer)){
            arrayRemove(this.observers, peer);

            checkPeer(peer);
            arrayRemove(peer.observed, this);

            this.peers.forEach(existing => {
                peer.sendPeerRemoved(existing); // Once the Observer is no longer observing the room, it should no longer see the rooms peers
            });
            console.log(peer.uuid + " stopped observing room " + this.uuid);
        }
        this.checkRoom();
    }

    // Every time a peer or observer leaves, check if the room should still exist
    checkRoom(){
        if(this.peers.length <= 0 && this.observers.length <= 0){
            this.server.removeRoom(this);
        }
    }
}

module.exports = {
    HexRoom
}