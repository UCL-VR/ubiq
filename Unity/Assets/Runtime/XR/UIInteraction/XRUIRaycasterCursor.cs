using UnityEngine;

namespace Ubiq.XR
{
    [RequireComponent(typeof(XRUIRaycaster))]
    public class XRUIRaycasterCursor : MonoBehaviour
    {
        public new Renderer renderer;

        private XRUIRaycaster xruiRaycaster;

        private void Awake()
        {
            xruiRaycaster = GetComponent<XRUIRaycaster>();
        }

        private void OnEnable()
        {
            xruiRaycaster.onRaycastHit.AddListener(XRUIRaycaster_OnRaycastHit);
            xruiRaycaster.onRaycastMiss.AddListener(XRUIRaycaster_OnRaycastMiss);
        }

        private void OnDisable()
        {
            if (xruiRaycaster)
            {
                xruiRaycaster.onRaycastHit.RemoveListener(XRUIRaycaster_OnRaycastHit);
                xruiRaycaster.onRaycastMiss.RemoveListener(XRUIRaycaster_OnRaycastMiss);
            }
        }

        private void XRUIRaycaster_OnRaycastHit (Vector3 hit, Vector3 normal)
        {
            renderer.enabled = true;
            renderer.transform.position = hit;
            renderer.transform.rotation = Quaternion.LookRotation(-normal);
        }

        private void XRUIRaycaster_OnRaycastMiss ()
        {
            renderer.enabled = false;
        }

    }
}