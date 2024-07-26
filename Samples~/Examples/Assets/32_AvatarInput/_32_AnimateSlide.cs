using UnityEngine;

namespace Ubiq.Examples
{
    public class _32_AnimateSlide : MonoBehaviour
    {
        private float theta = 0.0f;
        
        private void Update()
        {
            theta += Time.deltaTime * 3.0f;
            transform.position = new Vector3(
                Mathf.Sin(theta) - 3.0f,
                transform.position.y,
                transform.position.z);
        }
    }
}