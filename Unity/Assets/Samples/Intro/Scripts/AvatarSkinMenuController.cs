using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Avatars;
using UnityEngine;
using UnityEngine.UI;
using Avatar = Ubiq.Avatars.Avatar;

namespace Ubiq.Samples
{
    public class AvatarSkinMenuController : MonoBehaviour
    {
        public AvatarManager Manager;
        public TexturedAvatar PreviewAvatar;
        public GameObject ControlPrefab;
        public GameObject Container;

        private AvatarTextureCatalogue Textures;
        private List<AvatarSkinMenuControl> Controls;

        private TexturedAvatar PreviewTexturedAvatar;

        private void Awake()
        {
            Controls = new List<AvatarSkinMenuControl>();
        }

        void Update()
        {
            if(Manager.LocalAvatar != null)
            {
                var Textured = Manager.LocalAvatar.GetComponent<TexturedAvatar>();
                if (Textured != null)
                {
                    if (Textures != Textured.Textures)
                    {
                        Textures = Textured.Textures;

                        while (Controls.Count < Textures.Count)
                        {
                            Controls.Add(GameObject.Instantiate(ControlPrefab, Container.transform).GetComponent<AvatarSkinMenuControl>());
                        }
                        while (Controls.Count > Textures.Count)
                        {
                            Destroy(Controls[0].gameObject);
                            Controls.RemoveAt(0);
                        }
                        for (int i = 0; i < Textures.Count; i++)
                        {
                            Controls[i].Bind(this, Textures.Get(i));
                        }
                    }

                    PreviewAvatar.Textures = Textured.Textures;
                    // PreviewAvatar.SetTexture(Textured.GetTexture());
                }
            }
        }

        public void ChangeTexture(Texture2D texture)
        {
            if(Manager.LocalAvatar.GetComponent<TexturedAvatar>() != null)
            {
                Manager.LocalAvatar.GetComponent<TexturedAvatar>().SetTexture(texture);
            }
        }
    }
}