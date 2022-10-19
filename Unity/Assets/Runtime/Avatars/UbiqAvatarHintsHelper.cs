using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(AvatarManager))]
    public class UbiqAvatarHintsHelper : MonoBehaviour
    {
        [SerializeField] private string headPositionNode = "HeadPosition";
        [SerializeField] private string headRotationNode = "HeadRotation";
        [SerializeField] private string leftHandPositionNode = "LeftHandPosition";
        [SerializeField] private string leftHandRotationNode = "LeftHandRotation";
        [SerializeField] private string leftWristPositionNode = "LeftWristPosition";
        [SerializeField] private string leftWristRotationNode = "LeftWristRotation";
        [SerializeField] private string rightHandPositionNode = "RightHandPosition";
        [SerializeField] private string rightHandRotationNode = "RightHandRotation";
        [SerializeField] private string rightWristPositionNode = "RightWristPosition";
        [SerializeField] private string rightWristRotationNode = "RightWristRotation";
        [SerializeField] private string leftGripNode = "LeftGrip";
        [SerializeField] private string rightGripNode = "RightGrip";

        private void Start()
        {
            var pcs = FindObjectsOfType<XRPlayerController>(includeInactive:true);

            if (pcs.Length == 0)
            {
                Debug.LogWarning("No Ubiq player controller found");
            }
            else if (pcs.Length > 1)
            {
                Debug.LogWarning("Multiple Ubiq player controllers found. Using: " + pcs[0].name);
            }

            var pc = pcs[0];
            SetTransformProvider(headPositionNode,headRotationNode,
                pc.GetComponentInChildren<Camera>()?.transform);

            var hcs = pc.GetComponentsInChildren<HandController>();

            GetLeftHand(hcs, out var leftHand, out var leftWrist, out var leftHc);
            SetTransformProvider(leftHandPositionNode,leftHandRotationNode,leftHand);
            SetTransformProvider(leftWristPositionNode,leftWristRotationNode,leftWrist);
            SetGripProvider(leftGripNode,leftHc);

            GetRightHand(hcs, out var rightHand, out var rightWrist, out var rightHc);
            SetTransformProvider(rightHandPositionNode,rightHandRotationNode,rightHand);
            SetTransformProvider(rightWristPositionNode,rightWristRotationNode,rightWrist);
            SetGripProvider(rightGripNode,rightHc);
        }

        private void GetLeftHand(HandController[] handControllers,
            out Transform hand, out Transform wrist, out HandController handController)
        {
            if (handControllers != null && handControllers.Length > 0)
            {
                foreach(var hc in handControllers)
                {
                    if (hc.Left)
                    {
                        hand = hc.transform.Find("Anchor/Hints/Hand");
                        wrist = hc.transform.Find("Anchor/Hints/Wrist");
                        handController = hc;
                        return;
                    }
                }
            }
            hand = null;
            wrist = null;
            handController = null;
        }

        private void GetRightHand(HandController[] handControllers,
            out Transform hand, out Transform wrist, out HandController handController)
        {
            if (handControllers != null && handControllers.Length > 0)
            {
                foreach(var hc in handControllers)
                {
                    if (hc.Right)
                    {
                        hand = hc.transform.Find("Anchor/Hints/Hand");
                        wrist = hc.transform.Find("Anchor/Hints/Wrist");
                        handController = hc;
                        return;
                    }
                }
            }
            hand = null;
            wrist = null;
            handController = null;
        }

        private void SetTransformProvider (string posNode, string rotNode, Transform transform)
        {
            if (posNode == string.Empty && rotNode == string.Empty)
            {
                return;
            }

            if (!transform)
            {
                Debug.LogWarning("Could not find a hint source. Has the Ubiq player prefab changed?");
                return;
            }

            var hp = gameObject.AddComponent<TransformAvatarHintProvider>();
            var manager = GetComponent<AvatarManager>();
            hp.hintTransform = transform;
            if (posNode != string.Empty)
            {
                manager.hints.SetProvider(posNode,AvatarHints.Type.Vector3,hp);
            }
            if (rotNode != string.Empty)
            {
                manager.hints.SetProvider(rotNode,AvatarHints.Type.Quaternion,hp);
            }
        }

        private void SetGripProvider (string node, HandController handController)
        {
            if (node == string.Empty)
            {
                return;
            }

            if (!handController)
            {
                Debug.LogWarning("Could not find a hint source. Has the Ubiq player prefab changed?");
                return;
            }

            var hp = gameObject.AddComponent<GripAvatarHintProvider>();
            var manager = GetComponent<AvatarManager>();
            hp.controller = handController;
            manager.hints.SetProvider(node,AvatarHints.Type.Float,hp);
        }
    }
}
