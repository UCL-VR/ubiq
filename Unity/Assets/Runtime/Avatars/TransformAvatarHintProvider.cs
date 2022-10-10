using UnityEngine;

namespace Ubiq.Avatars
{
    public class TransformAvatarHintProvider : AvatarHintProvider
    {
        public Transform hintTransform;

        public override Vector3 ProvideVector3()
        {
            return hintTransform ? hintTransform.position : Vector3.zero;
        }

        public override Quaternion ProvideQuaternion()
        {
            return hintTransform ? hintTransform.rotation : Quaternion.identity;
        }
    }
}
