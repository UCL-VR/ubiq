using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Avatars
{
    [CreateAssetMenu(menuName = "Avatar Catalogue")]
    public class AvatarCatalogue : ScriptableObject
    {
        public List<GameObject> prefabs;

        public GameObject GetPrefab(string uuid)
        {
            foreach (var item in prefabs)
            {
                if (item.GetComponent<Avatar>().PrefabUuid == uuid)
                {
                    return item;
                }
            }

            throw new KeyNotFoundException("Avatar Uuid \"" + uuid + "\"");
        }
    }
}