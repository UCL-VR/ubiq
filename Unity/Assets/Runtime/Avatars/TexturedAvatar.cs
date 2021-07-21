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
    public class TexturedAvatar : MonoBehaviour, INetworkComponent
    {
        public AvatarTextureCatalogue Textures;
        public bool RandomTextureOnSpawn;
        public bool SaveTextureSetting;

        [Serializable]
        public class TextureEvent : UnityEvent<Texture2D> { }
        public TextureEvent OnTextureChanged;

        private Avatar avatar;
        private string uuid;

        private NetworkContext context;

        private Texture2D cached; // Cache for GetTexture. Do not do anything else with this; use the uuid.

        private void Awake()
        {
            avatar = GetComponent<Avatar>();
            context = NetworkScene.Register(this);
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

        public struct Message
        {
            public string uuid;

            public Message(string uuid)
            {
                this.uuid = uuid;
            }
        }

        void OnPeerUpdated(IPeer peer)
        {
            SetTexture(peer["ubiq.avatar.texture.uuid"]);
        }

        public string GetTextureUuid()
        {
            return uuid;
        }

        /// <summary>
        /// Try to set the Texture by reference to a Texture in the Catalogue. If the Texture is not in the 
        /// catalogue then this method has no effect, as Texture2Ds cannot be streamed yet.
        /// </summary>
        public void SetTexture(Texture2D texture)
        {
            SetTexture(Textures.Get(texture));
        }

        public void SetTextureFromRemote(string uuid)
        {
            if (String.IsNullOrWhiteSpace(uuid))
            {
                return;
            }
            if (this.uuid != uuid)
            {
                var texture = Textures.Get(uuid);
                this.uuid = uuid;
                this.cached = texture;

                Debug.Log("Remote set texture: " + uuid);
                OnTextureChanged.Invoke(texture);

            }
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
                    avatar.Peer["ubiq.avatar.texture.uuid"] = this.uuid;
                }                

                if (avatar.IsLocal && SaveTextureSetting)
                {
                    SaveSettings();
                }
                if (avatar.Peer.UUID == null && !avatar.IsLocal) // e.g. recorded avatar which does not represent a valid peer
                {
                    Debug.Log("Local send texture: " + uuid);
                    //context.SendJson(new Message(uuid));
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

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            Debug.Log("Remote Set Texture: " + msg.uuid);
            SetTextureFromRemote(msg.uuid);
        }
    }
}