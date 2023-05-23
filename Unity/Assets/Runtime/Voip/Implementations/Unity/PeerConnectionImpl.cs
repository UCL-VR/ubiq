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
                SignallingMessage
            }

            public readonly Type type;
            public readonly string json;

            public Event(string json) : this(Type.SignallingMessage,json) { }
            public Event(Type type, string json = null)
            {
                this.type = type;
                this.json = json;
            }
        }

        public event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        public event PeerConnectionStateChangedDelegate peerConnectionStateChanged;

        // Unity Peer Connection
        private RTCPeerConnection peerConnection;

        private IPeerConnectionContext context;

        private AudioSource receiverAudioSource;
        private AudioSource senderAudioSource;
        private PeerConnectionMicrophone microphone;

        private bool polite;

        private List<Event> events = new List<Event>();
        private List<Coroutine> coroutines = new List<Coroutine>();

        public void Dispose()
        {
            if (context.behaviour)
            {
                foreach(var coroutine in coroutines)
                {
                    if (context.behaviour)
                    {
                        context.behaviour.StopCoroutine(coroutine);
                    }
                }

                microphone.RemoveUser(context.behaviour.gameObject);
            }
            coroutines.Clear();
        }

        public void Setup(IPeerConnectionContext context,
            bool polite, List<IceServerDetails> iceServers)
        {
            if (this.context != null)
            {
                // Already setup
                return;
            }

            this.context = context;
            this.polite = polite;

            var configuration = GetConfiguration(iceServers);

            var receiverStream = new MediaStream();
            RequireReceiverAudioSource();
            receiverStream.OnAddTrack += (MediaStreamTrackEvent e) =>
            {
                RequireComponent<SpatialisationCacheAudioFilter>(context.behaviour.gameObject);
                receiverAudioSource.SetTrack(e.Track as AudioStreamTrack);
                RequireComponent<SpatialisationRestoreAudioFilter>(context.behaviour.gameObject);
                receiverAudioSource.loop = true;
                receiverAudioSource.Play();
            };

            this.peerConnection = new RTCPeerConnection(ref configuration)
            {
                OnConnectionStateChange = (RTCPeerConnectionState state) =>
                {
                    peerConnectionStateChanged(PeerConnectionStatePkgToUbiq(state));
                    Debug.Log($"Peer connection state change to {state}.");
                },
                OnIceConnectionChange = (RTCIceConnectionState state) =>
                {
                    iceConnectionStateChanged(IceConnectionStatePkgToUbiq(state));
                    Debug.Log($"Ice connection state change to {state}.");
                },
                OnIceCandidate = (RTCIceCandidate candidate) =>
                {
                    Send(context,candidate);
                },
                OnTrack = (RTCTrackEvent e) =>
                {
                    receiverStream.AddTrack(e.Track);
                },
                OnNegotiationNeeded = () =>
                {
                    events.Add(new Event(Event.Type.NegotiationNeeded));
                },
                OnIceGatheringStateChange = (RTCIceGatheringState state) =>
                {
                    Debug.Log($"Ice gathering state change to {state}.");
                }
            };

            coroutines.Add(context.behaviour.StartCoroutine(DoSignalling()));
            coroutines.Add(context.behaviour.StartCoroutine(StartMicrophoneTrack()));

            // peerConnection.AddTrack(senderAudioTrack,senderStream);

            // Diagnostics.
            // pc.OnReceiveReport += (re, media, rr) => mainThreadActions.Enqueue(
            //     () => Debug.Log($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}"));
            // pc.OnSendReport += (media, sr) => mainThreadActions.Enqueue(
            //     () => Debug.Log($"RTCP Send for {media}\n{sr.GetDebugSummary()}"));
            // pc.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => mainThreadActions.Enqueue(
            //     () => Debug.Log($"STUN {msg.Header.MessageType} received from {ep}."));
        }

        private IEnumerator StartMicrophoneTrack()
        {
            var manager = context.behaviour.transform.parent.gameObject;

            microphone = manager.GetComponent<PeerConnectionMicrophone>();
            if (!microphone)
            {
                microphone = manager.AddComponent<PeerConnectionMicrophone>();
            }

            yield return microphone.AddUser(context.behaviour.gameObject);
            peerConnection.AddTrack(microphone.audioStreamTrack);
        }

        private bool ignoreOffer;

        // Manage all signalling, sending and receiving offers.
        // Attempt to implement 'Perfect Negotiation', but with some changes
        // as we fully finish consuming each signalling message before starting
        // on the next.
        // https://w3c.github.io/webrtc-pc/#example-18
        private IEnumerator DoSignalling()
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

                if (e.type == Event.Type.NegotiationNeeded)
                {
                    var op = peerConnection.SetLocalDescription();
                    yield return op;
                    Send(context,peerConnection.LocalDescription);
                    continue;
                }

                // e.type == Signalling message
                var msg = SignallingMessageHelper.FromJson(e.json);
                if (msg.type != null)
                {
                    ignoreOffer = !polite
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

                        Send(context,peerConnection.LocalDescription);
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

        private void RequireReceiverAudioSource ()
        {
            if (receiverAudioSource)
            {
                return;
            }

            // Setup receive audio source
            receiverAudioSource = context.behaviour.gameObject.AddComponent<AudioSource>();

            receiverAudioSource.spatialize = true;
            receiverAudioSource.spatialBlend = 1.0f;

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
        }

        public void ProcessSignallingMessage (string json)
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
            context.behaviour.transform.position = sourcePosition;
            context.behaviour.transform.rotation = sourceRotation;
        }

        public PlaybackStats GetLastFramePlaybackStats ()
        {
            // if (sink != null)
            // {
            //     return sink.GetLastFramePlaybackStats();
            // }
            // else
            // {
                return new PlaybackStats();
            // }
        }

        private static RTCIceCandidate IceCandidateUbiqToPkg(SignallingMessage msg)
        {
            Debug.Log($"candidate: {msg.candidate}, sdpMid: {msg.sdpMid}, sdpMLineIndex: {msg.sdpMLineIndex}");

            return new RTCIceCandidate (new RTCIceCandidateInit()
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex
            });
        }

        private static RTCSessionDescription SessionDescriptionUbiqToPkg(SignallingMessage msg)
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
            context.Send(SignallingMessageHelper.ToJson(new SignallingMessage{
                sdp = sd.sdp,
                type = SdpTypeToString(sd.type)
            }));
        }

        private static void Send(IPeerConnectionContext context, RTCIceCandidate ic)
        {
            context.Send(SignallingMessageHelper.ToJson(new SignallingMessage{
                candidate = ic.Candidate,
                sdpMid = ic.SdpMid,
                sdpMLineIndex = (ushort?)ic.SdpMLineIndex,
                usernameFragment = ic.UserNameFragment
            }));
        }

        private static T RequireComponent<T>(GameObject gameObject) where T : MonoBehaviour
        {
            var c = gameObject.GetComponent<T>();
            if (c)
            {
                return c;
            }
            return gameObject.AddComponent<T>();
        }
    }
}