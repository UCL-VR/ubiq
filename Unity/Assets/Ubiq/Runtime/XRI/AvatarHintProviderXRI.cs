using UnityEngine;
using Ubiq.Avatars;
#if XRI_2_5_2_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Inputs;
#endif

namespace Ubiq.XRI
{
    public class AvatarHintProviderXRI : AvatarHintProvider
    {
        [Tooltip("The AvatarManager to provide hints for. If null, will try to find an AvatarManager in the scene at start.")]
        [SerializeField] private AvatarManager avatarManager;

        [SerializeField] private string headPositionNode = "HeadPosition";
        [SerializeField] private string headRotationNode = "HeadRotation";
        [SerializeField] private string leftHandPositionNode = "LeftHandPosition";
        [SerializeField] private string leftHandRotationNode = "LeftHandRotation";
        [SerializeField] private string rightHandPositionNode = "RightHandPosition";
        [SerializeField] private string rightHandRotationNode = "RightHandRotation";

#if XRI_2_5_2_OR_NEWER
        private XRInputModalityManager modalityManager;

        private void Start()
        {
            if (!avatarManager)
            {
                avatarManager = FindAnyObjectByType<AvatarManager>();

                if (!avatarManager)
                {
                    Debug.LogWarning("No NetworkScene could be found in this Unity scene. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }

            modalityManager = GetComponentInChildren<XRInputModalityManager>();

            if (!modalityManager)
            {
                Debug.LogWarning("No XRInputModalityManager on child objects. Cannot provide hints. This script will be disabled.");
                enabled = false;
                return;
            }

            avatarManager.hints.SetProvider(headPositionNode,AvatarHints.Type.Vector3,this);
            avatarManager.hints.SetProvider(headRotationNode,AvatarHints.Type.Quaternion,this);
            avatarManager.hints.SetProvider(leftHandPositionNode,AvatarHints.Type.Vector3,this);
            avatarManager.hints.SetProvider(leftHandRotationNode,AvatarHints.Type.Quaternion,this);
            avatarManager.hints.SetProvider(rightHandPositionNode,AvatarHints.Type.Vector3,this);
            avatarManager.hints.SetProvider(rightHandRotationNode,AvatarHints.Type.Quaternion,this);
        }

        private Transform GetActiveHand(GameObject controller, GameObject hand)
        {
            return controller != null && controller.activeInHierarchy
                ? controller.transform
                : hand != null && hand.activeInHierarchy
                    ? hand.transform
                    : null;
        }

        private Vector3 GetActiveHandPosition(GameObject controller, GameObject hand)
        {
            var handObject = GetActiveHand(controller,hand);
            return handObject != null ? handObject.position : Vector3.zero;
        }

        private Quaternion GetActiveHandRotation(GameObject controller, GameObject hand)
        {
            var handObject = GetActiveHand(controller,hand);
            return handObject != null ? handObject.rotation : Quaternion.identity;
        }

        public override Vector3 ProvideVector3(string node)
        {
            if (node == headPositionNode)
            {
                return Camera.main.transform.position;
            }
            if (node == leftHandPositionNode)
            {
                return GetActiveHandPosition(modalityManager.leftController,
                                                modalityManager.leftHand);
            }
            if (node == rightHandPositionNode)
            {
                return GetActiveHandPosition(modalityManager.rightController,
                                                modalityManager.rightHand);
            }

            return default(Vector3);
        }

        public override Quaternion ProvideQuaternion(string node)
        {
            if (node == headPositionNode)
            {
                return Camera.main.transform.rotation;
            }
            if (node == leftHandRotationNode)
            {
                return GetActiveHandRotation(modalityManager.leftController,
                                                modalityManager.leftHand);
            }
            if (node == rightHandRotationNode)
            {
                return GetActiveHandRotation(modalityManager.rightController,
                                                modalityManager.rightHand);
            }

            return default(Quaternion);
        }
#endif
    }
}