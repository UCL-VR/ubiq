#if XRI_3_0_7_OR_NEWER
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace Ubiq.XRI.TraditionalControls
{
    public class TraditionalControlsWalkThumbstick : MonoBehaviour
    {
        private class MoveReader : IXRInputValueReader<Vector2>
        {
            private TraditionalControlsThumbstick thumbstick; 
            private float sensitivity;
                
            Vector2 IXRInputValueReader<Vector2>.ReadValue()
            {
                return thumbstick.ReadCurrentValue();
            }

            bool IXRInputValueReader<Vector2>.TryReadValue(out Vector2 value)
            {
                value = thumbstick.ReadCurrentValue() * sensitivity;
                return true;
            }
            
            public MoveReader(TraditionalControlsThumbstick thumbstick,float sensitivity)
            {
                this.thumbstick = thumbstick;
                this.sensitivity = sensitivity;
            }
        }
        
        public float sensitivity = 1.0f;
        public TraditionalControlsThumbstick thumbstick;
        public TraditionalControlsLookController look;
        public ContinuousMoveProvider moveProvider;
        
        private MoveReader moveReader;

        private void Start()
        {
            moveReader = new MoveReader(thumbstick, sensitivity);
        }

        private void Update()
        {
            if (!thumbstick.IsPressed())
            {
                if (moveProvider.leftHandMoveInput.bypass == moveReader)
                {
                    moveProvider.leftHandMoveInput.bypass = null;
                }
                return;
            }
            
            look.SuppressThisFrame();
            moveProvider.leftHandMoveInput.bypass = moveReader;
        }
        
    }
}
#endif