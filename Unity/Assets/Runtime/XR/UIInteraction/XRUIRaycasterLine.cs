using UnityEngine;

namespace Ubiq.XR
{
    [RequireComponent(typeof(XRUIRaycaster))]
    [RequireComponent(typeof(LineRenderer))]
    public class XRUIRaycasterLine : MonoBehaviour
    {
        private XRUIRaycaster xruiRaycaster;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            xruiRaycaster = GetComponent<XRUIRaycaster>();
            lineRenderer = GetComponent<LineRenderer>();
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
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, hit);
        }

        private void XRUIRaycaster_OnRaycastMiss ()
        {
            lineRenderer.enabled = false;
        }

    }
}