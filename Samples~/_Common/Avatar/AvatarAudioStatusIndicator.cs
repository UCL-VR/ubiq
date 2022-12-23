using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Extensions;
using Ubiq.Voip;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace Ubiq.Samples
{
    /// <summary>
    /// Shows a warning above the Avatar for a Remote player if an Audio Channel cannot be established.
    /// </summary>
    public class AvatarAudioStatusIndicator : MonoBehaviour
    {
        public Color attemptingColor = new Color(1.0f,0.4f,0,1.0f); // Orange-ish
        public Color failedColor = Color.red;
        public Image indicator;

        private Avatars.Avatar avatar;
        private VoipPeerConnectionManager peerConnectionManager;
        private VoipPeerConnection peerConnection;

        private void Start()
        {
            avatar = GetComponentInParent<Avatars.Avatar>();

            if (!avatar || avatar.IsLocal)
            {
                indicator.enabled = false;
                return;
            }

            peerConnectionManager = GetComponentInParent<NetworkScene>()?.
                GetComponentInChildren<VoipPeerConnectionManager>();

            if (peerConnectionManager == null || !peerConnectionManager)
            {
                indicator.enabled = false;
                return;
            }

            UpdateIndicator(VoipPeerConnection.IceConnectionState.@new);
            peerConnectionManager.OnPeerConnection.AddListener(
                PeerConnectionManager_OnPeerConnection,runExisting:true);
        }

        private void OnDestroy()
        {
            if (peerConnection)
            {
                peerConnection.OnIceConnectionStateChanged.RemoveListener(PeerConnection_OnIceConnectionStateChanged);
            }

            if (peerConnectionManager)
            {
                peerConnectionManager.OnPeerConnection.RemoveListener(PeerConnectionManager_OnPeerConnection);
            }
        }

        private void PeerConnectionManager_OnPeerConnection(VoipPeerConnection pc)
        {
            if (pc == peerConnection || pc.peerUuid != avatar.Peer.uuid)
            {
                return;
            }

            if (peerConnection)
            {
                peerConnection.OnIceConnectionStateChanged.RemoveListener(PeerConnection_OnIceConnectionStateChanged);
            }

            peerConnection = pc;
            UpdateIndicator(peerConnection.iceConnectionState);
            peerConnection.OnIceConnectionStateChanged.AddListener(PeerConnection_OnIceConnectionStateChanged);
        }

        private void PeerConnection_OnIceConnectionStateChanged(VoipPeerConnection.IceConnectionState state)
        {
            UpdateIndicator(state);
        }

        private void UpdateIndicator (VoipPeerConnection.IceConnectionState state)
        {
            switch (state)
            {
                case VoipPeerConnection.IceConnectionState.closed:
                case VoipPeerConnection.IceConnectionState.failed:
                case VoipPeerConnection.IceConnectionState.disconnected:
                    indicator.enabled = true;
                    indicator.color = failedColor;
                    break;
                case VoipPeerConnection.IceConnectionState.@new:
                case VoipPeerConnection.IceConnectionState.checking:
                    indicator.enabled = true;
                    indicator.color = attemptingColor;
                    break;
                case VoipPeerConnection.IceConnectionState.connected:
                default:
                    indicator.enabled = false;
                    break;
            }
        }
    }
}