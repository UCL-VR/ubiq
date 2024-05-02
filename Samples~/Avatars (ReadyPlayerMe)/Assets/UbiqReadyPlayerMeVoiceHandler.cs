#if READYPLAYERME_0_0_0_OR_NEWER
using UnityEngine;
using System.Collections.Generic;
using Ubiq.Voip;
using ReadyPlayerMe.AvatarLoader;

namespace Ubiq.ReadyPlayerMe
{
    // Fork of VoiceHandler from the ReadyPlayerMe plugin
    // This uses Ubiq voip stats rather than directly grabbing the
    // microphone, allowing the microphone to be used for VOIP
    public class UbiqReadyPlayerMeVoiceHandler : MonoBehaviour
    {
        private const string MOUTH_OPEN_BLEND_SHAPE_NAME = "mouthOpen";
        private const string MISSING_BLENDSHAPE_MESSAGE = "The 'mouthOpen' morph target is required for VoiceHandler.cs but it was not found on Avatar mesh. Use an AvatarConfig to specify the blendshapes to be included on loaded avatars.";
        
        private Dictionary<SkinnedMeshRenderer, int> blendshapeMeshIndexMap;
        
        private readonly MeshType[] faceMeshTypes = { MeshType.HeadMesh, MeshType.BeardMesh, MeshType.TeethMesh };
        
        private VoipPeerConnection peerConnection;
        private Ubiq.Avatars.Avatar avatar;
        
        private VolumeEstimator volumeEstimator = new VolumeEstimator(0.0f,0.6f);
        private const float DECIBELS_REFERENCE_VALUE = 0.001f;
        private const float DECIBELS_MULTIPLIER = 0.04f;
        
        
        private void Start()
        {
            CreateBlendshapeMeshMap();
            if (!HasMouthOpenBlendshape())
            {
                Debug.LogWarning(MISSING_BLENDSHAPE_MESSAGE);
                enabled = false;
                return;
            }
        
            avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
            var peerConnectionManager = VoipPeerConnectionManager.Find(this);
            if (peerConnectionManager)
            {
                peerConnectionManager.GetPeerConnectionAsync(avatar.Peer.uuid,OnPeerConnection);
            }
        }
        
        private void Update()
        {
            var volume = Mathf.Max(volumeEstimator.volume,0.00001f);
            var decibels = 20.0f * Mathf.Log10(volume/DECIBELS_REFERENCE_VALUE);
            SetBlendShapeWeights(Mathf.Clamp01(decibels * DECIBELS_MULTIPLIER));
        }
        
        private void OnDestroy()
        {
            if (peerConnection)
            {
                peerConnection.playbackStatsPushed -= PeerConnection_PlaybackStatsPushed;
            }
        }
        
        private void OnPeerConnection(VoipPeerConnection peerConnection)
        {
            // If we're very unlucky, this is called after we're destroyed
            if (!this || !enabled)
            {
                return;
            }
        
            this.peerConnection = peerConnection;
            peerConnection.playbackStatsPushed += PeerConnection_PlaybackStatsPushed;
        }
        
        private void PeerConnection_PlaybackStatsPushed(VoipPeerConnection.AudioStats stats)
        {
            volumeEstimator.PushAudioStats(stats);
        }
        
        private bool HasMouthOpenBlendshape()
        {
            foreach (KeyValuePair<SkinnedMeshRenderer, int> blendshapeMeshIndex in blendshapeMeshIndexMap)
            {
                if (blendshapeMeshIndex.Value >= 0)
                {
                    return true;
                }
        
            }
            return false;
        }
        
        private void CreateBlendshapeMeshMap()
        {
            blendshapeMeshIndexMap = new Dictionary<SkinnedMeshRenderer, int>();
            foreach (MeshType faceMeshType in faceMeshTypes)
            {
                SkinnedMeshRenderer faceMesh = gameObject.GetMeshRenderer(faceMeshType);
                if (faceMesh)
                {
                    TryAddSkinMesh(faceMesh);
                }
            }
        }
        
        private void TryAddSkinMesh(SkinnedMeshRenderer skinMesh)
        {
            if (skinMesh != null)
            {
                var index = skinMesh.sharedMesh.GetBlendShapeIndex(MOUTH_OPEN_BLEND_SHAPE_NAME);
                blendshapeMeshIndexMap.Add(skinMesh, index);
            }
        }
        
        private void SetBlendShapeWeights(float weight)
        {
            foreach (KeyValuePair<SkinnedMeshRenderer, int> blendshapeMeshIndex in blendshapeMeshIndexMap)
            {
                if (blendshapeMeshIndex.Value >= 0)
                {
                    blendshapeMeshIndex.Key.SetBlendShapeWeight(blendshapeMeshIndex.Value, weight);
                }
            }
        }
    }
}
#endif