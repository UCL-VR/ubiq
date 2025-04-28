#if XRI_3_0_7_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Ubiq.XR.Notifications;

namespace Ubiq.XRI.TraditionalControls
{
    public class TraditionalControlsXRStateManager : MonoBehaviour
    {
        private List<InputDevice> hmds = new List<InputDevice>();
        private bool userPresent = false;

        void Start()
        {
            InputDevices.deviceConnected += InputDevices_deviceConnected;
            InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;

            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            foreach (var item in devices)
            {
                InputDevices_deviceConnected(item);
            }
        }

        private void InputDevices_deviceDisconnected(InputDevice obj)
        {
            hmds.Remove(obj);
        }

        private void InputDevices_deviceConnected(InputDevice obj)
        {
            if ((obj.characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
            {
                hmds.Add(obj);
            }
        }

        void Update()
        {
            bool anyPresent = false;

            foreach (var item in hmds)
            {
                bool present;
                if(item.TryGetFeatureValue(CommonUsages.userPresence, out present))
                {
                    if (present)
                    {
                        anyPresent = true;
                    }
                }
            }

            if(anyPresent && !userPresent)
            {
                userPresent = true;
                XRNotifications.HmdMounted();
            }
            else if(!anyPresent && userPresent)
            {
                userPresent = false;
                XRNotifications.HmdUnmounted();
            }
        }
    }
}
#endif