#if !XRI_2_4_3_OR_NEWER
using UnityEngine;
using UnityEditor;
using UbiqEditor;

namespace Ubiq.Samples.WebXR.Editor
{
    [InitializeOnLoad]
    public class AddPackageXRI_WebXR
    {
        static AddPackageXRI_WebXR()
        {
            // Custom for WebXR sample begins
            if (RequireSamplesXRIUtil.AssetExistsFromGUID(RequireSamplesXRIUtil.DEMO_SCENE_GUID))
            {
                // XRI sample is already in the project, let that handle it
                return;
            }
            // Custom for WebXR sample ends

#if XRI_0_0_0_OR_NEWER
    #if !UBIQ_SILENCEWARNING_XRIVERSION
            Debug.LogWarning(
                "Ubiq sample DemoScene (XRI) requires XRI > 2.4.3, but an" +
                " earlier version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_XRIVERSION to your scripting define" +
                " symbols");
    #endif
#else
            // Safer to interact with Unity on main thread
            EditorApplication.update += Update;
#endif
        }

        static void Update()
        {
            PackageManagerHelper.Add("com.unity.xr.interaction.toolkit");
            EditorApplication.update -= Update;
        }
    }
}
#endif