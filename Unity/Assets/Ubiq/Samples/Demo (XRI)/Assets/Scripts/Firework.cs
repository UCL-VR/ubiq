using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.Geometry;
#if XRI_3_0_7_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
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

#if XRI_3_0_7_OR_NEWER

        private NetworkContext context;
        private Vector3 flightForce;
        private float explodeTime;

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
            grab.selectExited.AddListener(Interactable_SelectExited);
        }

        private void Interactable_SelectExited(SelectExitEventArgs eventArgs)
        {
            if (fired)
            {
                return;
            }
            
            fired = true;
            
            flightForce = new Vector3(
                x:(Random.value - 0.5f)*0.05f, 
                y:3.0f, 
                z:(Random.value - 0.5f)*0.05f);
            explodeTime = Time.time + 10.0f; 

            // No longer interactable
            var interactable = (XRGrabInteractable)eventArgs.interactableObject;
            interactable.enabled = false;
        }

        private void FixedUpdate()
        {
            if(owner)
            {
                SendMessage();
            }
            if(owner && fired)
            {
                body.isKinematic = false;
                body.AddForce(flightForce, ForceMode.Force);

                if (!particles.isPlaying)
                {
                    particles.Play();
                    body.AddForce(transform.up * 2.0f, ForceMode.Impulse);
                }
                
                if (Time.time > explodeTime)
                {
                    NetworkSpawnManager.Find(this).Despawn(gameObject);
                    return;
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

        private struct Message
        {
            public Pose pose;
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