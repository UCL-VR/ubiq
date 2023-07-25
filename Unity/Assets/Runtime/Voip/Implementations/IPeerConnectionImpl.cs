using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip.Implementations
{
    public interface IPeerConnectionContext
    {
        MonoBehaviour behaviour { get; }
        void Send(string json);
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

    [Serializable]
    public struct AudioStats : IEquatable<AudioStats>
    {
        [field: SerializeField] public int sampleCount { get; private set; }
        [field: SerializeField] public float volumeSum { get; private set; }
        [field: SerializeField] public int sampleRate { get; private set; }

        public AudioStats(int sampleCount, float volumeSum, int sampleRate)
        {
            this.sampleCount = sampleCount;
            this.volumeSum = volumeSum;
            this.sampleRate = sampleRate;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioStats stats && Equals(stats);
        }

        public bool Equals(AudioStats other)
        {
            return sampleCount == other.sampleCount &&
                   volumeSum == other.volumeSum &&
                   sampleRate == other.sampleRate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(sampleCount, volumeSum, sampleRate);
        }

        public static bool operator ==(AudioStats left, AudioStats right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AudioStats left, AudioStats right)
        {
            return !(left == right);
        }
    }

    public interface IPeerConnectionImpl : IDisposable
    {
        void Setup(IPeerConnectionContext context,
            bool polite,
            List<IceServerDetails> iceServers,
            Action<AudioStats> playbackStatsPushed,
            Action<AudioStats> recordStatsPushed,
            Action<IceConnectionState> iceConnectionStateChanged,
            Action<PeerConnectionState> peerConnectionStateChanged);

        void ProcessSignalingMessage (string json);

        void UpdateSpatialization (Vector3 sourcePosition,
            Quaternion sourceRotation, Vector3 listenerPosition,
            Quaternion listenerRotation);
    }
}