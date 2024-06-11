#if !READYPLAYERME_0_0_0_OR_NEWER || READYPLAYERME_1_3_4_OR_NEWER

namespace Ubiq.ReadyPlayerMe.Editor
{
    [UnityEditor.InitializeOnLoad]
    public class AddPackagesReadyPlayerMe
    {
        static AddPackagesReadyPlayerMe()
        {
#if !UBIQ_SILENCEWARNING_READYPLAYERMEVERSION
    #if READYPLAYERME_1_3_4_OR_NEWER
            var package = "ReadyPlayerMe";
            var version = "1.3.3";
            UnityEngine.Debug.LogWarning(
                $"Ubiq sample Avatars (ReadyPlayerMe) requires {package} = {version}, but a" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_READYPLAYERMEVERSION to your scripting define" +
                " symbols");
    #endif
#endif

#if !READYPLAYERME_0_0_0_OR_NEWER 
            // Safer to interact with Unity on main thread
            UnityEditor.EditorApplication.update += Update;
#endif
        }

        static void Update()
        {
            Ubiq.Editor.PackageManagerHelper.AddPackage("https://github.com/UCL-VR/readyplayerme-core-ubiq-fork.git");
            Ubiq.Editor.PackageManagerHelper.AddPackage("https://github.com/readyplayerme/rpm-unity-sdk-avatar-loader.git#v1.3.4");
            Ubiq.Editor.PackageManagerHelper.AddPackage("https://github.com/atteneder/glTFast.git#v5.0.0");
            UnityEditor.EditorApplication.update -= Update;
        }
    }
}
#endif