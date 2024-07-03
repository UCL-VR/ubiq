using UnityEngine;
using UnityEngine.Serialization;
using Handedness = Ubiq.HandSkeleton.Handedness;

namespace Ubiq
{
    /// <summary>
    /// Connects <see cref="HandSkeletonInput"/> and
    /// <see cref="HandSkeletonDriver"/>. Note that this will not set the bone
    /// mapping as it is unique to each hand model.
    /// </summary>
    public class HandSkeletonListener : MonoBehaviour
    {
        [Tooltip("The source of the hand skeleton. If null, will try to find a HandSkeletonInput among parents at start.")]
        [SerializeField] private HandSkeletonInput input;
        [Tooltip("The driver responsible for manipulating the bones. If null, will try to find a HandSkeletonDriver among children at start.")]
        [SerializeField] private HandSkeletonDriver driver;
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
            if (!input)
            {
                input = GetComponentInParent<HandSkeletonInput>();

                if (!input)
                {
                    Debug.LogWarning("No HandSkeletonInput could be found among parents. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            if (!driver)
            {
                driver = GetComponentInChildren<HandSkeletonDriver>();
                
                if (!driver)
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
                    input?.OnLeftHandUpdate.AddListener(Events_OnHandUpdate); break;
                case Handedness.Right : 
                    input?.OnRightHandUpdate.AddListener(Events_OnHandUpdate); break;
            }
        }
        
        private void UnsubscribeEvents()
        {
            switch (handedness)
            {
                case Handedness.Left : 
                    input?.OnLeftHandUpdate.RemoveListener(Events_OnHandUpdate); break;
                case Handedness.Right : 
                    input?.OnRightHandUpdate.RemoveListener(Events_OnHandUpdate); break;
            }
        }
        
        private void Events_OnHandUpdate(HandSkeleton skeleton)
        {
            driver?.SetPoses(skeleton);
        }
    }
}