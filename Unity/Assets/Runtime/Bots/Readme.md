# Bots

The Bots Sample shows how NPCs can join and participate in Rooms. One use case is for stress testing.

The Bots Application Sample contains two scenes, one showing a minimal working example of a Bot, and the other a full interface for generating multiple bots and measuring performance with them running.

## Bots

The Bots scene demonstrates a computer controlled peer in local loopback. The Bot Peer is almost identical to the regular Player Peer, as would be seen in the Hello World sample.

The main difference is the Bot Peer does not have a Player Controller. Instead, a Bot prefab has a set of GameObjects representing Avatar targets that are controlled programmatically. These, and the other Bot behaviour, are controlled by a set of Components attached to the Bot GameObject.

Additionally, the Bot Prefab overrides the Audio Inputs and Outputs on the VoipManager. Instead of transmitting the microphone, the Bot transmits an Audio file on repeat. The Bot receives audio as well, but anything it hears is discarded instead of playing out to the speakers.

When the Scene runs, the Network Scene Prefab operates as usual; an Avatar is created but instead of finding the GameObjects controlled by the Player Controller, it finds those controlled by the Bot. The Bot uses a NavMesh to walk randomly around the environment.

In the Bots scene the user must click the Join All RoomClients button on the Server GameObject to join both the Player Peer and Bot Peer.
Though, a Component could easily be written to have the Bot join a particular Room through its RoomClient.

## Bots Server

The Bots Server scene shows how one process could manage a number of Bot peers. 

This Scene includes one initial Bot Peer, and a Bots Manager object. The Bots Manager object can be used to add additional Bots. 

The Bot Peer prefabs the manager creates are the same as local loopback _forests_. That is, each instance is an independent Peer. The NavMesh Agents re-use the same Environment Prefab for performance however.

The Bots Manager Component has a Unity UI, allowing the process to run outside the Editor. It also has a CLI interface, allowing it to run Headless.

These interfaces allow multiple Bots Server Processes to run on one computer, to add many bots to a scene.

## Profiling

The first Bot in Bots Scene includes a Performance Monitor prefab. This Prefab is added only to the first bot. It includes a Controller Component, a Log Collector and Throughouput Monitor. 

These Components allow measuring the latency to all other Peers in a room, and the Throughput at the first Bot. 

The measurements are sent via the Logging System. By default, the local Log Collector will recieve them, but they could also be sent to a remote Peer.
