using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

namespace Ubiq.Avatars
{
    public class GripAvatarHintProvider : AvatarHintProvider
    {
        public HandController controller;

        public override float ProvideFloat()
        {
            return controller ? controller.GripValue : 0.0f;
        }
    }
}
