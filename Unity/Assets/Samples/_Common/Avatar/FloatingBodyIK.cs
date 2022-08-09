using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FloatingBodyIK : MonoBehaviour {

    protected Animator animator;

    public Transform leftHandTarget;
    public Transform rightHandTarget;

    private void Start ()
    {
        animator = GetComponent<Animator>();
    }

    //a callback for calculating IK
    private void OnAnimatorIK()
    {
        if(animator) {
            if(rightHandTarget != null) {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand,1);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand,1);
                animator.SetIKPosition(AvatarIKGoal.RightHand,rightHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand,rightHandTarget.rotation);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand,0);
            }

            if(leftHandTarget != null) {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,1);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand,leftHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand,leftHandTarget.rotation);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,0);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,0);
            }
        }
    }
}
