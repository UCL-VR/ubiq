#if XRI_3_0_7_OR_NEWER
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

using Ubiq.XR.Notifications;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Ubiq.XRI.TraditionalControls
{
    /// <summary>
    /// Allows mouse or touch inputs to control the orientation of the Camera.
    /// </summary>
    public class TraditionalControlsLookController : MonoBehaviour
    {
        public InputActionReference Look;
        public InputActionReference Enable;
        public InputActionReference EnableOverride;

        public XROrigin XROrigin;
        public XRInteractionManager interactionManager;
        public float Sensitivity = 0.25f;

        private bool isSuppressed;
        
        void Start()
        {
            XROrigin = XROrigin 
                ? XROrigin 
                : GetComponentInParent<XROrigin>();
            interactionManager = interactionManager 
                ? interactionManager 
                : FindAnyObjectByType<XRInteractionManager>();
            Look.action.Enable();
            Enable.action.Enable();
            EnableOverride.action.Enable();
            XRNotifications.OnHmdMounted += ResetPitch;
        }
        
        void OnDestroy()
        {
            XRNotifications.OnHmdMounted -= ResetPitch;
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
            if (isSuppressed)
            {
                isSuppressed = false;
                return;
            }
            
            if ((Enable.action.ReadValue<float>() > 0 
                 && !interactionManager.IsHandSelecting(InteractorHandedness.Left)
                 && !interactionManager.IsHandSelecting(InteractorHandedness.Right)) 
                || EnableOverride.action.ReadValue<float>() > 0)
            {
                AddDelta(Look.action.ReadValue<Vector2>());
            }
        }
        
        public void AddDelta(Vector2 delta)
        {
            XROrigin.transform.Rotate(Vector3.up, delta.x * Sensitivity);
            XROrigin.CameraFloorOffsetObject.transform.Rotate(Vector3.right, delta.y * Sensitivity * -0.5f);
        }
        
        /// <summary>
        /// Prevent camera rotations for one frame. Resume immediately with
        /// <see cref="ClearSuppression"/>. 
        /// </summary>
        public void SuppressThisFrame()
        {
            isSuppressed = true;
        }
        
        /// <summary>
        /// Immediately resume camera rotations.
        /// </summary>
        public void ClearSuppression()
        {
            isSuppressed = false;
        }

        public void ResetPitch()
        {
            XROrigin.CameraFloorOffsetObject.transform.rotation = Quaternion.identity;
        }
    }
}
#endif