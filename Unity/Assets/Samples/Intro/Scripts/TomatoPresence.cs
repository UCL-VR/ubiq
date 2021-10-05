using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Avatars;
using Avatar = Ubiq.Avatars.Avatar;
/// <summary>
///  Hides the hands when objects are grasped
/// </summary>
public class TomatoPresence : MonoBehaviour, INetworkComponent
{
    public GameObject leftHand;
    public GameObject rightHand;
    
    private Renderer leftHandRenderer;
    private Renderer rightHandRenderer;
    private Outline leftHandOutline;
    private Outline rightHandOutline;
    private HandAnimation handAnimation;

    private bool isGraspingLeft;
    private bool previousStateRight = false;
    private bool isGraspingRight;

    private NetworkContext context;
    private Avatar avatar;

    public struct Message
    {
        public bool l; // left hand grasp
        public bool r; // right hand grasp

        public Message(bool l, bool r)
        {
            this.l = l;
            this.r = r;
        }
    }


    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        Message msg = message.FromJson<Message>();
        previousStateRight = isGraspingRight;
        isGraspingLeft = msg.l;
        isGraspingRight = msg.r;
    }


    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
        leftHandRenderer = leftHand.GetComponent<Renderer>();
        rightHandRenderer = rightHand.GetComponent<Renderer>();
        leftHandOutline = leftHand.GetComponent<Outline>();
        rightHandOutline = rightHand.GetComponent<Outline>();
        handAnimation = GetComponent<HandAnimation>();
        avatar = GetComponent<Avatar>();
    }

    // Update is called once per frame
    void Update()
    {
        if (avatar.IsLocal)
        {
            isGraspingLeft = GetHintNode(AvatarHints.NodeBool.LeftGraspObject);
            isGraspingRight = GetHintNode(AvatarHints.NodeBool.RightGraspObject);
            context.SendJson(new Message(isGraspingLeft, isGraspingRight));
        }

        if (isGraspingLeft)
        {
            leftHandRenderer.enabled = false;
            leftHandOutline.enabled = false;
            handAnimation.enabled = false;
        }
        else
        {
            leftHandRenderer.enabled = true;
            leftHandOutline.enabled = true;
            handAnimation.enabled = true;
        }
        if (isGraspingRight)
        {
            rightHandRenderer.enabled = false;
            rightHandOutline.enabled = false;
            handAnimation.enabled = false;
        }
        else
        {
            rightHandRenderer.enabled = true;
            rightHandOutline.enabled = true;
            handAnimation.enabled = true;
        }
    }
    private bool GetHintNode(AvatarHints.NodeBool node)
    {
        if (AvatarHints.TryGet(node, out bool nodeBool))
        {
            return nodeBool;
        }
        return false;
    }
}
