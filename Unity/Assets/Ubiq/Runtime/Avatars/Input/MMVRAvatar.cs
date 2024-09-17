using System;
using System.Runtime.InteropServices;
using Ubiq.Messaging;
using Ubiq.MotionMatching;
using UnityEngine;
using UnityEngine.Events;
using Avatar = Ubiq.Avatars.Avatar;

namespace Ubiq
{
    /// <summary>
    /// Makes <see cref="IMMVRInput"/> information available to an
    /// avatar. Input will be sourced from <see cref="Avatar.input"/> if this is
    /// a local avatar or over the network if this is remote.
    /// </summary>
    public class MMVRAvatar : MonoBehaviour
    {
        [Tooltip("The Avatar to use as the source of input. If null, will try to find an Avatar among parents at start.")]
        [SerializeField] private Avatar avatar;
        
        /// <summary>
        /// Event triggered whenever a new set of poses is received, locally or
        /// over the network. Will be triggered even if no value is changed.
        /// changed.
        /// </summary>
        public PosesUpdatedEvent OnPosesUpdated;
        [Serializable] public class PosesUpdatedEvent : UnityEvent { }
        
        /// <summary>
        /// Event triggered whenever the value of  <see cref="hasInput"/> is
        /// changed.
        /// </summary>
        public HasInputChangedEvent OnHasInputChanged;
        [Serializable] public class HasInputChangedEvent : UnityEvent { }
        
        /// <summary>
        /// Pose of this peer's neck. Sanitised for better results
        /// through MMVR. Invalid if <see cref="hasInput"/> is false. 
        /// </summary>
        public Pose neck { get; private set; }
        
        /// <summary>
        /// Pose of this peer's left hand. Sanitised for better results
        /// through MMVR. Invalid if <see cref="hasInput"/> is false. 
        /// </summary>
        public Pose leftHand { get; private set; }
        
        /// <summary>
        /// Pose of this peer's right hand. Sanitised for better results
        /// through MMVR. Invalid if <see cref="hasInput"/> is false. 
        /// </summary>
        public Pose rightHand { get; private set; }
        
        /// <summary>
        /// Pose of this peer's left leg. Sanitised for better results
        /// through MMVR. Invalid if <see cref="hasInput"/> is false. 
        /// </summary>
        public LegPose leftLeg { get; private set; }
        
        /// <summary>
        /// Pose of this peer's right leg. Sanitised for better results
        /// through MMVR. Invalid if <see cref="hasInput"/> is false. 
        /// </summary>
        public LegPose rightLeg { get; private set; }
        
        /// <summary>
        /// True when this class is being driven by input. May be false if the
        /// avatar has not yet gone through setup, or if currently no input is
        /// being provided (f.ex., the user local to this avatar removes their
        /// headset).
        /// </summary>
        public bool hasInput { get; private set; }

        [Serializable]
        private struct State
        {
            public Pose neck;
            public Pose leftHand;
            public Pose rightHand;
            public LegPose leftLeg;
            public LegPose rightLeg;
        }
        
        private State[] state = new State[1];
        private NetworkContext context;
        private Transform networkSceneRoot;
        private float lastTransmitTime;
        
        private void Start()
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
            
            context = NetworkScene.Register(this, NetworkId.Create(avatar.NetworkId, nameof(HeadAndHandsAvatar)));
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
            state[0] = avatar.input.TryGet(out IMMVRInput src)
                ? new State
                {
                    neck = src.neck,
                    leftHand = src.leftHand,
                    rightHand = src.rightHand,
                    leftLeg = src.leftLeg,
                    rightLeg = src.rightLeg
                }
                : new State
                {
                    neck = GetInvalidPose(),
                };

            // Send it through network
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                lastTransmitTime = Time.time;
                Send();
            }

            // Update local listeners
            OnStateChange();
        }
    
        private void Send()
        {
            var transformBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<State>(state));

            var message = ReferenceCountedSceneGraphMessage.Rent(transformBytes.Length);
            transformBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));

            context.Send(message);
        }
        
        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            MemoryMarshal.Cast<byte, State>(
                    new ReadOnlySpan<byte>(message.bytes, message.start, message.length))
                .CopyTo(new Span<State>(state));
            OnStateChange();
        }
            
        // State has been set either remotely or locally so update listeners
        private void OnStateChange ()
        {
            if (IsInvalid(state[0].neck))
            {
                if (hasInput)
                {
                    hasInput = false;
                    OnHasInputChanged.Invoke();
                }
                return;
            }
            
            if (!hasInput)
            {
                hasInput = true;
                OnHasInputChanged.Invoke();
            }
            
            neck = state[0].neck;
            leftHand = state[0].leftHand;
            rightHand = state[0].rightHand;
            leftLeg = state[0].leftLeg;
            rightLeg = state[0].rightLeg;
            
            OnPosesUpdated.Invoke();
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