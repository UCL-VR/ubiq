using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(AvatarManager))]
    public class BotAvatarHintsHelper : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;

        [SerializeField] private string headPositionNode = "HeadPosition";
        [SerializeField] private string headRotationNode = "HeadRotation";
        [SerializeField] private string leftHandPositionNode = "LeftHandPosition";
        [SerializeField] private string leftHandRotationNode = "LeftHandRotation";
        [SerializeField] private string rightHandPositionNode = "RightHandPosition";
        [SerializeField] private string rightHandRotationNode = "RightHandRotation";

        private void Start()
        {
            SetTransformProvider(headPositionNode,headRotationNode,head);
            SetTransformProvider(leftHandPositionNode,leftHandRotationNode,leftHand);
            SetTransformProvider(rightHandPositionNode,rightHandRotationNode,rightHand);
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
    }
}
