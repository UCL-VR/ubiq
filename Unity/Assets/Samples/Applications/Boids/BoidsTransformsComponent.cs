using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using Ubiq.Messaging;
using UnityEditor;
using UnityEngine;

namespace Ubiq.Samples.Boids
{
    [RequireComponent(typeof(Boids))]
    public class BoidsTransformsComponent : MonoBehaviour, INetworkComponent
    {
        private NetworkContext context;

        private TransformMessage[] transforms;

        private Boids flock;

        private void Awake()
        {
            flock = GetComponent<Boids>();
        }

        // Start is called before the first frame update
        void Start()
        {
            context = NetworkScene.Register(this);
        }

        // Update is called once per frame
        void Update()
        {
            if (flock.local)
            {
                Send();
            }
        }

        private void Send()
        {
            if (transforms == null || transforms.Length != flock.boids.Length)
            {
                transforms = new TransformMessage[flock.boids.Length];
            }

            for (int i = 0; i < flock.boids.Length; i++)
            {
                transforms[i].position = flock.boids[i].transform.localPosition;
                transforms[i].rotation = flock.boids[i].transform.localRotation;
            }

            var transformsBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<TransformMessage>(transforms));

            var message = ReferenceCountedSceneGraphMessage.Rent(transformsBytes.Length);
            transformsBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));

            context.Send(message);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            if (transforms == null || transforms.Length != flock.boids.Length)
            {
                transforms = new TransformMessage[flock.boids.Length];
            }

            MemoryMarshal.Cast<byte, TransformMessage>(
                new ReadOnlySpan<byte>(message.bytes, message.start, message.length))
                .CopyTo(
                new Span<TransformMessage>(transforms));

            for (int i = 0; i < flock.boids.Length; i++)
            {
                flock.boids[i].transform.localPosition = transforms[i].position;
                flock.boids[i].transform.localRotation = transforms[i].rotation;
            }
        }
    }
}