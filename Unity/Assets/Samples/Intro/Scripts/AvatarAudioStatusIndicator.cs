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

            UpdateIndicator(SIPSorcery.Net.RTCIceConnectionState.disconnected);
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
            if (pc == peerConnection || pc.PeerUuid != avatar.Peer.UUID)
            {
                return;
            }

            if (peerConnection)
            {
                peerConnection.OnIceConnectionStateChanged.RemoveListener(PeerConnection_OnIceConnectionStateChanged);
            }

            peerConnection = pc;
            peerConnection.OnIceConnectionStateChanged.AddListener(PeerConnection_OnIceConnectionStateChanged);
        }

        private void PeerConnection_OnIceConnectionStateChanged(SIPSorcery.Net.RTCIceConnectionState state)
        {
            UpdateIndicator(state);
        }

        private void UpdateIndicator (SIPSorcery.Net.RTCIceConnectionState state)
        {
            switch (state)
            {
                case SIPSorcery.Net.RTCIceConnectionState.closed:
                case SIPSorcery.Net.RTCIceConnectionState.failed:
                case SIPSorcery.Net.RTCIceConnectionState.disconnected:
                    indicator.enabled = true;
                    indicator.color = failedColor;
                    break;
                case SIPSorcery.Net.RTCIceConnectionState.@new:
                case SIPSorcery.Net.RTCIceConnectionState.checking:
                    indicator.enabled = true;
                    indicator.color = attemptingColor;
                    break;
                case SIPSorcery.Net.RTCIceConnectionState.connected:
                default:
                    indicator.enabled = false;
                    break;
            }
        }
    }
}