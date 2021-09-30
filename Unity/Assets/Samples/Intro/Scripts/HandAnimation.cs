using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;

[RequireComponent(typeof(Animator))]
public class HandAnimation : MonoBehaviour
{
    public AvatarHints.NodeFloat node;
    public float speed;

    private Animator animator;
    private float gripTarget;
    private float gripCurrent;
    private string animatorGripParam = "Grip";

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        AnimateHand();
    }

    private void AnimateHand()
    {
        gripTarget = GetHintNode(node);
        
        if (gripCurrent != gripTarget)
        {
            gripCurrent = Mathf.MoveTowards(gripCurrent, gripTarget, Time.deltaTime * speed);
            animator.SetFloat(animatorGripParam, gripCurrent);
        }
    }

    private float GetHintNode(AvatarHints.NodeFloat node)
    {
        if (AvatarHints.TryGet(node, out float nodeFloat))
        {
            return nodeFloat;
        }
        return 0.0f;
    }
}
