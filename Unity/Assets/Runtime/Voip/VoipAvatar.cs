using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Voip;
using Ubiq.Messaging;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(Avatar))]
    public class VoipAvatar : MonoBehaviour
    {
        public Transform audioSourcePosition;
        public VoipPeerConnection peerConnection { get; private set; }

        private Avatar avatar;
        private Transform sinkTransform;
        private VoipPeerConnectionManager peerConnectionManager;

        private void Awake()
        {
            avatar = GetComponent<Avatars.Avatar>();
        }

        private void Start()
        {
            peerConnectionManager = NetworkScene.FindNetworkScene(this).
                GetComponentInChildren<VoipPeerConnectionManager>();
            if (peerConnectionManager)
            {
                peerConnectionManager.OnPeerConnectionAdded.AddListener(OnPeerConnection, true);
            }
        }

        private void OnDestroy()
        {
            if (peerConnectionManager)
            {
                peerConnectionManager.OnPeerConnectionAdded.RemoveListener(OnPeerConnection);
            }
        }

        private void OnPeerConnection(VoipPeerConnection peerConnection)
        {
            if (peerConnection.PeerUuid == avatar.Peer.UUID)
            {
                var sink = peerConnection.audioSink;
                if (sink is VoipAudioSourceOutput)
                {
                    var voipOutput = sink as VoipAudioSourceOutput;
                    voipOutput.unityAudioSource.spatialBlend = 1.0f;
                    sinkTransform = voipOutput.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (sinkTransform)
            {
                sinkTransform.position = audioSourcePosition.position;
            }
        }
    }
}
