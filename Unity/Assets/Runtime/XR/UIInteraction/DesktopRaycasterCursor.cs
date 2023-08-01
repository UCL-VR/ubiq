using UnityEngine;

namespace Ubiq.XR
{
    [RequireComponent(typeof(DesktopUIRaycaster))]
    public class DesktopRaycasterCursor : MonoBehaviour
    {
        public new Renderer renderer;

        [Tooltip("When enabled, the cursor will scale depending on the distance to the user.")]
        public bool ScaleCursor = true;

        private DesktopUIRaycaster desktopRaycaster;

        private Vector3 localScale;

        private void Awake()
        {
            desktopRaycaster = GetComponent<DesktopUIRaycaster>();

            localScale = renderer.transform.localScale;
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
            if(ScaleCursor)
            {
                Vector3 scale = transform.position - hit;
                renderer.transform.localScale = localScale * scale.magnitude;
            }

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