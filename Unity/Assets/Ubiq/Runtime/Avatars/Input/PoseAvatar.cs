using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;
using Ubiq.Geometry;
using Avatar = Ubiq.Avatars.Avatar;

namespace Ubiq
{
    /// <summary>
    /// Makes <see cref="IPoseInput"/> information available to an
    /// avatar. Input will be sourced from <see cref="Avatar.input"/> if this is
    /// a local avatar or over the network if this is remote.
    /// </summary>
    public class PoseAvatar : MonoBehaviour
    {
        [Tooltip("The Avatar to use as the source of input. If null, will try to find an Avatar among parents at start.")]
        [SerializeField] private Avatar avatar;

        [Serializable]
        public class PoseUpdateEvent : UnityEvent<InputVar<Pose>> { }
        public PoseUpdateEvent OnPoseUpdate;

        private Pose[] state = new Pose[1];
        private NetworkContext context;
        private Transform networkSceneRoot;
        private float lastTransmitTime;
        
        protected void Start()
        {
            if (!avatar)
            {
                avatar = GetComponentInParent<Avatar>();
                if (!avatar)
                {
                    Debug.LogWarning("No Avatar could be found among parents. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            context = NetworkScene.Register(this, NetworkId.Create(avatar.NetworkId, nameof(PoseAvatar)));
            networkSceneRoot = context.Scene.transform;
            lastTransmitTime = Time.time;
        }

        private void Update ()
        {
            if (!avatar.IsLocal)
            {
                return;
            }
            
            // Update state from input
            state[0] = avatar.input.TryGet(out IPoseInput input)
                ? ToNetwork(input.pose)
                : ToNetwork(InputVar<Pose>.invalid);

            // Send it through network
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                lastTransmitTime = Time.time;
                Send();
            }

            // Update local listeners
            OnStateChange();
        }

        private Pose ToNetwork (InputVar<Pose> input)
        {
            return input.valid
                ? Transforms.ToLocal(input.value,networkSceneRoot)
                : GetInvalidPose();
        }

        private InputVar<Pose> FromNetwork (Pose net)
        {
            return !IsInvalid(net)
                ? new InputVar<Pose>(Transforms.ToWorld(net,networkSceneRoot))
                : InputVar<Pose>.invalid;
        }
        
        private void Send()
        {
            var transformBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<Pose>(state));

            var message = ReferenceCountedSceneGraphMessage.Rent(transformBytes.Length);
            transformBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));

            context.Send(message);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            MemoryMarshal.Cast<byte, Pose>(
                    new ReadOnlySpan<byte>(message.bytes, message.start, message.length))
                .CopyTo(new Span<Pose>(state));
            OnStateChange();
        }
        
        // State has been set either remotely or locally so update listeners
        private void OnStateChange ()
        {
            OnPoseUpdate.Invoke(FromNetwork(state[0]));
        }
        
        private static Pose GetInvalidPose()
        {
            return new Pose(new Vector3{x = float.NaN},Quaternion.identity);
        }
        
        private static bool IsInvalid(Pose p)
        {
            return float.IsNaN(p.position.x); 
        }
    }
}
