#if !UNITY_WEBRTC_NO_VULKAN_HOOK && !UBIQ_SKIPCHECK_WEBRTCCOMPATIBILITY
using UnityEngine;
using UnityEditor;
using UbiqEditor;

namespace UbiqEditor
{
    [InitializeOnLoad]
    public class AddPackageWebRTC
    {
        static AddPackageWebRTC()
        {
            // Safer to interact with Unity on main thread
            EditorApplication.update += Update;
        }

        static void Update()
        {
#if UNITY_WEBRTC
            Debug.LogWarning("Ubiq has detected an existing com.unity.webrtc" +
                " package. This package has compatibility issues with the" +
                " Oculus/OpenXR SDK on Android. Ubiq will remove this package" +
                " and replace it with a modified fork which is compatible. If" +
                " you would prefer to skip this check and prevent this" +
                " behaviour, add the string" +
                " UBIQ_SKIPCHECK_WEBRTCCOMPATIBILITY to your scripting define" +
                " symbols.");
            PackageManagerHelper.Remove("com.unity.webrtc");
#endif
            PackageManagerHelper.Add("https://github.com/UCL-VR/unity-webrtc-no-vulkan-hook.git");
            EditorApplication.update -= Update;
        }
    }
}
#endif