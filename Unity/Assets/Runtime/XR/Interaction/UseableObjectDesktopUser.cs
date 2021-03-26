using System.Linq;
using UnityEngine;

namespace Ubiq.XR
{
    /// <summary>
    /// Uses the object under the cursor.
    /// </summary>
    [RequireComponent(typeof(DesktopHand))]
    public class UseableObjectDesktopUser : MonoBehaviour
    {
        public IUseable used; // single item this time because raycast will only intersect first object
        public Camera mainCamera;

        private DesktopHand hand;

        private void Awake()
        {
            hand = GetComponent<DesktopHand>();
        }

        private void Update()
        {
            TestUse();
        }

        private void TestUse()
        {
            if (!Input.GetMouseButton(0))
            {
                if (used != null)
                {
                    used.UnUse(hand);
                    used = null;
                }
            }

            if(Input.GetMouseButtonDown(0))
            {
                var mainCamera = FindCamera();

                RaycastHit hit = new RaycastHit();
                if (!Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition).origin,
                                     mainCamera.ScreenPointToRay(Input.mousePosition).direction, out hit, 100,
                                     Physics.DefaultRaycastLayers)
                )
                {
                    return;
                }

                used = hit.collider.gameObject.GetComponentsInParent<MonoBehaviour>().Where(mb => mb is IUseable).FirstOrDefault() as IUseable;

                if (used != null)
                {
                    Debug.Log("Using " + hit.collider.gameObject.name);
                    used.Use(hand);
                }
            }
        }

        private Camera FindCamera()
        {
            if (mainCamera != null)
            {
                return mainCamera;
            }

            return Camera.main;
        }
    }
}