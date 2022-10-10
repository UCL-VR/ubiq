using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;

public class GripAnimator : MonoBehaviour
{

    public enum Side
    {
        Left,
        Right
    }

    // []
    public Side side;

    public Transform indexProximal;
    public Transform indexIntermediate;
    public Transform thumbProximal;
    public Transform thumbIntermediate;

    public Vector3 indexProximalGrippedEuler;
    public Vector3 indexIntermediateGrippedEuler;
    public Vector3 thumbProximalGrippedEuler;
    public Vector3 thumbIntermediateGrippedEuler;

    private Quaternion indexProximalOriginal;
    private Quaternion indexIntermediateOriginal;
    private Quaternion thumbProximalOriginal;
    private Quaternion thumbIntermediateOriginal;

    private Quaternion indexProximalTarget;
    private Quaternion indexIntermediateTarget;
    private Quaternion thumbProximalTarget;
    private Quaternion thumbIntermediateTarget;

    private ThreePointTrackedAvatar trackedAvatar;

    private float leftGrip;
    private float rightGrip;

    private void Start()
    {
        trackedAvatar = GetComponentInParent<ThreePointTrackedAvatar>();
        trackedAvatar.OnLeftGripUpdate.AddListener(OnLeftGripUpdate);
        trackedAvatar.OnRightGripUpdate.AddListener(OnRightGripUpdate);

        var animator = GetComponent<Animator>();
        if (animator && !indexProximal)
        {
            indexProximal = animator.GetBoneTransform(side == Side.Left
                ? HumanBodyBones.LeftIndexProximal
                : HumanBodyBones.RightIndexProximal);
        }
        if (animator && !indexIntermediate)
        {
            indexIntermediate = animator.GetBoneTransform(side == Side.Left
                ? HumanBodyBones.LeftIndexIntermediate
                : HumanBodyBones.RightIndexIntermediate);
        }
        if (animator && !thumbProximal)
        {
            thumbProximal = animator.GetBoneTransform(side == Side.Left
                ? HumanBodyBones.LeftThumbProximal
                : HumanBodyBones.RightThumbProximal);
        }
        if (animator && !thumbIntermediate)
        {
            thumbIntermediate = animator.GetBoneTransform(side == Side.Left
                ? HumanBodyBones.LeftThumbIntermediate
                : HumanBodyBones.RightThumbIntermediate);
        }

        if (indexProximal)
        {
            indexProximalOriginal = Quaternion.Euler(indexProximal.localEulerAngles);
        }
        if (indexIntermediate)
        {
            indexIntermediateOriginal = Quaternion.Euler(indexIntermediate.localEulerAngles);
        }
        if (thumbProximal)
        {
            thumbProximalOriginal = Quaternion.Euler(thumbProximal.localEulerAngles);
        }
        if (thumbIntermediate)
        {
            thumbIntermediateOriginal = Quaternion.Euler(thumbIntermediate.localEulerAngles);
        }

        this.indexProximalTarget = Quaternion.Euler(indexProximalGrippedEuler);
        this.indexIntermediateTarget = Quaternion.Euler(indexIntermediateGrippedEuler);
        this.thumbProximalTarget = Quaternion.Euler(thumbProximalGrippedEuler);
        this.thumbIntermediateTarget = Quaternion.Euler(thumbIntermediateGrippedEuler);
    }

    private void OnDestroy ()
    {
        if (trackedAvatar)
        {
            trackedAvatar.OnLeftGripUpdate.RemoveListener(OnLeftGripUpdate);
            trackedAvatar.OnRightGripUpdate.RemoveListener(OnRightGripUpdate);
        }
    }

    private void OnLeftGripUpdate (float leftGrip)
    {
        this.leftGrip = leftGrip;
    }

    private void OnRightGripUpdate (float rightGrip)
    {
        this.rightGrip = rightGrip;
    }

    private void LateUpdate()
    {
        // UpdateAnim()
    }

    private void UpdateAnim (float value)
    {
        if (indexProximal)
        {
            indexProximal.localRotation = Quaternion.Lerp(
                indexProximalOriginal,indexProximalTarget,value);
        }
        if (indexIntermediate)
        {
            indexIntermediate.localRotation = Quaternion.Lerp(
                indexIntermediateOriginal,indexIntermediateTarget,value);
        }
        if (thumbProximal)
        {
            thumbProximal.localRotation = Quaternion.Lerp(
                thumbProximalOriginal,thumbProximalTarget,value);
        }
        if (thumbIntermediate)
        {
            thumbIntermediate.localRotation = Quaternion.Lerp(
                thumbIntermediateOriginal,thumbIntermediateTarget,value);
        }
    }

}
