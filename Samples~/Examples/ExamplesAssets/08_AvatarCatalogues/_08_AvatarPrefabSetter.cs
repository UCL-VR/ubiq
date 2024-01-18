using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;

namespace Ubiq.Samples
{
    public class _08_AvatarPrefabSetter : MonoBehaviour
    {
        public void Set(int index)
        {
            var manager = GetComponent<AvatarManager>();
            manager.avatarPrefab = manager.avatarCatalogue.prefabs[index];
        }
    }
}