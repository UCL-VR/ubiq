using Org.BouncyCastle.Crypto.Macs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Samples
{
    public class DesktopControlsCanvas : MonoBehaviour
    {
        private void Awake()
        {
            XR.Notifications.XRNotifications.OnHmdMounted += XRNotifications_OnHmdMounted;
            XR.Notifications.XRNotifications.OnHmdUnmounted += XRNotifications_OnHmdUnmounted;
        }

        private void XRNotifications_OnHmdUnmounted()
        {
            gameObject.SetActive(true);
        }

        private void XRNotifications_OnHmdMounted()
        {
            gameObject.SetActive(false);
        }
    }
}