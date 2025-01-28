using UnityEngine;
using UnityEngine.EventSystems;

namespace Ubiq.Compatibility.XRI.TraditionalControls
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
     
//     public class TraditionalControlsThumbstick : MonoBehaviour, IPointerDownHandler
//     {
//         public float maxDistance;
//         
//         private RectTransform rectTransform;
//         private Vector3 touchToThumbstickCenter;
//         private bool isPressed = false; 
//         
// #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
//         private Finger touchingFinger;
// #endif
//         
//         private void Start()
//         {
//             rectTransform = GetComponent<RectTransform>();
//             GetComponent<UnityEngine.UI.Image>().alphaHitTestMinimumThreshold = 0.5f;
//             // var eventSystem = new EventSystem();
//             // eventSystem.currentInputModule.poi
//             
// #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
//             EnhancedTouchSupport.Enable();
// #endif
//         }
//
// #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
//         private void OnDestroy()
//         {
//             // Only undoes a single Enable()
//             EnhancedTouchSupport.Disable();
//         }
// #endif
//
//         public bool IsPressed()
//         {
//             return isPressed;
//         }
//
//         public Vector2 ReadCurrentValue()
//         {
//             var position = rectTransform.position - rectTransform.parent.position; 
//             return (Vector2)position / new Vector2(maxDistance,maxDistance);
//         }
//
//         private void Update()
//         {
//             if (!isPressed)
//             {
//                 return;
//             }
//             
//             // Check for release
// #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
//             if (!touchingFinger.isActive)
//             {
//                 touchingFinger = null;
//                 isPressed = false;
//             }
// #else
//             if (!Mouse.current.leftButton.IsPressed())
//             {
//                 isPressed = false;
//             }
// #endif
//             
//             if (!isPressed)
//             {
//                 rectTransform.position = rectTransform.parent.position;
//                 return;
//             }
//
//             // Get current position
// #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
//             var touchPosition = touchingFinger.screenPosition;
// #else
//             var touchPosition = Mouse.current.position.ReadValue();
// #endif
//             
//             var targetPosition = (Vector3)touchPosition + touchToThumbstickCenter;
//             var vector = targetPosition - rectTransform.parent.position; 
//             if (vector.magnitude > maxDistance)
//             {
//                 targetPosition = rectTransform.parent.position + (vector.normalized * maxDistance);
//             }
//             rectTransform.position = targetPosition;
//         }
//
//         void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
//         {
//             // Debug.Log($"ZIAAG {rectTransform.parent.name} - OnPointerDown - {eventData.pointerCurrentRaycast.gameObject.transform.parent.name}");
//             if (eventData.pointerCurrentRaycast.gameObject != gameObject)
//             {
//                 return;
//             }
//             
// #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
//             var closestFinger = -1;
//             var closestFingerSqrDistance = Mathf.Infinity;
//             for (int i = 0; i < Touch.activeFingers.Count; i++)
//             {
//                 var fingerPos = Touch.activeFingers[i].screenPosition;
//                 var to = eventData.position - fingerPos;
//                 var sqrDistance = Vector2.SqrMagnitude(to);
//                 if (sqrDistance < closestFingerSqrDistance)
//                 {
//                     closestFingerSqrDistance = sqrDistance;
//                     closestFinger = i;
//                 }
//             }
//
//             if (closestFinger < 0)
//             {           
//                 Debug.LogWarning("Could not find a finger to match Unity UI pointer event");
//             }
//             touchingFinger = Touch.activeFingers[closestFinger];
// #endif
//             
//             isPressed = true;
//             touchToThumbstickCenter = rectTransform.position - (Vector3)eventData.position;
//         }
//     }   
}
