using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_VR || ENABLE_AR
using UnityEngine.XR;
#endif

// Some subsystems (like Quest) use Device tracking origin by default. We need
// to force it to use the floor as the origin. This class now applies camera
// offset for any user where no subsystems are present (i.e., desktop users),
// regardless of tracking origin
namespace Ubiq.XR
{
    public class CameraOffsetter : MonoBehaviour
    {
        public float cameraOffset = 1.36144f;
        public Transform offsetRootObject;

#if ENABLE_VR || ENABLE_AR
        private static List<XRInputSubsystem> tmpSubsystems = new List<XRInputSubsystem>();
        private List<XRInputSubsystem> lastSubsystems = new List<XRInputSubsystem>();

        private void Update()
        {
            SubsystemManager.GetSubsystems<XRInputSubsystem>(tmpSubsystems);
            if (!Equals(tmpSubsystems, lastSubsystems))
            {
                Copy(tmpSubsystems, lastSubsystems);

                var offset = cameraOffset;
                foreach(var subsystem in lastSubsystems)
                {
                    if (subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                    {
                        offset = 0.0f;
                    }
                }

                offsetRootObject.localPosition = Vector3.up * offset;
            }
        }

        private bool Equals(List<XRInputSubsystem> a, List<XRInputSubsystem> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private void Copy(List<XRInputSubsystem> from, List<XRInputSubsystem> to)
        {
            to.Clear();
            foreach(var s in from)
            {
                to.Add(s);
            }
        }
#endif
    }
}