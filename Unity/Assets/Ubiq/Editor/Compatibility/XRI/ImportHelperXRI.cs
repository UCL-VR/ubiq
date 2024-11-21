namespace Ubiq.Editor.XRI
{
    public static class ImportHelperXRI
    {
        public static void Import()
        {
#if (XRI_3_1_0_OR_NEWER || (!XRI_3_0_7_OR_NEWER && XRI_0_0_0_OR_NEWER)) && !UBIQ_SILENCEWARNING_XRIVERSION
            UnityEngine.Debug.LogWarning(
                "Ubiq samples require XRI = 3.0.[7+], but a" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_XRIVERSION to your scripting define" +
                " symbols");
#endif
#if (XRHANDS_1_6_0_OR_NEWER || (!XRHANDS_1_5_0_OR_NEWER && XRHANDS_0_0_0_OR_NEWER)) && !UBIQ_SILENCEWARNING_XRHANDSVERSION
            UnityEngine.Debug.LogWarning(
                "Ubiq samples require XRHands = 1.5.[0+], but a" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_XRHANDSVERSION to your scripting define" +
                " symbols");
#endif

#if !XRI_0_0_0_OR_NEWER
            PackageManagerHelper.AddPackage("com.unity.xr.interaction.toolkit@3.0.7");
#endif
#if !XRHANDS_0_0_0_OR_NEWER
            PackageManagerHelper.AddPackage("com.unity.xr.hands@1.5.0");
#endif
            
#if XRI_0_0_0_OR_NEWER
            PackageManagerHelper.RequireSample("com.unity.xr.interaction.toolkit","Starter Assets");
            PackageManagerHelper.RequireSample("com.unity.xr.interaction.toolkit","XR Device Simulator");
            PackageManagerHelper.RequireSample("com.unity.xr.interaction.toolkit","Hands Interaction Demo");
#endif
#if XRHANDS_0_0_0_OR_NEWER
            PackageManagerHelper.RequireSample("com.unity.xr.hands","HandVisualizer");
#endif
        }
    }
}