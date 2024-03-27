using UnityEditor;
using Ubiq.Editor.XRI;

namespace Ubiq.Samples.Demo.Editor
{
    [InitializeOnLoad]
    public class AddPackageXRI
    {
        static AddPackageXRI()
        {
            // Safer to interact with Unity on main thread
            EditorApplication.update += Update;
        }

        static void Update()
        {
            ImportHelperXRI.Import();
            EditorApplication.update -= Update;
        }
    }
}