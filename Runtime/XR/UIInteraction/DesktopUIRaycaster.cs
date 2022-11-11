using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Ubiq.XR
{
    /// <summary>
    /// A component that interacts with the standard Unity UI using the mouse and a fixed cursor.
    /// This component operates outside of the Input Module for parity with XRUIRaycaster
    /// </summary>
    /// <remarks>
    /// This code is based on the Unity Interactive 360 samples, but modified so it doesn't need physics collisions.
    /// </remarks>
    [RequireComponent(typeof(Camera))]
    public class DesktopUIRaycaster : MonoBehaviour
    {
        [System.Serializable]
        public class RaycastHitEvent : UnityEvent<Vector3,Vector3> { };
        [System.Serializable]
        public class RaycastMissEvent : UnityEvent { };

        public RaycastHitEvent onRaycastHit;
        public RaycastMissEvent onRaycastMiss;

        private PointerEventData eventData;
        private List<RaycastResult> raycastResults;

        private Camera mainCamera;

        private void Awake()
        {
            raycastResults = new List<RaycastResult>();
            mainCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            //Generate a new event data container
            eventData = new PointerEventData(EventSystem.current);
            eventData.pointerId = 0;
        }

        private void Update()
        {
            // Desktop only
            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                enabled = false;
            }

            PerformRaycast();
            CheckInput();
        }

        private void PerformRaycast()
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Check if there is a 3d object between us and the canvas.
            var distance = float.PositiveInfinity;
            RaycastHit rayHit;
            if(Physics.Raycast(ray, out rayHit, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                distance = rayHit.distance;
            }

            RaycastResult raycastResult = new RaycastResult();

            foreach (var canvas in XRUICanvas.Canvases)
            {
                // Raycast against the canvas
                var canvasTransform = canvas.GetComponent<RectTransform>();
                var graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();

                if (RayIntersectsRectTransform(canvasTransform, ray, ref distance))
                {
                    eventData.position = Input.mousePosition;

                    raycastResults.Clear();
                    graphicRaycaster.Raycast(eventData, raycastResults);

                    if (raycastResults.Count > 0)
                    {
                        raycastResult = raycastResults[0];
                    }
                }
            }

            if(!raycastResult.isValid)
            {
                LookAway();
                onRaycastMiss.Invoke();
                return;
            }

            onRaycastHit.Invoke(raycastResult.worldPosition, raycastResult.worldNormal);

            //If we are looking at the same object that we were looking at, we don't need to do anything and can exit
            if (eventData.pointerEnter == raycastResult.gameObject)
            {
                return;
            }

            //Otherwise we are looking at something new and should look away from the old object
            LookAway();

            //Record this data and tell the object that we are pointing at them (OnPointerEnter)
            eventData.pointerEnter = raycastResult.gameObject;
            eventData.pointerCurrentRaycast = raycastResult;

            ExecuteEvents.ExecuteHierarchy(eventData.pointerEnter, eventData, ExecuteEvents.pointerEnterHandler);
        }

        void CheckInput()
        {
            if (!eventData.pointerEnter)
            {
                return;
            }

            if (Input.GetMouseButton(0) && eventData.pointerEnter != null)
            {
                //...tell the object that we have pressed it (OnPointerDown)
                eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
                eventData.pressPosition = eventData.position;
                eventData.pointerPress = eventData.pointerEnter;
                ExecuteEvents.ExecuteHierarchy(eventData.pointerEnter, eventData, ExecuteEvents.pointerDownHandler);
            }
            else if(!Input.GetMouseButton(0))
            {
                //...tell the object than we have stopped pressing it (OnPointerUp)
                if (eventData.pointerPress != null)
                {
                    ExecuteEvents.ExecuteHierarchy(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);
                }

                //...finally, if we pressed and released the same object, then we have clicked it (OnPointerClick)
                if (eventData.pointerPress == eventData.pointerEnter)
                {
                    ExecuteEvents.ExecuteHierarchy(eventData.pointerEnter, eventData, ExecuteEvents.pointerClickHandler);
                }

                eventData.pointerPress = null;
            }
        }

        private void LookAway()
        {
            //If we are currently looking at something, stop looking at it and tell the object (OnPointerExit)
            if (eventData.pointerEnter != null)
            {
                ExecuteEvents.ExecuteHierarchy(eventData.pointerEnter, eventData, ExecuteEvents.pointerExitHandler);
                eventData.pointerEnter = null;
            }
        }

        /// <summary>
        /// Intersects the Ray with the RectTransform Rectangle in world space, and returns the distance, if it is closer
        /// than previous Raycasts.
        /// </summary>
        /// <remarks>
        /// Based on the Unity XR Interaction Toolkit function.
        /// </remarks>
        private bool RayIntersectsRectTransform(RectTransform transform, Ray ray, ref float distance)
        {
            Vector3[] corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            var plane = new Plane(corners[0], corners[1], corners[2]);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                var intersection = ray.GetPoint(enter);

                var bottomEdge = corners[3] - corners[0];
                var leftEdge = corners[1] - corners[0];
                var bottomDot = Vector3.Dot(intersection - corners[0], bottomEdge);
                var leftDot = Vector3.Dot(intersection - corners[0], leftEdge);

                // If the intersection is right of the left edge and above the bottom edge.
                if (leftDot >= 0 && bottomDot >= 0)
                {
                    var topEdge = corners[1] - corners[2];
                    var rightEdge = corners[3] - corners[2];
                    var topDot = Vector3.Dot(intersection - corners[2], topEdge);
                    var rightDot = Vector3.Dot(intersection - corners[2], rightEdge);

                    //If the intersection is left of the right edge, and below the top edge
                    if (topDot >= 0 && rightDot >= 0)
                    {
                        if (enter < distance)
                        {
                            distance = enter;
                            return true;

                        }
                    }
                }
            }
            return false;
        }

    }
}