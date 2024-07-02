using UnityEngine;
using Ubiq.Avatars;

#if XRI_2_5_2_OR_NEWER
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
#endif

namespace Ubiq.XRI
{
    public class HeadAndHandsProviderXRI : MonoBehaviour, IHeadAndHandsProvider
    {
        [Tooltip("The AvatarManager to provide input to. If null, will try to find an AvatarManager in the scene at start.")]
        [SerializeField] private AvatarManager avatarManager;

        public int priority => 0;
        public bool isProviding => isActiveAndEnabled;
        public InputVar<Pose> head => Head();
        public InputVar<Pose> leftHand => LeftHand();
        public InputVar<Pose> rightHand => RightHand();
        public InputVar<float> leftGrip => LeftGrip();
        public InputVar<float> rightGrip => RightGrip();
        
        
#if !XRI_2_5_2_OR_NEWER

        private InputVar<Pose> Head() => InputVar<Pose>.invalid;
        private InputVar<Pose> LeftHand() => InputVar<Pose>.invalid;
        private InputVar<Pose> RightHand() => InputVar<Pose>.invalid;
        private InputVar<float> LeftGrip() => InputVar<float>.invalid;
        private InputVar<float> RightGrip() => InputVar<float>.invalid;
        
#else // XRI_2_5_2_OR_NEWER

        private XROrigin origin;
        private XRInputModalityManager modalityManager;
        private XRBaseController leftController;
        private XRBaseController rightController;
            
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
        
        private void Start()
        {
            if (!avatarManager)
            {
                avatarManager = FindAnyObjectByType<AvatarManager>();

                if (!avatarManager)
                {
                    Debug.LogWarning("No AvatarManager could be found in this Unity scene. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            modalityManager = GetComponentInChildren<XRInputModalityManager>();
            origin = GetComponentInChildren<XROrigin>();

            if (!origin || !modalityManager)
            {
                Debug.LogWarning("No XROrigin or XRInputModalityManager on child objects. Cannot provide input. This script will be disabled.");
                enabled = false;
            }
            
            avatarManager.input.AddProvider((IHeadAndHandsProvider)this);
        }

        private void OnDestroy()
        {
            avatarManager?.input?.RemoveProvider((IHeadAndHandsProvider)this);
        }
#endif
    }
}