using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Extensions;
using Ubiq.WebRtc;
using UnityEngine;
using Ubiq.Rooms;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    /// <summary>
    /// Shows a warning above the Avatar for a Remote player if an Audio Channel cannot be established.
    /// </summary>
    public class AvatarAudioStatusIndicator : MonoBehaviour
    {
        public Image indicator;

        /// <summary>
        /// The Avatar that this Indicator sits underneath. The Indicator must exist under an Avatar.
        /// </summary>
        private Avatars.Avatar avatar;

        private WebRtcPeerConnection pc;

        private void Awake()
        {
            avatar = GetComponentInParent<Avatars.Avatar>();
        }

        private void Start()
        {
            // Start by finding the WebRtcPeerConnectionManager for the Network Scene. 
            // We will listen to events on this to find when a peer connection has been created to monitor.
            // There may not be a manager in scene if audio is not desired, so be prepared for null references.

            try
            {
                // The NetworkScene is usually found in Start, so no objects will contain direct references to it. Find the manager by searching the graph instead.
                var peerConnectionManager = avatar.AvatarManager.RoomClient.GetClosestComponent<WebRtcPeerConnectionManager>();
                if (peerConnectionManager)
                {
                    peerConnectionManager.OnPeerConnection.AddListener(OnPeerConnection);
                }
            }
            catch (System.NullReferenceException)
            {
                return;
            }
        }

        void OnPeerConnection(WebRtcPeerConnection connection)
        {
            if(avatar.Properties["peer-uuid"] == connection.State.Peer)
            {
                connection.OnStateChanged.AddListener(OnStateChange);
            }
        }

        void OnStateChange(WebRtcPeerConnection.PeerConnectionState state)
        {
            switch (state.ConnectionState)
            {
                case Pixiv.Webrtc.PeerConnectionInterface.IceConnectionState.Disconnected:
                case Pixiv.Webrtc.PeerConnectionInterface.IceConnectionState.Failed:
                case Pixiv.Webrtc.PeerConnectionInterface.IceConnectionState.Closed:
                    indicator.gameObject.SetActive(true);
                    break;
                default:
                    indicator.gameObject.SetActive(false);
                    break;
            }
        }
    }
}