using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.XR.Notifications;

/// <summary>
/// Controls the Transform of the Mouse Cursor GameObject, ensuring Interactors,
/// such as the Ray Interactor, that operate via the Transform behave
/// intuitively. This Component also maintains a reticule to help users identify
/// when they are over an interactive object.
/// </summary>
public class DesktopCursorController : MonoBehaviour
{
    public InputActionReference CursorPosition;
    public InputActionReference TranslateAnchor;
    public InputActionReference Select;
    public InputActionReference Activate;

    public XROrigin XROrigin;
    public XRRayInteractor RayInteractor;
    public UnityEngine.UI.Image CursorImage;

    public Color InteractableHoverColour = Color.cyan;

    private void Awake()
    {
        if (!XROrigin)
        {
            XROrigin = GetComponentInParent<XROrigin>();
        }
    }

    void Start()
    {
        CursorPosition.action.Enable();
        TranslateAnchor.action.Enable();
        Activate.action.Enable();
        Select.action.Enable();
        XRNotifications.OnHmdMounted += OnHmdAdded;
        XRNotifications.OnHmdUnmounted += OnHmdRemoved;
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
        CursorImage.enabled = false;
        UpdateTransform();
        UpdateCursor();
    }

    private void UpdateTransform()
    {
        Vector3 position = CursorPosition.action.ReadValue<Vector2>();
        position.z = XROrigin.Camera.nearClipPlane;
        var ray = XROrigin.Camera.ScreenPointToRay(position, Camera.MonoOrStereoscopicEye.Mono);
        var origin = XROrigin.Camera.ScreenToWorldPoint(position, Camera.MonoOrStereoscopicEye.Mono);
        transform.position = origin;
        transform.forward = ray.direction;
    }

    private void UpdateCursor()
    {
        Vector3 position = CursorPosition.action.ReadValue<Vector2>();

        Vector3 hitPosition;
        Vector3 hitNormal;
        int positionInLine;
        bool isValidTarget;

        CursorImage.enabled = true;
        CursorImage.color = Color.white;

        if (RayInteractor.TryGetHitInfo(out hitPosition, out hitNormal, out positionInLine, out isValidTarget))
        {
            if (isValidTarget)
            {
                CursorImage.enabled = true;
                CursorImage.color = InteractableHoverColour;
            }

            RaycastHit? raycastHit;
            int raycastHitIndex;
            RaycastResult? uiRaycastHit;
            int uiRaycastHitIndex;
            bool isUIHitClosest;

            if (RayInteractor.TryGetCurrentRaycast(out raycastHit, out raycastHitIndex, out uiRaycastHit, out uiRaycastHitIndex, out isUIHitClosest))
            {
                if (uiRaycastHit.HasValue && isUIHitClosest)
                {
                    CursorImage.enabled = false;
                }
            }
        }

        CursorImage.rectTransform.anchoredPosition = position;
    }
}
