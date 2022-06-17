using System;
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

        /// <summary>
        /// Returns the Component closest to this one in the Scene Graph. This method will first search for descendents of this node, then
        /// the parents of this node recursively. If that fails, it will begin to search each Scene in a random order, starting with the
        /// DontDestroyOnLoad scene.
        /// Use this method to find Components that should be under the same, e.g. NetworkScene, according to the Ubiq rules.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
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

        public static T GetClosestPredicate<T>(this Component component, Func<T, bool> predicate) where T : class
        {
            do
            {
                var behaviours = component.GetComponentsInChildren<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is T)
                    {
                        if (predicate(behaviour as T))
                        {
                            return behaviour as T;
                        }
                    }
                }
                component = component.transform.parent;
            } while (component != null);

            foreach (var root in DontDestroyOnLoadGameObjects)
            {
                var behaviours = root.GetComponentsInChildren<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is T)
                    {
                        if (predicate(behaviour as T))
                        {
                            return behaviour as T;
                        }
                    }
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var root in scene.GetRootGameObjects())
                {
                    var behaviours = root.GetComponentsInChildren<MonoBehaviour>();
                    foreach (var behaviour in behaviours)
                    {
                        if (behaviour is T)
                        {
                            if (predicate(behaviour as T))
                            {
                                return behaviour as T;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static IEnumerable<T> GetInterfaces<T>(this Component component) where T : class
        {
            foreach (var item in component.GetComponents<MonoBehaviour>())
            {
                if (item is T)
                {
                    yield return item as T;
                }
            }
        }

        public static T GetInterface<T>(this MonoBehaviour component) where T : class
        {
            foreach (var item in component.GetComponents<MonoBehaviour>())
            {
                if (item is T)
                {
                    return item as T;
                }
            }
            return null;
        }

        public static IEnumerable<T> GetComponentsInScene<T>() where T : Component
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var item in root.GetComponentsInChildren<T>())
                    {
                        yield return item;
                    }
                }
            }
        }
    }
}