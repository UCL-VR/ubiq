using UnityEngine;

namespace Ubiq.XR
{
    [RequireComponent(typeof(DesktopRaycaster))]
    public class DesktopRaycasterCursor : MonoBehaviour
    {
        public new Renderer renderer;

        private DesktopRaycaster desktopRaycaster;

        private void Awake()
        {
            desktopRaycaster = GetComponent<DesktopRaycaster>();
        }

        private void OnEnable()
        {
            desktopRaycaster.onRaycastHit.AddListener(DesktopRaycaster_OnRaycastHit);
            desktopRaycaster.onRaycastMiss.AddListener(DesktopRaycaster_OnRaycastMiss);
        }

        private void OnDisable()
        {
            if (desktopRaycaster)
            {
                desktopRaycaster.onRaycastHit.RemoveListener(DesktopRaycaster_OnRaycastHit);
                desktopRaycaster.onRaycastMiss.RemoveListener(DesktopRaycaster_OnRaycastMiss);
            }
        }

        private void Update()
        {
            if (!desktopRaycaster.enabled)
            {
                enabled = false;
            }
        }

        private void DesktopRaycaster_OnRaycastHit (Vector3 hit, Vector3 normal)
        {
            renderer.enabled = true;
            renderer.transform.position = hit;
            renderer.transform.rotation = Quaternion.LookRotation(-normal);
        }

        private void DesktopRaycaster_OnRaycastMiss ()
        {
            renderer.enabled = false;
        }

    }
}