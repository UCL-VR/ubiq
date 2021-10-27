using UnityEditor;

namespace Ubiq.Voip
{
    // Warn the user if they're using Mono rather than IL2CPP as their scripting
    // backend. The Mono backend takes a long time to generate certificates
    // when used with our VOIP library on Android.
    public static class ScriptingBackendWarning
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded() {
#if !UBIQ_SILENCEWARNING_SCRIPTINGBACKEND
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (buildTarget != BuildTarget.Android)
            {
                return;
            }

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var backend = PlayerSettings.GetScriptingBackend(buildTargetGroup);
            if (backend == ScriptingImplementation.IL2CPP)
            {
                return;
            }

            UnityEngine.Debug.LogWarning(
                "IL2CPP is the preferred scripting backend for Ubiq on Android." +
                " VOIP connections will take significantly longer to establish" +
                " when using Mono. To silence this warning, add the string " +
                " UBIQ_SILENCEWARNING_SCRIPTINGBACKEND to your scripting define symbols");
#endif
        }
    }
}
