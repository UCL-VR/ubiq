using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Dictionaries;
using Ubiq.Avatars;
using UnityEngine.Events;
using UnityEngine;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Rooms;
using Ubiq.Messaging;

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
        private string uuid;
        private RoomClient roomClient;

        private Texture2D cached; // Cache for GetTexture. Do not do anything else with this; use the uuid

        private void Awake()
        {
            avatar = GetComponent<Avatar>();
            avatar.OnPeerUpdated.AddListener(OnPeerUpdated);
        }

        private void Start()
        {
            roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();

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
            SetTexture(peer["ubiq.avatar.texture.uuid"]);
        }

        /// <summary>
        /// Try to set the Texture by reference to a Texture in the Catalogue. If the Texture is not in the
        /// catalogue then this method has no effect, as Texture2Ds cannot be streamed yet.
        /// </summary>
        public void SetTexture(Texture2D texture)
        {
            SetTexture(Textures.Get(texture));
        }

        public void SetTexture(string uuid)
        {
            if(String.IsNullOrWhiteSpace(uuid))
            {
                return;
            }

            if (this.uuid != uuid)
            {
                var texture = Textures.Get(uuid);
                this.uuid = uuid;
                this.cached = texture;

                OnTextureChanged.Invoke(texture);

                if(avatar.IsLocal)
                {
                    roomClient.Me["ubiq.avatar.texture.uuid"] = this.uuid;
                }

                if (avatar.IsLocal && SaveTextureSetting)
                {
                    SaveSettings();
                }
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetString("ubiq.avatar.texture.uuid", uuid);
        }

        private bool LoadSettings()
        {
            var uuid = PlayerPrefs.GetString("ubiq.avatar.texture.uuid", "");
            SetTexture(uuid);
            return !String.IsNullOrWhiteSpace(uuid);
        }

        public void ClearSettings()
        {
            PlayerPrefs.DeleteKey("ubiq.avatar.texture.uuid");
        }

        public Texture2D GetTexture()
        {
            return cached;
        }
    }
}