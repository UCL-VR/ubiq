#if READYPLAYERME_0_0_0_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Voip;
using ReadyPlayerMe.AvatarLoader;
using Pose = UnityEngine.Pose;
using Joint = Ubiq.HandSkeleton.Joint;

namespace Ubiq.ReadyPlayerMe
{
    public class UbiqReadyPlayerMeAvatar : MonoBehaviour
    {
        public bool useEyeAnimations;
        public bool useVoiceAnimations;
        public bool useHandSkeletonAnimations;
        
        public GameObject speechIndicatorPrefab;
        
        private Vector3 headPositionOffset = new (0.0f,-0.063000001f,0.0f);
        private Quaternion headRotationOffset = new (0.0f,0.0f,0.0f,1.0f);
        
        [SerializeField] [HideInInspector] private List<Quaternion> leftGripRotations = new List<Quaternion>();
        [SerializeField] [HideInInspector] private List<Quaternion> rightGripRotations = new List<Quaternion>();
        [SerializeField] [HideInInspector] private List<Quaternion> leftReleaseRotations = new List<Quaternion>();
        [SerializeField] [HideInInspector] private List<Quaternion> rightReleaseRotations = new List<Quaternion>();
        
        private class HandInfo
        {
            public InputVar<Pose> pose = InputVar<Pose>.invalid;
            public Pose poseOffset;
            public InputVar<float> grip = InputVar<float>.invalid;
            public float lastGrip = -1;
            public Transform transform;
            public List<Transform> bones;
            public HandSkeleton skeleton;
            public HandSkeletonDriver driver;
            public List<Quaternion> gripRotations;
            public List<Quaternion> releaseRotations;
            public List<Vector3> initialLocalPositions;
        }
        
        private Vector3 speechIndicatorPositionOffset = new (0.0f,0.0f,0.0f);
        
        private UbiqReadyPlayerMeLoader loader;
        private HeadAndHandsAvatar headAndHandsAvatar;
        private HandSkeletonAvatar handSkeletonAvatar;
        
        private Transform head;
        private Transform armature;
        
        private InputVar<Pose> headInputVar;
        
        private HandInfo left;
        private HandInfo right;
        
        private void Start()
        {
            left = new HandInfo {
                poseOffset = new Pose(
                    new Vector3(-0.0149999997f,-0.0299999993f,-0.0700000003f),
                    new Quaternion(0.488744467f,0.575827956f,0.42057842f,0.502657831f)),
                gripRotations = leftGripRotations,
                releaseRotations = leftReleaseRotations
            };
            right = new HandInfo {
                poseOffset = new Pose(
                    new Vector3(0.0149999997f,-0.0299999993f,-0.0700000003f),
                    new Quaternion(-0.488744438f,0.575827897f,0.42057842f,-0.502657771f)),
                gripRotations = rightGripRotations,
                releaseRotations = rightReleaseRotations
            };
            
            headAndHandsAvatar = GetComponentInParent<HeadAndHandsAvatar>();
            Debug.Assert(headAndHandsAvatar,"Requires HeadAndHandsAvatar");
            headAndHandsAvatar.OnHeadUpdate.AddListener(HeadAndHandsEvents_OnHeadUpdate);
            headAndHandsAvatar.OnLeftHandUpdate.AddListener(HeadAndHandsEvents_OnLeftHandUpdate);
            headAndHandsAvatar.OnRightHandUpdate.AddListener(HeadAndHandsEvents_OnRightHandUpdate);
            headAndHandsAvatar.OnLeftGripUpdate.AddListener(HeadAndHandsEvents_OnLeftGripUpdate);
            headAndHandsAvatar.OnRightGripUpdate.AddListener(HeadAndHandsEvents_OnRightGripUpdate);
        
            handSkeletonAvatar = GetComponentInParent<HandSkeletonAvatar>();
            Debug.Assert(handSkeletonAvatar,"Requires HandSkeletonAvatar");
            handSkeletonAvatar.OnHandUpdate.AddListener(HandSkeletonEvents_OnHandUpdate);
            
            loader = GetComponent<UbiqReadyPlayerMeLoader>();
            Debug.Assert(loader,"Requires UbiqReadyPlayerMeLoader");
        
            loader.completed.AddListener(Loader_Completed);
            loader.failed.AddListener(Loader_Failed);
        
            if (loader.isLoaded)
            {
                Loader_Completed(loader.loadedArgs);
            }
        }
        
        private void OnDestroy()
        {
            if (headAndHandsAvatar)
            {
                headAndHandsAvatar.OnHeadUpdate.RemoveListener(HeadAndHandsEvents_OnHeadUpdate);
                headAndHandsAvatar.OnLeftHandUpdate.RemoveListener(HeadAndHandsEvents_OnLeftHandUpdate);
                headAndHandsAvatar.OnRightHandUpdate.RemoveListener(HeadAndHandsEvents_OnRightHandUpdate);
                headAndHandsAvatar.OnLeftGripUpdate.RemoveListener(HeadAndHandsEvents_OnLeftGripUpdate);
                headAndHandsAvatar.OnRightGripUpdate.RemoveListener(HeadAndHandsEvents_OnRightGripUpdate);
            }
            headAndHandsAvatar = null;
            
            if (handSkeletonAvatar)
            {
                handSkeletonAvatar.OnHandUpdate.RemoveListener(HandSkeletonEvents_OnHandUpdate);
            }
            handSkeletonAvatar = null;
        
            if (loader)
            {
                loader.completed.RemoveListener(Loader_Completed);
                loader.failed.RemoveListener(Loader_Failed);
            }
            loader = null;
        }
        
        private void UpdateHead()
        {
            if (!loader.isLoaded)
            {
                return;
            }
            
            if (!headInputVar.valid || !armature)
            {
                return;
            }
            
            var headLocalPos = armature.InverseTransformPoint(head.position);
            var headLocalRot = Quaternion.Inverse(armature.rotation) * head.rotation;
    
            var localHeadTRS = Matrix4x4.Translate(headLocalPos) 
                               * Matrix4x4.Rotate(headLocalRot);
            var armatureTRS = Matrix4x4.Translate(headInputVar.value.position) 
                              * Matrix4x4.Rotate(headInputVar.value.rotation) 
                              * localHeadTRS.inverse;
            armature.position = armatureTRS.GetPosition();
            armature.rotation = armatureTRS.rotation;
    
            armature.Translate(headPositionOffset,Space.Self);
            armature.localRotation *= headRotationOffset;
    
            var indicator = GetComponentInChildren<VoipSpeechIndicator>();
            indicator.transform.localPosition = speechIndicatorPositionOffset;
        }
        
        private void HeadAndHandsEvents_OnHeadUpdate(InputVar<Pose> input)
        {
            headInputVar = input;
            UpdateHead();
        }
        
        private void HeadAndHandsEvents_OnLeftHandUpdate(InputVar<Pose> input)
        {
            left.pose = input;
            UpdateHand(left);
        }
        
        private void HeadAndHandsEvents_OnRightHandUpdate(InputVar<Pose> input)
        {
            right.pose = input;
            UpdateHand(right);
        }
        
        private void HeadAndHandsEvents_OnLeftGripUpdate(InputVar<float> input)
        {
            left.grip = input;
            UpdateHand(left);
        }
        
        private void HeadAndHandsEvents_OnRightGripUpdate(InputVar<float> input)
        {
            right.grip = input;
            UpdateHand(right);
        }
        
        private void HandSkeletonEvents_OnHandUpdate(HandSkeleton hand)
        {
            if (hand.handedness == HandSkeleton.Handedness.Left)
            {
                left.skeleton = hand;
                UpdateHand(left);
            }
            else if (hand.handedness == HandSkeleton.Handedness.Right)
            {
                right.skeleton = hand;
                UpdateHand(right);
            }
        }
        
        private void UpdateHand(HandInfo hand)
        {
            if (!loader.isLoaded)
            {
                return;
            }
            
            if (hand.skeleton.TryGetPose(Joint.Wrist,out _))
            {
                hand.transform.localScale = Vector3.one;
                
                // If we can get a wrist pose, assume we have hand tracking
                hand.driver.SetPoses(hand.skeleton);
                
                // Mark grip animation dirty in case we switch back to controllers
                hand.lastGrip = -1;
                return;
            }
            
            if (hand.pose.valid)
            {
                hand.transform.localScale = Vector3.one;
                
                // We have a pose for the hand, probably from a controller
                hand.transform.SetPositionAndRotation(
                    hand.pose.value.position,
                    hand.pose.value.rotation);
                hand.transform.Translate(hand.poseOffset.position,Space.Self);
                hand.transform.localRotation *= hand.poseOffset.rotation;
                
                // We may also have data for a simple grip animation
                var grip = hand.grip.valid 
                    ? hand.grip.value 
                    : 0.0f;
                
                // Avoid unnecessary slerps
                if (!Mathf.Approximately(hand.lastGrip, grip))
                {
                    for(var i = 0; i < hand.bones.Count; i++)
                    {
                        hand.bones[i].localPosition = hand.initialLocalPositions[i];
                        hand.bones[i].localRotation = Quaternion.Slerp(
                            hand.releaseRotations[i],
                            hand.gripRotations[i],
                            grip);
                    }
                    hand.lastGrip = grip;
                }
                
                return;
            }
            
            // We have neither pose nor skeleton, so hide the hands
            hand.transform.localScale = Vector3.zero;
        }
        
        private void Loader_Completed(CompletionEventArgs args)
        {
            if (useEyeAnimations)
            {
                args.Avatar.AddComponent<EyeAnimationHandler>();
            }
            if (useVoiceAnimations)
            {
                args.Avatar.AddComponent<UbiqReadyPlayerMeVoiceHandler>();
            }
        
            if (args.Metadata.BodyType == BodyType.FullBody)
            {
                InitFullBody();
            }
            else
            {
                InitHalfBody();
            }
            
            return;
            
            void InitFullBody()
            {
                var t = args.Avatar.transform;
                head = t.Find("Armature/Hips/Spine/Spine1/Spine2/Neck/Head");
                left.transform = t.Find("Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
                right.transform = t.Find("Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand");
            }
            
            void InitHalfBody()
            {
                var t = args.Avatar.transform;
                armature = t.Find("Armature");
                head = t.Find("Armature/Hips/Spine/Neck/Head");
                left.transform = t.Find("Armature/Hips/Spine/LeftHand");
                right.transform = t.Find("Armature/Hips/Spine/RightHand");
        
                if (head && TryGetComponent<VoipAvatar>(out var voipAvatar))
                {
                    var go = Instantiate(speechIndicatorPrefab,head);
                    go.transform.localPosition = speechIndicatorPositionOffset;
                    voipAvatar.audioSourcePosition = head;
                    voipAvatar.speechIndicator = go.GetComponentInChildren<VoipSpeechIndicator>();
                }
        
                // Animate manually. Animation component is added by the GLTF
                // loader and does not seem to be functional
                Destroy(GetComponentInChildren<Animation>());
        
                if (left.transform)
                {
                    if (left.bones == null)
                    {
                        left.bones = new List<Transform>();
                    }
                    left.bones.Clear();
                    if (left.initialLocalPositions == null)
                    {
                        left.initialLocalPositions = new List<Vector3>();
                    }
                    left.initialLocalPositions.Clear();
                    AddBone(left,"LeftHandIndex1");
                    AddBone(left,"LeftHandIndex1/LeftHandIndex2");
                    AddBone(left,"LeftHandIndex1/LeftHandIndex2/LeftHandIndex3");
                    AddBone(left,"LeftHandMiddle1");
                    AddBone(left,"LeftHandMiddle1/LeftHandMiddle2");
                    AddBone(left,"LeftHandMiddle1/LeftHandMiddle2/LeftHandMiddle3");
                    AddBone(left,"LeftHandRing1");
                    AddBone(left,"LeftHandRing1/LeftHandRing2");
                    AddBone(left,"LeftHandRing1/LeftHandRing2/LeftHandRing3");
                    AddBone(left,"LeftHandPinky1");
                    AddBone(left,"LeftHandPinky1/LeftHandPinky2");
                    AddBone(left,"LeftHandPinky1/LeftHandPinky2/LeftHandPinky3");
                    AddBone(left,"LeftHandThumb1");
                    AddBone(left,"LeftHandThumb1/LeftHandThumb2");
                    AddBone(left,"LeftHandThumb1/LeftHandThumb2/LeftHandThumb3");
                }
                if (right.transform)
                {
                    if (right.bones == null)
                    {
                        right.bones = new List<Transform>();
                    }
                    right.bones.Clear();
                    if (right.initialLocalPositions == null)
                    {
                        right.initialLocalPositions = new List<Vector3>();
                    }
                    right.initialLocalPositions.Clear();
                    AddBone(right,"RightHandIndex1");
                    AddBone(right,"RightHandIndex1/RightHandIndex2");
                    AddBone(right,"RightHandIndex1/RightHandIndex2/RightHandIndex3");
                    AddBone(right,"RightHandMiddle1");
                    AddBone(right,"RightHandMiddle1/RightHandMiddle2");
                    AddBone(right,"RightHandMiddle1/RightHandMiddle2/RightHandMiddle3");
                    AddBone(right,"RightHandRing1");
                    AddBone(right,"RightHandRing1/RightHandRing2");
                    AddBone(right,"RightHandRing1/RightHandRing2/RightHandRing3");
                    AddBone(right,"RightHandPinky1");
                    AddBone(right,"RightHandPinky1/RightHandPinky2");
                    AddBone(right,"RightHandPinky1/RightHandPinky2/RightHandPinky3");
                    AddBone(right,"RightHandThumb1");
                    AddBone(right,"RightHandThumb1/RightHandThumb2");
                    AddBone(right,"RightHandThumb1/RightHandThumb2/RightHandThumb3");
                }
                
                if (useHandSkeletonAnimations && left.transform)
                {
                    var offset = new Pose(Vector3.zero,Quaternion.Euler(90,0,0));
                    left.driver = args.Avatar.AddComponent<HandSkeletonDriver>();
                    var i = 0;
                    left.driver.ClearBones();
                    left.driver.SetBone(Joint.Wrist,left.transform,offset);
                    left.driver.SetBone(Joint.IndexProximal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.IndexIntermediate,left.bones[i++],offset);
                    left.driver.SetBone(Joint.IndexDistal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.MiddleProximal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.MiddleIntermediate,left.bones[i++],offset);
                    left.driver.SetBone(Joint.MiddleDistal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.RingProximal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.RingIntermediate,left.bones[i++],offset);
                    left.driver.SetBone(Joint.RingDistal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.LittleProximal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.LittleIntermediate,left.bones[i++],offset);
                    left.driver.SetBone(Joint.LittleDistal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.ThumbMetacarpal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.ThumbProximal,left.bones[i++],offset);
                    left.driver.SetBone(Joint.ThumbDistal,left.bones[i],offset);
                }
                
                if (useHandSkeletonAnimations && right.transform)
                {
                    var offset = new Pose(Vector3.zero,Quaternion.Euler(90,0,0));
                    right.driver = args.Avatar.AddComponent<HandSkeletonDriver>();
                    var i = 0;
                    right.driver.ClearBones();
                    right.driver.SetBone(Joint.Wrist,right.transform,offset);
                    right.driver.SetBone(Joint.IndexProximal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.IndexIntermediate,right.bones[i++],offset);
                    right.driver.SetBone(Joint.IndexDistal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.MiddleProximal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.MiddleIntermediate,right.bones[i++],offset);
                    right.driver.SetBone(Joint.MiddleDistal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.RingProximal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.RingIntermediate,right.bones[i++],offset);
                    right.driver.SetBone(Joint.RingDistal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.LittleProximal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.LittleIntermediate,right.bones[i++],offset);
                    right.driver.SetBone(Joint.LittleDistal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.ThumbMetacarpal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.ThumbProximal,right.bones[i++],offset);
                    right.driver.SetBone(Joint.ThumbDistal,right.bones[i],offset);
                }
                
                right.lastGrip = left.lastGrip = -1; // Force update
                HeadAndHandsEvents_OnLeftGripUpdate(new InputVar<float>(0.0f));
                HeadAndHandsEvents_OnRightGripUpdate(new InputVar<float>(0.0f));
            }
        }
        
        private void Loader_Failed(FailureEventArgs args)
        {
        
        }
        
        private static void AddBone(HandInfo hand, string path)
        {
            var bone = hand.transform.Find(path);
            hand.bones.Add(bone);
            hand.initialLocalPositions.Add(bone.localPosition);
        }
    }
}
#endif