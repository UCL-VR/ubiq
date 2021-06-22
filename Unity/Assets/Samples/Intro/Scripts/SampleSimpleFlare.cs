using System;
using Ubiq.Messaging;
using Ubiq.XR;
using UnityEngine;

namespace Ubiq.Samples
{
    public class SampleSimpleFlare : MonoBehaviour, INetworkObject, INetworkComponent, IGraspable, IUseable
    {
        public bool owner = false;

        private NetworkContext context;
        private ParticleSystem particles;
        private Rigidbody body;

        private Transform follow;

        [Serializable]
        private class State
        {
            public TransformMessage transform;
            public bool lit;
        }

        private State state = new State();

        private void Awake()
        {
            particles = GetComponentInChildren<ParticleSystem>();
            body = GetComponent<Rigidbody>();
        }

        public void Grasp(Hand controller)
        {
            follow = controller.transform;
            owner = true;
        }

        public void Release(Hand controller)
        {
            follow = null;
        }

        public void Use(Hand controller)
        {
            state.lit = true;
        }

        public void UnUse(Hand controller)
        {
            state.lit = false;
        }

        public NetworkId Id { get; private set; }

        // Start is called before the first frame update
        void Start()
        {
            Id = NetworkScene.ObjectIdFromName(this);
            context = NetworkScene.Register(this);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            JsonUtility.FromJsonOverwrite(message.ToString(), state);
        }

        // Update is called once per frame
        void Update()
        {
            if (owner)
            {
                if (follow != null)
                {
                    transform.position = follow.transform.position;
                    transform.rotation = follow.transform.rotation;
                    body.isKinematic = true;
                }
                else
                {
                    body.isKinematic = false;
                }

                state.transform.position = transform.localPosition;
                state.transform.rotation = transform.localRotation;

                context.Send(ReferenceCountedSceneGraphMessage.Rent(JsonUtility.ToJson(state)));
            }
            else
            {
                transform.localPosition = state.transform.position;
                transform.localRotation = state.transform.rotation;
                body.isKinematic = true;
            }

    #pragma warning disable 0618
            particles.enableEmission = state.lit; //using deprecated one because its not clear how to update the non-deprecated emissions struct. play/stop dont do the same thing.
    #pragma warning restore 0618
        }
    }
}