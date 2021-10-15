using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Avatar = Ubiq.Avatars.Avatar;

public class HandAnimation : MonoBehaviour, INetworkComponent
{
    //public AvatarHints.NodeFloat node;
    public float speed;

    public Animator leftHandAnimator;
    public Animator rightHandAnimator;

    private float gripTargetLeft;
    private float gripTargetRight;

    private float gripCurrentLeft;
    private float gripCurrentRight;

    private string animatorGripParam = "Grip";

    private NetworkContext context;
    private Avatar avatar;

    public struct Message
    {
        //public string animatorGripParam;
        public float l;
        public float r;

        public Message(float gripTargetL, float gripTargetR)
        {
            this.l = gripTargetL;
            this.r = gripTargetR;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
        avatar = GetComponent<Avatar>();

    }

    // Update is called once per frame
    void Update()
    {
        AnimateHands();
    }

    private void AnimateHands()
    {
        if (avatar.IsLocal)
        {
            gripTargetLeft = GetHintNode(AvatarHints.NodeFloat.LeftHandGrip);
            gripTargetRight = GetHintNode(AvatarHints.NodeFloat.RightHandGrip);

            context.SendJson(new Message(gripTargetLeft, gripTargetRight)); // sent every frame currently...put into if() ?
            //Debug.Log("Anim: " + gripCurrent + " " + gripTarget);
        }

        if (gripCurrentLeft != gripTargetLeft)
        {
            gripCurrentLeft = Mathf.MoveTowards(gripCurrentLeft, gripTargetLeft, Time.deltaTime * speed);
            leftHandAnimator.SetFloat(animatorGripParam, gripCurrentLeft);
        }
        if (gripCurrentRight != gripTargetRight)
        {
            gripCurrentRight = Mathf.MoveTowards(gripCurrentRight, gripTargetRight, Time.deltaTime * speed);
            rightHandAnimator.SetFloat(animatorGripParam, gripCurrentRight);
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

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        Message msg = message.FromJson<Message>();
        gripTargetLeft = msg.l;
        gripTargetRight = msg.r;
        //Debug.Log("Anim Remote: " + gripCurrent + " " + gripTarget);

    }
}
