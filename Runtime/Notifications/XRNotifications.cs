using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Ubiq.XR.Notifications
{
    /// <summary>
    /// Maintains a set of events that can notify the system of XR-relevant
    /// changes. This class only defines the events. The events are fired by
    /// Compatability classes depending on the implementation. For example,
    /// there is a dedicated path for WebXR.
    /// </summary>
    public static class XRNotifications
    {
#pragma warning disable CS0067

        public delegate void HmdEvent();

        /// <summary>
        /// Fired when the HMD is placed on the users head
        /// </summary>
        public static event HmdEvent OnHmdMounted;

        /// <summary>
        /// Fired when the HMD is removed from the users head
        /// </summary>
        public static event HmdEvent OnHmdUnmounted;

        public static void HmdMounted()
        {
            OnHmdMounted?.Invoke();
        }

        public static void HmdUnmounted() 
        { 
            OnHmdUnmounted?.Invoke(); 
        }

#pragma warning restore CS0067
    }
}