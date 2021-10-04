using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;


namespace Ubiq.Avatars
{

    public class AvatarHintFloat : MonoBehaviour, IAvatarHintProvider<float>
    {
        public AvatarHints.NodeFloat node;
        HandController controller;

        void OnEnable()
        {
            AvatarHints.AddProvider(node, this);
            if (node == AvatarHints.NodeFloat.LeftHandGrip || node == AvatarHints.NodeFloat.RightHandGrip)
            {
                controller = GetComponent<HandController>();
            }
        }

        void OnDisable()
        {
            AvatarHints.RemoveProvider(node, this);
        }

        public float Provide()
        {
            if (controller != null)
            {
                if(node == AvatarHints.NodeFloat.LeftHandGrip || node == AvatarHints.NodeFloat.RightHandGrip)
                {
                    return controller.GripValue;
                }
                else
                {
                    return 0.0f;
                }
                
            }
            else
            {
                return 0.0f;
            }
        }
    }

}

