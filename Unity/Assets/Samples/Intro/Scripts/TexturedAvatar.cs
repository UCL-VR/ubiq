using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Dictionaries;
using Ubiq.Avatars;
using UnityEngine.Events;
using UnityEngine;
using Avatar = Ubiq.Avatars.Avatar;

namespace Ubiq.Samples
{
    [RequireComponent(typeof(Avatar))]
    public class TexturedAvatar : MonoBehaviour
    {
        public AvatarTextureCatalogue Textures;
        public bool RandomTextureOnSpawn;
        public bool SaveTextureSetting;

        [Serializable]
        public class TextureEvent : UnityEvent<Texture2D> { }
        public TextureEvent OnTextureChanged;

        private Avatar avatar;

        private string uid;
        private Texture2D texture; // Cache for GetTexture. Do not do anything else with this; use uid.

        private void Awake()
        {
            avatar = GetComponent<Avatar>();
            avatar.OnUpdated.AddListener(OnAvatarUpdated);
        }

        private void Start()
        {
            if(avatar.local)
            {
                var hasSavedSettings = false;
                if(SaveTextureSetting)
                {
                    hasSavedSettings = LoadSettings();
                }
                if(RandomTextureOnSpawn && !hasSavedSettings)
                {
                    SetTexture(Textures.Get(UnityEngine.Random.Range(0, Textures.Count)));
                }
            }
        }

        void OnAvatarUpdated(Avatar avatar)
        {
            var uid = avatar.Properties["texture-uid"];
            if(uid != null)
            {
                SetTexture(uid);
            }
        }

        public void SetTexture(Texture2D texture)
        {
            var uid = Textures.Get(texture);
            if (uid != null)
            {
                SetTexture(uid);
            }
        }

        public void SetTexture(string uid)
        {
            var texture = Textures.Get(uid);

            if (this.uid != uid)
            {
                OnTextureChanged.Invoke(texture);

                this.uid = uid;
                this.texture = texture;

                avatar.Properties["texture-uid"] = this.uid;

                if (avatar.local && SaveTextureSetting)
                {
                    SaveSettings();
                }
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetString("inbuilt-texturedavatar-uid", uid);
        }

        private bool LoadSettings()
        {
            var uid = PlayerPrefs.GetString("inbuilt-texturedavatar-uid", "");
            if (uid != "")
            {
                SetTexture(uid);
                return true;
            }
            return false;
        }

        public void ClearSettings()
        {
            PlayerPrefs.DeleteKey("inbuilt-texturedavatar-uid");
        }

        private void OnDestroy()
        {
            if (avatar != null && avatar.OnUpdated != null)
            {
                avatar.OnUpdated.RemoveListener(OnAvatarUpdated);
            }
        }

        public Texture2D GetTexture()
        {
            return texture;
        }
    }
}