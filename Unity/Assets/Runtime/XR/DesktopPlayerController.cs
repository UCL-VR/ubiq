using System;
using System.Linq;
using UnityEngine;

namespace Ubiq.XR
{
    /// <summary>
    /// This Desktop Player Controller supports a mouse and keyboard interaction with a scene.
    /// </summary>
    public class DesktopPlayerController : MonoBehaviour
    {
        [NonSerialized]

        private Vector3 velocity;
        private Vector3 userLocalPosition;

        public Camera headCamera;
        public Transform cameraContainer;
        public AnimationCurve cameraRubberBand;

        [Tooltip("Joystick and Keyboard Speed in m/s")]
        public float movementSpeed = 2f;

        [Tooltip("Rotation Speed in degrees per second")]
        public float rotationSpeed = 360f;

        [Tooltip("Maximum view angle of user head in degrees, where 0 is forward and 90 is up")]
        [Range(0,80.0f)]
        public float maxHeadPitch = 40.0f;

        [Tooltip("Minimum view angle of user head in degrees, where 0 is forward and -90 is down")]
        [Range(0,-80.0f)]
        public float minHeadPitch = -40.0f;

        private void OnMouse()
        {
            var deltaTime = Time.deltaTime;

#if UNITY_EDITOR
            // If using the editor, use smoothed time rather than frame-to-frame delta
            // Helps counteract significant performance dips on editor update causing mouse movement to jump
            deltaTime = Time.smoothDeltaTime;
#endif

            if (Input.GetMouseButton(1))
            {
                Cursor.lockState = CursorLockMode.Locked;

                float xRotation = Input.GetAxisRaw("Mouse X");
                float yRotation = Input.GetAxisRaw("Mouse Y");

                var eul = transform.localEulerAngles;
                eul.y += xRotation * rotationSpeed * deltaTime;
                transform.localEulerAngles = eul;

                eul = cameraContainer.localEulerAngles;
                eul.x -= yRotation * rotationSpeed * deltaTime;
                var delta = Mathf.DeltaAngle(eul.x, 0.0f);
                if (delta > -minHeadPitch)
                {
                    eul.x = minHeadPitch;
                }
                else if (delta < -maxHeadPitch)
                {
                    eul.x = maxHeadPitch;
                }
                cameraContainer.localEulerAngles = eul;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void OnKeys()
        {
            Vector3 movement = new Vector3(0f, 0f, 0f);
            if (Input.GetKey(KeyCode.A))
            {
                movement += new Vector3(-1f, 0f, 0f);
            }
            if (Input.GetKey(KeyCode.D))
            {
                movement += new Vector3(1f, 0f, 0f);
            }
            if (Input.GetKey(KeyCode.W))
            {
                movement += new Vector3(0f, 0f, 1f);
            }
            if (Input.GetKey(KeyCode.S))
            {
                movement += new Vector3(0f, 0f, -1f);
            }
            movement = movement.normalized * (movementSpeed) * Time.deltaTime;
            movement = headCamera.transform.TransformDirection(movement);
            movement.y = 0f;

            transform.position += movement;
        }

        private void OnGround()
        {
            var height = Mathf.Clamp(transform.InverseTransformPoint(headCamera.transform.position).y, 0.1f, float.PositiveInfinity);
            var origin = transform.position + userLocalPosition + Vector3.up * height;
            var direction = Vector3.down;

            RaycastHit hitInfo;
            if(Physics.Raycast(new Ray(origin, direction), out hitInfo))
            {
                var virtualFloorHeight = hitInfo.point.y;

                if (transform.position.y < virtualFloorHeight)
                {
                    transform.position += Vector3.up * (virtualFloorHeight - transform.position.y) * Time.deltaTime * 3f;
                    velocity = Vector3.zero;
                }
                else
                {
                    velocity += Physics.gravity * Time.deltaTime;
                }
            }
            else
            {
                velocity = Vector3.zero; // if there is no 'ground' in the scene, then do nothing
            }

            transform.position += velocity * Time.deltaTime;
        }

        private void FixedUpdate()
        {
            // Update the foot position. This is done by pulling the feet using a rubber band.
            // Decoupling the feet in this way allows the user to do things like lean over edges, when the ground check is enabled.
            // This can be effectively disabled by setting the animation curve to a constant high value.

            var headProjectionXZ = transform.InverseTransformPoint(headCamera.transform.position);
            headProjectionXZ.y = 0;
            userLocalPosition.x += (headProjectionXZ.x - userLocalPosition.x) * Time.deltaTime * cameraRubberBand.Evaluate(Mathf.Abs(headProjectionXZ.x - userLocalPosition.x));
            userLocalPosition.z += (headProjectionXZ.z - userLocalPosition.z) * Time.deltaTime * cameraRubberBand.Evaluate(Mathf.Abs(headProjectionXZ.z - userLocalPosition.z));
            userLocalPosition.y = 0;
        }

        private void Update()
        {
            // Desktop only
            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                enabled = false;
                transform.localEulerAngles = Vector3.zero;
                cameraContainer.localEulerAngles = Vector3.zero;
            }

            OnMouse();
            OnKeys();
            //OnGround(); //todo: finish implementation
        }

    }
}
