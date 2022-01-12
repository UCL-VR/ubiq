using System;
using System.Linq;
using UnityEngine;

namespace Ubiq.XR
{
    /// <summary>
    /// This VR Player Controller supports a typical Head and Two Hand tracked rig.
    /// </summary>
    public class XRPlayerController : MonoBehaviour
    {
        public bool dontDestroyOnLoad = true;

        private static GameObject singleton;

        [NonSerialized]
        public HandController[] handControllers;

        private Vector3 velocity;
        private Vector3 userLocalPosition;

        public float joystickDeadzone = 0.1f;
        public float joystickFlySpeed = 1.2f;

        public Camera headCamera;
        public Transform cameraContainer;
        public AnimationCurve cameraRubberBand;

        private void Awake()
        {
            if(dontDestroyOnLoad)
            {
                if(singleton != null)
                {
                    gameObject.SetActive(false);
                    DestroyImmediate(gameObject);
                    return;
                }

                singleton = gameObject;
                DontDestroyOnLoad(gameObject);
                Extensions.MonoBehaviourExtensions.DontDestroyOnLoadGameObjects.Add(gameObject);
            }

            handControllers = GetComponentsInChildren<HandController>();
        }

        private void Start()
        {
            foreach (var item in GetComponentsInChildren<TeleportRay>())
            {
                item.OnTeleport.AddListener(OnTeleport);
            }
        }

        public void OnTeleport(Vector3 position)
        {
            userLocalPosition = transform.InverseTransformPoint(headCamera.transform.position);
            userLocalPosition.y = 0;

            var movement = position - transform.TransformPoint(userLocalPosition);  // move so the foot position is over the new teleport location
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


        private void Update()
        {
            // Update the foot position. This is done by pulling the feet using a rubber band.
            // Decoupling the feet in this way allows the user to do things like lean over edges, when the ground check is enabled.
            // This can be effectively disabled by setting the animation curve to a constant high value.

            foreach (var item in handControllers)
            {
                if (item.Right)
                {
                    if (item.JoystickSwipe.Trigger)
                    {
                        transform.RotateAround(headCamera.transform.position, Vector3.up, 45f * Mathf.Sign(item.JoystickSwipe.Value));
                    }
                }
                else if (item.Left)
                {
                    var dir = item.Joystick.normalized;
                    var mag = item.Joystick.magnitude;
                    if (mag > joystickDeadzone)
                    {
                        var speedMultiplier = Mathf.InverseLerp(joystickDeadzone,1.0f,mag);
                        var worldDir = headCamera.transform.TransformDirection(dir.x,0,dir.y);
                        worldDir.y = 0;
                        var distance = (joystickFlySpeed * Time.deltaTime);
                        transform.position += distance * worldDir.normalized;
                    }
                }
            }

            var headProjectionXZ = transform.InverseTransformPoint(headCamera.transform.position);
            headProjectionXZ.y = 0;
            userLocalPosition.x += (headProjectionXZ.x - userLocalPosition.x) * Time.deltaTime * cameraRubberBand.Evaluate(Mathf.Abs(headProjectionXZ.x - userLocalPosition.x));
            userLocalPosition.z += (headProjectionXZ.z - userLocalPosition.z) * Time.deltaTime * cameraRubberBand.Evaluate(Mathf.Abs(headProjectionXZ.z - userLocalPosition.z));
            userLocalPosition.y = 0;
        }

        private void OnDrawGizmos()
        {
            if (!headCamera) {
                return;
            }

            Gizmos.color = Color.blue;
            Gizmos.matrix = transform.localToWorldMatrix;
           //Gizmos.DrawWireCube(Vector3.zero, new Vector3(Radius, 0.1f, Radius));
            Gizmos.DrawLine(userLocalPosition, transform.InverseTransformPoint(headCamera.transform.position));
        }
    }
}
