using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.EditorCoroutines.Editor;

namespace Ubiq.Editor
{
    public static class PackageManagerHelper
    {
        private struct SampleInfo
        {
            public Sample sample { get; private set; }
            public string package { get; private set; }

            public SampleInfo(Sample sample, string package)
            {
                this.sample = sample;
                this.package = package;
            }
        }

        private class SampleRequest
        {
            public string package { get; private set; }
            public string sample { get; private set; }

            public SampleRequest(string package, string sample)
            {
                this.package = package;
                this.sample = sample;
            }
        }

        private class PackageRequest
        {
            public enum Mode
            {
                Add,
                Remove
            }

            public Mode mode { get; private set; }
            public string package { get; private set; }

            public PackageRequest(Mode mode, string package)
            {
                this.mode = mode;
                this.package = package;
            }
        }

        private static List<PackageRequest> packageRequests = new List<PackageRequest>();
        private static List<SampleRequest> sampleRequests = new List<SampleRequest>();
        private static bool assetsDirty = false;
        private static EditorCoroutine coroutine;

        public static void RequireSample(string package, string sample)
        {
            sampleRequests.Add(new SampleRequest(package,sample));
            EnsureCoroutine();
        }

        public static void AddPackage(string package)
        {
            packageRequests.Add(new PackageRequest(PackageRequest.Mode.Add,package));
            EnsureCoroutine();
        }

        public static void RemovePackage(string package)
        {
            packageRequests.Add(new PackageRequest(PackageRequest.Mode.Remove,package));
            EnsureCoroutine();
        }

        private static void EnsureCoroutine()
        {
            if (coroutine != null)
            {
                return;
            }

            coroutine = EditorCoroutineUtility.StartCoroutineOwnerless(Process());
        }

        private static IEnumerator Process()
        {
            yield return null; // Wait to allow startup set of deps to accrue

            while (packageRequests.Count > 0 || sampleRequests.Count > 0)
            {
                yield return ProcessPackages();
                yield return ProcessSamples();
                yield return null;
            }

            if (assetsDirty)
            {
                AssetDatabase.Refresh();
            }
        }

        private static IEnumerator ProcessPackages()
        {
#if UBIQ_DISABLE_PACKAGEIMPORT
    #if !UBIQ_SILENCEWARNING_DISABLEPACKAGEIMPORT
            Debug.LogWarning("Ubiq will not modify packages as the" +
                    " scripting define symbol UBIQ_DISABLE_PACKAGEIMPORT is" +
                    " present. Please ensure you manage the required packages" +
                    " manually, or Ubiq may not function as intended. To" +
                    " silence this warning, add the string"
                    " UBIQ_SILENCEWARNING_DISABLEPACKAGEIMPORT to your scripting"
                    " define symbols.");
    #endif
            packageRequests.Clear();
            yield break;
#endif
            
            if (packageRequests.Count == 0)
            {
                yield break;
            }
            
            GetUnique(packageRequests, out var adds, out var removes);
            packageRequests.Clear();
            LogPackageModificationMessage(adds,removes);
            
            // First check if the packages for removal are present
            var listRequest = Client.List(
                offlineMode:true,includeIndirectDependencies:false);
            while (listRequest != null)
            {
                if (listRequest.IsCompleted)
                {
                    Filter(ref removes, listRequest.Result);
                    listRequest = null;
                }
                
                yield return null;
            }

            // Now do the actual add/remove
            var upmRequest = Client.AddAndRemove(adds,removes);
            while (upmRequest != null)
            {
                if (upmRequest.Status == StatusCode.Failure)
                {
                    var error = upmRequest.Error != null
                        ? upmRequest.Error.message
                        : "None specified";
                    Debug.LogError($"Ubiq was unable to modify project requirements. Error: {error}");
                    upmRequest = null;
                }
                else if (upmRequest.Status == StatusCode.Success)
                {
                    Debug.Log("Ubiq successfully modified project requirements.");
                    upmRequest = null;
                    assetsDirty = true;
                }

                yield return null;
            }
            
            yield return null;
        }

        private static IEnumerator ProcessSamples()
        {
            var requiredSamplesInfos = GetRequiredSamples(sampleRequests);
            if (requiredSamplesInfos != null && requiredSamplesInfos.Count > 0)
            {
                LogSampleModificationMessage(requiredSamplesInfos);

#if UBIQ_DISABLE_SAMPLEIMPORT
                requiredSamplesInfos.Clear();
    #if !UBIQ_SILENCEWARNING_DISABLESAMPLEIMPORT
                Debug.LogWarning("Ubiq will not modify samples as the" +
                    " scripting define symbol UBIQ_DISABLE_SAMPLEIMPORT is" +
                    " present. Please ensure you manage the required samples" +
                    " manually, or Ubiq may not function as intended. To" +
                    " silence this warning, add the string"
                    " UBIQ_SILENCEWARNING_DISABLESAMPLEIMPORT to your scripting"
                    " define symbols.");
    #endif
#endif
                sampleRequests.Clear();
            }

            foreach (var info in requiredSamplesInfos)
            {
                yield return null;

                if (info.sample.Import(Sample.ImportOptions.OverridePreviousImports
                    | Sample.ImportOptions.HideImportWindow))
                {
                    Debug.Log("Ubiq successfully imported sample" +
                        $"\"{info.sample}\" from package \"{info.package}\".");
                    assetsDirty = true;
                }
                else
                {
                    Debug.LogWarning("Ubiq failed to import sample" +
                        $"\"{info.sample}\" from package \"{info.package}\".");
                }
            }
        }

        private static List<SampleInfo> GetRequiredSamples(List<SampleRequest> requests)
        {
            if (requests == null || requests.Count == 0)
            {
                return null;
            }

            var samples = new List<SampleInfo>();
            foreach(var request in requests)
            {
                var packageSamples = null as IEnumerable<Sample>;
                try
                {
                    packageSamples = Sample.FindByPackage(request.package, string.Empty);
                }
                catch
                {
                    packageSamples = null;
                }

                if (packageSamples == null)
                {
                    Debug.LogWarning("Ubiq is trying to find a sample" +
                        " for the package" +
                        $" \"{ request.package }\", but the package could not" +
                        " be found.");
                    continue;
                }

                var found = false;
                foreach (var packageSample in packageSamples)
                {
                    if (packageSample.displayName == request.sample)
                    {
                        if (!packageSample.isImported)
                        {
                            samples.Add(new SampleInfo(packageSample,request.package));
                        }
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                Debug.LogWarning("Ubiq is trying to find the sample" +
                    $" \"{ request.sample }\" for the package" +
                    $" \"{ request.package }\", but the sample could not" +
                    " be found in the package.");
            }
            return samples;
        }
        
        private static void Filter(ref string[] names, PackageCollection collection)
        {
            var result = new List<string>();
            foreach (var requestedPackage in names)
            {
                foreach (var existingPackage in collection)
                {
                    if (existingPackage.name == requestedPackage)
                    {
                        result.Add(requestedPackage);
                        break;
                    }
                }
            }
            
            names = result.ToArray();
        }

        private static void GetUnique(List<PackageRequest> requests,
            out string[] adds, out string[] removes)
        {
            var addSet = new HashSet<string>();
            var removeSet = new HashSet<string>();

            foreach (var request in packageRequests)
            {
                if (request.mode == PackageRequest.Mode.Add)
                {
                    addSet.Add(request.package);
                }
                else if (request.mode == PackageRequest.Mode.Remove)
                {
                    removeSet.Add(request.package);
                }
            }

            adds = ToArray(addSet);
            removes = ToArray(removeSet);
        }

        static string[] ToArray(HashSet<string> set)
        {
            var arr = new string[set.Count];
            set.CopyTo(arr);
            return arr;
        }

        static void LogSampleModificationMessage(List<SampleInfo> infos)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Ubiq attempting to import required samples.");
            stringBuilder.Append(" Importing: {");
            for (int i = 0; i < infos.Count; i++)
            {
                stringBuilder.Append($" {infos[i].package}:{infos[i].sample.displayName} ");
                if (i < infos.Count-1)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append("}");

            Debug.Log(stringBuilder.ToString());
        }

        static void LogPackageModificationMessage(string[] adds, string[] removes)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Ubiq attempting to modify project requirements.");

            if (adds.Length > 0)
            {
                stringBuilder.Append(" Adding: {");
                for (int i = 0; i < adds.Length; i++)
                {
                    stringBuilder.Append($" {adds[i]} ");
                    if (i < adds.Length-1)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append("}");
            }

            if (removes.Length > 0)
            {
                stringBuilder.Append(" Removing: {");
                for (int i = 0; i < removes.Length; i++)
                {
                    stringBuilder.Append($" {removes[i]} ");
                    if (i < removes.Length-1)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append("}");
            }

            Debug.Log(stringBuilder.ToString());
        }
    }
}