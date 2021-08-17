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
                peerConnectionManager.OnPeerConnection.AddListener(OnPeerConnection, true);
            }
        }

        private void OnDestroy()
        {
            if (peerConnectionManager)
            {
                peerConnectionManager.OnPeerConnection.RemoveListener(OnPeerConnection);
            }
        }

        private void OnPeerConnection(VoipPeerConnection peerConnection)
        {
            if (peerConnection.PeerUuid == avatar.Peer.UUID)
            {
                this.peerConnection = peerConnection;
                peerConnection.audioSink.unityAudioSource.spatialBlend = 1.0f;
                sinkTransform = peerConnection.audioSink.transform;
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
