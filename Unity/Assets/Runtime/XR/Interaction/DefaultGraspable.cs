using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.XR
{
    [RequireComponent(typeof(Rigidbody))]

    // This default class is needed because the default behaviour is just to get picked up and moved around without further scripting
    public class DefaultGraspable : MonoBehaviour, IGraspable
    {
        public void Grasp(Hand proxy)
        { }

        public void Release(Hand proxy)
        { }
    }
}
