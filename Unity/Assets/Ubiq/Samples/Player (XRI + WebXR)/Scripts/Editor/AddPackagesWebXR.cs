using UnityEngine;
using UnityEditor;
using Ubiq.Editor;
using Ubiq.Editor.XRI;
#if WEBXR_0_22_0_OR_NEWER
using WebXR;
#endif

namespace Ubiq.Samples.WebXR.Editor
{
    [InitializeOnLoad]
    public class AddPackageWebXR
    {
        static AddPackageWebXR()
        {
#if !UBIQ_SILENCEWARNING_WEBXRVERSION
    #if WEBXR_0_22_1_OR_NEWER && WEBXR_0_0_0_OR_NEWER
            var package = "WebXR";
            var version = "0.22.0";
            Debug.LogWarning(
                $"Ubiq sample Player (XRI + WebXR) requires {package} = {version}, but an" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_WEBXRVERSION to your scripting define" +
                " symbols");
    #endif
    #if WEBXRINTERACTIONS_0_22_1_OR_NEWER && WEBXRINTERACTIONS_0_0_0_OR_NEWER
            var package = "WebXR-Interactions";
            var version = "0.22.0";
            Debug.LogWarning(
                $"Ubiq sample Player (XRI + WebXR) requires {package} = {version}, but a" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_WEBXRVERSION to your scripting define" +
                " symbols");
    #endif
#endif

            // Safer to interact with Unity on main thread
            EditorApplication.update += Update;
        }

        static void Update()
        {
#if !WEBXR_0_0_0_OR_NEWER
            PackageManagerHelper.AddPackage("https://github.com/De-Panther/unity-webxr-export.git?path=/Packages/webxr#webxr/0.22.0");
#endif

#if !WEBXRINTERACTIONS_0_0_0_OR_NEWER
            PackageManagerHelper.AddPackage("https://github.com/De-Panther/unity-webxr-export.git?path=/Packages/webxr-interactions#webxr-interactions/0.22.0");
#endif

#if WEBXR_0_22_0_OR_NEWER
            PackageManagerHelper.RequireSample("com.de-panther.webxr-interactions","XR Interaction Toolkit Sample");

    #if !UBIQ_DISABLE_WEBXRAUTOLOADOFF
            var modified = false;
            var settings = WebXRSettings.GetSettings();
            if (settings != null && settings.AutoLoadWebXRInputSystem)
            {
                modified = true;
                settings.AutoLoadWebXRInputSystem = false;
                Debug.Log("Ubiq has set AutoLoadWebXRInputSystem to FALSE" +
                    " in the WebXR settings. This is to allow you to build" +
                    " for other platforms without including WebXR. If you" +
                    " would prefer to skip this check and prevent this" +
                    " behaviour, add the string UBIQ_DISABLE_WEBXRAUTOLOADOFF" +
                    " to your scripting define symbols.");
            }
            if (settings != null && settings.AutoLoadWebXRManager)
            {
                modified = true;
                settings.AutoLoadWebXRManager = false;
                Debug.Log("Ubiq has set AutoLoadWebXRManager to FALSE" +
                    " in the WebXR settings. This is to allow you to build" +
                    " for other platforms without including WebXR. If you" +
                    " would prefer to skip this check and prevent this" +
                    " behaviour, add the string UBIQ_DISABLE_WEBXRAUTOLOADOFF" +
                    " to your scripting define symbols.");
            }
            if (modified)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
    #endif
#endif

            ImportHelperXRI.Import();

            EditorApplication.update -= Update;
        }
    }
}