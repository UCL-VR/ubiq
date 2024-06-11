using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;

namespace Ubiq.Examples
{
    public class _09_HintSwapper : MonoBehaviour
    {
        public Transform movingHint;
        public TransformAvatarHintProvider movingHintProvider;
        public TransformAvatarHintProvider defaultHintProvider;

        private AvatarManager avatarManager;

        private Coroutine animatePositionCoroutine;
        private Coroutine animateRotationCoroutine;

        private void Start()
        {
            avatarManager = GetComponent<AvatarManager>();
        }

        public void TogglePositionHint()
        {
            if (animatePositionCoroutine != null)
            {
                StopCoroutine(animatePositionCoroutine);
                animatePositionCoroutine = null;
                avatarManager.hints.SetProvider("Position",AvatarHints.Type.Vector3,defaultHintProvider);
            }
            else
            {
                animatePositionCoroutine = StartCoroutine(AnimatePosition());
                avatarManager.hints.SetProvider("Position",AvatarHints.Type.Vector3,movingHintProvider);
            }
        }

        public void ToggleRotationHint()
        {
            if (animateRotationCoroutine != null)
            {
                StopCoroutine(animateRotationCoroutine);
                animateRotationCoroutine = null;
                avatarManager.hints.SetProvider("Rotation",AvatarHints.Type.Quaternion,defaultHintProvider);
            }
            else
            {
                animateRotationCoroutine = StartCoroutine(AnimateRotation());
                avatarManager.hints.SetProvider("Rotation",AvatarHints.Type.Quaternion,movingHintProvider);
            }
        }

        private IEnumerator AnimatePosition()
        {
            var theta = 0.0f;
            var pos = movingHint.position;
            while (true)
            {
                theta += Time.deltaTime * 3.0f;
                pos.x = Mathf.Sin(theta) - 3.0f;
                movingHint.position = pos;
                yield return null;
            }
        }

        private IEnumerator AnimateRotation()
        {
            while (true)
            {
                movingHint.Rotate(Vector3.up * Time.deltaTime * 180.0f);
                yield return null;
            }
        }
    }
}