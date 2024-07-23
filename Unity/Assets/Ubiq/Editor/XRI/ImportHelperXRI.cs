namespace Ubiq.Editor.XRI
{
    public static class ImportHelperXRI
    {
        public static void Import()
        {
#if XRI_2_6_0_OR_NEWER && !UBIQ_SILENCEWARNING_XRIVERSION
            UnityEngine.Debug.LogWarning(
                "Ubiq samples require XRI = 2.5.[2+], but a" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_XRIVERSION to your scripting define" +
                " symbols");
#endif
#if XRHANDS_1_5_0_OR_NEWER && !UBIQ_SILENCEWARNING_XRHANDSVERSION
            UnityEngine.Debug.LogWarning(
                "Ubiq samples require XRHands = 1.4.[1+], but a" +
                " different version is installed. The sample may not work" +
                " correctly. To silence this warning, add the string" +
                " UBIQ_SILENCEWARNING_XRHANDSVERSION to your scripting define" +
                " symbols");
#endif

#if !XRI_0_0_0_OR_NEWER
            PackageManagerHelper.AddPackage("com.unity.xr.interaction.toolkit@2.5.2");
#endif
#if !XRHANDS_0_0_0_OR_NEWER
            PackageManagerHelper.AddPackage("com.unity.xr.hands@1.3.0");
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