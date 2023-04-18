#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    [InitializeOnLoad]
    public static class Startup
    {
        /// <summary>
        /// This start-up script makes sure that the Sample Room asset has been
        /// initialised.
        /// This asset should hold a unique value for each user, so is checked-
        /// out uninitialised.
        /// Once cloned, the user can override it at any time, if they like, for
        /// example, to copy the Guid of another project so those projects 
        /// samples will always share a session.
        /// </summary>
        static Startup()
        {
            foreach (var guid in AssetDatabase.FindAssets("Sample Room"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == typeof(RoomGuid))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(path) as RoomGuid;
                    if (string.IsNullOrEmpty(asset.Guid) || string.IsNullOrWhiteSpace(asset.Guid))
                    {
                        asset.Guid = System.Guid.NewGuid().ToString();
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.SaveAssets();
                        Utilities.SamplesHelper.UpdateSampleConfigs(true);
                        Debug.Log("Welcome to Ubiq! Your Sample Room has been updated with a unique Guid for your project.");
                    }
                }
            }
        }
    }
}
#endif