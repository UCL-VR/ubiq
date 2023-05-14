using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;

namespace Ubiq.Voip.Implementations.Unity
{
    public class PeerConnectionImpl : IPeerConnectionImpl {

        private class SignallingEvent
        {
            public enum Type
            {
                NegotiationNeeded,
                SignallingMessage
            }

            public readonly Type type;
            public readonly SignallingMessage message;

            public SignallingEvent(SignallingMessage message) : this(Type.SignallingMessage,message) { }
            public SignallingEvent(Type type, SignallingMessage message = new SignallingMessage())
            {
                this.type = type;
                this.message = message;
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

        private List<SignallingEvent> events = new List<SignallingEvent>();
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
                    events.Add(new SignallingEvent(SignallingEvent.Type.NegotiationNeeded));
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
            senderAudioSource = microphone.audioSource;

            var senderStream = new MediaStream();
            var senderAudioTrack = new AudioStreamTrack(senderAudioSource);
            peerConnection.AddTrack(senderAudioTrack,senderStream);
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

                if (e.type == SignallingEvent.Type.NegotiationNeeded)
                {
                    var op = peerConnection.SetLocalDescription();
                    yield return op;
                    Send(context,peerConnection.LocalDescription);
                    continue;
                }

                // e.type == Signalling message
                if (e.message.ParseSessionDescription(out var description))
                {
                    ignoreOffer = !polite
                        && description.type == "offer"
                        && peerConnection.SignalingState != RTCSignalingState.Stable;
                    if (ignoreOffer)
                    {
                        continue;
                    }

                    var desc = SessionDescriptionUbiqToPkg(description);
                    var op = peerConnection.SetRemoteDescription(ref desc);
                    yield return op;
                    if (description.type == "offer")
                    {
                        op = peerConnection.SetLocalDescription();
                        yield return op;

                        Send(context,peerConnection.LocalDescription);
                    }
                    continue;
                }

                if (e.message.ParseIceCandidate(out var candidate))
                {
                    if (!ignoreOffer)
                    {
                        peerConnection.AddIceCandidate(IceCandidateUbiqToPkg(candidate));
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

        public void ProcessSignallingMessage (SignallingMessage message)
        {
            events.Add(new SignallingEvent(message));
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

        private static RTCSessionDescription SessionDescriptionUbiqToPkg(SessionDescriptionArgs descriptionArgs)
        {
            var desc = new RTCSessionDescription();
            desc.sdp = descriptionArgs.sdp;
            switch(descriptionArgs.type)
            {
                case SessionDescriptionArgs.TYPE_ANSWER : desc.type = RTCSdpType.Answer; break;
                case SessionDescriptionArgs.TYPE_OFFER : desc.type = RTCSdpType.Offer; break;
                case SessionDescriptionArgs.TYPE_PRANSWER : desc.type = RTCSdpType.Pranswer; break;
                case SessionDescriptionArgs.TYPE_ROLLBACK : desc.type = RTCSdpType.Rollback; break;
            }
            return desc;
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

        private static RTCIceCandidate IceCandidateUbiqToPkg(IceCandidateArgs candidate)
        {
            return new RTCIceCandidate (new RTCIceCandidateInit()
            {
                candidate = candidate.candidate,
                sdpMid = candidate.sdpMid,
                sdpMLineIndex = candidate.sdpMLineIndex
            });
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

        private static void Send(IPeerConnectionContext context, RTCSessionDescription sd)
        {
            var offer = new SessionDescriptionArgs();
            offer.sdp = sd.sdp;
            switch(sd.type)
            {
                case RTCSdpType.Answer : offer.type = SessionDescriptionArgs.TYPE_ANSWER; break;
                case RTCSdpType.Offer : offer.type = SessionDescriptionArgs.TYPE_OFFER; break;
                case RTCSdpType.Pranswer : offer.type = SessionDescriptionArgs.TYPE_PRANSWER; break;
                case RTCSdpType.Rollback : offer.type = SessionDescriptionArgs.TYPE_ROLLBACK; break;
            }
            context.Send(SignallingMessage.FromSessionDescription(offer));
        }

        private static void Send(IPeerConnectionContext context, RTCIceCandidate iceCandidate)
        {
            if (iceCandidate == null || string.IsNullOrEmpty(iceCandidate.Candidate))
            {
                return;
            }

            context.Send(SignallingMessage.FromIceCandidate(new IceCandidateArgs
            {
                candidate = iceCandidate.Candidate,
                sdpMid = iceCandidate.SdpMid,
                sdpMLineIndex = (ushort)iceCandidate.SdpMLineIndex,
                usernameFragment = iceCandidate.UserNameFragment
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