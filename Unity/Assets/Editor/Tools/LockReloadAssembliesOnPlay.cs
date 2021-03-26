using UnityEngine;
using UnityEditor;

// ensure class initializer is called whenever scripts recompile
[InitializeOnLoadAttribute]
public static class LockReloadAssembliesOnPlay
{
    // register an event handler when the class is initialized
    static LockReloadAssembliesOnPlay()
    {
        EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
    }

    private static void EditorApplication_PlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorApplication.LockReloadAssemblies();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}