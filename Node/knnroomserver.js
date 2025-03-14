const { Message, NetworkId, Schema, SerialisedDictionary, Uuid } = require("./ubiq");
const { RoomServer, Room, JoinCode } = require('./rooms');
const { KnnRoom } = require('./knnroom');
const fs = require('fs');
const csv = require('csv-parser');

class KnnRoomServer extends RoomServer {
    constructor() {
        super();
        this.roomGuids = {};
        this.loadRoomGuids();
    }

    loadRoomGuids() {
        const filePath = __dirname + '/../Scalability-Experiment/RoomGuids.csv';
        fs.createReadStream(filePath)
            .pipe(csv())
            .on('data', (row) => {
                this.roomGuids[row.Guid] = parseInt(row.k, 10);
                console.log(row.Guid + " has k = " + this.roomGuids[row.Guid]);
            })
            .on('end', () => {
                console.log('RoomGuids.csv file successfully processed');
            });
    }

    async join(peer, args) {
        var room = null;
        if (args.hasOwnProperty("uuid") && args.uuid != "") {
            // Room join request by uuid
            if (!Uuid.validate(args.uuid)) {
                console.log(peer.uuid + " attempted to join room with uuid " + args.uuid + " but the we were expecting an RFC4122 v4 uuid.");
                peer.sendRejected(args, "Could not join room with uuid " + args.uuid + ". We require an RFC4122 v4 uuid.");
                return;
            }

            // Not a problem if no such room exists - we'll create one
            room = this.roomDatabase.uuid(args.uuid);
        } else if (args.hasOwnProperty("joincode") && args.joincode != "") {
            // Room join request by joincode
            room = this.roomDatabase.joincode(args.joincode);

            if (room === null) {
                console.log(peer.uuid + " attempted to join room with code " + args.joincode + " but no such room exists");
                peer.sendRejected(args, "Could not join room with code " + args.joincode + ". No such room exists.");
                return;
            }
        }

        if (room !== null && peer.room.uuid === room.uuid) {
            console.log(peer.uuid + " attempted to join room with code " + args.joincode + " but peer is already in room");
            return;
        }

        if (room === null) {
            // Otherwise new room requested
            var uuid = "";
            if (args.hasOwnProperty("uuid") && args.uuid != "") {
                // Use specified uuid
                // we're sure it's correctly formatted and isn't already in db
                uuid = args.uuid;
            } else {
                // Create new uuid if none specified
                while (true) {
                    uuid = Uuid.generate();
                    if (this.roomDatabase.uuid(uuid) === null) {
                        break;
                    }
                }
            }
            var joincode = "";
            while (true) {
                joincode = JoinCode();
                if (this.roomDatabase.joincode(joincode) === null) {
                    break;
                }
            }
            var publish = false;
            if (args.hasOwnProperty("publish")) {
                publish = args.publish;
            }
            var name = uuid;
            if (args.hasOwnProperty("name") && args.name.length != 0) {
                name = args.name;
            }

            // Check if the Guid exists in the RoomGuids.csv file
            var k = 256; // Default k value
            var room;
            if (this.roomGuids.hasOwnProperty(uuid)) {
                k = this.roomGuids[uuid];
                room = new KnnRoom(this);
                room.uuid = uuid;
                room.joincode = joincode;
                room.publish = publish;
                room.name = name;
                room.k = k; // Set the k value for the KnnRoom    
                console.log(room.uuid + " created with joincode " + joincode + " and k = " + k);

            }
            // Bots Room (Meeting hall corridor) also needs to be a KnnRoom
            else if (name.includes("Bots Room"))
            {
                k = Object.values(this.roomGuids)[0];
                room = new KnnRoom(this);
                room.uuid = uuid;
                room.joincode = joincode;
                room.publish = publish;
                room.name = name;
                room.k = k; // Set the k value for the KnnRoom    
                console.log(name + " | " + room.uuid + " created with joincode " + joincode + " and k = " + k);
            }
            else {
                room = new Room(this);
                room.uuid = uuid;
                room.joincode = joincode;
                room.publish = publish;
                room.name = name;
                console.log(room.uuid + " created with joincode " + joincode);
            }
            this.roomDatabase.add(room);
            this.emit("create", room);

        }

        if (peer.room.uuid != null) {
            peer.room.removePeer(peer);
        }
        room.addPeer(peer);
    }
}

module.exports = {
    KnnRoomServer
};