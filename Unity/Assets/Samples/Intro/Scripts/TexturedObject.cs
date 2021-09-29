using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;

public class TexturedObject : MonoBehaviour, INetworkComponent, ITexturedObject
{
    public ImageCatalogue catalogue;

    private string uuid;
    
    private Material material;
    private Texture2D texture;
    private new Renderer renderer;
    private NetworkContext context;

    public struct Message
    {
        public string imageName;

        public Message(string imageName)
        {
            this.imageName = imageName;
        }

    }

    //public void SetMaterial(Material mat)
    //{
    //    material = mat;
    //    renderer.material = mat;
    //}

    public void SetTexture(string uid)
    {
        SetTexture(catalogue.Get(uid));
    }

    public void SetTexture(Texture2D tex)
    {
        texture = tex;
        renderer.material.SetTexture("_MainTex", tex);
        material = renderer.material;
        uuid = catalogue.Get(tex);
    }

    //public Material GetMaterial()
    //{
    //    return material;
    //}

    public string GetTextureUid()
    {
        return uuid;
    }

    public Texture2D GetTexture()
    {
        return texture;
    }
    
    // Start is called before the first frame update
    void Awake()
    {
        renderer = GetComponentInChildren<Renderer>();
        context = NetworkScene.Register(this);

    }
    void Start()
    {
        if (context == null)
        {
            context = NetworkScene.Register(this);
        }
    }

    public void SetNetworkedTexture(Texture2D tex)
    {
        SetTexture(tex);
        Debug.Log("Set networked texture: " + tex.name);
        context.SendJson(new Message(tex.name));
    }

    //public void SetNetworkedMaterial(Material mat)
    //{
    //    SetMaterial(mat);
    //    Debug.Log("Set networked material: " + mat.name);
    //    context.SendJson(new Message(mat.name));
    //}

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        SetTexture(catalogue.GetImage(msg.imageName));
    }
}
