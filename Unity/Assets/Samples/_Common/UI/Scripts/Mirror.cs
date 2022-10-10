using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Avatars;

namespace Ubiq.Samples.Social
{
    [RequireComponent(typeof(RawImage))]
    public class Mirror : MonoBehaviour
    {
        public enum RenderMode
        {
            None,
            AvatarOnly,
            Layers,
        }

        [System.Serializable]
        public class Config
        {
            public float resolutionMultiplier;
            public RenderMode renderMode;
            public LayerMask layers;
        }

        [System.Serializable]
        public class PlatformConfig
        {
            public RuntimePlatform platform;
            public Config config;
        }

        public Config defaultConfig;
        public List<PlatformConfig> platformConfigs;

        public LayerMask layers;
        private Camera mirrorCamera;
        private RawImage image;
        private RenderTexture renderTexture;
        private Vector3[] cornersTmp = new Vector3[4];
        private Ubiq.Avatars.Avatar avatarTmp;
        private CommandBuffer avatarRenderCommandBufferTmp;
        private List<Renderer> avatarRenderersTmp = new List<Renderer>();

        private AvatarManager avatarManager;

        private void Awake()
        {
            image = GetComponent<RawImage>();
            mirrorCamera = new GameObject("Mirror Camera").AddComponent<Camera>();
            mirrorCamera.transform.parent = transform;
            mirrorCamera.enabled = false;
            mirrorCamera.clearFlags = CameraClearFlags.Color;
            mirrorCamera.backgroundColor = Color.clear;
        }

        private void OnDestroy()
        {
            if (mirrorCamera && mirrorCamera.gameObject)
            {
                Destroy(mirrorCamera.gameObject);
            }

            if (avatarRenderCommandBufferTmp != null)
            {
                avatarRenderCommandBufferTmp.Dispose();
                avatarRenderCommandBufferTmp = null;
            }
        }

        private void LateUpdate()
        {
            var config = GetConfig();
            if (PrepareCamera(config))
            {
                Render(config);
                mirrorCamera.Render();
            }
        }

        private void Render(Config config)
        {
            switch(config.renderMode)
            {
                case RenderMode.None: break;
                case RenderMode.AvatarOnly: RenderAvatar(config); break;
                case RenderMode.Layers: RenderLayers(config); break;
            }
        }

        private bool TryGetAvatarManager(out AvatarManager avatarManager)
        {
            try {
                avatarManager = NetworkScene.Find(this)
                    .GetComponentInChildren<AvatarManager>();
            }
            catch (System.NullReferenceException)
            {
                avatarManager = null;
            }

            return avatarManager;
        }

        private void RenderAvatar(Config config)
        {
            if (!avatarManager && !TryGetAvatarManager(out avatarManager))
            {
                return;
            }

            if (avatarRenderCommandBufferTmp == null)
            {
                avatarRenderCommandBufferTmp = new CommandBuffer();
            }

            avatarTmp = avatarManager.LocalAvatar;
            avatarRenderCommandBufferTmp.Clear();

            // We could gather renderers only once, but it fails if materials are swapped
            avatarTmp.GetComponentsInChildren<Renderer>(avatarRenderersTmp);
            for (int i = 0; i < avatarRenderersTmp.Count; i++)
            {
                avatarRenderCommandBufferTmp.DrawRenderer(
                    avatarRenderersTmp[i],avatarRenderersTmp[i].material);
            }

            mirrorCamera.RemoveAllCommandBuffers();
            mirrorCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,
                avatarRenderCommandBufferTmp);
            mirrorCamera.cullingMask = 0;
            mirrorCamera.Render();
        }

        private void RenderLayers(Config config)
        {
            mirrorCamera.RemoveAllCommandBuffers();
            mirrorCamera.cullingMask = config.layers;
            mirrorCamera.Render();
        }

        private bool PrepareCamera(Config config)
        {
            var camera = Camera.main;

            if (!camera)
            {
                return false;
            }

            var observer = camera.transform;
            var mirror = transform;
            var observerToMirror = (mirror.position - observer.position).normalized;

            var mirrorForward = Vector3.Dot(mirror.forward,observerToMirror) < 0
                ? mirror.forward
                : -mirror.forward;

            // Point camera s.t. its forward direction is the reflected
            // direction after hitting the mirror plane
            var mirrorCameraForward = observerToMirror -
                2 * Vector3.Dot(observerToMirror,mirrorForward) * mirrorForward;
            if (mirrorCameraForward.sqrMagnitude > 0)
            {
                mirrorCamera.transform.rotation =
                    Quaternion.LookRotation(mirrorCameraForward,mirror.up);
            }

            // Position camera same distance from mirror as observer
            mirrorCamera.transform.position = mirror.position -
                mirrorCameraForward * observerToMirror.magnitude;

            // Shape frustrum s.t. it fits the four corners of the mirror
            image.rectTransform.GetWorldCorners(cornersTmp);
            var worldWidth = (cornersTmp[3] - cornersTmp[0]).magnitude;
            var worldHeight = (cornersTmp[1] - cornersTmp[0]).magnitude;
            var w = worldWidth;
            var h = worldHeight;
            var d = observerToMirror.magnitude;
            mirrorCamera.stereoTargetEye = StereoTargetEyeMask.None;
            mirrorCamera.aspect = w/h;
            mirrorCamera.fieldOfView = 2 * Mathf.Atan2(h,2*d) * Mathf.Rad2Deg;
            mirrorCamera.nearClipPlane = d * 0.5f;

            // Check resolution of texture
            var scaler = image.canvas.GetComponent<CanvasScaler>();
            if (!scaler)
            {
                return false;
            }
            var scale = scaler.dynamicPixelsPerUnit * config.resolutionMultiplier;

            var texWidth = Mathf.RoundToInt(image.rectTransform.rect.width * scale);
            var texHeight = Mathf.RoundToInt(image.rectTransform.rect.height * scale);
            var rt = RequireTex(texWidth,texHeight);

            if (rt)
            {
                mirrorCamera.targetTexture = rt;
                image.texture = rt;
            }

            return rt;
        }

        Config GetConfig()
        {
            for (int i = 0; i < platformConfigs.Count; i++)
            {
                if (platformConfigs[i].platform == Application.platform)
                {
                    return platformConfigs[i].config;
                }
            }

            return defaultConfig;
        }

        RenderTexture RequireTex(int width, int height)
        {
            if (renderTexture &&
                (renderTexture.width != width || renderTexture.height != height))
            {
                RenderTexture.ReleaseTemporary(renderTexture);
                renderTexture = null;
            }

            if (!renderTexture && width > 0 && height > 0)
            {
                renderTexture = RenderTexture.GetTemporary(width,height,0);
            }
            return renderTexture;
        }
    }
}
