using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Ubiq.Samples.Demo.Editor
{
    internal static class RequireSamplesXRIUtil
    {
        public const string DEMO_SCENE_GUID = "2de505d34345ab340bdde30d419b24eb";
        public const string LOOPBACK_SCENE_GUID = "44835542044d9c1469efdf6740cdc7ba";
        public const string XRI_SAMPLE_PLAYER_GUID = "f6336ac4ac8b4d34bc5072418cdc62a0";
        public const string XRI_SAMPLE_SIMULATOR_GUID = "18ddb545287c546e19cc77dc9fbb2189";

        public static bool IsSamplesLoaded()
        {
            return AssetExistsFromGUID(RequireSamplesXRIUtil.XRI_SAMPLE_PLAYER_GUID)
                && AssetExistsFromGUID(RequireSamplesXRIUtil.XRI_SAMPLE_SIMULATOR_GUID);
        }

        public static bool IsUbiqSampleScene(string guid)
        {
            return guid == DEMO_SCENE_GUID || guid == LOOPBACK_SCENE_GUID;
        }

        public static bool AssetExistsFromGUID(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            // If an asset previously existed in this editor session, Unity will
            // cache the path and still return it, even if the asset is missing.
            // We use the OnlyExistingAssets option (only available on
            // AssetPathToGUID) to make sure it's really present.
            var resultGuid = AssetDatabase.AssetPathToGUID(path,AssetPathToGUIDOptions.OnlyExistingAssets);
            return !string.IsNullOrEmpty(resultGuid);
        }
    }

#if XRI_2_4_3_OR_NEWER
    [InitializeOnLoad]
    public class RequireSamplesXRI
    {
        static RequireSamplesXRI()
        {
            if (RequireSamplesXRIUtil.IsSamplesLoaded())
            {
                return;
            }

            // Many things won't work in this static function, so do everything
            // in callbacks.
            EditorApplication.delayCall += Init;
            EditorSceneManager.sceneOpened += SceneOpened;
        }

        static void Init()
        {
            // We only need to run this init once per assembly reload
            EditorApplication.delayCall -= Init;

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                SceneOpened(EditorSceneManager.GetSceneAt(i),OpenSceneMode.Single);
            }
        }

        static void SceneOpened(Scene scene, OpenSceneMode mode)
        {
            // Check if the scene we're opening is the demo scene
            var openedSceneGuid = AssetDatabase.AssetPathToGUID(scene.path);
            if (!RequireSamplesXRIUtil.IsUbiqSampleScene(openedSceneGuid))
            {
                return;
            }

            // Popup a window to let the user know they need the samples
            RequireSamplesXRIWindow.Get();

            // We don't need to check if the samples are imported again, or
            // remove this callback. If the samples are imported, there'll
            // be an assembly reload and this will all be cleared.
        }
    }
#endif

    public class RequireSamplesXRIWindow : EditorWindow
    {
        public static void Get()
        {
            var window = GetWindow<RequireSamplesXRIWindow>(utility:true);

            var size = new Vector2(520, 180);
            var center = EditorGUIUtility.GetMainWindowPosition().center;
            var position = center - size * 0.5f;

            window.titleContent = new GUIContent("XRI Samples Required");
            window.minSize = size;
            window.position = new Rect(position,size);
        }

        void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            var header = new Label(
                "<b>Ubiq Demo: Please import XRI Samples</b>"
            );
            header.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            header.style.fontSize = 18;
            header.style.paddingTop = 18;
            header.style.paddingBottom = 18;
            root.Add(header);

            if (RequireSamplesXRIUtil.IsSamplesLoaded())
            {
                // Starter assets loaded. This means the user has added them
                // while this window was open.
                var thanks = new Label(
                    "XRI Samples detected!\n\nThank you. Please close this" +
                    " window. It will not be shown again.");
                thanks.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
                thanks.style.paddingBottom = 18;
                thanks.style.paddingLeft = 5;
                thanks.style.paddingRight = 5;
                root.Add(thanks);
                return;
            }

            // XRI StarterAssets not found. Give user instructions on importing
            var instructions = new Label(
                "This scene requires the Starter Assets and XR Device" +
                " Simulator from Unity's XR" +
                " Interaction Toolkit, and it looks like these are not" +
                " yet imported in your project. To get this scene" +
                " working correctly, please import them manually at the" +
                " following:");
            instructions.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            instructions.style.paddingBottom = 18;
            instructions.style.paddingLeft = 5;
            instructions.style.paddingRight = 5;
            root.Add(instructions);

            var path = new Label(
                "1) Window > Package Manager > XR Interaction Toolkit > Samples > Starter Assets\n" +
                "2) Window > Package Manager > XR Interaction Toolkit > Samples > XR Device Simulator");
            path.style.paddingLeft = 5;
            path.style.paddingRight = 5;
            root.Add(path);
        }
    }
}