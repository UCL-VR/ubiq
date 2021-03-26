# WebRtc

Real-time Audio and Video in Ubiq is supported by WebRtc. WebRtc is the media stack supporting RTC (real-time communication) in modern web-browsers. WebRtc allows Ubiq to interoperate with browsers and second-party native applications on multiple platforms.

WebRtc supports everything required for RTC except signalling, by design. The WebRtc native libraries make their own P2P connections using ICE, and drive the audio devices. Ubiq exchanges signalling messages on behalf of the media stack to support this. 

It is possible to recieve audio in Unity, in addition to, or instead of, the native audio device. However this is not recommended as the Unity Audio DSP pipeline has much higher latency than the native media stack.

WebRtc was chosen as it is the only stack that interoperates with web-browsers. It is also the only way to exchange UDP with browsers. The Chromium toolchain handles building to multiple platforms.

It is recommended to use Ubiq's Audio API, rather than work with WebRtc directly.

## Library

The WebRtc library used by Ubiq is based on a [fork](https://github.com/UCL-VR/WebRtc) of [Pixiv's](https://github.com/pixiv) WebRtc C & C# bindings. The fork has been modified with additional API bindings, audio buffers and slight modifications to the WebRtc defaults. See the [fork repo](https://github.com/UCL-VR/WebRtc) for additional details.

## Operation

WebRtc operates using PeerConnection objects. These objects manage the connectivity with one or more other peers. RTC occurs in a P2P fashion, but PeerConnection objects require an out-of-band signalling system to establish their own RTP connections.

While PCs (PeerConnections) can form a one-to-many mesh themselves by fanning-out offer and answer messages, typically they operate in tandem. We assume PCs always form pairs.

# Negotiation

WebRtc peers establish P2P RTP connections to pass audio, video and blob data through a process known as Interactive Connectivity Establishment (ICE). ICE is performed by having an out-of-band signalling system exchange SDP and ICE messages. SDP messages describe what streams a peer would like to send and consume, and in what format. ICE messages test different methods of direct connections that perform NAT and firewall traversal, until a preferred one is found.

The PC API is based on callbacks.

When a track is added or removed from a PC, it raises an event, OnNegotiationNeeded. This signals to the host application that it should initiate an exchange of SDP and ICE messages. The host does this by generating an Offer SDP message, and transmitting it to the counterpart PC. The PCs will themselves generate subsequent messages.

