using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexturedObject : MonoBehaviour
{
    private Texture2D texture;
    private GameObject quad;
    private new Renderer renderer;

    public void SetTexture(Texture2D tex)
    {
        texture = tex;
        renderer.material.SetTexture("_MainTex", tex);
    }

    public Texture2D GetTexture()
    {
        return texture;
    }
    
    // Start is called before the first frame update
    void Awake()
    {
        quad = gameObject.transform.GetChild(0).gameObject;
        renderer = quad.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
