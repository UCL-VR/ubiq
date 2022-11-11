using System.Collections;
using System.Linq;
using UnityEngine;
using Ubiq.Geometry;

namespace Ubiq.XR
{
    /// <summary>
    /// This component acts as a hand for a desktop user, by placing a proxy at components found through raycasting.
    /// It is designed to operate alongside a VR Hand Controller, modifying the local transform, and doing so only
    /// when explicit user input is given.
    /// </summary>
    [RequireComponent(typeof(DesktopHand))]
    public class GraspableObjectDesktopGrasper : MonoBehaviour
    {
        public Camera mainCamera;

        public float scrollScale = 0.1f;

        private IGraspable grasped;
        private Vector3 startingPosition;
        private float previousProjectedHeight;

        private DesktopHand hand;

        private void Awake()
        {
            hand = GetComponent<DesktopHand>();
        }

        private void Start()
        {
            startingPosition = transform.localPosition;
        }

        private void Update()
        {
            UpdateGrasp();
        }

        private float GetProjectedHeight(Vector3 position)
        {
            var mainCamera = FindCamera();
            var up = Query.ClosestPointRayRay(
                new Ray(new Vector3(position.x, 0f, position.z), Vector3.up),
                new Ray(mainCamera.transform.position, mainCamera.transform.forward))
                .start;
            return up.y;
        }

        private void UpdateGrasp()
        {
            int mouseButton = 2;
            var mainCamera = FindCamera();

            if (Input.GetMouseButtonDown(mouseButton) && grasped != null)
            {
                grasped.Release(hand);
                grasped = null;
                transform.localPosition = startingPosition;
            }

            if(Input.GetMouseButtonDown(mouseButton) && grasped == null)
            {
                if (Input.GetMouseButtonDown(mouseButton)) // test if there is a graspable object under the cursor on this click. todo: key modifider for mac
                {
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hit, 100,
                                         Physics.DefaultRaycastLayers))
                    {
                        grasped = hit.collider.gameObject.GetComponentsInParent<MonoBehaviour>().Where(mb => mb is IGraspable).FirstOrDefault() as IGraspable;
                        if (grasped != null)
                        {
                            hand.MoveTo(hit.point);
                            previousProjectedHeight = GetProjectedHeight(hit.point);
                            grasped.Grasp(hand);
                        }
                    }
                }
            }

            if(Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
            {
                transform.localPosition += (transform.localPosition - startingPosition) * Input.mouseScrollDelta.y * scrollScale;
            }

            if (grasped != null)
            {
                var projectedHeight = GetProjectedHeight(transform.position);
                var d = projectedHeight - previousProjectedHeight;
                previousProjectedHeight = projectedHeight;
                transform.localPosition += new Vector3(0, d, 0);
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