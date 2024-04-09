namespace Ubiq.Editor.XRI
{
    public static class ImportHelperXRI
    {
        public static void Import()
        {
#if XRI_2_5_3_OR_NEWER && XRI_0_0_0_OR_NEWER
    #if !UBIQ_SILENCEWARNING_XRIVERSION
            Debug.LogWarning(
                "Ubiq samples require XRI = 2.5.2, but a" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_XRIVERSION to your scripting define" +
                " symbols");
    #endif
#endif

#if !XRI_0_0_0_OR_NEWER
            PackageManagerHelper.AddPackage("com.unity.xr.interaction.toolkit@2.5.2");
            PackageManagerHelper.AddPackage("com.unity.xr.hands@1.3.0");
#else
            PackageManagerHelper.RequireSample("com.unity.xr.interaction.toolkit","Starter Assets");
            PackageManagerHelper.RequireSample("com.unity.xr.interaction.toolkit","XR Device Simulator");
            PackageManagerHelper.RequireSample("com.unity.xr.interaction.toolkit","Hands Interaction Demo");
            PackageManagerHelper.RequireSample("com.unity.xr.hands","HandVisualizer");
#endif
        }
    }
}
