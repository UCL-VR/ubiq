using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;

namespace Ubiq.Examples
{
    public class _08_AvatarPrefabSetter : MonoBehaviour
    {
        public void Set(GameObject prefab)
        {
            var manager = GetComponent<AvatarManager>();
            manager.avatarPrefab = prefab;
        }
    }
}