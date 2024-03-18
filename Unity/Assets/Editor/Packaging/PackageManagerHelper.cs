using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace UbiqEditor
{
    public static class PackageManagerHelper
    {
        private abstract class RequestInfo
        {
            public enum Mode
            {
                Add,
                Remove
            }

            public Mode mode { get; private set; }
            public string package { get; private set; }

            public RequestInfo(Mode mode, string package)
            {
                this.mode = mode;
                this.package = package;
            }
        }

        private class AddRequest : RequestInfo
        {
            public AddRequest(string package) : base(RequestInfo.Mode.Add,package) { }
        }

        private class RemoveRequest : RequestInfo
        {
            public RemoveRequest(string package) : base(RequestInfo.Mode.Remove,package) { }
        }

        private static ConcurrentQueue<RequestInfo> requestInfos = new ConcurrentQueue<RequestInfo>();
        private static AddAndRemoveRequest currentRequest;

        public static void Add(string packageToAdd)
        {
            EnqueueRequest(new AddRequest(packageToAdd));
        }

        public static void Remove(string packageToRemove)
        {
            EnqueueRequest(new RemoveRequest(packageToRemove));
        }

        private static void EnqueueRequest(RequestInfo request)
        {
            PackageManagerHelper.requestInfos.Enqueue(request);

            // Ensure we're only subscribed at most once
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (currentRequest == null)
            {
                // Sets for uniqueness
                var addSet = new HashSet<string>();
                var removeSet = new HashSet<string>();
                while(requestInfos.TryDequeue(out var info))
                {
                    if (info.mode == RequestInfo.Mode.Add)
                    {
                        addSet.Add(info.package);
                    }
                    else if (info.mode == RequestInfo.Mode.Remove)
                    {
                        removeSet.Add(info.package);
                    }
                }

                if (addSet.Count == 0 && removeSet.Count == 0)
                {
                    EditorApplication.update -= Update;
                    return;
                }

                var addArray = ToArray(addSet);
                var removeArray = ToArray(removeSet);
                currentRequest = Client.AddAndRemove(addArray,removeArray);

                PrintModificationMessage(addArray,removeArray);
            }

            if (currentRequest.Status == StatusCode.Failure)
            {
                var error = currentRequest.Error != null
                    ? currentRequest.Error.message
                    : "None specified";
                Debug.LogError($"Ubiq was unable to modify project requirements. Error: {error}");
                currentRequest = null;
            }

            if (currentRequest.Status == StatusCode.Success)
            {
                var collection = (PackageCollection)currentRequest.Result;
                Debug.Log("Ubiq successfully modified project requirements");
                AssetDatabase.Refresh();
                currentRequest = null;
            }
        }

        static string[] ToArray(HashSet<string> set)
        {
            var arr = new string[set.Count];
            set.CopyTo(arr);
            return arr;
        }

        static void PrintModificationMessage(string[] toAdd, string[] toRemove)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Ubiq attempting to modify project requirements.");

            if (toAdd.Length > 0)
            {
                stringBuilder.Append(" Adding: {");
                for (int i = 0; i < toAdd.Length; i++)
                {
                    stringBuilder.Append($" {toAdd[i]} ");
                    if (i < toAdd.Length-1)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append("}");
            }

            if (toRemove.Length > 0)
            {
                stringBuilder.Append(" Removing: {");
                for (int i = 0; i < toRemove.Length; i++)
                {
                    stringBuilder.Append($" {toRemove[i]} ");
                    if (i < toRemove.Length-1)
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