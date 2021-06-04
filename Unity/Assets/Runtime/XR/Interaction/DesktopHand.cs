using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.XR
{
    public class DesktopHand : Hand, IPrimaryButtonProvider
    {
        [HideInInspector]
        public Vector3 previousPosition;

        public ButtonEvent PrimaryButtonPress { get; private set; }

        public Camera mainCamera;

        private Vector3 previousMousePosition;

        private void Awake()
        {
            PrimaryButtonPress = new ButtonEvent();
        }

        public void MoveTo(Vector3 position)
        {
            transform.position = position;
            previousPosition = position;
            velocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            velocity = (transform.position - previousPosition) / Time.deltaTime;
            previousPosition = transform.position;
        }

        private void Update()
        {
            var mainCamera = FindCamera();
            var mouseDelta = Input.mousePosition - previousMousePosition;
            previousMousePosition = Input.mousePosition;

            // Rotate the hand using the mouse
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                transform.Rotate(new Vector3(-mouseDelta.y, mouseDelta.x, 0f), Space.Self);
            }

            // Rotate the hand using the raycaster
            if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray.origin, ray.direction, out hit, 100, Physics.AllLayers))
                {
                    transform.LookAt(hit.point);
                }
                else
                {
                    transform.LookAt(ray.origin + ray.direction * 10);
                }
            }

            PrimaryButtonPress.Update(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));

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