using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.Geometry;
#if XRI_2_5_2_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace Ubiq.Samples
{
    public class Firework : MonoBehaviour, INetworkSpawnable
    {
        private Rigidbody body;
        private ParticleSystem particles;

        public NetworkId NetworkId { get; set; }

        public bool owner;
        public bool fired;

#if XRI_2_5_2_OR_NEWER

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

            var grab = GetComponent<XRGrabInteractable>();
            grab.activated.AddListener(XRGrabInteractable_Activated);
        }

        public void XRGrabInteractable_Activated(ActivateEventArgs eventArgs)
        {
            fired = true;

            // Force the interactor(hand) to drop the firework
            var interactor = (XRBaseInteractor)eventArgs.interactorObject;
            interactor.allowSelect = false;
            var interactable = (XRGrabInteractable)eventArgs.interactableObject;
            interactable.enabled = false;
            interactor.allowSelect = true;
        }

        private void Update()
        {
            if(owner)
            {
                SendMessage();
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

        public struct Message
        {
            public PositionRotation pose;
            public bool fired;
        }

        private void SendMessage()
        {
            var message = new Message();
            message.pose = Transforms.ToLocal(transform,context.Scene.transform);
            message.fired = fired;
            context.SendJson(message);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            var pose = Transforms.ToWorld(msg.pose,context.Scene.transform);
            transform.position = pose.position;
            transform.rotation = pose.rotation;
            fired = msg.fired;
        }

#endif
    }
}