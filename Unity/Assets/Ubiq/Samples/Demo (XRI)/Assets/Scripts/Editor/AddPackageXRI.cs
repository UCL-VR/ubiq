using UnityEngine;
using UnityEditor;
using UbiqEditor;

namespace Ubiq.Samples.Demo.Editor
{
    [InitializeOnLoad]
    public class AddPackageXRI
    {
        static AddPackageXRI()
        {
#if XRI_2_5_3_OR_NEWER && XRI_0_0_0_OR_NEWER
    #if !UBIQ_SILENCEWARNING_XRIVERSION
            Debug.LogWarning(
                "Ubiq sample DemoScene (XRI) requires XRI = 2.5.2, but a" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_XRIVERSION to your scripting define" +
                " symbols");
    #endif
#endif
            // Safer to interact with Unity on main thread
            EditorApplication.update += Update;
        }

        static void Update()
        {
#if !XRI_0_0_0_OR_NEWER
            PackageManagerHelper.AddPackage("com.unity.xr.interaction.toolkit@2.5.2");
#else
            PackageManagerHelper.RequireSample("com.unity.xr.interaction.toolkit","Starter Assets");
            PackageManagerHelper.RequireSample("com.unity.xr.interaction.toolkit","XR Device Simulator");
#endif
            EditorApplication.update -= Update;
        }
    }
}