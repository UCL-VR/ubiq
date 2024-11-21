using UnityEngine;
using Ubiq.Avatars;

#if XRI_3_0_7_OR_NEWER
using Unity.XR.CoreUtils;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
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
        
#if XRI_3_0_7_OR_NEWER
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
        private NearFarInteractor leftInteractor;
        private NearFarInteractor rightInteractor;
        private Transform leftHandPalmJoint;
        private Transform rightHandPalmJoint;
        
        // Additional transformations to make the palm pose roughly match 
        // the pointerPose provided by OpenXR. These are only approximations 
        // and may not be a good fit for different hand sizes/poses.
        private Matrix4x4 leftPalmOffset = Matrix4x4.TRS(
            pos: new Vector3(0.0199999996f,-0.00999999978f,0.0700000003f),
            q: Quaternion.Euler(0,-9.93000031f,-84.8099976f),
            s: Vector3.one);
        
        private Matrix4x4 rightPalmOffset = Matrix4x4.TRS(
            pos: new Vector3(-0.0199999996f,-0.00999999978f,0.0700000003f),
            q: Quaternion.Euler(0,9.93000031f,84.8099976f),
            s: Vector3.one);
            
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
            
        private static void GetActiveHand(GameObject controller, 
            GameObject hand, ref Transform cachedPalmJoint, 
            out Transform trans, out bool isHand)
        {
            if (controller && controller.activeInHierarchy)
            {
                trans = controller.transform;
                isHand = false;
                return;
            }
            
            if (hand && hand.activeInHierarchy)
            {
                if (cachedPalmJoint)
                {
                    trans = cachedPalmJoint;
                    isHand = true;
                }
                
                var driver = hand.GetComponentInChildren<XRHandSkeletonDriver>();
                if (driver && driver.jointTransformReferences.Count > 0)
                {
                    for (var i = 0; i < driver.jointTransformReferences.Count; i++)
                    {
                        var joint = driver.jointTransformReferences[i];  
                        if (joint.xrHandJointID == XRHandJointID.Palm)
                        {
                            trans = cachedPalmJoint = joint.jointTransform;
                            isHand = true;
                            return;
                        }
                    }
                    
                    Debug.LogWarning("No palm pose in hand driver. This may " +
                                     "indicate that the hand tracking " +
                                     "provider does not provide palm " +
                                     "poses. Cannot supply hand position to " +
                                     "avatar through hand tracking.");
                }
            }
            
            trans = null;
            isHand = false;
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
            GetActiveHand(
                modalityManager.leftController,
                modalityManager.leftHand, 
                ref leftHandPalmJoint, out var trans, out var isHand);
            if (!trans)
            {
                return InputVar<Pose>.invalid;
            }
            
            trans.GetPositionAndRotation(out var p, out var r);
            if (isHand)
            {
                var mat = trans.localToWorldMatrix * leftPalmOffset;
                p = mat.GetPosition();
                r = mat.rotation;
            }
            return new InputVar<Pose>(new Pose(p,r));
        }
        
        private InputVar<Pose> RightHand()
        {
            GetActiveHand(
                modalityManager.rightController,
                modalityManager.rightHand,
                ref rightHandPalmJoint, out var trans, out var isHand);
            if (!trans)
            {
                return InputVar<Pose>.invalid;
            }
                
            trans.GetPositionAndRotation(out var p, out var r);
            if (isHand)
            {
                var mat = trans.localToWorldMatrix * rightPalmOffset;
                p = mat.GetPosition();
                r = mat.rotation;
            }
            return new InputVar<Pose>(new Pose(p,r));
        }
        
        private InputVar<float> LeftGrip()
        {
            if (!leftInteractor && modalityManager.leftController)
            {
                leftInteractor = modalityManager.leftController.
                    GetComponentInChildren<NearFarInteractor>();
            }
            
            return leftInteractor 
                   && leftInteractor.selectInput.TryReadValue(out var grip)
                ? new InputVar<float>(grip)
                : InputVar<float>.invalid;
        }
        
        private InputVar<float> RightGrip()
        {
            if (!rightInteractor && modalityManager.rightController)
            {
                rightInteractor = modalityManager.rightController.
                    GetComponentInChildren<NearFarInteractor>();
            }
            
            return rightInteractor 
                   && rightInteractor.selectInput.TryReadValue(out var grip) 
                ? new InputVar<float>(grip)
                : InputVar<float>.invalid;  
        }
#endif
    }
}