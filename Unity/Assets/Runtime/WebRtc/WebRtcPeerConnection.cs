using Pixiv.Cricket;
using Pixiv.Webrtc;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Ubiq.Logging;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.WebRtc
{
    public interface IWebRtcSource
    {
        string StreamID { get; }
        void Close();
    }

    public interface IWebRtcSink
    {
        string StreamID { get; }
    }

    public class WebRtcBinding
    {
        public string binding;
    }

    public class WebRtcAudioBinding : WebRtcBinding
    {
        public IAudioTrackSinkInterface sink;
        public IAudioTrackInterface track;
    }

    /// <summary>
    /// Manages a WebRTC PeerConnection. See the WebRTC native API documentation for how to use the library (https://webrtc.github.io/webrtc-org/native-code/native-apis/) with this class.
    /// Signalling is through the Ubiq (WebRtcPeerConnection implements INetworkComponent, and expects to register for a NetworkContext)
    /// </summary>
    [NetworkComponentId(typeof(WebRtcPeerConnection), 7)]
    public class WebRtcPeerConnection : MonoBehaviour, IManagedPeerConnectionObserver, INetworkComponent, INetworkObject
    {
        public NetworkId Id { get; set; }

        private WebRtcPeerConnectionFactory factory;
        private DisposablePeerConnectionInterface pc;
        private ConcurrentQueue<Action> unityJobs;
        private List<Pixiv.Webrtc.Interop.AudioSourceInterface> audioInterfaces = new List<Pixiv.Webrtc.Interop.AudioSourceInterface>();
        private EventLogger debug;

        public float volume = 1;
        private float _volume = 1; // default gain in webrtc source

        [NonSerialized]
        public float volumeModifier;

        private NetworkContext context;

        private bool polite = false;
        private bool hasRemotePeer = false;

        public bool IsReady { get { return pc != null; } }

        public struct PeerConnectionState
        {
            public string Peer;
            public string LastMessageReceived;
            public bool HasRemote;
            /// <summary>
            /// https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection/signalingState
            /// </summary>
            public volatile PeerConnectionInterface.SignalingState SignalingState;
            /// <summary>
            /// https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection/iceConnectionState
            /// </summary>
            public volatile PeerConnectionInterface.IceConnectionState ConnectionState;
            /// <summary>
            /// https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection/iceGatheringState
            /// </summary>
            public volatile PeerConnectionInterface.IceGatheringState IceState;

            public override string ToString()
            {
                return $"Peer: {Peer}\nHasRemote: {HasRemote}\n{SignalingState},{ConnectionState},{IceState}";
            }
        }

        public PeerConnectionState State;

        private volatile PeerConnectionInterface.SignalingState signallingState;

        [Serializable]
        public class StateChangedEvent : UnityEvent<PeerConnectionState> 
        {
            public new void AddListener(UnityAction<PeerConnectionState> call)
            {
                base.AddListener(call);
                if(connection)
                {
                    call(connection.State);
                }
            }

            protected WebRtcPeerConnection connection;

            public void SetConnection(WebRtcPeerConnection connection)
            {
                this.connection = connection;
                Invoke(connection.State);
            }
        }
        public StateChangedEvent OnStateChanged;

        private void RaiseStateChange()
        {
            OnMainThread(() => 
            { 
                OnStateChanged.Invoke(State); 
            });
        }

        [Serializable]
        public struct Message
        {
            public string type;
            public string args;
        }

        [Serializable]
        public class AnnouncementMessage
        {
            public bool acknowledged; // Whether the sender of the message has received an announcement from the receiver

            public AnnouncementMessage(bool hasRemotePeer)
            {
                acknowledged = hasRemotePeer;
            }
        }

        [Serializable]
        public class SessionDescriptionMessage
        {
            public string type;
            public string sdp;
        }

        [Serializable]
        public class IceCandidateMessage
        {
            public string sdpMid;
            public int sdpMlineIndex;
            public string candidate;

            public IceCandidateMessage(IceCandidateInterface candidate)
            {
                candidate.TryToString(out this.candidate);
                this.sdpMid = candidate.SdpMid();
                this.sdpMlineIndex = candidate.SdpMlineIndex();
            }
        }

        public void MakePolite()
        {
            polite = true;
        }

        private void Send<T>(string type, T args)
        {
            context.SendJson(new Message()
            {
                type = type,
                args = JsonUtility.ToJson(args)
            });
        }

        /// <summary>
        /// Sends a message after the Remote PeerConnection has announced it has finished its initialisation
        /// </summary>
        private void SendOnRemotePeer<T>(string type, T args)
        {
            OnMainThread(() =>
            {
                OnRemotePeer(() =>
                {
                    Send(type, args);
                });
            });
        }

        private void Awake()
        {
            if (OnStateChanged == null)
            {
                OnStateChanged = new StateChangedEvent();
            }
            OnStateChanged.SetConnection(this);
            unityJobs = new ConcurrentQueue<Action>();
            volumeModifier = 1f;
        }

        void Start()
        {
            context = NetworkScene.Register(this);
            debug = new ContextEventLogger(context);

            factory = GetComponentInParent<WebRtcPeerConnectionFactory>();
            if (factory == null)
            {
                factory = GetComponentInParent<NetworkScene>().gameObject.AddComponent<WebRtcPeerConnectionFactory>();
            }

            // A single PeerConnection attaches to one other client, and transports any number of video and audio tracks.
            // Once the PeerConnection is created, tracks can be added and removed dynamically.
            // The re-negotiation needed event should prompt the exchange of SDP and ICE Candidate messages.

            factory.GetRtcConfiguration(config =>
            {
                pc = factory.CreatePeerConnection(config, this);
            });

            OnPeerConnection(SendAnnouncement);
        }

        private void OnMainThread(Action Action)
        {
            unityJobs.Enqueue(Action);
        }

        /// <summary>
        /// Calls OnPcCreated after the underlying Peer Connection has been made.
        /// </summary>
        public void OnPeerConnection(Action OnPcCreated)
        {
            if (pc != null)
            {
                OnPcCreated();
            }
            else
            {
                StartCoroutine(WaitForPeerConnection(OnPcCreated));
            }
        }

        private IEnumerator WaitForPeerConnection(Action OnPcCreated)
        {
            while (pc == null)
            {
                yield return null;
            }
            OnPcCreated();
        }

        /// <summary>
        /// Calls OnRemoteCreated when the counterpart Peer Connection has been made.
        /// </summary>
        /// <remarks>
        /// Naive use of this method assumes PeerConnections are dyads. This is not enforced technically however; do not create three-way PeerConnection objects with the same Id without handling this case explicitly.
        /// </remarks>
        public void OnRemotePeer(Action OnRemote)
        {
            if (hasRemotePeer)
            {
                OnRemote();
            }
            else
            {
                StartCoroutine(WaitForRemotePeerConnection(OnRemote));
            }
        }

        private IEnumerator WaitForRemotePeerConnection(Action OnPcCreated)
        {
            while (!hasRemotePeer)
            {
                yield return null;
            }
            OnPcCreated();
        }

        public void AddLocalAudioSource()
        {
            OnPeerConnection(() =>
            {
                debug.Log("AddLocalAudioSource");
                var audiosource = factory.CreateAudioSource();
                var audiotrack = factory.CreateAudioTrack("localAudioSource", audiosource);
                pc.AddTrack(audiotrack, new[] { "localAudioSource" });
            });
        }

        public void CreateDataChannel(string label, string protocol, int id, bool ordered, int maxRetransmitTime, Action<DisposableDataChannelInterface> OnDataChannel)
        {
            OnPeerConnection(() =>
            {
                OnDataChannel(pc.CreateDataChannel(label, id, maxRetransmitTime, ordered, protocol));
            });
        }

        // for now the unity-based audio and video are unsupported. support will be added back when we have a good and stable native audio experience.
        // support for video is the priority, followed by unity audio sinks for functional processing (i.e. not in the dsp pipeline)
        // then support for unity as an audio source, and audio device

        public void AddSource(IWebRtcSource source)
        {
            throw new NotImplementedException();
        }

        public void AddSink(IWebRtcSink sink)
        {
            throw new NotImplementedException();
        }

        private void Update()
        {
            lock (audioInterfaces)
            {
                foreach (var item in audioInterfaces)
                {
                    var newvolume = Mathf.Clamp(volume * volumeModifier, 0, 10);
                    if (_volume != newvolume)
                    {
                        _volume = newvolume;
                        item.SetVolume(_volume);
                    }
                }
            }

            Action action;
            while (unityJobs.TryDequeue(out action))
            {
                action();
            }
        }

        public void OnRenegotiationNeeded()
        {
            OnPeerConnection(() => // This may be called externally, before the pc is created
            {
                pc.CreateOffer(new DisposableCreateSessionDescriptionObserver(new CreateSessionDescriptionObserver((offer) =>
                {
                    if (signallingState != PeerConnectionInterface.SignalingState.Stable)
                    {
                        return;
                    }

                    var message = CreateSessionDescriptionMessage(offer);
                    pc.SetLocalDescription(new DisposableSetSessionDescriptionObserver(new SetSessionDecsriptionObserver(debug)), offer);

                    debug.Log("SendOffer", polite);
                    SendOnRemotePeer("description", message);
                },
                debug)), new PeerConnectionInterface.RtcOfferAnswerOptions());
                RaiseStateChange();
            });
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
        {
            try
            {
                OnRtcMessage(data.FromJson<Message>());
            }
            catch (Exception e)
            {
                debug.Log("Exception", e.Message);
                return;
            }
        }

        /// <summary>
        /// Receive an RTC signalling message. If this is an offer, create an answer and transmit it. If if is an answer to an offer created elsewhere, all we have to do is set it.
        /// </summary>
        public void OnRtcMessage(Message message)
        {
            State.LastMessageReceived = DateTime.Now.ToShortTimeString();

            if (message.type == "description")
            {
                var sessionDescription = JsonUtility.FromJson<SessionDescriptionMessage>(message.args);
                if (sessionDescription.type == "offer")
                {
                    debug.Log("ReceivedOffer");
                    if (signallingState != PeerConnectionInterface.SignalingState.Stable)
                    {
                        if (!polite)
                        {
                            // Ignore the other peer's offer. Our offer will take precedence over theirs and we can expect an answer soon.
                            debug.Log("IgnoredOffer");
                            return;
                        }
                        else
                        {
                            debug.Log("Rollback");
                            pc.SetLocalDescription(new DisposableSetSessionDescriptionObserver(new SetSessionDecsriptionObserver(() =>
                            {
                                debug.Log("AcceptOffer");
                                pc.SetRemoteDescription(new DisposableSetSessionDescriptionObserver(new SetSessionDecsriptionObserver(CreateAnswer, debug)), SessionDescription.Create(SdpType.Offer, sessionDescription.sdp, IntPtr.Zero));
                            }, debug
                            )), SessionDescription.Create(SdpType.Rollback, "", IntPtr.Zero));

                        }
                    }
                    else
                    {
                        debug.Log("AcceptOffer");
                        pc.SetRemoteDescription(new DisposableSetSessionDescriptionObserver(new SetSessionDecsriptionObserver(CreateAnswer, debug)), SessionDescription.Create(SdpType.Offer, sessionDescription.sdp, IntPtr.Zero));
                    }
                }
                else // desc.type == "answer"
                {
                    debug.Log("ReceivedAnswer");
                    pc.SetRemoteDescription(new DisposableSetSessionDescriptionObserver(new SetSessionDecsriptionObserver(debug)), SessionDescription.Create(SdpType.Answer, sessionDescription.sdp, IntPtr.Zero));
                }
            }

            if (message.type == "icecandidate")
            {
                debug.Log("ReceivedIceCandidate", message.args);

                if (message.args == "null")
                {
                    return;
                }
                if (message.args == "")
                {
                    return;
                }

                var desc = JsonUtility.FromJson<IceCandidateMessage>(message.args);

                if (desc.candidate == "")
                {
                    return;
                }

                using (var candidate = IceCandidate.Create(desc.sdpMid, desc.sdpMlineIndex, desc.candidate, IntPtr.Zero))
                {
                    pc.AddIceCandidate(candidate);
                }
            }

            if (message.type == "announcement")
            {
                debug.Log("ReceivedAnnouncement", hasRemotePeer, message.args, true);
                hasRemotePeer = true;
                var args = JsonUtility.FromJson<AnnouncementMessage>(message.args);
                if (!args.acknowledged)
                {
                    SendAnnouncement();
                }
                State.HasRemote = hasRemotePeer;
                RaiseStateChange();
            }
        }

        private void CreateAnswer()
        {
            pc.CreateAnswer(new DisposableCreateSessionDescriptionObserver(new CreateSessionDescriptionObserver((answer) =>
            {
                var message = CreateSessionDescriptionMessage(answer);
                pc.SetLocalDescription(new DisposableSetSessionDescriptionObserver(new SetSessionDecsriptionObserver(debug)), answer);
                SendOnRemotePeer("description", message);
                debug.Log("SendAnswer");
            },
            debug)),
            new PeerConnectionInterface.RtcOfferAnswerOptions());
        }

        private SessionDescriptionMessage CreateSessionDescriptionMessage(DisposableSessionDescriptionInterface desc)
        {
            SessionDescriptionMessage message = new SessionDescriptionMessage();
            switch (desc.GetSdpType())
            {
                case SdpType.Answer:
                    message.type = "answer";
                    break;
                case SdpType.Offer:
                    message.type = "offer";
                    break;
                default:
                    message.type = "answer";
                    break;
            }
            desc.TryToString(out message.sdp);  // note that the setlocaldescription call will dispose of desc. this might be a bug. in any case call before setlocaldescription
            return message;
        }

        /// <summary>
        /// Called when a new Track has been added by the other peer.
        /// </summary>
        public void OnTrack(DisposableRtpTransceiverInterface transceiver)
        {
            using (transceiver)
            {
                using (var receiver = transceiver.Receiver())
                {
                    using (var track = receiver.Track())
                    {
                        if (track is IAudioTrackInterface)
                        {
                            lock (audioInterfaces)
                            {
                                audioInterfaces.Add((track as IAudioTrackInterface).GetSource());
                            }
                        }
                    }
                }
            }
        }

        public void OnAddStream(DisposableMediaStreamInterface stream)
        {
        }

        public void OnAddTrack(DisposableRtpReceiverInterface receiver, DisposableMediaStreamInterface[] streams)
        {
        }

        public void OnConnectionChange()
        {
            RaiseStateChange();
        }

        public void OnDataChannel(DisposableDataChannelInterface dataChannel)
        {
            unityJobs.Enqueue(() =>
            {
                foreach (var item in GetComponentsInChildren<WebRtcDataChannel>())
                {
                    if (item.Matches(dataChannel))
                    {
                        item.OnDataChannelCreated(dataChannel);
                        return;
                    }
                }
            });
        }

        public void OnIceCandidate(IceCandidateInterface candidate)
        {
            SendOnRemotePeer("icecandidate", new IceCandidateMessage(candidate));
            debug.Log("SendIceCandidate");
        }

        public void OnIceCandidatesRemoved(DisposableCandidate[] candidates)
        {
            debug.Log("OnIceCandidatesRemoved");
        }

        public void OnIceConnectionChange(PeerConnectionInterface.IceConnectionState newState)
        {
            debug.Log("OnIceConnectionChange", newState);
            State.ConnectionState = newState;
            RaiseStateChange();
        }

        public void OnIceConnectionReceivingChange(bool receiving)
        {
            debug.Log("OnIceConnectionReceivingChange", receiving);
            RaiseStateChange();
        }

        public void OnIceGatheringChange(PeerConnectionInterface.IceGatheringState newState)
        {
            debug.Log("OnIceGatheringChange", newState);
            State.IceState = newState;
            RaiseStateChange();
        }

        public void OnInterestingUsage(int usagePattern)
        {
            debug.Log("OnInterestingUsage", usagePattern);
            RaiseStateChange();
        }

        public void OnRemoveStream(DisposableMediaStreamInterface stream)
        {
            debug.Log("OnRemoveStream");
        }

        public void OnRemoveTrack(DisposableRtpReceiverInterface receiver)
        {
            debug.Log("OnRemoveTrack");
        }

        public void OnSignalingChange(PeerConnectionInterface.SignalingState newState)
        {
            debug.Log("OnSignalingChange", newState);
            signallingState = newState;
            RaiseStateChange();
        }

        public void OnStandardizedIceConnectionChange(PeerConnectionInterface.IceConnectionState newState)
        {
            debug.Log("OnStandardizedIceConnectionChange", newState);
            State.ConnectionState = newState;
            RaiseStateChange();
        }

        private void SendAnnouncement()
        {
            debug.Log("SendingAnnoucement", hasRemotePeer);
            Send("announcement", new AnnouncementMessage(hasRemotePeer));
        }

        private void OnDestroy()
        {
            pc.Dispose(); // this is for if the connection is destroyed early (e.g. someone leaves the room). pc.dispose() should still be called in the factory to ensure correct order of disposal.
        }

        public void Dispose()
        {
            pc.Dispose();
        }
    }

    public class CreateSessionDescriptionObserver : IManagedCreateSessionDescriptionObserver
    {
        private EventLogger debug;

        CreateSessionDescriptionObserver(EventLogger debug)
        {
            this.debug = debug;
            this._OnSuccess = (a) => { };
        }

        public CreateSessionDescriptionObserver(Action<DisposableSessionDescriptionInterface> OnSuccess, EventLogger debug) :this(debug)
        {
            this._OnSuccess = OnSuccess;
        }

        private Action<DisposableSessionDescriptionInterface> _OnSuccess;

        public void OnFailure(RtcError error)
        {
            debug.Log(error.Message);
        }

        public void OnSuccess(DisposableSessionDescriptionInterface desc)
        {
            _OnSuccess(desc);
        }
    }


    public class SetSessionDecsriptionObserver : IManagedSetSessionDescriptionObserver
    {
        private EventLogger debug;

        public SetSessionDecsriptionObserver(EventLogger debug)
        {
            this.debug = debug;
            this._OnSuccess = () => { };
        }

        public SetSessionDecsriptionObserver(Action OnSuccess, EventLogger debug):this(debug)
        {
            this._OnSuccess = OnSuccess;
        }

        private Action _OnSuccess;

        public void OnSuccess()
        {
            _OnSuccess();
        }

        public void OnFailure(RtcError error)
        {
            debug.Log(error.Message);
        }
    }
}