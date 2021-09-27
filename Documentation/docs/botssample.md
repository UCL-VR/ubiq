Bots Sample

The Bots Application Sample shows how to create and use non-player controlled peers.

This Sample introduces two Prefabs: Bot and Bot Peer

A player-controlled Peer's behaviour is the interaction between the user and the network via the interface provided by the application.

It is the same with the Bot and Bot Peer. The Bot itself contains Components to move it around the world, and the Bot Peer represents an 'application' designed for the Bot to interact with.
Putting a Bot in a Bot Peer creates a peer that can connect to a room and act on its own.

*Bot Peer*

**Audio**

The Bot Peer can 'speak' with a pre-recorded audio clip. This is through a Component that takes the place of the Microphone input in a player-controlled Peer.


*Stress Testing*

One of the uses for Bots is stress testing. The Bots application is set up to show this.

The Bots Scene contains a number of Components, and  UI.

**Bots Manager**

The Bots Manager is the Component that actually creates and manages the Bots within a process. The Bot Manager can be controlled programatically. The user will usually control it via the Control Panel (Bot Controller).

**Control Panel**

This implements the UI to allow a user to control a Bot Manager, or set of Bot Managers. This also includes the Camera and Event Manager. The Bots Controller Component is the one that actually drives the Bot Managers. The Control Panel is the UI to this.

**Environment**

To reduce memory overhead, multiple Bots Peers share the same Environment instance, though as the have their own NetworkScene they don't directly interact outside of Ubiq networking.

**Bot Peers**

The Scene includes one Bot Peer instance. The Control Panel can be used to add more.

**Bots Room Peer**

An empty Peer that joins the same room as the Bots Peers. Since the Bot Peers are all programmatically controlled, this Peer provides an entry point into the Room containing the bots to collect data (such as ping times), logs, or just monitor how many Bots are in the room.
A regular Ubiq client - such as the Hello World Sample - can also join the Bots room.


*Controlling Bots*

The Bots Manager is used to spawn new Bot Peer instances. Bot Peers exist in the same application but are completley independent as far as the network, and other peers, are concerned.
The UI in the Bots scene is for the Bots Manager. There is no interface for interacting with the virtual world, VR or otherwise, as there is no Network Scene for players in the Bots example.
Bots can be instructed to join any room however, including those with regular players.

*Command and Control Room*

When the Scene is started, the Control Panel UI will control the local Bot Manager. However, there is a limit to how many Bot Peers a single Unity process can host.

Bot Managers across different Unity processes can work together to control very large numbers of bots.

The Control Panel and Bot Manager are actually two distinct Ubiq Peers. They communicate using Ubiq Messages through a 'Command and Control' Room.

When the Control Panel starts, it creates a new Room and has the local Bot Manager join it. Additional Bot Managers from other processes can join this room too and fall under the control of the Control Panel.

The Command and Control Room can be any Ubiq Room, including the one that the Bots join. In practice though, when doing things like stress testing, the Room should be different, probably even on different servers.

*Servers*

The servers for the Command and Control Room and the Bots Room are set in the Bot Peer prototype, and on the Peers for the Control Panel and Bot Manager. They can be overridden using command line arguments.


**Bots Controls**

The UI has two sets of controls: Common controls and Instance controls.

Common Controls apply to all Bot Managers, and Instance controls apply to just that Bot Manager.

When only one Bot Manager instance is known, the controls behave identically.

As each Peer acts as if it were the only one in the process, each Peer will show all other Peers Avatars. This can create high rendering loads. Avatars can be hidden by toggling the Camera, which changes the Culling Mask. The camera is not completley disabled, as it is needed for the UI.

