using System;
using UnityEngine;

namespace Ubiq.Voip.Implementations.JsonHelpers
{
    // Optional helper class to provide SignalingMessage structs, working
    // around JSONUtility's inability to handle null, and to minimise
    // network traffic, allocations and boxing (strings will still allocate).

    // IceCandidate and SessionDescription params work as expected for WebRTC

    // Ubiq params are not required for WebRTC and are instead used to identify
    // the implementation in use. The purpose of this system is to help
    // workaround some issues in the dotnet implementation. To identify itself,
    // an implementation can send a message with the 'implementation' param.
    // This must be the FIRST message sent in the peer connection. All other
    // params in that message will be ignored. If no such message is sent, it is
    // assumed the implementation requires no special workarounds
    public static class SignalingMessageHelper
    {
        private const string NULL_MAGIC_STR = "\0";
        private const int NULL_MAGIC_USHORT = -1;

        private enum Classification
        {
            Implementation,
            IceCandidate,
            Sdp
        }

        [Serializable]
        private struct InternalImplementationMessage
        {
            public Classification cls;
            public string implementation;
        }

        [Serializable]
        private struct InternalIceCandidateMessage
        {
            public Classification cls;
            public string candidate;
            public string sdpMid;
            public int sdpMLineIndex;
            public string usernameFragment;
        }

        [Serializable]
        private struct InternalSdpMessage
        {
            public Classification cls;
            public string type;
            public string sdp;
        }

        [Serializable]
        private struct InternalMessage
        {
            public Classification cls;
            public string implementation;
            public string candidate;
            public string sdpMid;
            public int sdpMLineIndex;
            public string usernameFragment;
            public string type;
            public string sdp;
        }

        public static SignalingMessage FromJson(string json)
        {
            var m = JsonUtility.FromJson<InternalMessage>(json);
            if (m.cls == Classification.Implementation)
            {
                return new SignalingMessage
                (
                    implementation: m.implementation != NULL_MAGIC_STR ? m.implementation : null,
                    candidate: null,
                    sdpMid: null,
                    sdpMLineIndex: null,
                    usernameFragment: null,
                    type: null,
                    sdp: null
                );
            }
            else if (m.cls == Classification.Sdp)
            {
                return new SignalingMessage
                (
                    implementation: null,
                    candidate: null,
                    sdpMid: null,
                    sdpMLineIndex: null,
                    usernameFragment: null,
                    type: m.type != NULL_MAGIC_STR ? m.type : null,
                    sdp: m.sdp != NULL_MAGIC_STR ? m.sdp : null
                );
            }
            else
            {
                return new SignalingMessage
                (
                    implementation: null,
                    candidate: m.candidate != NULL_MAGIC_STR ? m.candidate : null,
                    sdpMid: m.sdpMid != NULL_MAGIC_STR ? m.sdpMid : null,
                    sdpMLineIndex: m.sdpMLineIndex != NULL_MAGIC_USHORT ? (ushort)m.sdpMLineIndex : null,
                    usernameFragment: m.usernameFragment != NULL_MAGIC_STR ? m.usernameFragment : null,
                    type: null,
                    sdp: null
                );
            }
        }

        public static string ToJson(ImplementationMessage message)
        {
            return JsonUtility.ToJson(new InternalImplementationMessage {
                cls = Classification.Implementation,
                implementation = message.implementation ?? NULL_MAGIC_STR
            });
        }

        public static string ToJson(SdpMessage message)
        {
            return JsonUtility.ToJson(new InternalSdpMessage {
                cls = Classification.Sdp,
                type = message.type ?? NULL_MAGIC_STR,
                sdp = message.sdp ?? NULL_MAGIC_STR
            });
        }

        public static string ToJson(IceCandidateMessage message)
        {
            return JsonUtility.ToJson(new InternalIceCandidateMessage {
                cls = Classification.IceCandidate,
                candidate = message.candidate ?? NULL_MAGIC_STR,
                sdpMid = message.sdpMid ?? NULL_MAGIC_STR,
                sdpMLineIndex = message.sdpMLineIndex ?? NULL_MAGIC_USHORT,
                usernameFragment = message.usernameFragment ?? NULL_MAGIC_STR
            });
        }
    }

    // Input struct. Don't directly serialize - use ToJson
    public struct ImplementationMessage : IEquatable<ImplementationMessage>
    {
        public readonly string implementation;

        public ImplementationMessage(string implementation)
        {
            this.implementation = implementation;
        }

        public static string ToJson(ImplementationMessage message)
        {
            return SignalingMessageHelper.ToJson(message);
        }

        public override bool Equals(object obj)
        {
            return obj is ImplementationMessage message && Equals(message);
        }

        public bool Equals(ImplementationMessage other)
        {
            return implementation == other.implementation;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(implementation);
        }

        public static bool operator ==(ImplementationMessage left, ImplementationMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImplementationMessage left, ImplementationMessage right)
        {
            return !(left == right);
        }
    }

    // Input struct. Don't directly serialize - use ToJson
    public struct SdpMessage : IEquatable<SdpMessage>
    {
        public readonly string type;
        public readonly string sdp;

        public SdpMessage(string type, string sdp)
        {
            this.type = type;
            this.sdp = sdp;
        }

        public static string ToJson(SdpMessage message)
        {
            return SignalingMessageHelper.ToJson(message);
        }

        public override bool Equals(object obj)
        {
            return obj is SdpMessage message && Equals(message);
        }

        public bool Equals(SdpMessage other)
        {
            return type == other.type &&
                   sdp == other.sdp;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(type, sdp);
        }

        public static bool operator ==(SdpMessage left, SdpMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SdpMessage left, SdpMessage right)
        {
            return !(left == right);
        }
    }

    // Input struct. Don't directly serialize - use ToJson
    public struct IceCandidateMessage : IEquatable<IceCandidateMessage>
    {
        public readonly string candidate;
        public readonly string sdpMid;
        public readonly ushort? sdpMLineIndex;
        public readonly string usernameFragment;

        public IceCandidateMessage(string candidate, string sdpMid, ushort? sdpMLineIndex, string usernameFragment)
        {
            this.candidate = candidate;
            this.sdpMid = sdpMid;
            this.sdpMLineIndex = sdpMLineIndex;
            this.usernameFragment = usernameFragment;
        }

        public static string ToJson(IceCandidateMessage message)
        {
            return SignalingMessageHelper.ToJson(message);
        }

        public override bool Equals(object obj)
        {
            return obj is IceCandidateMessage message && Equals(message);
        }

        public bool Equals(IceCandidateMessage other)
        {
            return candidate == other.candidate &&
                   sdpMid == other.sdpMid &&
                   sdpMLineIndex == other.sdpMLineIndex &&
                   usernameFragment == other.usernameFragment;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(candidate, sdpMid, sdpMLineIndex, usernameFragment);
        }

        public static bool operator ==(IceCandidateMessage left, IceCandidateMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IceCandidateMessage left, IceCandidateMessage right)
        {
            return !(left == right);
        }
    }

    // Output struct. Intended to provide javascript-like interface
    public struct SignalingMessage : IEquatable<SignalingMessage>
    {
        // Ubiq vars, not required for WebRTC, ignored by most implementations
        public readonly string implementation;

        // SessionDescription
        public readonly string type;
        public readonly string sdp;

        // IceCandidate
        public readonly string candidate;
        public readonly string sdpMid;
        public readonly ushort? sdpMLineIndex;
        public readonly string usernameFragment;

        public SignalingMessage(string implementation, string candidate,
            string sdpMid, ushort? sdpMLineIndex, string usernameFragment,
            string type, string sdp)
        {
            this.implementation = implementation;
            this.candidate = candidate;
            this.sdpMid = sdpMid;
            this.sdpMLineIndex = sdpMLineIndex;
            this.usernameFragment = usernameFragment;
            this.type = type;
            this.sdp = sdp;
        }

        public static SignalingMessage FromJson(string json)
        {
            return SignalingMessageHelper.FromJson(json);
        }

        public override bool Equals(object obj)
        {
            return obj is SignalingMessage message && Equals(message);
        }

        public bool Equals(SignalingMessage other)
        {
            return implementation == other.implementation &&
                   candidate == other.candidate &&
                   sdpMid == other.sdpMid &&
                   sdpMLineIndex == other.sdpMLineIndex &&
                   usernameFragment == other.usernameFragment &&
                   type == other.type &&
                   sdp == other.sdp;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(implementation, candidate, sdpMid,
                sdpMLineIndex, usernameFragment, type, sdp);
        }

        public static bool operator ==(SignalingMessage left, SignalingMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SignalingMessage left, SignalingMessage right)
        {
            return !(left == right);
        }
    }
}