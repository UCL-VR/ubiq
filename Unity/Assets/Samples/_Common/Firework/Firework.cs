using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.XR;
using UnityEngine;

namespace Ubiq.Samples
{
    public class Firework : MonoBehaviour, IUseable, IFirework, INetworkSpawnable
    {
        private Hand attached;
        private Rigidbody body;
        private ParticleSystem particles;

        public NetworkId NetworkId { get; set; }

        public bool owner;
        public bool fired;

        private NetworkContext context;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            particles = GetComponentInChildren<ParticleSystem>();
            owner = false;
        }

        private void Start()
        {
            context = NetworkScene.Register(this);
        }

        public void Attach(Hand hand)
        {
            attached = hand;
            owner = true;
        }

        public void UnUse(Hand controller)
        {
        }

        public void Use(Hand controller)
        {
            attached = null;
            fired = true;
        }

        public struct Message
        {
            public TransformMessage transform;
            public bool fired;

            public Message(Transform transform, bool fired)
            {
                this.transform = new TransformMessage(transform);
                this.fired = fired;
            }
        }

        private void Update()
        {
            if(attached)
            {
                transform.position = attached.transform.position;
                transform.rotation = attached.transform.rotation;
            }
            if(owner)
            {
                context.SendJson(new Message(transform, fired));
            }
            if(owner && fired)
            {
                body.isKinematic = false;
                body.AddForce(transform.up * 0.75f, ForceMode.Force);

                if (!particles.isPlaying)
                {
                    particles.Play();
                    body.AddForce(new Vector3(Random.value, Random.value, Random.value) * 1.1f, ForceMode.Force);
                }
            }
            if(!owner && fired)
            {
                if (!particles.isPlaying)
                {
                    particles.Play();
                }
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            transform.localPosition = msg.transform.position; // The Message constructor will take the *local* properties of the passed transform.
            transform.localRotation = msg.transform.rotation;
            fired = msg.fired;
        }
    }
}