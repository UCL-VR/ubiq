#if !UNITY_WEBRTC_NO_VULKAN_HOOK && !UBIQ_SKIPCHECK_WEBRTCCOMPATIBILITY
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Ubiq.Samples.Demo.Editor
{
    [InitializeOnLoad]
    public class AddPackageWebRTC
    {
        private const string FORK_URL = "https://github.com/UCL-VR/unity-webrtc-no-vulkan-hook.git";
#if UNITY_WEBRTC
        AddAndRemoveRequest request;
#else
        AddRequest request;
#endif
        AddAndRemoveRequest foo;

        static AddPackageWebRTC()
        {
            Debug.Log("Ubiq attempting to add WebRTC to project requirements." +
                " Please wait...");
            var instance = new AddPackageWebRTC();
#if UNITY_WEBRTC
            Debug.LogWarning("Ubiq has detected an existing com.unity.webrtc" +
                " package. This package has compatibility issues with the" +
                " Oculus/OpenXR SDK. Ubiq will remove this package and" +
                " replace it with a modified fork which is compatible. If you" +
                " would prefer to skip this check and prevent this behaviour," +
                " add the string UBIQ_SKIPCHECK_WEBRTCCOMPATIBILITY to your" +
                " scripting define symbols.");
            instance.request = Client.AddAndRemove(
                packagesToAdd : new string[] {FORK_URL},
                packagesToRemove : new string[] {"com.unity.webrtc"}
            );
#else
            instance.request = Client.Add(FORK_URL);
#endif
            EditorApplication.update += instance.Update;
        }

        void Update()
        {
            if (request == null)
            {
                EditorApplication.update -= Update;
            }

            if (request.Status == StatusCode.Failure)
            {
                var error = request.Error != null ? request.Error.message : "None specified";
                Debug.LogError($"Ubiq was unable to add WebRTC to project requirements. Error: {error}");
                EditorApplication.update -= Update;
            }

            if (request.Status == StatusCode.Success)
            {
                EditorApplication.update -= Update;
                Debug.Log("Ubiq added WebRTC to project requirements.");
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif