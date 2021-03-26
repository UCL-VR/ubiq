# Servers and Rooms

Ubiq clients can connect directly to eachother, but most applications will use a room server.

The room server is a central service which clients connect to and use to rendezvous with eachother.
A server can host multiple *Rooms*. Rooms are a collection of users in a scene, who can talk to eachother and exchange messages. The `RoomClient` is used to find, join and leave rooms. 

The RoomClient must be provided with a server to connect to. The VECG team runs a public server, `nexus.cs.ucl.ac.uk`, or you can set up your own.

`RoomClient` will connect to the server in the `Default Server` property on start-up, but you must join a room before you can communicate with other users. This can be done in the Editor through the button in the Inspector, through the RoomClient's API or through the example UI.