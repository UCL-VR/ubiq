using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

namespace Ubiq.Avatars
{
    /// <summary>
    /// A helper Component to tell the closest AvatarManager to use the nominated
    /// transforms of the Bot to control the Avatar.
    /// </summary>
    public class BotAvatarHelper : MonoBehaviour
    {
        public GameObject AvatarPrefab;

        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;

        [SerializeField] private string headPositionNode = "HeadPosition";
        [SerializeField] private string headRotationNode = "HeadRotation";
        [SerializeField] private string leftHandPositionNode = "LeftHandPosition";
        [SerializeField] private string leftHandRotationNode = "LeftHandRotation";
        [SerializeField] private string rightHandPositionNode = "RightHandPosition";
        [SerializeField] private string rightHandRotationNode = "RightHandRotation";

        private AvatarManager manager;

        private void Awake()
        {
            manager = AvatarManager.Find(this);
        }

        private void Start()
        {
            if (manager && AvatarPrefab)
            {
                manager.avatarPrefab = AvatarPrefab;
                SetTransformProvider(headPositionNode, headRotationNode, head);
                SetTransformProvider(leftHandPositionNode, leftHandRotationNode, leftHand);
                SetTransformProvider(rightHandPositionNode, rightHandRotationNode, rightHand);
            }
        }

        private void SetTransformProvider (string posNode, string rotNode, Transform transform)
        {
            if (posNode == string.Empty && rotNode == string.Empty)
            {
                return;
            }

            var hp = gameObject.AddComponent<TransformAvatarHintProvider>();
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
