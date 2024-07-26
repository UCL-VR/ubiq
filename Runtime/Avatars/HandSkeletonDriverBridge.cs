using UnityEngine;
using UnityEngine.Serialization;
using Handedness = Ubiq.HandSkeleton.Handedness;

namespace Ubiq
{
    /// <summary>
    /// Connects <see cref="HandSkeletonAvatar"/> and
    /// <see cref="HandSkeletonDriver"/>. Note that this will not set the bone
    /// mapping as it is unique to each hand model.
    /// </summary>
    public class HandSkeletonDriverBridge : MonoBehaviour
    {
        [Tooltip("The source of the hand skeleton. If null, will try to find a HandSkeletonInput among parents at start.")]
        [SerializeField] private HandSkeletonAvatar handSkeletonAvatar;
        [Tooltip("The driver responsible for manipulating the bones. If null, will try to find a HandSkeletonDriver among children at start.")]
        [SerializeField] private HandSkeletonDriver handSkeletonDriver;
        [Tooltip("Whether this component drives the left or right hand. Invalid handedness means no hand will be driven.")]
        [SerializeField] private Handedness _handedness; 
        
        /// <summary>
        /// Whether this component drives the left or right hand. Invalid
        /// handedness means no hand will be driven.
        /// </summary>
        public Handedness handedness
        {
            get => _handedness;
            set
            {
                if (value == _handedness)
                {
                    return;
                }

                UnsubscribeEvents();
                _handedness = value;
                SubscribeEvents();
            }
        }
        
        private void Start()
        {
            if (!handSkeletonAvatar)
            {
                handSkeletonAvatar = GetComponentInParent<HandSkeletonAvatar>();

                if (!handSkeletonAvatar)
                {
                    Debug.LogWarning("No HandSkeletonAvatar could be found among parents. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            if (!handSkeletonDriver)
            {
                handSkeletonDriver = GetComponentInChildren<HandSkeletonDriver>();
                
                if (!handSkeletonDriver)
                {
                    Debug.LogWarning("No HandSkeletonDriver could be found among children. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            SubscribeEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        
        private void SubscribeEvents()
        {
            switch (handedness)
            {
                case Handedness.Left : 
                    handSkeletonAvatar?.OnLeftHandUpdate.AddListener(Events_OnHandUpdate); break;
                case Handedness.Right : 
                    handSkeletonAvatar?.OnRightHandUpdate.AddListener(Events_OnHandUpdate); break;
            }
        }
        
        private void UnsubscribeEvents()
        {
            switch (handedness)
            {
                case Handedness.Left : 
                    handSkeletonAvatar?.OnLeftHandUpdate.RemoveListener(Events_OnHandUpdate); break;
                case Handedness.Right : 
                    handSkeletonAvatar?.OnRightHandUpdate.RemoveListener(Events_OnHandUpdate); break;
            }
        }
        
        private void Events_OnHandUpdate(HandSkeleton skeleton)
        {
            handSkeletonDriver?.SetPoses(skeleton);
        }
    }
}