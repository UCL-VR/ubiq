#if UNITY_EDITOR
using UnityEditor;

namespace Ubiq.Voip
{
    public static class VoipUnsupportedPlatformWarning
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded() {
#if !UBIQ_SILENCEWARNING_VOIPUNSUPPORTEDPLATFORM
            UnityEngine.Debug.LogWarning(
                "Ubiq has no WebRTC/VOIP support for this platform. Supported" +
                " platforms are:" +
                " Windows (64bit)" +
                ", MacOS" +
                ", Linux (64bit)" +
                ", Android" +
                ", iOS" +
                ", WebGL (Unity 2021.3 LTS onwards)"
                ", and the Unity Editor."
                " To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_VOIPUNSUPPORTEDPLATFORM to your scripting" +
                " define symbols");
#endif
        }
    }
}
#endif