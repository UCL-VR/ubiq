using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip.Implementations
{
    public interface IPeerConnectionContext
    {
        MonoBehaviour behaviour { get; }
        void Send(SignallingMessage message);
    }

    public enum IceConnectionState
    {
        closed = 0,
        failed = 1,
        disconnected = 2,
        @new = 3,
        checking = 4,
        connected = 5,
        completed = 6
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

    public enum PeerSignallingState : int
    {
        stable = 0,
        have_local_offer = 1,
        have_remote_offer = 2,
        have_local_pranswer = 3,
        have_remote_pranswer = 4,
        closed = 5
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

    public interface IOutputVolume
    {
        float Volume { get; set; }
    }

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

        public bool ParseSessionDescription(out SessionDescriptionArgs description)
        {
            if (type != Type.SessionDescription)
            {
                description = new SessionDescriptionArgs();
                return false;
            }
            try
            {
                description = JsonUtility.FromJson<SessionDescriptionArgs>(args);
                return true;
            }
            catch
            {
                description = new SessionDescriptionArgs();
                return false;
            }
        }

        public bool ParseIceCandidate(out IceCandidateArgs candidate)
        {
            if (type != Type.IceCandidate)
            {
                candidate = new IceCandidateArgs();
                return false;
            }
            try
            {
                candidate = JsonUtility.FromJson<IceCandidateArgs>(args);
                return true;
            }
            catch
            {
                candidate = new IceCandidateArgs();
                return false;
            }
        }
    }

    public struct PlaybackStats
    {
        public int sampleCount;
        public float volumeSum;
        public int sampleRate;
    }

    public delegate void IceConnectionStateChangedDelegate(IceConnectionState state);
    public delegate void PeerConnectionStateChangedDelegate(PeerConnectionState state);

    public interface IPeerConnectionImpl : IDisposable
    {
        event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        event PeerConnectionStateChangedDelegate peerConnectionStateChanged;

        void Setup(IPeerConnectionContext context, bool polite,
            List<IceServerDetails> iceServers);

        void ProcessSignallingMessage (SignallingMessage message);

        void UpdateSpatialization (Vector3 sourcePosition,
            Quaternion sourceRotation, Vector3 listenerPosition,
            Quaternion listenerRotation);

        PlaybackStats GetLastFramePlaybackStats();
    }
}