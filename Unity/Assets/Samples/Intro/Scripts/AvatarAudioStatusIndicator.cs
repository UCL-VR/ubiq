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
        public Button indicator;

        /// <summary>
        /// The Avatar that this Indicator sits underneath. The Indicator must exist under an Avatar.
        /// </summary>
        private Avatars.Avatar avatar;
        private Text messageBox;

        private void Awake()
        {
            avatar = GetComponentInParent<Avatars.Avatar>();
            messageBox = indicator.GetComponentInChildren<Text>();
        }

        private void Start()
        {
            if (avatar.IsLocal)
            {
                indicator.gameObject.SetActive(false);
                return;
            }

            VoipPeerConnectionManager.GetPeerConnectionAsync(this, avatar.Peer.UUID, pc =>
            {
                pc.OnIceConnectionStateChanged.AddListener(OnStateChange);
            });
        }

        void OnStateChange(SIPSorcery.Net.RTCIceConnectionState state)
        {
            if (this)
            {
                switch (state)
                {
                    case SIPSorcery.Net.RTCIceConnectionState.connected:
                        indicator.gameObject.SetActive(false);
                        break;
                    default:
                        messageBox.text = state.ToString();
                        indicator.gameObject.SetActive(true);
                        break;
                }
            }
        }
    }
}