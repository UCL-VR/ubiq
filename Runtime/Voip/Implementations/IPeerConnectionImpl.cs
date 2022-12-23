using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip.Implementations
{
    public enum IceConnectionState
    {
        closed = 0,
        failed = 1,
        disconnected = 2,
        @new = 3,
        checking = 4,
        connected = 5
    }

    public enum PeerConnectionState
    {
        closed = 0,
        failed = 1,
        disconnected = 2,
        @new = 3,
        connecting = 4,
        connected = 5
    }

    [Serializable]
    public class IceServerDetails
    {
        public string uri;
        public string username;
        public string password;

        public IceServerDetails(string uri, string username, string password)
        {
            this.uri = uri;
            this.username = username;
            this.password = password;
        }
    }

    public enum MediaFormat
    {
        PCMU = 0,
        GSM = 3,
        G723 = 4,
        DVI4 = 5,
        DVI4_16K = 6,
        LPC = 7,
        PCMA = 8,
        G722 = 9,
        L16_2 = 10,
        L16 = 11,
        QCELP = 12,
        CN = 13,
        MPA = 14,
        G728 = 15,
        DVI4_11K = 16,
        DVI4_22K = 17,
        G729 = 18,
        CELB = 24,
        JPEG = 26,
        NV = 28,
        H261 = 31,
        MPV = 32,
        MP2T = 33,
        H263 = 34
    }

    // public struct Stats
    // {
    //     public float volume;
    //     public int samples;
    //     public int sampleRate;
    // }

    // public interface IAudioStats
    // {
    //     Stats lastFrameStats  {get; }
    // }

    public interface IOutputVolume
    {
        float Volume { get; set; }
    }

    // public enum MediaStreamStatus
    // {
    //     Inactive,
    //     RecvOnly,
    //     SendOnly,
    //     SendRecv
    // }

    // public interface IPeerConnection
    // {
    //     void Setup()
    //     void AddTrack(List<MediaFormat> formats,MediaStreamStatus streamStatus);
    //     event Action<List<MediaFormat>> AudioFormatsNegotiated;
    // }

    [System.Serializable]
    public struct SessionDescriptionArgs
    {
        public const string TYPE_ANSWER = "answer";
        public const string TYPE_OFFER = "offer";
        public const string TYPE_PRANSWER = "pranswer";
        public const string TYPE_ROLLBACK = "rollback";

        public string type;
        public string sdp;
    }

    [System.Serializable]
    public struct IceCandidateArgs
    {
        public string candidate;
        public string sdpMid;
        public ushort sdpMLineIndex;
        public string usernameFragment;
    }

    // public class MessageBus
    // {
    //     public delegate void MessageDelegate(Message message);
    //     public event MessageDelegate onMessage;

    //     public void Send(Message message)
    //     {
    //         onMessage(message);
    //     }
    // }

    [System.Serializable]
    public struct SignallingMessage
    {
        public enum Type
        {
            SessionDescription = 0,
            IceCandidate = 1
        }

        public Type type;
        public string args;

        public static SignallingMessage FromSessionDescription(SessionDescriptionArgs args)
        {
            return new SignallingMessage
            {
                type = Type.SessionDescription,
                args = JsonUtility.ToJson(args)
            };
        }

        public static SignallingMessage FromIceCandidate(IceCandidateArgs args)
        {
            return new SignallingMessage
            {
                type = Type.IceCandidate,
                args = JsonUtility.ToJson(args)
            };
        }

        public bool ParseSessionDescription(out SessionDescriptionArgs offer)
        {
            if (type != Type.SessionDescription)
            {
                offer = new SessionDescriptionArgs();
                return false;
            }
            try
            {
                offer = JsonUtility.FromJson<SessionDescriptionArgs>(args);
                return true;
            }
            catch
            {
                offer = new SessionDescriptionArgs();
                return false;
            }
        }

        public bool ParseIceCandidate(out IceCandidateArgs iceCandidate)
        {
            if (type != Type.IceCandidate)
            {
                iceCandidate = new IceCandidateArgs();
                return false;
            }
            try
            {
                iceCandidate = JsonUtility.FromJson<IceCandidateArgs>(args);
                return true;
            }
            catch
            {
                iceCandidate = new IceCandidateArgs();
                return false;
            }
        }
    }

    // public abstract class PeerConnection : MonoBehaviour
    // {
    //     public IAudioSink audioSink { get; protected set; }
    //     public IAudioSource audioSource { get; protected set; }
    //     public string peerUuid { get; protected set; }
    //     public IceConnectionState iceConnectionState { get; protected set; } = IceConnectionState.@new;
    //     public PeerConnectionState peerConnectionState { get; protected set; } = PeerConnectionState.@new;
    //     [Serializable] public class IceConnectionStateEvent : UnityEvent<IceConnectionState> { }
    //     [Serializable] public class PeerConnectionStateEvent : UnityEvent<PeerConnectionState> { }
    //     public IceConnectionStateEvent OnIceConnectionStateChanged = new IceConnectionStateEvent();
    //     public PeerConnectionStateEvent OnPeerConnectionStateChanged = new PeerConnectionStateEvent();

    //     public abstract void Setup (NetworkId objectId, string peerUuid, bool polite, IAudioSink sink, IAudioSource source);
    //     public abstract void Teardown();
    // }

    public struct PlaybackStats
    {
        public int sampleCount;
        public float volumeSum;
        public int sampleRate;
    }

    public delegate void MessageEmittedDelegate(SignallingMessage message);
    public delegate void IceConnectionStateChangedDelegate(IceConnectionState state);
    public delegate void PeerConnectionStateChangedDelegate(PeerConnectionState state);

    public interface IPeerConnectionImpl : IDisposable
    {
        event MessageEmittedDelegate signallingMessageEmitted;
        event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        event PeerConnectionStateChangedDelegate peerConnectionStateChanged;

        void Setup(MonoBehaviour context,
            bool polite,
            List<IceServerDetails> iceServers);

        void ProcessSignallingMessage (SignallingMessage message);

        void UpdateSpatialization (Vector3 sourcePosition,
            Quaternion sourceRotation, Vector3 listenerPosition,
            Quaternion listenerRotation);

        PlaybackStats GetLastFramePlaybackStats();
    }

        // delegate void EncodedSampleDelegate(uint durationRtpUnits, byte[] sample);

        // event EncodedSampleDelegate AudioSourceEncodedSample;

        // Action CloseAudio();
        // // void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample);
        // List<AudioFormat> GetAudioSourceFormats();
        // bool HasEncodedAudioSubscribers();
        // bool IsAudioSourcePaused();
        // Action PauseAudio();
        // void RestrictFormats(Func<AudioFormat, bool> filter);
        // Action ResumeAudio();
        // void SetAudioSourceFormat(AudioFormat audioFormat);
        // Action StartAudio();


        // event SourceErrorDelegate OnAudioSinkError;

        // Action CloseAudioSink();
        // List<AudioFormat> GetAudioSinkFormats();
        // void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload);
        // Action PauseAudioSink();
        // void RestrictFormats(Func<AudioFormat, bool> filter);
        // Action ResumeAudioSink();
        // void SetAudioSinkFormat(AudioFormat audioFormat);
        // Action StartAudioSink();
}