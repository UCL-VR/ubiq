using UnityEngine;

namespace Ubiq.Voip.Implementations.JsonHelpers
{
    // Optional helper class to provide SignalingMessage structs, working
    // around a deficiency in Unity's JSONUtility

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
        private struct SignalingMessageInternal
        {
            // Ubiq vars, not required for WebRTC
            public string implementation;
            public bool hasImplementation;

            // IceCandidate
            public string candidate;
            public bool hasCandidate;
            public string sdpMid;
            public bool hasSdpMid;
            public int sdpMLineIndex;
            public bool hasSdpMLineIndex;
            public string usernameFragment;
            public bool hasUsernameFragment;

            // SessionDescription
            public string type;
            public bool hasType;
            public string sdp;
            public bool hasSdp;
        }

        public static SignalingMessage FromJson(string json)
        {
            var inter = JsonUtility.FromJson<SignalingMessageInternal>(json);
            return new SignalingMessage
            {
                implementation = inter.hasImplementation ? inter.implementation : null,
                candidate = inter.hasCandidate ? inter.candidate : null,
                sdpMid = inter.hasSdpMid ? inter.sdpMid : null,
                sdpMLineIndex = inter.hasSdpMLineIndex ? (ushort?)inter.sdpMLineIndex : null,
                usernameFragment = inter.hasUsernameFragment ? inter.usernameFragment : null,
                type = inter.hasType ? inter.type : null,
                sdp = inter.hasSdp ? inter.sdp : null
            };
        }

        public static string ToJson(SignalingMessage message)
        {
            return JsonUtility.ToJson(new SignalingMessageInternal
            {
                implementation = message.implementation,
                hasImplementation = message.implementation != null,
                candidate = message.candidate,
                hasCandidate = message.candidate != null,
                sdpMid = message.sdpMid,
                hasSdpMid = message.sdpMid != null,
                sdpMLineIndex = message.sdpMLineIndex ?? 0,
                hasSdpMLineIndex = message.sdpMLineIndex != null,
                usernameFragment = message.usernameFragment,
                hasUsernameFragment = message.usernameFragment != null,
                type = message.type,
                hasType = message.type != null,
                sdp = message.sdp,
                hasSdp = message.sdp != null
            });
        }
    }

    // Optional helper struct to act as a JSON object, working around a
    // deficiency in Unity's JSONUtility. Do not directly serialize/deserialize
    // this. Use the accompanying helper class.
    public struct SignalingMessage
    {
        // Ubiq vars, not required for WebRTC, ignored by most implementations
        public string implementation;

        // IceCandidate
        public string candidate;
        public string sdpMid;
        public ushort? sdpMLineIndex;
        public string usernameFragment;

        // SessionDescription
        public string type;
        public string sdp;
    }
}