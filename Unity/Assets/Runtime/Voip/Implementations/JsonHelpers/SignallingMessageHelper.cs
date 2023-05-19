using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip.Implementations.JsonHelpers
{
    // Optional helper class to provide SignallingMessage structs, working
    // around a deficiency in Unity's JSONUtility
    public static class SignallingMessageHelper
    {
        private struct SignallingMessageInternal
        {
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

        public static SignallingMessage FromJson(string json)
        {
            var inter = JsonUtility.FromJson<SignallingMessageInternal>(json);
            return new SignallingMessage
            {
                candidate = inter.hasCandidate ? inter.candidate : null,
                sdpMid = inter.hasSdpMid ? inter.sdpMid : null,
                sdpMLineIndex = inter.hasSdpMLineIndex ? (ushort?)inter.sdpMLineIndex : null,
                usernameFragment = inter.hasUsernameFragment ? inter.usernameFragment : null,
                type = inter.hasType ? inter.type : null,
                sdp = inter.hasSdp ? inter.sdp : null
            };
        }

        public static string ToJson(SignallingMessage message)
        {
            return JsonUtility.ToJson(new SignallingMessageInternal
            {
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
    public struct SignallingMessage
    {
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