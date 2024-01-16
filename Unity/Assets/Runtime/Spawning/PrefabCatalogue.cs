using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Spawning
{
    [CreateAssetMenu(menuName = "Prefab Catalogue")]
    public class PrefabCatalogue : ScriptableObject
    {
        public List<GameObject> prefabs;
    }
}