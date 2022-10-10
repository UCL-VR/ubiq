using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(Avatar))]
    public class AvatarLocalRemoteSwitcher : MonoBehaviour
    {
        public GameObject local;
        public GameObject remote;

        private void Start()
        {
            var avatar = GetComponent<Avatar>();
            local.SetActive(avatar.IsLocal);
            remote.SetActive(!avatar.IsLocal);
        }
    }
}
