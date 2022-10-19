using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.NetworkedBehaviour;
using UnityEngine;

namespace Ubiq.Samples
{
    /// <summary>
    /// This class shows how multiple networked Components can be added to one
    /// GameObject and register correctly.
    /// This Component makes use of the NetworkedBehaviours functionality, 
    /// demonstrating how custom and NetworkedBehaviour synchronised classes
    /// can be mixed.
    /// </summary>
    public class HelloWorldCubeColour : MonoBehaviour
    {
        public Color color;

        private Material material;

        private void Awake()
        {
            material = GetComponent<MeshRenderer>().material;
        }

        void Start()
        {
            NetworkedBehaviours.Register(this);
        }

        void Update()
        {
            material.color = color;
        }
    }
}