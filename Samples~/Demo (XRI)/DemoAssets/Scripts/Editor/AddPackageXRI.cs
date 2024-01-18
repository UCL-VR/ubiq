#if !XRI_2_4_3_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Ubiq.Samples.Demo.Editor
{
    [InitializeOnLoad]
    public class AddPackageXRI
    {
        AddRequest request;

        static AddPackageXRI()
        {
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
            Debug.Log("Ubiq attempting to add XRI to project requirements. Please wait...");
            var instance = new AddPackageXRI();
            instance.request = Client.Add("com.unity.xr.interaction.toolkit");
            EditorApplication.update += instance.Update;
#endif
        }

        void Update()
        {
            if (request == null || request.Status == StatusCode.Failure)
            {
                EditorApplication.update -= Update;
            }

            if (request.Status == StatusCode.Success)
            {
                EditorApplication.update -= Update;
                Debug.Log("Ubiq added XRI to project requirements. You may be prompted to restart to enable Input backends.");
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif