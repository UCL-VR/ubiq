using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Avatars
{
    [CreateAssetMenu(menuName = "Avatar Texture Catalogue")]
    public class AvatarTextureCatalogue : ScriptableObject, IEnumerable<Texture2D>
    {
        public List<Texture2D> Textures;

        public bool Contains(Texture2D texture)
        {
            return Textures.Contains(texture);
        }

        public IEnumerator<Texture2D> GetEnumerator()
        {
            return Textures.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Textures.GetEnumerator();
        }

        public int Count
        {
            get
            {
                return Textures.Count;
            }
        }

        public Texture2D Get(int i)
        {
            return Textures[i];
        }

        public Texture2D Get(string luid)
        {
            if(luid == null)
            {
                return null;
            }
            return Textures[int.Parse(luid)];
        }

        public string Get(Texture2D texture)
        {
            var index = Textures.IndexOf(texture);
            return index > -1 ? $"{index}" : null;
        }
    }
}