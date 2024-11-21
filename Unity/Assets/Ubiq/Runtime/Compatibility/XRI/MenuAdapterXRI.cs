using UnityEngine;
#if XRI_3_0_7_OR_NEWER

using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace Ubiq.XRI
{
    public class MenuAdapterXRI : MonoBehaviour
    {
#if XRI_3_0_7_OR_NEWER
        private void Start()
        {
            var grab = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.throwOnDetach = false;
            var canvas = GetComponentInChildren<Canvas>();
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
#endif
    }
}
