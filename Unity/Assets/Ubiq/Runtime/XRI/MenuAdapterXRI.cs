using UnityEngine;
#if XRI_2_5_2_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace Ubiq.XRI
{
    public class MenuAdapterXRI : MonoBehaviour
    {
#if XRI_2_5_2_OR_NEWER
        private void Start()
        {
            var grab = gameObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            var canvas = GetComponentInChildren<Canvas>();
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
#endif
    }
}
