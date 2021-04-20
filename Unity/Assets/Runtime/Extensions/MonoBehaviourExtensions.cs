using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ubiq.Extensions
{
    public static class MonoBehaviourExtensions
    {
        public static T GetClosestComponent<T>(this Component component) where T : MonoBehaviour
        {
            do
            {
                var behaviour = component.GetComponentInChildren<T>();
                if (behaviour)
                {
                    return behaviour;
                }
                component = component.transform.parent;
            } while (component != null);

            return null;
        }
    }
}