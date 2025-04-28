#if XRI_3_0_7_OR_NEWER
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ubiq.XRI.TraditionalControls
{
     public class TraditionalControlsThumbstick : MonoBehaviour, 
         IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public float maxDistance;
        
        private RectTransform rectTransform;
        private Vector3 touchToThumbstickCenter;
        private bool isPressed = false; 
        
        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            GetComponent<UnityEngine.UI.Image>().alphaHitTestMinimumThreshold = 0.5f;
        }

        public bool IsPressed()
        {
            return isPressed;
        }

        public Vector2 ReadCurrentValue()
        {
            var position = rectTransform.position - rectTransform.parent.position; 
            return (Vector2)position / new Vector2(maxDistance,maxDistance);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            touchToThumbstickCenter = rectTransform.position - (Vector3)eventData.position;
            isPressed = true;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
             var targetPosition = (Vector3)eventData.position + touchToThumbstickCenter;
             var vector = targetPosition - rectTransform.parent.position; 
             if (vector.magnitude > maxDistance)
             {
                 targetPosition = rectTransform.parent.position + (vector.normalized * maxDistance);
             }
             rectTransform.position = targetPosition;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            rectTransform.position = rectTransform.parent.position;
            isPressed = false;
            
        }
    }
}
#endif
