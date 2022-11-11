using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;
using UnityEngine.Events;
using UnityEngine.XR.Management;
using static UnityEngine.SpatialTracking.TrackedPoseDriver;

namespace Ubiq.XR
{
    public interface IMenuButtonProvider
    {
        ButtonEvent MenuButtonPress { get; }
    }

    public interface IPrimaryButtonProvider
    {
        ButtonEvent PrimaryButtonPress { get; }
    }

    [Serializable]
    public class ButtonEvent : UnityEvent<bool>
    {
        public bool previousvalue;

        public void Update(bool newvalue)
        {
            if (newvalue != previousvalue)
            {
                previousvalue = newvalue;
                Invoke(newvalue);
            }
        }
    }

    [Serializable]
    public class SwipeEvent : UnityEvent<float>
    {
        public bool cooldown;
        public bool Trigger;
        public float Value;

        public void Update(float newvalue)
        {
            if (Mathf.Abs(newvalue) > 0.2f)
            {
                if (cooldown)
                {
                    Trigger = false;
                }
                else
                {
                    Trigger = true;
                    Value = newvalue;
                    cooldown = true;
                }
            }
            else
            {
                cooldown = false;
                Trigger = false;
                Value = 0;
            }
        }
    }

    /// <summary>
    /// A Hand represents an entity in the world that can manipulate objects. The hand does not have to have a mass or volume, but does have a physical presence.
    /// </summary>
    public class Hand : MonoBehaviour
    {
        [HideInInspector]
        public Vector3 velocity;
    }
}