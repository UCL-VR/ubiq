using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.NetworkedBehaviour;

namespace Ubiq.Examples
{
    /// <summary>
    /// This class shows how to implement a networked object using
    /// NetworkedBehaviours.
    ///
    /// In Start, the Component is registered with NetworkedBehaviours.Register
    /// and from then on all its serialisable members are synchronised
    /// automatically across all Peers.
    /// Serialisable members are public fields, and private fields with the
    /// [SerializeField] attribute.
    ///
    /// This class also has the NetworkTransform attribute applied. When this
    /// attribute is present, the GameObjects Transform will be synchronised
    /// as well.
    /// </summary>
    [NetworkTransform]
    public class _13_NetworkedBehaviourCube : MonoBehaviour
    {
        // The synchronisation follows the same rules as the Unity Json
        // serialiser: we can synchronise fields of simple types, including
        // our own types. We can also synchronise lists, but not dictionaries.
        // See here for full info on what can be synchronised:
        // https://docs.unity3d.com/ScriptReference/JsonUtility.ToJson.html
        [HideInInspector] public Color color;

        private void Start()
        {
            NetworkedBehaviours.Register(this);
        }

        private void Update()
        {
            GetComponent<Renderer>().material.color = color;
        }
    }
}