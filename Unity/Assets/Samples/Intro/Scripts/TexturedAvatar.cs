using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Dictionaries;
using Ubiq.Avatars;
using UnityEngine.Events;
using UnityEngine;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    [RequireComponent(typeof(Avatar))]
    public class TexturedAvatar : MonoBehaviour
    {
        [Serializable]
        private class TextureConfig
        {
            public List<string> ids = new List<string>();
            public List<string> uuids = new List<string>();
        }

        public AvatarTextureCatalogue Textures;
        public bool RandomTextureOnSpawn;
        public bool SaveTextureSetting;

        [Serializable]
        public class TextureEvent : UnityEvent<Texture2D, string> { }
        public TextureEvent OnTextureChanged;

        private Avatar avatar;
        private TextureConfig config = new TextureConfig();
        private string configString;

        private void Awake()
        {
            avatar = GetComponent<Avatar>();
            avatar.OnPeerUpdated.AddListener(OnPeerUpdated);
        }

        private void Start()
        {
            if (avatar.IsLocal)
            {
                var hasSavedSettings = false;
                if (SaveTextureSetting)
                {
                    hasSavedSettings = LoadSettings();
                }
                if (!hasSavedSettings && RandomTextureOnSpawn)
                {
                    SetTexture(Textures.Get(UnityEngine.Random.Range(0, Textures.Count)));
                }
            }
        }

        void OnPeerUpdated(IPeer peer)
        {
            SetTexture(peer["ubiq.samples.social.texture"]);
        }

        /// <summary>
        /// Try to set the Texture by reference to a Texture in the Catalogue. If the Texture is not in the
        /// catalogue then this method has no effect, as Texture2Ds cannot be streamed yet.
        /// </summary>
        public void SetTexture(Texture2D texture)
        {
            SetTexture(texture,"");
        }

        public void SetTexture(Texture2D texture, string id)
        {
            SetTexture(Textures.Get(texture),id);
        }

        public void SetTexture(string uuid)
        {
            SetTexture(uuid,"");
        }

        public void SetTexture(string uuid, string id)
        {
            if (uuid == null)
            {
                return;
            }

            var index = config.ids.IndexOf(id);

            // Add texture to config if id is new
            if (index < 0)
            {
                config.ids.Add(id);
                config.uuids.Add(null);
                index = config.ids.Count-1;
            }

            // Load and set texture for uuid if different from existing record
            if (config.uuids[index] != uuid)
            {
                var texture = Textures.Get(uuid);
                config.uuids[index] = uuid;
                configString = JsonUtility.ToJson(config);

                OnTextureChanged.Invoke(texture,id);

                if(avatar.IsLocal)
                {
                    avatar.Peer["ubiq.samples.social.texture"] = configString;
                }

                if (avatar.IsLocal && SaveTextureSetting)
                {
                    SaveSettings();
                }
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetString("ubiq.samples.social.texture", JsonUtility.ToJson(config));
        }

        private bool LoadSettings()
        {
            configString = PlayerPrefs.GetString("ubiq.samples.social.texture", "");

            if (!string.IsNullOrEmpty(configString))
            {
                config = JsonUtility.FromJson<TextureConfig>(configString);

                for (int i = 0; i < config.ids.Count; i++)
                {
                    SetTexture(config.uuids[i],config.ids[i]);
                }

                return true;
            }

            return false;
        }

        public void ClearSettings()
        {
            PlayerPrefs.DeleteKey("ubiq.samples.social.texture");
        }
    }
}