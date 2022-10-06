# Introduction

Ubiq is built around exchanging exchanges between instances of networked Components. A message sent by one Component is received by all other similar Components (multicasting). 

This logical behaviour is decoupled from the actual connection architecture, which is built around unicast TCP connections intended to operate over the internet. Messages are variable size with binary payloads. All messages passed by Ubiq have the same header.

## The Medium is the Message

Ubiq delivers individual messages between instances of networked objects (most commonly Unity Components). How messages get from Component A to Component B is the responsiblity of the Messaging Layer, and is abstracted from the developer.

Network Components only recieve messages addressed to them directly, so they can rely on knowing the type of the message by virtue of having recieved it.

The expected programming model is that Components implement send and recieve functionality in the same script, which is also where the format is defined. By the time a message reaches a Component, it is received as the exact raw sequence of bytes sent by the Component's counterpart. Individual components choose the best serialisation method and transmission frequency for their use case.

## Scene Graph as a Bus

Ubiq closely matches Unity's programming model. Since user code is placed in Components in Unity, networked objects in Ubiq are also Unity Components.

Each Component has a Network Id which uniquely identifies that instance on the Peer. The Id is shared by equivalent instances on other Peers.

All Components are associated with one `NetworkScene`. This is the networking equivalent to the root of the scene graph. Components find their Network Scene based on the distance through the scene graph to the closest instance of a `NetworkScene` Component.


## Fan-Out at the Network Layer

When a Component transmits a message, it is recieved by every other Component with the same address (Network Id). This is the case regardless of how many instances there are with the same combination. Fanning out - or multicasting - the message is done at the network layer. Individual Components don't know how many counterparts they have.