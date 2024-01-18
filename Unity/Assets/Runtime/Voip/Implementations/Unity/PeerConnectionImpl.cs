using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Ubiq.Voip.Implementations;
using Ubiq.Voip.Implementations.JsonHelpers;

namespace Ubiq.Voip.Implementations.Unity
{
    public class PeerConnectionImpl : IPeerConnectionImpl
    {
        private class Event
        {
            public enum Type
            {
                NegotiationNeeded,
                OnIceCandidate,
                SignalingMessage
            }

            public readonly Type type;
            public readonly string json;
            public readonly RTCIceCandidate iceCandidate;

            public Event(string json) : this(Type.SignalingMessage,json) { }
            public Event(RTCIceCandidate iceCandidate) : this(Type.OnIceCandidate,null,iceCandidate) { }
            public Event(Type type, string json = null, RTCIceCandidate iceCandidate = null)
            {
                this.type = type;
                this.json = json;
                this.iceCandidate = iceCandidate;
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
            Other,
        }

        // Unity Peer Connection
        private RTCPeerConnection peerConnection;

        private AudioSource receiverAudioSource;
        private SpatialisationCacheFilter cacheFilter;
        private AudioStatsFilter statsFilter;
        private SpatialisationRestoreFilter restoreFilter;
        private PeerConnectionMicrophone microphone;

        private List<Event> events = new List<Event>();
        private List<Coroutine> coroutinesForCleanup = new List<Coroutine>();
        private List<UnityEngine.Object> objectsForCleanup = new List<UnityEngine.Object>();

        private Implementation otherPeerImplementation = Implementation.Unknown;

        private Context ctx;

        public void Dispose()
        {
            if (ctx.behaviour)
            {
                foreach(var coroutine in coroutinesForCleanup)
                {
                    ctx.behaviour.StopCoroutine(coroutine);
                }
                coroutinesForCleanup.Clear();

                if (microphone)
                {
                    microphone.statsPushed -= PeerConnectionMicrophone_OnStats;
                    microphone.RemoveUser(ctx.gameObject);
                }
            }

            foreach(var obj in objectsForCleanup)
            {
                if (obj)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            objectsForCleanup.Clear();

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

            var configuration = GetConfiguration(iceServers);

            var receiverStream = new MediaStream();
            RequireReceiverAudioSource();
            receiverStream.OnAddTrack += (MediaStreamTrackEvent e) =>
            {
                if (e.Track.Kind != TrackKind.Audio)
                {
                    return;
                }

                // Restore spatialisation for Unity's WebRTC package. First,
                // give the output AudioSource a clip full of 1s (elsewhere),
                // then apply filters in order.
                // 1. Cache the spatialised output
                RequireCacheFilter();
                // 2. Play back WebRTC audio (added through SetTrack)
                receiverAudioSource.SetTrack(e.Track as AudioStreamTrack);
                // 3. (unrelated) Add a filter to gather audio stats
                RequireStatsFilter();
                // 4. Restore spatialisation by multiplying output of 2 with 1
                RequireRestoreFilter();

                receiverAudioSource.Play();
            };

            this.peerConnection = new RTCPeerConnection(ref configuration)
            {
                OnConnectionStateChange = (RTCPeerConnectionState state) =>
                {
                    peerConnectionStateChanged?.Invoke(PeerConnectionStatePkgToUbiq(state));
                },
                OnIceConnectionChange = (RTCIceConnectionState state) =>
                {
                    iceConnectionStateChanged?.Invoke(IceConnectionStatePkgToUbiq(state));
                },
                OnIceCandidate = (RTCIceCandidate candidate) =>
                {
                    events.Add(new Event(candidate));
                },
                OnTrack = (RTCTrackEvent e) =>
                {
                    receiverStream.AddTrack(e.Track);
                },
                OnNegotiationNeeded = () =>
                {
                    events.Add(new Event(Event.Type.NegotiationNeeded));
                },
            };

            coroutinesForCleanup.Add(context.behaviour.StartCoroutine(DoSignaling()));
            coroutinesForCleanup.Add(context.behaviour.StartCoroutine(StartMicrophoneTrack()));
        }

        private IEnumerator StartMicrophoneTrack()
        {
            var manager = ctx.transform.parent.gameObject;

            microphone = manager.GetComponent<PeerConnectionMicrophone>();
            if (!microphone)
            {
                microphone = manager.AddComponent<PeerConnectionMicrophone>();
            }

            yield return microphone.AddUser(ctx.gameObject);
            microphone.statsPushed += PeerConnectionMicrophone_OnStats;
            peerConnection.AddTrack(microphone.audioStreamTrack);
        }

        private void PeerConnectionMicrophone_OnStats(AudioStats stats)
        {
            ctx.recordStatsPushed.Invoke(stats);
        }

        private bool ignoreOffer;

        // Manage all signaling, sending and receiving offers.
        // Attempt to implement 'Perfect Negotiation', but with some changes
        // as we fully finish consuming each signaling message before starting
        // on the next.
        // https://w3c.github.io/webrtc-pc/#example-18
        private IEnumerator DoSignaling()
        {
            while(true)
            {
                if (events.Count == 0)
                {
                    yield return null;
                    continue;
                }

                var e = events[0];
                events.RemoveAt(0);

                if (e.type == Event.Type.OnIceCandidate)
                {
                    Send(ctx.context,e.iceCandidate);
                    continue;
                }

                if (e.type == Event.Type.NegotiationNeeded)
                {
                    var op = peerConnection.SetLocalDescription();
                    yield return op;
                    Send(ctx.context,peerConnection.LocalDescription);
                    continue;
                }

                // e.type == Signaling message
                var msg = SignalingMessageHelper.FromJson(e.json);

                // Id the other implementation as Dotnet requires special treatment
                if (otherPeerImplementation == Implementation.Unknown)
                {
                    otherPeerImplementation = msg.implementation == "dotnet"
                        ? Implementation.Dotnet
                        : Implementation.Other;

                    if (otherPeerImplementation == Implementation.Dotnet)
                    {
                        // If just one of the two peers is dotnet, the
                        // non-dotnet peer always takes on the role of polite
                        // peer as the dotnet implementaton isn't smart enough
                        // to handle rollback
                        ctx.polite = true;
                    }
                }

                if (msg.type != null)
                {
                    ignoreOffer = !ctx.polite
                        && msg.type == "offer"
                        && peerConnection.SignalingState != RTCSignalingState.Stable;
                    if (ignoreOffer)
                    {
                        continue;
                    }

                    var desc = SessionDescriptionUbiqToPkg(msg);
                    var op = peerConnection.SetRemoteDescription(ref desc);
                    yield return op;
                    if (msg.type == "offer")
                    {
                        op = peerConnection.SetLocalDescription();
                        yield return op;

                        Send(ctx.context,peerConnection.LocalDescription);
                    }
                    continue;
                }

                if (msg.candidate != null && !string.IsNullOrWhiteSpace(msg.candidate))
                {
                    if (!ignoreOffer)
                    {
                        peerConnection.AddIceCandidate(IceCandidateUbiqToPkg(msg));
                    }
                    continue;
                }
            }
        }

        private void RequireCacheFilter()
        {
            if (cacheFilter)
            {
                return;
            }

            cacheFilter = ctx.gameObject.AddComponent<SpatialisationCacheFilter>();
            cacheFilter.hideFlags = HideFlags.HideInInspector;
            objectsForCleanup.Add(cacheFilter);
        }

        private void RequireStatsFilter()
        {
            if (statsFilter)
            {
                return;
            }

            statsFilter = ctx.gameObject.AddComponent<AudioStatsFilter>();
            statsFilter.SetStatsPushedCallback(ctx.playbackStatsPushed);
            statsFilter.hideFlags = HideFlags.HideInInspector;
            objectsForCleanup.Add(statsFilter);
        }

        private void RequireRestoreFilter()
        {
            if (restoreFilter)
            {
                return;
            }

            restoreFilter = ctx.gameObject.AddComponent<SpatialisationRestoreFilter>();
            restoreFilter.hideFlags = HideFlags.HideInInspector;
            objectsForCleanup.Add(restoreFilter);
        }

        private void RequireReceiverAudioSource ()
        {
            if (receiverAudioSource)
            {
                return;
            }

            // Setup receive audio source
            receiverAudioSource = ctx.gameObject.AddComponent<AudioSource>();

            receiverAudioSource.spatialize = true;
            receiverAudioSource.spatialBlend = 1.0f;
            receiverAudioSource.loop = true;

            // Use a clip filled with 1s
            // This helps us piggyback on Unity's spatialisation using filters
            var samples = new float[AudioSettings.outputSampleRate];
            for(int i = 0; i < samples.Length; i++)
            {
                samples[i] = 1.0f;
            }
            receiverAudioSource.clip = AudioClip.Create("outputclip",
                samples.Length,
                1,
                AudioSettings.outputSampleRate,
                false);
            receiverAudioSource.clip.SetData(samples,0);

            objectsForCleanup.Add(receiverAudioSource.clip);
            objectsForCleanup.Add(receiverAudioSource);
        }

        public void ProcessSignalingMessage (string json)
        {
            events.Add(new Event(json));
        }

        private static RTCConfiguration GetConfiguration(List<IceServerDetails> iceServers)
        {
            var config = new RTCConfiguration();
            config.iceServers = new RTCIceServer[iceServers.Count];
            for(int i = 0; i < iceServers.Count; i++)
            {
                config.iceServers[i] = IceServerUbiqToPkg(iceServers[i]);
            }
            return config;
        }

        public void UpdateSpatialization(Vector3 sourcePosition, Quaternion sourceRotation, Vector3 listenerPosition, Quaternion listenerRotation)
        {
            var t = ctx.transform;
            t.position = sourcePosition;
            t.rotation = sourceRotation;
        }

        private static RTCIceCandidate IceCandidateUbiqToPkg(SignalingMessage msg)
        {
            return new RTCIceCandidate (new RTCIceCandidateInit()
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex
            });
        }

        private static RTCSessionDescription SessionDescriptionUbiqToPkg(SignalingMessage msg)
        {
            return new RTCSessionDescription{
                sdp = msg.sdp,
                type = StringToSdpType(msg.type)
            };
        }

        private static RTCIceServer IceServerUbiqToPkg(IceServerDetails details)
        {
            if (string.IsNullOrEmpty(details.username) ||
                string.IsNullOrEmpty(details.password))
            {
                return new RTCIceServer {
                    urls = new string[] { details.uri }
                };
            }
            else
            {
                return new RTCIceServer {
                    urls = new string[] { details.uri },
                    username = details.username,
                    credential = details.password,
                    credentialType = RTCIceCredentialType.Password
                };
            }
        }

        private static PeerConnectionState PeerConnectionStatePkgToUbiq(RTCPeerConnectionState state)
        {
            switch(state)
            {
                case RTCPeerConnectionState.Closed : return PeerConnectionState.closed;
                case RTCPeerConnectionState.Failed : return PeerConnectionState.failed;
                case RTCPeerConnectionState.Disconnected : return PeerConnectionState.disconnected;
                case RTCPeerConnectionState.New : return PeerConnectionState.@new;
                case RTCPeerConnectionState.Connecting : return PeerConnectionState.connecting;
                case RTCPeerConnectionState.Connected : return PeerConnectionState.connected;
                default : return PeerConnectionState.failed;
            }
        }

        private static IceConnectionState IceConnectionStatePkgToUbiq(RTCIceConnectionState state)
        {
            switch(state)
            {
                case RTCIceConnectionState.Closed : return IceConnectionState.closed;
                case RTCIceConnectionState.Failed : return IceConnectionState.failed;
                case RTCIceConnectionState.Disconnected : return IceConnectionState.disconnected;
                case RTCIceConnectionState.New : return IceConnectionState.@new;
                case RTCIceConnectionState.Checking : return IceConnectionState.checking;
                case RTCIceConnectionState.Connected : return IceConnectionState.connected;
                case RTCIceConnectionState.Completed : return IceConnectionState.completed;
                default : return IceConnectionState.failed;
            }
        }

        private static string SdpTypeToString(RTCSdpType type)
        {
            switch(type)
            {
                case RTCSdpType.Answer : return "answer";
                case RTCSdpType.Offer : return "offer";
                case RTCSdpType.Pranswer : return "pranswer";
                case RTCSdpType.Rollback : return "rollback";
                default : return null;
            }
        }

        private static RTCSdpType StringToSdpType(string type)
        {
            switch(type)
            {
                case "answer" : return RTCSdpType.Answer;
                case "offer" : return RTCSdpType.Offer;
                case "pranswer" : return RTCSdpType.Pranswer;
                case "rollback" : return RTCSdpType.Rollback;
                default : return RTCSdpType.Offer;
            }
        }

        private static void Send(IPeerConnectionContext context, RTCSessionDescription sd)
        {
            context.Send(SdpMessage.ToJson(new SdpMessage
            (
                type: SdpTypeToString(sd.type),
                sdp: sd.sdp
            )));
        }

        private static void Send(IPeerConnectionContext context, RTCIceCandidate ic)
        {
            context.Send(IceCandidateMessage.ToJson(new IceCandidateMessage
            (
                candidate: ic.Candidate,
                sdpMid: ic.SdpMid,
                sdpMLineIndex: (ushort?)ic.SdpMLineIndex,
                usernameFragment: ic.UserNameFragment
            )));
        }
    }
}