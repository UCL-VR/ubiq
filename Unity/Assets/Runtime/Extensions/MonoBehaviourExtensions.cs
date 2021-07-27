using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ubiq.Extensions
{
    public static class MonoBehaviourExtensions
    {
        // The Constructor is executed the first time the Static Class is accessed (which can even be seen in the callstack)
        // (https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors)
        static MonoBehaviourExtensions()
        {
            DontDestroyOnLoadGameObjects = new List<GameObject>();
        }

        /// <summary>
        /// The DontDestroyOnLoad scene is inaccessible at runtime (https://docs.unity3d.com/Manual/MultiSceneEditing.html),
        /// meaning objects that are moved under it cannot be found with the GetClosestComponent method.
        /// This helper member allows objects to register themselves, should they hold Components that may be required by
        /// callers of this method.
        /// Objects must add themselves to this list explicitly when they call DontDestroyOnLoad; they will not be added automatically.
        /// </summary>
        public static List<GameObject> DontDestroyOnLoadGameObjects { get; private set; }

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

            foreach (var root in DontDestroyOnLoadGameObjects)
            {
                var behaviour = root.GetComponentInChildren<T>();
                if (behaviour)
                {
                    return behaviour;
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var root in scene.GetRootGameObjects())
                {
                    var behaviour = root.GetComponentInChildren<T>();
                    if (behaviour)
                    {
                        return behaviour;
                    }
                }
            }

            return null;
        }
    }
}