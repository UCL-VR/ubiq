using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Ubiq.Avatars;
using Ubiq.XR;

namespace Ubiq.Samples
{
    public class WristMenuInvoker : MonoBehaviour, IUseable
    {
        private void Start()
        {
            var avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
            if (!avatar || !avatar.IsLocal)
            {
                gameObject.SetActive(false);
            }
        }

        public void Use(Hand controller)
        {
            MenuRequestSource.RequestAll(requester:gameObject);
        }

        public void UnUse(Hand controller) { }
    }
}
