using UnityEngine;

namespace Ubiq.Examples
{
    public class _32_AnimateSpin : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(Vector3.up * (Time.deltaTime * 180.0f));
        }
    }
}