using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Dictionaries;
using Ubiq.Avatars;
using UnityEngine.Events;
using UnityEngine;

namespace Ubiq.Samples
{
    [RequireComponent(typeof(TexturedAvatar))]
    public class SkinnedMeshAvatar : MonoBehaviour
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        private TexturedAvatar skinnable;

        private void Awake()
        {
            skinnable = GetComponent<TexturedAvatar>();

            skinnable.OnTextureChanged.AddListener(OnSkinUpdated);
        }

        private void OnDestroy ()
        {
            if (skinnable && skinnable != null)
            {
                skinnable.OnTextureChanged.RemoveListener(OnSkinUpdated);
            }
        }

        private void OnSkinUpdated (Texture2D skin)
        {
            // This should clone the material just once, and re-use the clone
            // on subsequent calls. Whole avatar can still use the one material
            var material = skinnedMeshRenderer.material;
            material.mainTexture = skin;

            skinnedMeshRenderer.material = material;
        }
    }
}