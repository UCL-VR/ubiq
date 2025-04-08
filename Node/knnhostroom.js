const { KnnRoom } = require("./knnroom");

class KnnHostRoom extends KnnRoom{
    constructor(server){
        super(server);
        this.host = null;
    }

   addPeer(peer){
        peer.kNearestPeers = [];
        peer.kNearestPeerTimeouts = {};
        peer.kObservers = [];

        this.peers.push(peer);
        peer.setRoom(this);

        if (this.host == null){
            this.host = peer;
            this.host.kObservers = this.peers.filter(p => p != this.host);
        }
        else 
        {
            this.host.kObservers.push(peer);
            peer.sendPeerAdded(this.host);
            this.host.sendPeerAdded(peer);
        }
    }

    removePeer(peer){
        const index = this.peers.indexOf(peer);
        if (index > -1) {
          this.peers.splice(index, 1);
        }
        peer.clearRoom(); // signal that the leave is complete
        for(var existing of this.peers){
            existing.sendPeerRemoved(peer); // Tell the remaining peers about the missing peer (no check here because the peer was already removed from the list)
            peer.sendPeerRemoved(existing);
        }

        console.log(peer.uuid + " left room " + this.name);
        this.checkRoom();

        if(peer == this.host){
            // randomly pick a new host
            this.host = this.peers[Math.floor(Math.random() * this.peers.length)];
            // add all peers to the kobserver list of the new host
            this.peers.forEach(peer => {
                if (peer == this.host) return;
                
                this.host.kObservers.push(peer);  // The new host now needs to send messages to all peers in the room 
                KnnRoom.removeNearPeerImmediate(peer, this.host); // The new host is no longer interested in receiving messages from its peers
            })
        }
    }

    processMessage(source, message){
        source.kObservers.forEach(other =>{
            other.send(message);
        })
    }

    // This subclass ignores the observer system

    addObserver(peer){
    }

    removeObserver(peer){
    }

    static distance(v1, v2){
        var x_dist = (v1.x - v2.x) * (v1.x - v2.x);
		var y_dist = (v1.y - v2.y) * (v1.y - v2.y);
		var z_dist = (v1.z - v2.z) * (v1.z - v2.z);
        return Math.sqrt( x_dist + y_dist + z_dist);
    }

    async updateNearest(){
        this.peers.forEach(peer =>{
            if (peer == this.host) return;

            this.updateNearestPeers(peer);
        })
    }

    async updateNearestPeers(me){

        // Get a list of other peers closest to this peer

        var peerDistances = [];
        this.peers.forEach(peer => {
            if(peer != me && peer != this.host){
                if(peer.position !== undefined && me.position !== undefined){
                    peerDistances.push({
                        peer: peer,
                        distance: KnnRoom.distance(peer.position, me.position)
                    })
                }
            }
        });

        peerDistances.sort((a,b) =>{
            return a.distance - b.distance;
        });

        var nearest = peerDistances.slice(0, this.k).map(d => d.peer);

        // intersect to find who we have to drop, and who we have to add 

        var added = nearest.filter(x => !me.kNearestPeers.includes(x));
        var removed = me.kNearestPeers.filter(x => !nearest.includes(x));

        // Clear any timeouts on existing peers that may have gone out of range
        // and are now back within the timeout window

        nearest.forEach(peer =>{
            if(me.kNearestPeerTimeouts.hasOwnProperty(peer)){
                clearTimeout(me.kNearestPeerTimeouts[peer]);
                delete me.kNearestPeerTimeouts[peer];
            }
        })

        // For new peers, we can add immediately (meaning the peer count will 
        // actually be slightly larger than k some of the time).

        added.forEach(peer =>{
            this.addNearPeer(me, peer);
        });

        // For peers that have gone out of range, we use a timeout to avoid
        // peers popping in and out at high rate.
        // This method starts the timeout (where needed) to remove the peer
        // some time in the future.
        // This timeout sets the frequency at which peers can come in and out

        removed.forEach(peer =>{
            KnnRoom.removeNearPeerImmediate(me, peer);
        });

        // if(me.kNearestPeers.length > 0 && me.kNearestPeers.length != this.k){
        //     console.log("Unexpected Peer Count");
        // }
    }

    addNearPeer(me, peer){
        me.kNearestPeers.push(peer);
        me.sendPeerAdded(peer);

        // add me to observers of the other peer, so I get its messages even if
        // I am not in its k-nearest group..
        peer.kObservers.push(me);
    }

    removeNearPeer(me, peer){
        if(!me.kNearestPeerTimeouts.hasOwnProperty(peer)){
            me.kNearestPeerTimeouts[peer] = setTimeout(KnnRoom.removeNearPeerImmediate, 500, me, peer);
        }
    }

    static removeNearPeerImmediate(me, peer){
        me.kNearestPeers.splice(me.kNearestPeers.indexOf(peer),1);
        me.sendPeerRemoved(peer); 

        // remove me from observer of the other peer, as I am no longer 
        // interested in it. If it needs my messages, it will be in my observers
        // group.
        peer.kObservers.splice(peer.kObservers.indexOf(me), 1);

        delete me.kNearestPeerTimeouts[peer];
    }
}

module.exports = {
    KnnHostRoom
}