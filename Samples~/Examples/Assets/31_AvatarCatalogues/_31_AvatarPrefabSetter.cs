using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;

namespace Ubiq.Examples
{
    public class _31_AvatarPrefabSetter : MonoBehaviour
    {
        public void Set(int index)
        {
            var manager = GetComponent<AvatarManager>();
            manager.avatarPrefab = manager.avatarCatalogue.prefabs[index];
        }
    }
}