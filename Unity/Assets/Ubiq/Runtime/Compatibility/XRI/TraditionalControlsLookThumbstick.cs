using UnityEngine;

namespace Ubiq.Compatibility.XRI.TraditionalControls
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
