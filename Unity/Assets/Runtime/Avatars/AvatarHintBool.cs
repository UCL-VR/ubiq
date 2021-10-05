using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

namespace Ubiq.Avatars
{
    public class AvatarHintBool : MonoBehaviour, IAvatarHintProvider<bool>
    {
        public AvatarHints.NodeBool node;

        // Add components here that need a bool hint
        public GraspableObjectGrasper graspableObjectGrasper;

        void OnEnable()
        {
            AvatarHints.AddProvider(node, this);
        }

        void OnDisable()
        {
            AvatarHints.RemoveProvider(node, this);
        }

        public bool Provide()
        {
            if (graspableObjectGrasper != null)
            {
                return graspableObjectGrasper.isGrasping();
            }
            else
            {
                Debug.Log("No GraspableObjectGrasper assigned");
                return false;
            }
        }
    }

}
