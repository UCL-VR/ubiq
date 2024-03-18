#if !WEBXR_0_22_0_OR_NEWER || !WEBXRINTERACTIONS_0_0_0_OR_NEWER
using UnityEngine;
using UnityEditor;
using UbiqEditor;

namespace Ubiq.Samples.WebXR.Editor
{
    [InitializeOnLoad]
    public class AddPackageWebXR
    {
        static AddPackageWebXR()
        {
#if !UBIQ_SILENCEWARNING_WEBXRVERSION
    #if !WEBXR_0_22_0_OR_NEWER && WEBXR_0_0_0_OR_NEWER
            var package = "WebXR";
            var version = "0.22.0";
            Debug.LogWarning(
                $"Ubiq sample DemoScene (XRI) requires {package} > {version}, but an" +
                " earlier version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_WEBXRVERSION to your scripting define" +
                " symbols");
    #endif
    #if !WEBXRINTERACTIONS_0_22_0_OR_NEWER && WEBXRINTERACTIONS_0_0_0_OR_NEWER
            var package = "WebXR-Interactions";
            var version = "0.22.0";
            Debug.LogWarning(
                $"Ubiq sample DemoScene (XRI) requires {package} > {version}, but an" +
                " earlier version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_WEBXRVERSION to your scripting define" +
                " symbols");
    #endif
#endif

#if !WEBXR_0_0_0_OR_NEWER || !WEBXRINTERACTIONS_0_0_0_OR_NEWER
            // Safer to interact with Unity on main thread
            EditorApplication.update += Update;
#endif
        }

        static void Update()
        {
#if !WEBXR_0_0_0_OR_NEWER
            PackageManagerHelper.Add("https://github.com/De-Panther/unity-webxr-export.git?path=/Packages/webxr");
#endif
#if !WEBXRINTERACTIONS_0_0_0_OR_NEWER
            PackageManagerHelper.Add("https://github.com/De-Panther/unity-webxr-export.git?path=/Packages/webxr-interactions");
#endif
            EditorApplication.update -= Update;
        }
    }
}
#endif