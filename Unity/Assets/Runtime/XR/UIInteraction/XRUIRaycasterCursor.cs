using UnityEngine;

namespace Ubiq.XR
{
    [RequireComponent(typeof(XRUIRaycaster))]
    public class XRUIRaycasterCursor : MonoBehaviour
    {
        public new Renderer renderer;

        [Tooltip("When enabled, the cursor will scale depending on the distance to the user.")]
        public bool ScaleCursor = true;

        private XRUIRaycaster xruiRaycaster;

        private Vector3 localScale;

        private void Awake()
        {
            xruiRaycaster = GetComponent<XRUIRaycaster>();

            localScale = renderer.transform.localScale;
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

        private void XRUIRaycaster_OnRaycastHit(Vector3 hit, Vector3 normal)
        {
            if (ScaleCursor)
            {
                Vector3 scale = transform.position - hit;
                renderer.transform.localScale = localScale * scale.magnitude;
            }

            renderer.enabled = true;
            renderer.transform.position = hit;
            renderer.transform.rotation = Quaternion.LookRotation(-normal);
        }

        private void XRUIRaycaster_OnRaycastMiss()
        {
            renderer.enabled = false;
        }

    }
}