using UnityEngine;

namespace Ubiq.Compatibility.XRI.TraditionalControls
{
    public class TraditionalControlsMobileOrientation : MonoBehaviour
    {
        public GameObject portrait;
        public GameObject landscape;

        private void Update()
        {
            var active = portrait;
            var inactive = landscape;
            if (Screen.width > Screen.height)
            {
                active = landscape;
                inactive = portrait;
            }
            
            if (!active.activeSelf)
            {
                active.SetActive(true);
            }
            if (inactive.activeSelf)
            {
                inactive.SetActive(false);
            }
        }
    }
}