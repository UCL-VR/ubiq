using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using SIPSorcery.Net;
using Ubiq.Voip.Implementations.JsonHelpers;

namespace Ubiq.Voip.Implementations.Dotnet
{
    public class PeerConnectionImpl : IPeerConnectionImpl
    {
        private class Event
        {
            // Ideally we'd let the implementation guide us on when to negotiate
            // with the OnNegotiationNeeded event, but SipSorcery seems to never
            // generate these events, so comment out for now and workaround it
            public enum Type
            {
                // NegotiationNeeded,
                SignalingMessage
            }

            public readonly Type type;
            public readonly string json;

            public Event(string json) : this(Type.SignalingMessage,json) { }
            public Event(Type type, string json = null, RTCIceCandidate iceCandidate = null)
            {
                this.type = type;
                this.json = json;
            }
        }

        private class Context
        {
            public IPeerConnectionContext context;
            public Action<AudioStats> playbackStatsPushed;
            public Action<AudioStats> recordStatsPushed;
            public Action<IceConnectionState> iceConnectionStateChanged;
            public Action<PeerConnectionState> peerConnectionStateChanged;
            public bool polite;

            public MonoBehaviour behaviour => context.behaviour;
            public GameObject gameObject => context.behaviour.gameObject;
            public Transform transform => context.behaviour.transform;
        }

        private enum Implementation
        {
            Unknown,
            Dotnet,
            Other
        }

        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private ConcurrentQueue<Event> events = new ConcurrentQueue<Event>();
        private Task<RTCPeerConnection> setupTask;

        // SipSorcery Peer Connection
        private RTCPeerConnection peerConnection;

        private IVoipSink sink;
        private IVoipSource source;

        private List<Coroutine> coroutinesForCleanup = new List<Coroutine>();

        private Implementation otherPeerImplementation = Implementation.Unknown;
        private List<RTCIceCandidate> bufferedIceCandidates = new List<RTCIceCandidate>();
        private bool hasSentLocalSdp;

        private Context ctx;

        public async void Dispose()
        {
            if (sink != null)
            {
                sink.statsPushed -= Sink_StatsPushed;
            }

            if (source != null)
            {
                source.statsPushed -= Source_StatsPushed;
            }

            if (ctx != null && ctx.behaviour)
            {
                foreach(var coroutine in coroutinesForCleanup)
                {
                    ctx.behaviour.StopCoroutine(coroutine);
                }
                coroutinesForCleanup.Clear();
            }
            coroutinesForCleanup.Clear();

            if (setupTask != null)
            {
                await setupTask.ConfigureAwait(false);
                setupTask.Result.Dispose();
            }

            setupTask = null;
            ctx = null;
        }

        public void Setup(IPeerConnectionContext context,
            bool polite, List<IceServerDetails> iceServers,
            Action<AudioStats> playbackStatsPushed,
            Action<AudioStats> recordStatsPushed,
            Action<IceConnectionState> iceConnectionStateChanged,
            Action<PeerConnectionState> peerConnectionStateChanged)
        {
            if (ctx != null)
            {
                // Already setup
                return;
            }

            ctx = new Context();
            ctx.context = context;
            ctx.polite = polite;
            ctx.playbackStatsPushed = playbackStatsPushed;
            ctx.recordStatsPushed = recordStatsPushed;
            ctx.iceConnectionStateChanged = iceConnectionStateChanged;
            ctx.peerConnectionStateChanged = peerConnectionStateChanged;

            // Copy ice servers before entering multithreaded context
            var iceServersCopy = new List<IceServerDetails>();
            for (int i = 0; i < iceServers.Count; i++)
            {
                iceServersCopy.Add(new IceServerDetails(
                    uri: iceServers[i].uri,
                    username: iceServers[i].username,
                    password: iceServers[i].password));
            }

            RequireSource();
            RequireSink();

            setupTask = Task.Run(() => DoSetup(polite,iceServersCopy));

            coroutinesForCleanup.Add(ctx.behaviour.StartCoroutine(DoUpdate()));
        }

        private void RequireSource ()
        {
            if (source != null)
            {
                return;
            }

            var manager = ctx.transform.parent;

            // First, see if an source already exists among siblings
            source = manager.GetComponentInChildren<IVoipSource>();

            // If not, check if a hint exists and use it
            if (source == null)
            {
                var hint = manager.GetComponent<VoipSourceHint>();
                if (hint && hint.prefab)
                {
                    var go = GameObject.Instantiate(hint.prefab);
                    go.transform.parent = manager;
                    source = go.GetComponentInChildren<IVoipSource>();
                }
            }

            // If still nothing, use default
            if (source == null)
            {
                var go = new GameObject("Microphone Dotnet Voip Source");
                go.transform.parent = manager;
                source = go.AddComponent<MicrophoneVoipSource>();
            }

            source.statsPushed += Source_StatsPushed;
        }

        private void RequireSink ()
        {
            if (sink != null)
            {
                return;
            }

            var manager = ctx.transform.parent;

            // First, check if a hint exists and use it
            var hint = manager.GetComponent<VoipSinkHint>();
            if (hint && hint.prefab)
            {
                var go = GameObject.Instantiate(hint.prefab);
                go.transform.parent = ctx.transform;
                sink = go.GetComponentInChildren<IVoipSink>();
            }

            // If still nothing, use default
            if (sink == null)
            {
                var go = new GameObject("Dotnet Voip Output");
                go.transform.parent = ctx.transform;
                sink = go.AddComponent<AudioSourceVoipSink>();
            }

            sink.statsPushed += Sink_StatsPushed;
        }

        public void ProcessSignalingMessage (string json)
        {
            events.Enqueue(new Event(json));
        }

        private void Source_StatsPushed (AudioStats stats)
        {
            ctx.recordStatsPushed?.Invoke(stats);
        }

        private void Sink_StatsPushed (AudioStats stats)
        {
            ctx.playbackStatsPushed?.Invoke(stats);
        }

        private RTCPeerConnection DoSetup(bool polite,
            List<IceServerDetails> iceServers)
        {
            // Convert to sipsorcery ice server format
            var ssIceServers = new List<RTCIceServer>();
            for (int i = 0; i < iceServers.Count; i++)
            {
                if (string.IsNullOrEmpty(iceServers[i].username) ||
                    string.IsNullOrEmpty(iceServers[i].password))
                {
                    ssIceServers.Add(new RTCIceServer {
                        urls = iceServers[i].uri
                    });
                }
                else
                {
                    ssIceServers.Add(new RTCIceServer {
                        urls = iceServers[i].uri,
                        username = iceServers[i].username,
                        credential = iceServers[i].password,
                        credentialType = RTCIceCredentialType.password
                    });
                }
            }

            var pc = new RTCPeerConnection(new RTCConfiguration
            {
                iceServers = ssIceServers,
            });

            pc.OnAudioFormatsNegotiated += (formats) =>
            {
                source.SetAudioSourceFormat(formats[0]);
                sink.SetAudioSinkFormat(formats[0]);
            };

            pc.onconnectionstatechange += (state) =>
            {
                mainThreadActions.Enqueue(() =>
                {
                    switch(state)
                    {
                        case RTCPeerConnectionState.closed : ctx.peerConnectionStateChanged?.Invoke(PeerConnectionState.closed); break;
                        case RTCPeerConnectionState.failed : ctx.peerConnectionStateChanged?.Invoke(PeerConnectionState.failed); break;
                        case RTCPeerConnectionState.disconnected : ctx.peerConnectionStateChanged?.Invoke(PeerConnectionState.disconnected); break;
                        case RTCPeerConnectionState.@new : ctx.peerConnectionStateChanged?.Invoke(PeerConnectionState.@new); break;
                        case RTCPeerConnectionState.connecting : ctx.peerConnectionStateChanged?.Invoke(PeerConnectionState.connecting); break;
                        case RTCPeerConnectionState.connected : ctx.peerConnectionStateChanged?.Invoke(PeerConnectionState.connected); break;
                    }
                });

                if (state == RTCPeerConnectionState.connected)
                {
                    if (pc.HasAudio)
                    {
                        source.OnAudioSourceEncodedSample += pc.SendAudio;
                    }
                }
                else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                {
                    source.OnAudioSourceEncodedSample -= pc.SendAudio;
                }
            };

            // Ideally we'd let the implementation guide us on when to negotiate
            // with the OnNegotiationNeeded event, but SipSorcery seems to never
            // generate these events, so comment out for now and workaround it
            // pc.onnegotiationneeded += () =>
            // {
            //     events.Enqueue(new Event(Event.Type.NegotiationNeeded));
            // };

            pc.onicecandidate += (iceCandidate) =>
            {
                mainThreadActions.Enqueue(() => Send(iceCandidate));
            };

            pc.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt) =>
            {
                if (media == SDPMediaTypesEnum.audio)
                {
                    sink.GotAudioRtp(rep, rtpPkt.Header.SyncSource, rtpPkt.Header.SequenceNumber, rtpPkt.Header.Timestamp, rtpPkt.Header.PayloadType, rtpPkt.Header.MarkerBit == 1, rtpPkt.Payload);
                }
            };

            pc.oniceconnectionstatechange += (state) => mainThreadActions.Enqueue(() =>
            {
                switch(state)
                {
                    case RTCIceConnectionState.closed : ctx.iceConnectionStateChanged?.Invoke(IceConnectionState.closed); break;
                    case RTCIceConnectionState.failed : ctx.iceConnectionStateChanged?.Invoke(IceConnectionState.failed); break;
                    case RTCIceConnectionState.disconnected : ctx.iceConnectionStateChanged?.Invoke(IceConnectionState.disconnected); break;
                    case RTCIceConnectionState.@new : ctx.iceConnectionStateChanged?.Invoke(IceConnectionState.@new); break;
                    case RTCIceConnectionState.checking : ctx.iceConnectionStateChanged?.Invoke(IceConnectionState.checking); break;
                    case RTCIceConnectionState.connected : ctx.iceConnectionStateChanged?.Invoke(IceConnectionState.connected); break;
                }
            });

            pc.addTrack(new MediaStreamTrack(source.GetAudioSourceFormats()));

            return pc;
        }

        private IEnumerator DoUpdate()
        {
            while(setupTask == null || !setupTask.IsCompleted)
            {
                yield return null;
            }

            peerConnection = setupTask.Result;

            // Send id message as this implementation needs special workarounds
            Send(implementation:"dotnet");

            var time = Time.realtimeSinceStartup;
            while(true)
            {
                while (mainThreadActions.TryDequeue(out Action action))
                {
                    action();
                }

                while (events.TryDequeue(out Event ev))
                {
                    yield return HandleSignalingEvent(ev);
                }

                yield return null;
            }
        }

        private bool ignoreOffer;

        // Manage all signaling, sending and receiving offers.
        // Attempt to implement 'Perfect Negotiation', but with some changes
        // as we fully finish consuming each signaling message before starting
        // on the next.
        // https://w3c.github.io/webrtc-pc/#example-18
        private IEnumerator HandleSignalingEvent(Event e)
        {
            // e.type == Signaling message
            var msg = SignalingMessage.FromJson(e.json);

            // Id the other implementation as Dotnet requires special treatment
            if (otherPeerImplementation == Implementation.Unknown)
            {
                otherPeerImplementation = msg.implementation == "dotnet"
                    ? Implementation.Dotnet
                    : Implementation.Other;

                if (otherPeerImplementation == Implementation.Dotnet && !ctx.polite)
                {
                    yield return SetLocalDescription(peerConnection);
                    Send(peerConnection.localDescription);
                    yield break;
                }
                else
                {
                    // If just one of the two peers is dotnet, the
                    // non-dotnet peer always takes on the role of polite
                    // peer as the dotnet implementaton isn't smart enough
                    // to handle rollback
                    ctx.polite = false;
                }
            }

            if (msg.type != null)
            {
                ignoreOffer = !ctx.polite
                    && msg.type == "offer"
                    && !(peerConnection.signalingState == RTCSignalingState.stable
                        || peerConnection.signalingState == RTCSignalingState.closed);
                if (ignoreOffer)
                {
                    yield break;
                }

                var desc = SessionDescriptionUbiqToPkg(msg);
                var result = peerConnection.setRemoteDescription(desc);
                if (msg.type == "offer")
                {
                    yield return SetLocalDescription(peerConnection);
                    Send(peerConnection.localDescription);
                }
                yield break;
            }

            if (msg.candidate != null && !string.IsNullOrWhiteSpace(msg.candidate))
            {
                if (!ignoreOffer)
                {
                    peerConnection.addIceCandidate(IceCandidateUbiqToPkg(msg));
                }
                yield break;
            }
        }

        private void Send(string implementation)
        {
            ctx.context.Send(ImplementationMessage.ToJson(new ImplementationMessage
            (
                implementation: implementation
            )));
        }

        private void Send(RTCIceCandidate ic)
        {
            if (hasSentLocalSdp)
            {
                InternalSend(ctx.context,ic);
            }
            else
            {
                bufferedIceCandidates.Add(ic);
            }
        }

        private void Send(RTCSessionDescription sd)
        {
            InternalSend(ctx.context,sd);
            hasSentLocalSdp = true;
            while (bufferedIceCandidates.Count > 0)
            {
                var ic = bufferedIceCandidates[0];
                bufferedIceCandidates.RemoveAt(0);
                InternalSend(ctx.context,ic);
            }
        }

        private static IEnumerator SetLocalDescription(RTCPeerConnection pc)
        {
            // Should just be able to call setLocalDescription() here
            // and have the implementation generate an offer or an answer
            // as appropriate but SipSorcery doesn't implement the spec
            // completely. Instead need to call createOffer() or
            // createAnswer() depending on SignalingState, and then
            // setLocalDescription().
            // https://w3c.github.io/webrtc-pc/#dom-peerconnection-setlocaldescription
            RTCSessionDescriptionInit sd = null;
            if (pc.signalingState == RTCSignalingState.closed
                || pc.signalingState == RTCSignalingState.stable
                || pc.signalingState == RTCSignalingState.have_local_offer
                || pc.signalingState == RTCSignalingState.have_remote_pranswer)
            {
                sd = pc.createOffer();
            }
            else
            {
                sd = pc.createAnswer();
            }
            var op = Task.Run(() => pc.setLocalDescription(sd));
            yield return new WaitUntil(() => op.IsCompleted);
        }

        private static string SdpTypeToString(RTCSdpType type)
        {
            switch(type)
            {
                case RTCSdpType.answer : return "answer";
                case RTCSdpType.offer : return "offer";
                case RTCSdpType.pranswer : return "pranswer";
                case RTCSdpType.rollback : return "rollback";
                default : return null;
            }
        }

        private static RTCSdpType StringToSdpType(string type)
        {
            switch(type)
            {
                case "answer" : return RTCSdpType.answer;
                case "offer" : return RTCSdpType.offer;
                case "pranswer" : return RTCSdpType.pranswer;
                case "rollback" : return RTCSdpType.rollback;
                default : return RTCSdpType.offer;
            }
        }

        private static RTCIceCandidateInit IceCandidateUbiqToPkg(SignalingMessage msg)
        {
            return new RTCIceCandidateInit()
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex ?? 0,
                usernameFragment = msg.usernameFragment
            };
        }

        private static RTCSessionDescriptionInit SessionDescriptionUbiqToPkg(SignalingMessage msg)
        {
            return new RTCSessionDescriptionInit
            {
                sdp = msg.sdp,
                type = StringToSdpType(msg.type)
            };
        }

        private static void InternalSend(IPeerConnectionContext context, RTCSessionDescription sd)
        {
            context.Send(SdpMessage.ToJson(new SdpMessage
            (
                sdp: sd.sdp.RawString(),
                type: SdpTypeToString(sd.type)
            )));
        }

        private static void InternalSend(IPeerConnectionContext context, RTCIceCandidate ic)
        {
            context.Send(IceCandidateMessage.ToJson(new IceCandidateMessage
            (
                candidate: $"candidate:{ic.candidate}",
                sdpMid: ic.sdpMid,
                sdpMLineIndex: (ushort?)ic.sdpMLineIndex,
                usernameFragment: ic.usernameFragment
            )));
        }

        public void UpdateSpatialization(Vector3 sourcePosition, Quaternion sourceRotation, Vector3 listenerPosition, Quaternion listenerRotation)
        {
            if (sink != null)
            {
                sink.UpdateSpatialization(sourcePosition,sourceRotation,
                    listenerPosition,listenerRotation);
            }
        }
    }
}