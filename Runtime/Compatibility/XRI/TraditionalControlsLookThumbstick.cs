#if XRI_3_0_7_OR_NEWER
using UnityEngine;

namespace Ubiq.XRI.TraditionalControls
{
    public class TraditionalControlsLookThumbstick : MonoBehaviour
    {
        public float sensitivity = 5.0f;
        public TraditionalControlsThumbstick thumbstick;
        public TraditionalControlsLookController look;

        private void Update()
        {
            if (!thumbstick.IsPressed())
            {
                return;
            }
            
            look.SuppressThisFrame();
            look.AddDelta(thumbstick.ReadCurrentValue()*sensitivity);
        }
    }
}
#endif