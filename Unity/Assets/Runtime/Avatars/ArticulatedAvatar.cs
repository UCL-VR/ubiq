using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using Ubiq.Messaging;
using UnityEditor;
using UnityEngine;


namespace Ubiq.Avatars
{
    [RequireComponent(typeof(Avatar))]
    public class ArticulatedAvatar : MonoBehaviour, INetworkComponent
    {
        [HideInInspector]
        public Transform[] bones;

        private NetworkContext context;
        private TransformMessage[] transforms;
        private Avatar avatar;

        private void Awake()
        {
            avatar = GetComponent<Avatar>();
            if(bones == null)
            {
                bones = new Transform[0];
            }
        }

        private void Start()
        {
            context = NetworkScene.Register(this);
        }

        // Update is called once per frame
        void Update()
        {
            if(avatar.IsLocal)
            {
                Send();
            }
        }

        private void Send()
        {
            if (transforms == null || transforms.Length != bones.Length)
            {
                transforms = new TransformMessage[bones.Length + 1];
            }

            transforms[0].position = transform.localPosition;
            transforms[0].rotation = transform.localRotation;

            for (int i = 0; i < bones.Length; i++)
            {
                transforms[i + 1].position = bones[i].localPosition;
                transforms[i + 1].rotation = bones[i].localRotation;
            }

            var transformsBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<TransformMessage>(transforms));

            var message = ReferenceCountedSceneGraphMessage.Rent(transformsBytes.Length);
            transformsBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));

            context.Send(message);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var length = bones.Length + 1;

            if (transforms == null || transforms.Length != length)
            {
                transforms = new TransformMessage[length];
            }

            MemoryMarshal.Cast<byte, TransformMessage>(
                new ReadOnlySpan<byte>(message.bytes, message.start, message.length))
                .CopyTo(
                new Span<TransformMessage>(transforms));

            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = transforms[i + 1].position;
                bones[i].localRotation = transforms[i + 1].rotation;
            }

            transform.localPosition = transforms[0].position;
            transform.localRotation = transforms[0].rotation;
        }
    }
}