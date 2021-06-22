using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Voip;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(Avatars.Avatar))]
    public class VoipAvatar : MonoBehaviour
    {
        public Transform audioSourcePosition;

        private Avatars.Avatar avatar;
        private Transform sinkTransform;

        private void Awake()
        {
            avatar = GetComponent<Avatars.Avatar>();
        }

        private void Start()
        {
            var peerConnectionManager = avatar.AvatarManager.RoomClient.
                GetComponentInChildren<VoipPeerConnectionManager>();
            if (peerConnectionManager)
            {
                peerConnectionManager.OnPeerConnection.AddListener(OnPeerConnection);
            }
        }

        private void OnPeerConnection(VoipPeerConnection peerConnection)
        {
            if (peerConnection.PeerUuid == avatar.Properties["peer-uuid"])
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
