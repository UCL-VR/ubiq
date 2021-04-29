# Introduction

Ubiq is built around message-based exchanges on unicast connections between peers (and servers). Messages are variable size with binary payloads. All messages passed by Ubiq have the same header.

## The Medium is the Message

Ubiq delivers individual messages between instances of networked objects (most commonly Unity Components). How messages get from Component A to Component B is the responsiblity of the Messaging Layer, and is abstracted from the developer.

Network Components only recieve messages addressed to them directly, so they can rely on knowing the type of the message by virtue of having recieved it.

The expected programming model is that Components implement send and recieve functionality in the same script, which is also where the format is defined. By the time a message reaches a Component, it is received as the exact raw sequence of bytes sent by the Component's counterpart. Individual components choose the best serialisation method and transmission frequency for their use case.

## Scene Graph as a Bus

Ubiq closely matches Unity's programming model. Since user code is placed in Components in Unity, networked objects in Ubiq are also Unity Components.

Ubiq addresses have two elements: Object Id and Component Id. Object Id is analogous to the GameObject and Component Id is analgous to the Component script Type. Messages are only delivered between instances that have both the same Object Id *and* Component Id.

This model allows complete seperation between the code that spawns and destroys Objects and the Components that make up those Objects. When an Object is created, only one identity needs to be generated and communicated, and instances of Components on different Objects don't need to disambiguate between eachother.

## Fan-Out at the Network Layer

When a Component transmits a message, it is recieved by every other Component with the same address (Object & Component Ids). This is the case regardless of how many instances there are with the same combination. Fanning out - or multicasting - the message is done at the network layer. Individual Components don't know how many counterparts they have.


## Example

To see how these concepts work together, consider the example of an Avatar.

The avatar will have one Object Id, and two Components, one for the Skeletal Animation and one for Eye Gaze. Each Component knows only it's Component Id, which must be distinct to avoid eye gaze messages being routed to the skeletal animation and vice versa. When the Avatar is created, the Avatar Spawner generates a new unqiue identity. It instantiates a prefab locally, and communicates its identity to the other peers, so that they also may instantiate the prefab. This identity becomes the Object Id.

Each peer has an instance of the prefab with the new identity. This identity represents the players avatar. When the avatar changes, e.g. its skeleton is updated with tracking information, the avatars components send messages. These are received at remote peers by the avatar's counterparts, based on them having the same Object Ids. Within the Objects, messages are delivered to the correct Component based on the Component Id.

The Avatar Spawner doesn't know which Components are on the Prefab, so only has to communicate one new identity to its peers. Netiher do the Components don't need to know which Prefab they belong to, or what other Components exist on the Object. They just exchange messages with their direct counterparts.




