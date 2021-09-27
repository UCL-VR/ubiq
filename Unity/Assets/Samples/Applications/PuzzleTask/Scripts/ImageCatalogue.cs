using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Image Catalogue")]
public class ImageCatalogue : ScriptableObject
{
    public List<Material> materials;

    public Material GetRandomMaterial()
    {
        var i = Random.Range(0, materials.Count);
        return materials[i];
    }
    public Material GetMaterial(string name)
    {
        foreach (var m in materials)
        {
            if (m.name == name)
            {
                return m;
            }
        }
        return null;
    }
}
