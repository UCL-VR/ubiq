using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Spawning
{
    [CreateAssetMenu(menuName = "Prefab Catalogue")]
    public class PrefabCatalogue : ScriptableObject
    {
        public List<GameObject> prefabs;

        private Dictionary<string, GameObject> prefabNames = new Dictionary<string, GameObject>();

        public void OnEnable()
        {
            if (prefabs != null)
            {
                foreach (var prefab in prefabs)
                {
                    if (prefab != null && !prefabNames.ContainsKey(prefab.name))
                    {
                        prefabNames.Add(prefab.name, prefab);
                        Debug.Log(prefab.name);
                    }
                }
            }
            Debug.Log("Prefabs Catalogue size: " + prefabNames.Count);
        }

        public int IndexOf(GameObject gameObject)
        {
            return prefabs.IndexOf(gameObject);
        }

        public GameObject GetPrefab(string prefabName)
        {
            try
            {
                return prefabNames[prefabName];
            }
            catch (KeyNotFoundException)
            {
                Debug.Log(prefabName + "is not in the Prefab Catalogue");
                return null;
            }
            
        }
    }
}