using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Voip;
using Ubiq.Messaging;

namespace Ubiq.Avatars
{
    public class VoipAvatar : MonoBehaviour
    {
        public Transform audioSourcePosition;
        public VoipPeerConnection peerConnection { get; private set; }

        private AvatarManager avatarManager;
        private Avatar avatar;
        private VoipPeerConnectionManager peerConnectionManager;

        private VoipAvatar localVoipAvatar;

        private void Start()
        {
            avatarManager = GetComponentInParent<AvatarManager>();
            avatar = GetComponentInParent<Avatars.Avatar>();
            peerConnectionManager = VoipPeerConnectionManager.Find(this);
            if (peerConnectionManager)
            {
                peerConnectionManager.GetPeerConnectionAsync(avatar.Peer.uuid,OnPeerConnection);
            }
        }

        private void OnPeerConnection(VoipPeerConnection peerConnection)
        {
            // If we're very unlucky, this is called after we're destroyed
            if (!enabled)
            {
                return;
            }

            this.peerConnection = peerConnection;
        }

        private void LateUpdate()
        {
            if (!peerConnection || !avatarManager || !avatarManager.LocalAvatar)
            {
                return;
            }

            if (!localVoipAvatar)
            {
                if (avatar && avatar.IsLocal)
                {
                    localVoipAvatar = this;
                }
                else
                {
                    localVoipAvatar = avatarManager.LocalAvatar.GetComponentInChildren<VoipAvatar>();
                }
            }

            if (!localVoipAvatar || localVoipAvatar == this)
            {
                return;
            }

            var source = audioSourcePosition;
            var listener = localVoipAvatar.audioSourcePosition;
            peerConnection.UpdateSpatialization(
                source.position,source.rotation,
                listener.position,listener.rotation);
        }
    }
}
