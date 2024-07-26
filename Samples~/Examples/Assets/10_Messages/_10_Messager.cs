using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

namespace Ubiq.Examples
{
    public class _10_Messager : MonoBehaviour
    {
        private NetworkContext context;

        private void Start()
        {
            context = NetworkScene.Register(this);
        }

        public void SetColor(Color32 color)
        {
            GetComponentInChildren<Renderer>().material.color = color;

            if (context.Scene != null)
            {
                context.SendJson<Color32>(color);
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var color = message.FromJson<Color32>();
            GetComponentInChildren<Renderer>().material.color = color;
        }
    }
}