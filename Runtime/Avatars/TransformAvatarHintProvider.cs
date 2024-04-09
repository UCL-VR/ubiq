using UnityEngine;

namespace Ubiq.Avatars
{
    public class TransformAvatarHintProvider : AvatarHintProvider
    {
        public Transform hintTransform;

        public override Vector3 ProvideVector3(string node)
        {
            return hintTransform ? hintTransform.position : Vector3.zero;
        }

        public override Quaternion ProvideQuaternion(string node)
        {
            return hintTransform ? hintTransform.rotation : Quaternion.identity;
        }
    }
}
