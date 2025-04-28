#if XRI_3_0_7_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;

using Ubiq.XR.Notifications;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Ubiq.XRI.TraditionalControls
{
    /// <summary>
    /// Controls the Transform of the Mouse Cursor GameObject, ensuring Interactors,
    /// such as the Ray Interactor, that operate via the Transform behave
    /// intuitively. This Component also maintains a reticule to help users identify
    /// when they are over an interactive object.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class TraditionalControlsCursorController : MonoBehaviour
    {
        public InputActionReference CursorPosition;
        public InputActionReference TranslateAnchor;
        public InputActionReference Select;
        public InputActionReference Activate;
    
        public XROrigin XROrigin;
        public NearFarInteractor Interactor;
        public UnityEngine.UI.Image CursorImage;
    
        public Color InteractableHoverColour = Color.cyan;
    
        private bool disableCursor;
        private Canvas canvas;
    
        private void Awake()
        {
            if (!XROrigin)
            {
                XROrigin = GetComponentInParent<XROrigin>();
            }
        }
    
        void Start()
        {
            canvas = GetComponentInChildren<Canvas>();
            
            CursorPosition.action.Enable();
            TranslateAnchor.action.Enable();
            Activate.action.Enable();
            Select.action.Enable();
            XRNotifications.OnHmdMounted += OnHmdAdded;
            XRNotifications.OnHmdUnmounted += OnHmdRemoved;
            
            disableCursor = Application.isMobilePlatform;
        }
    
        public void OnHmdAdded()
        {
            gameObject.SetActive(false);
        }
    
        public void OnHmdRemoved()
        {
            gameObject.SetActive(true);
        }
    
        void Update()
        {
            // Seems to help prevent 'sticky' selects when using Touch controls 
            Interactor.allowSelect = Select.action.IsPressed();
            
            UpdateTransform();
            UpdateCursor();
        }
    
        private void UpdateTransform()
        {
            Vector3 position = CursorPosition.action.ReadValue<Vector2>();
            position.z = XROrigin.Camera.nearClipPlane;
            var ray = XROrigin.Camera.ScreenPointToRay(position, Camera.MonoOrStereoscopicEye.Mono);
            transform.position = ray.origin;
            transform.forward = ray.direction;
        }
    
        private void UpdateCursor()
        {
            CursorImage.enabled = false;

            if (disableCursor)
            {
                return;
            }
            
            Vector3 position = CursorPosition.action.ReadValue<Vector2>();
            position.x = position.x / canvas.scaleFactor;
            position.y = position.y / canvas.scaleFactor;
            
            var distance = Mathf.Infinity;
            if (Interactor.TryGetCurrentUIRaycastResult(out var uiRaycast))
            {
                distance = uiRaycast.distance * distance;
                CursorImage.enabled = true;
                CursorImage.color = Color.white;
            }
            
            var hovered = Interactor.interactablesHovered;
            for (int i = 0; i < hovered.Count; i++)
            {
                if (hovered[i].GetDistanceSqrToInteractor(Interactor) < distance)
                {
                    CursorImage.enabled = true;
                    CursorImage.color = InteractableHoverColour;
                }
            }
    
            CursorImage.rectTransform.anchoredPosition = position;
        }
    }
}
#endif