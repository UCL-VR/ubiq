using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.XR;
using UnityEngine;

namespace Ubiq.Samples
{
    public class SimpleBasketball : MonoBehaviour, IGraspable, INetworkObject, INetworkComponent, ISpawnable
    {
        private Hand follow;
        private NetworkContext context;
        private Rigidbody rb;

        public bool owner = true;

        public float throwStrength = 1f;

        public NetworkId Id { get; set; } = NetworkId.Unique();

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void OnSpawned(bool local)
        {
            owner = local;
        }

        public void Grasp(Hand controller)
        {
            follow = controller;
        }

        public void Release(Hand controller)
        {
            follow = null;
            rb.AddForce(-rb.velocity, ForceMode.VelocityChange);
            rb.AddForce(controller.velocity * throwStrength, ForceMode.VelocityChange);
        }

        // Start is called before the first frame update
        void Start()
        {
            context = NetworkScene.Register(this);
        }

        private void FixedUpdate()
        {
            if (follow != null)
            {
                owner = true;
                rb.isKinematic = false;
            }

            if (follow != null)
            {
                rb.AddForce(-rb.velocity, ForceMode.VelocityChange);
                rb.AddForce((follow.transform.position - rb.position) / Time.deltaTime, ForceMode.VelocityChange);
            }

            if (owner)
            {
                context.SendJson(new TransformMessage(transform));
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            // the other end has taken control of this object
            owner = false;
            rb.isKinematic = true;

            var state = message.FromJson<TransformMessage>();
            transform.localPosition = state.position;
            transform.localRotation = state.rotation;
        }
    }
}