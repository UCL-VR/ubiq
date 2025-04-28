#if XRI_3_0_7_OR_NEWER
using UnityEngine;

namespace Ubiq.XRI.TraditionalControls
{
    public class TraditionalControlsCanvas : MonoBehaviour
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
#endif