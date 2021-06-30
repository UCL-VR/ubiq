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

        private Avatar avatar;
        private Transform sinkTransform;

        private void Awake()
        {
            avatar = GetComponent<Avatars.Avatar>();
        }

        private void Start()
        {
            var manager = NetworkScene.FindNetworkScene(this).GetComponentInChildren<VoipPeerConnectionManager>();
            if (manager)
            {
                manager.OnPeerConnection.AddListener(OnPeerConnection);
            }
        }

        private void OnPeerConnection(VoipPeerConnection peerConnection)
        {
            if (peerConnection.PeerUuid == avatar.Peer.UUID)
            {
                var sink = peerConnection.audioSink;
                sink.unityAudioSource.spatialBlend = 1.0f;
                sinkTransform = sink.transform;
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
