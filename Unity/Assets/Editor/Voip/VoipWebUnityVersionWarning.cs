using UnityEditor;

namespace Ubiq.Voip
{
    // Warn the user if they're using Mono rather than IL2CPP as their scripting
    // backend. The Mono backend takes a long time to generate certificates
    // when used with our VOIP library on Android.
    public static class VoipWebUnityVersionWarning
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded() {
#if !UBIQ_SILENCEWARNING_WEBUNITYVERSION
    #if UNITY_WEBGL && !UNITY_2021_3_OR_NEWER
            UnityEngine.Debug.LogWarning(
                "Ubiq supports voice-over-IP in WebGL builds from Unity" +
                " 2021.3 LTS onwards. If you need VOIP in your WebGL" +
                " application, consider updating your Unity version." +
                " To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_WEBUNITYVERSION to your scripting" +
                " define symbols");
    #endif
#endif
        }
    }
}
