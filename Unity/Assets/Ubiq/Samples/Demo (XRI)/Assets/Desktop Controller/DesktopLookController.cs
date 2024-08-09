using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.XR.Notifications;

namespace Ubiq.Samples
{
    /// <summary>
    /// Allows the mouse to control the orientation of the Camera.
    /// </summary>
    public class DesktopLookController : MonoBehaviour
    {
        public InputActionReference Look;
        public InputActionReference Enable;
        public InputActionReference EnableOverride;

        public XRRayInteractor RayInteractor;

        public XROrigin XROrigin;
        public float Sensitivity = 0.25f;

        private void Awake()
        {
            if (!XROrigin)
            {
                XROrigin = GetComponentInParent<XROrigin>();
            }
        }

        void Start()
        {
            Look.action.Enable();
            Enable.action.Enable();
            EnableOverride.action.Enable();
            XRNotifications.OnHmdMounted += ResetPitch;
        }

        // This script works by adding Yaw to the rotation of the origin, and
        // Pitch to the Camera Offset. Both of these values should aggregate
        // with any transforms from the XR tracking system, and the rotation
        // can be overridden at any time via, e.g. Teleporting, if needed.

        // The pitch is the only 'unnatural' addition that cannot be explained
        // by the world transform of the user, so is reset every time the HMD is
        // put on.

        // Camera movement in this version is clutched by the Grab button and
        // based on whether the cursor is over an Interactable.

        void Update()
        {
            if ((Enable.action.ReadValue<float>() > 0 && RayInteractor.interactablesSelected.Count == 0) || EnableOverride.action.ReadValue<float>() > 0)
            {
                var look = Look.action.ReadValue<Vector2>();
                XROrigin.transform.Rotate(Vector3.up, look.x * Sensitivity);
                XROrigin.CameraFloorOffsetObject.transform.Rotate(Vector3.right, look.y * Sensitivity * -0.5f);
            }
        }

        public void ResetPitch()
        {
            XROrigin.CameraFloorOffsetObject.transform.rotation = Quaternion.identity;
        }
    }
}