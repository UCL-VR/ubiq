using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Image Catalogue")]
public class ImageCatalogue : ScriptableObject
{
    public List<Texture2D> images;

    public Texture2D GetRandomImage()
    {
        var i = Random.Range(0, images.Count);
        return images[i];
    }
    public Texture2D GetImage(string name)
    {
        foreach (var i in images)
        {
            if (i.name == name)
            {
                return i;
            }
        }
        return null;
    }

    public Texture2D Get(string uid)
    {
        if (uid == null)
        {
            return null;
        }
        return images[int.Parse(uid)];
    }

    public string Get(Texture2D texture)
    {
        var index = images.IndexOf(texture);
        return index > -1 ? $"{index}" : null;
    }
}
