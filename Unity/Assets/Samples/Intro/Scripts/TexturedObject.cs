using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ubiq.Messaging;

public class TexturedObject : MonoBehaviour, INetworkComponent
{
    public ImageCatalogue catalogue;
    
    private Material material;
    private Texture2D texture;
    private new Renderer renderer;
    private NetworkContext context;

    public struct Message
    {
        public string matName;

        public Message(string matName)
        {
            this.matName = matName;
        }

    }

    public void SetMaterial(Material mat)
    {
        material = mat;
        renderer.material = mat;
    }

    public void SetTexture(Texture2D tex)
    {
        texture = tex;
        renderer.material.SetTexture("_MainTex", tex);
    }

    public Material GetMaterial()
    {
        return material;
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

    public void SetNetworkedMaterial(Material mat)
    {
        SetMaterial(mat);
        Debug.Log("Set networked material: " + mat.name);
        context.SendJson(new Message(mat.name));
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        SetMaterial(catalogue.GetMaterial(msg.matName));
    }
}
