using System.Collections;
using System.Collections.Generic;
using Ubiq.Avatars;
using UnityEngine;

namespace Ubiq.Samples
{
    public class AvatarMenuController : MonoBehaviour
    {
        public GameObject[] avatars;
        public AvatarManager manager;

        public void ChangeAvatar(GameObject prefab)
        {
            if (Application.isPlaying)
            {
                manager.CreateLocalAvatar(prefab);
            }
        }

        public TexturedAvatar GetSkinnable()
        {
            if (manager.LocalAvatar)
            {
                return manager.LocalAvatar.GetComponent<TexturedAvatar>();
            }
            return null;
        }
    }
}