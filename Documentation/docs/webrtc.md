# WebRTC and VOIP

Real-time Audio in Ubiq is supported by WebRTC. WebRTC is the media stack supporting RTC (real-time communication) in modern web-browsers. WebRTC allows Ubiq to interoperate with browsers and second-party native applications on multiple platforms.

WebRTC supports everything required for RTC except signalling, by design. The WebRTC native libraries make their own P2P connections using ICE, and drive the audio devices. Ubiq exchanges signalling messages on behalf of the media stack to support this.

WebRTC was chosen as it is the only stack that interoperates with web-browsers. It is also the only way to exchange UDP with browsers.

It is recommended to use Ubiq's Audio API, rather than work with WebRTC directly.

## Library

Ubiq currently uses SIPSorcery's .NET WebRTC [library](https://github.com/sipsorcery-org/sipsorcery). Unlike typical WebRTC implementations, this library does not include a full media stack.  Ubiq provides a simple audio endpoint which integrates with the Unity audio system, allowing for microphone input and spatialised audio output.

## Operation

WebRTC operates using PeerConnection objects. These objects manage the connectivity with one or more other peers. RTC occurs in a P2P fashion, but PeerConnection objects require an out-of-band signalling system to establish their own RTP connections.

While PCs (PeerConnections) can form a one-to-many mesh themselves by fanning-out offer and answer messages, typically they operate in tandem. We assume PCs always form pairs.

# Negotiation

WebRTC peers establish P2P RTP connections to pass audio, video and blob data through a process known as Interactive Connectivity Establishment (ICE). ICE is performed by having an out-of-band signalling system exchange SDP and ICE messages. SDP messages describe what streams a peer would like to send and consume, and in what format. ICE messages test different methods of direct connections that perform NAT and firewall traversal, until a preferred one is found.

The PC API is based on callbacks.

When a track is added or removed from a PC, it raises an event, OnNegotiationNeeded. This signals to the host application that it should initiate an exchange of SDP and ICE messages. The host does this by generating an Offer SDP message, and transmitting it to the counterpart PC. The PCs will themselves generate subsequent messages.

