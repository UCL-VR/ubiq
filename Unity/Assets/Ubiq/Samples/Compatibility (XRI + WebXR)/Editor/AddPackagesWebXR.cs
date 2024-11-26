using UnityEngine;
using UnityEditor;
using Ubiq.Editor;
using Ubiq.Editor.XRI;
#if WEBXR_0_22_1_OR_NEWER
using WebXR;
#endif

namespace Ubiq.Samples.WebXR.Editor
{
    [InitializeOnLoad]
    public class AddPackageWebXR
    {
        static AddPackageWebXR()
        {
            // Safer to interact with Unity on main thread
            EditorApplication.update += Update;
        }

        static void Update()
        {
#if !WEBXR_0_22_1_OR_NEWER || WEBXR_0_22_2_OR_NEWER
    #if WEBXR_0_0_0_OR_NEWER 
            var corePackage = "WebXR";
            var coreVersion = "0.22.1";
            Debug.LogWarning(
                $"Ubiq sample Compatibility (XRI + WebXR) requires "+
                $"{corePackage} = {coreVersion}, but different version is installed. "+
                $"Ubiq will remove this package and replace it with "+
                $"version {coreVersion}. If you would prefer to skip this check"+
                $" and prevent this behaviour, add the string "+
                $"UBIQ_DISABLE_WEBXRCOMPATIBILITYCHECK to your scripting "+
                $"define symbols.");
            PackageManagerHelper.RemovePackage("com.de-panther.webxr");
    #endif
            PackageManagerHelper.AddPackage("https://github.com/De-Panther/unity-webxr-export.git?path=/Packages/webxr#webxr/0.22.1");
#endif
#if !WEBXRINTERACTIONS_0_22_0_OR_NEWER || WEBXRINTERACTIONS_0_22_1_OR_NEWER
    #if WEBXRINTERACTIONS_0_0_0_OR_NEWER 
            var interactionsPackage = "WebXR-Interactions";
            var interactionsVersion = "0.22.0";
            Debug.LogWarning(
                $"Ubiq sample Compatibility (XRI + WebXR) requires "+
                $"{interactionsPackage} = {interactionsVersion}, but different version is installed. "+
                $"Ubiq will remove this package and replace it with "+
                $"version {interactionsVersion}. If you would prefer to skip this check"+
                $" and prevent this behaviour, add the string "+
                $"UBIQ_DISABLE_WEBXRCOMPATIBILITYCHECK to your scripting "+
                $"define symbols.");
            PackageManagerHelper.RemovePackage("com.de-panther.webxr-interactions");
    #endif
            PackageManagerHelper.AddPackage("https://github.com/De-Panther/unity-webxr-export.git?path=/Packages/webxr-interactions#webxr-interactions/0.22.0");
#endif
            
#if WEBXR_0_22_1_OR_NEWER
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