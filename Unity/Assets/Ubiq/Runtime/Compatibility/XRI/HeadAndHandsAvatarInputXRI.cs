using UnityEngine;
using Ubiq.Avatars;

#if XRI_2_5_2_OR_NEWER
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
#endif

namespace Ubiq.XRI
{
    public class HeadAndHandsAvatarInputXRI : MonoBehaviour
    {
        [Tooltip("The AvatarManager to provide input to. If null, will try to find an AvatarManager in the scene at start.")]
        [SerializeField] private AvatarManager avatarManager;
        [Tooltip("The GameObject containing the XROrigin. If null, will try to find an XROrigin in the scene at Start. Note we use a GameObject reference here rather than a direct reference to avoid serialization issues should XR CoreUtils not be present.")]
        [SerializeField] private GameObject xrOriginGameObject;
        [Tooltip("Higher priority inputs will override lower priority inputs of the same type if multiple exist.")]
        public int priority = 0;
        
#if XRI_2_5_2_OR_NEWER
        private class HeadAndHandsInput : IHeadAndHandsInput
        {
            public int priority => owner.priority;
            public bool active => owner.isActiveAndEnabled;
            
            public InputVar<Pose> head => owner.Head();
            public InputVar<Pose> leftHand => owner.LeftHand();
            public InputVar<Pose> rightHand => owner.RightHand();
            public InputVar<float> leftGrip => owner.LeftGrip();
            public InputVar<float> rightGrip => owner.RightGrip();
            
            private HeadAndHandsAvatarInputXRI owner;
            
            public HeadAndHandsInput(HeadAndHandsAvatarInputXRI owner)
            {
                this.owner = owner;
            }
        }
        
        private XROrigin origin;
        private XRInputModalityManager modalityManager;
        private XRBaseController leftController;
        private XRBaseController rightController;
        
        private HeadAndHandsInput input;
        
        private void Start()
        {
            if (!avatarManager)
            {
                avatarManager = FindAnyObjectByType<AvatarManager>();

                if (!avatarManager)
                {
                    Debug.LogWarning("No AvatarManager could be found in this" +
                                     " Unity scene. This script will be" +
                                     " disabled.");
                    enabled = false;
                    return;
                }
            }
            
            if (xrOriginGameObject)
            {
                origin = xrOriginGameObject.GetComponent<XROrigin>();
                
                if (!origin)
                {
                    Debug.LogWarning("XROriginGameObject supplied but no " +
                                     "XROrigin component could be found. Will" +
                                     " attempt to find an XROrigin in scene.");
                }
            }
            
            if (!origin)
            {
                origin = FindObjectOfType<XROrigin>();
                
                if (!origin)
                {
                    Debug.LogWarning("No XROrigin found. The local avatar" +
                                     " will not be have its input driven by " +
                                     "XRI");
                    return;
                }
            }
            
            modalityManager = origin.GetComponentInChildren<XRInputModalityManager>();

            if (!modalityManager)
            {
                Debug.LogWarning("No XRInputModalityManager as child of " +
                                 "XROrigin. Cannot provide input. This script" +
                                 " will be disabled.");
                enabled = false;
            }
            
            input = new HeadAndHandsInput(this);
            avatarManager.input.Add((IHeadAndHandsInput)input);
        }

        private void OnDestroy()
        {
            if (avatarManager)
            {
                avatarManager.input?.Remove((IHeadAndHandsInput)input);
            }
        }
            
        private static Transform GetActiveHand(GameObject controller, 
            GameObject hand)
        {
            return controller && controller.activeInHierarchy
                ? controller.transform
                : hand && hand.activeInHierarchy
                    ? hand.transform
                    : null;
        }
        
        private InputVar<Pose> Head()
        {
            var cam = origin.Camera;
            if (!cam)
            {
                return InputVar<Pose>.invalid;
            }
                    
            cam.transform.GetPositionAndRotation(out var p, out var r);
            return new InputVar<Pose>(new Pose(p,r));
        }
        
        private InputVar<Pose> LeftHand()
        {
            var hand = GetActiveHand(
                modalityManager.leftController,
                modalityManager.leftHand);
            if (!hand)
            {
                return InputVar<Pose>.invalid;
            }
                
            hand.GetPositionAndRotation(out var p, out var r);
            return new InputVar<Pose>(new Pose(p,r));
        }
        
        private InputVar<Pose> RightHand()
        {
            var hand = GetActiveHand(
                modalityManager.rightController,
                modalityManager.rightHand);
            if (!hand)
            {
                return InputVar<Pose>.invalid;
            }
                
            hand.GetPositionAndRotation(out var p, out var r);
            return new InputVar<Pose>(new Pose(p,r));
        }
        
        private InputVar<float> LeftGrip()
        {
            if (!leftController && modalityManager.leftController)
            {
                leftController = modalityManager.leftController.
                    GetComponent<XRBaseController>();
            }
            
            return leftController
                ? new InputVar<float>(
                    leftController.selectInteractionState.value)
                : InputVar<float>.invalid;
        }
        
        private InputVar<float> RightGrip()
        {
            if (!rightController && modalityManager.rightController)
            {
                rightController = modalityManager.rightController.
                    GetComponent<XRBaseController>();
            }
            
            return rightController 
                ? new InputVar<float>(
                    rightController.selectInteractionState.value)
                : InputVar<float>.invalid;  
        }
#endif
    }
}