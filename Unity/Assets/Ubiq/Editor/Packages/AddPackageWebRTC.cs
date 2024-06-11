#if !UNITY_WEBRTC_UBIQ_FORK && !(UNITY_WEBRTC && UBIQ_DISABLE_WEBRTCCOMPATIBILITYCHECK)
using UnityEngine;
using UnityEditor;
using Ubiq.Editor;

namespace Ubiq.Editor
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
                " UBIQ_DISABLE_WEBRTCCOMPATIBILITYCHECK to your scripting define" +
                " symbols.");
            PackageManagerHelper.Remove("com.unity.webrtc");
#endif
            PackageManagerHelper.AddPackage("https://github.com/UCL-VR/unity-webrtc-ubiq-fork.git");
            EditorApplication.update -= Update;
        }
    }
}
#endif