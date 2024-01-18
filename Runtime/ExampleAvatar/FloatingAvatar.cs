using Ubiq.Avatars;
using UnityEngine;

namespace Ubiq.Samples
{
    /// <summary>
    /// Recroom/rayman style avatar with hands, torso and head
    /// </summary>
    public class FloatingAvatar : MonoBehaviour
    {
        public Transform head;
        public Transform torso;
        public Transform leftHand;
        public Transform rightHand;

        public Renderer headRenderer;
        public Renderer torsoRenderer;
        public Renderer leftHandRenderer;
        public Renderer rightHandRenderer;

        public Transform baseOfNeckHint;

        // public float torsoFacingHandsWeight;
        public AnimationCurve torsoFootCurve;

        public AnimationCurve torsoFacingCurve;

        private TexturedAvatar texturedAvatar;
        private ThreePointTrackedAvatar trackedAvatar;
        private Vector3 footPosition;
        private Quaternion torsoFacing;

        private void OnEnable()
        {
            trackedAvatar = GetComponentInParent<ThreePointTrackedAvatar>();

            if (trackedAvatar)
            {
                trackedAvatar.OnHeadUpdate.AddListener(ThreePointTrackedAvatar_OnHeadUpdate);
                trackedAvatar.OnLeftHandUpdate.AddListener(ThreePointTrackedAvatar_OnLeftHandUpdate);
                trackedAvatar.OnRightHandUpdate.AddListener(ThreePointTrackedAvatar_OnRightHandUpdate);
            }

            texturedAvatar = GetComponentInParent<TexturedAvatar>();

            if (texturedAvatar)
            {
                texturedAvatar.OnTextureChanged.AddListener(TexturedAvatar_OnTextureChanged);
            }
        }

        private void OnDisable()
        {
            if (trackedAvatar && trackedAvatar != null)
            {
                trackedAvatar.OnHeadUpdate.RemoveListener(ThreePointTrackedAvatar_OnHeadUpdate);
                trackedAvatar.OnLeftHandUpdate.RemoveListener(ThreePointTrackedAvatar_OnLeftHandUpdate);
                trackedAvatar.OnRightHandUpdate.RemoveListener(ThreePointTrackedAvatar_OnRightHandUpdate);
            }

            if (texturedAvatar && texturedAvatar != null)
            {
                texturedAvatar.OnTextureChanged.RemoveListener(TexturedAvatar_OnTextureChanged);
            }
        }

        private void ThreePointTrackedAvatar_OnHeadUpdate(Vector3 pos, Quaternion rot)
        {
            head.position = pos;
            head.rotation = rot;
        }

        private void ThreePointTrackedAvatar_OnLeftHandUpdate(Vector3 pos, Quaternion rot)
        {
            leftHand.position = pos;
            leftHand.rotation = rot;
        }

        private void ThreePointTrackedAvatar_OnRightHandUpdate(Vector3 pos, Quaternion rot)
        {
            rightHand.position = pos;
            rightHand.rotation = rot;
        }

        private void TexturedAvatar_OnTextureChanged(Texture2D tex)
        {
            headRenderer.material.mainTexture = tex;
            torsoRenderer.material = headRenderer.material;
            leftHandRenderer.material = headRenderer.material;
            rightHandRenderer.material = headRenderer.material;
        }

        private void Update()
        {
            UpdateTorso();
        }

        private void UpdateTorso()
        {
            // Give torso a bit of dynamic movement to make it expressive

            // Update virtual 'foot' position, just for animation, wildly inaccurate :)
            var neckPosition = baseOfNeckHint.position;
            footPosition.x += (neckPosition.x - footPosition.x) * Time.deltaTime * torsoFootCurve.Evaluate(Mathf.Abs(neckPosition.x - footPosition.x));
            footPosition.z += (neckPosition.z - footPosition.z) * Time.deltaTime * torsoFootCurve.Evaluate(Mathf.Abs(neckPosition.z - footPosition.z));
            footPosition.y = 0;

            // Forward direction of torso is vector in the transverse plane
            // Determined by head direction primarily, hint provided by hands
            var torsoRotation = Quaternion.identity;

            // Head: Just use head direction
            var headFwd = head.forward;
            headFwd.y = 0;

            // Hands: Imagine line between hands, take normal (in transverse plane)
            // Use head orientation as a hint to give us which normal to use
            // var handsLine = rightHand.position - leftHand.position;
            // var handsFwd = new Vector3(-handsLine.z,0,handsLine.x);
            // if (Vector3.Dot(handsFwd,headFwd) < 0)
            // {
            //     handsFwd = new Vector3(handsLine.z,0,-handsLine.x);
            // }
            // handsFwdStore = handsFwd;

            // var headRot = Quaternion.LookRotation(headFwd,Vector3.up);
            // var handsRot = Quaternion.LookRotation(handsFwd,Vector3.up);

            // // Rotation is handsRotation capped to a distance from headRotation
            // var headToHandsAngle = Quaternion.Angle(headRot,handsRot);
            // Debug.Log(headToHandsAngle);
            // var rot = Quaternion.RotateTowards(headRot,handsRot,Mathf.Clamp(headToHandsAngle,-torsoFacingHandsWeight,torsoFacingHandsWeight));

            // // var rot = Quaternion.SlerpUnclamped(handsRot,headRot,torsoFacingHeadToHandsWeightRatio);

            var rot = Quaternion.LookRotation(headFwd, Vector3.up);
            var angle = Quaternion.Angle(torsoFacing, rot);
            var rotateAngle = Mathf.Clamp(Time.deltaTime * torsoFacingCurve.Evaluate(Mathf.Abs(angle)), 0, angle);
            torsoFacing = Quaternion.RotateTowards(torsoFacing, rot, rotateAngle);

            // Place torso so it makes a straight line between neck and feet
            torso.position = neckPosition;
            torso.rotation = Quaternion.FromToRotation(Vector3.down, footPosition - neckPosition) * torsoFacing;
        }

        // private Vector3 handsFwdStore;

        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.blue;
        //     Gizmos.DrawLine(head.position, footPosition);
        //     // Gizmos.DrawLine(head.position,head.position + handsFwdStore);
        // }
    }
}