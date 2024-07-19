using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Examples
{
    public class _31_SimpleAvatar : MonoBehaviour
    {
        private PoseAvatar poseAvatar;

        private void Start()
        {
            poseAvatar = GetComponent<PoseAvatar>();
            poseAvatar.OnPoseUpdate.AddListener(PoseAvatar_OnPoseUpdate);
            
            SetVisibility(false, force:true);
        }

        private void OnDestroy()
        {
            if (poseAvatar)
            {
                poseAvatar.OnPoseUpdate.RemoveListener(PoseAvatar_OnPoseUpdate);
            }
        }

        private void PoseAvatar_OnPoseUpdate(InputVar<Pose> pose)
        {
            if (!pose.valid)
            {
                SetVisibility(false);
                return;
            }
            
            SetVisibility(true);
            transform.SetPositionAndRotation(pose.value.position, pose.value.rotation);
        }
        
        private readonly List<Renderer> _reusableRenderers = new List<Renderer>();
        private bool visible;
        
        private void SetVisibility(bool visible, bool force = false)
        {
            if (!force && this.visible == visible)
            {
                return;
            }
            
            GetComponentsInChildren(_reusableRenderers);
            for (int i = 0; i < _reusableRenderers.Count; i++)
            {
                _reusableRenderers[i].enabled = visible;
            }
            
            this.visible = visible;
        }
    }
}