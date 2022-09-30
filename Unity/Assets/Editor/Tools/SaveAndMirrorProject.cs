using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using System.Diagnostics;

public static class ProjectTools
{
    const string TARGET_DIR_PREFS_KEY = "Ubiq.PushProjectDir";

    [MenuItem ("Ubiq/Save And Mirror Project",true)]
    private static bool ValidateSaveAndMirrorProjectMenuItem ()
    {
        return EditorPrefs.HasKey(TARGET_DIR_PREFS_KEY);
    }

    [MenuItem ("Ubiq/Save And Mirror Project")]
    private static void SaveAndMirrorProjectMenuItem ()
    {
        if (!EditorPrefs.HasKey(TARGET_DIR_PREFS_KEY))
        {
            return;
        }

        DoSaveAndMirrorProject ();
    }

    [MenuItem ("Ubiq/Save And Mirror Project To...")]
    private static void SaveAndMirrorProjectToMenuItem ()
    {
        var dir = EditorPrefs.GetString(TARGET_DIR_PREFS_KEY,"");
        var newDir = EditorUtility.OpenFolderPanel("Destination Unity Project Folder",dir,"");

        if (!Directory.Exists(newDir))
        {
            // Assume user abandoned
            return;
        }

        EditorPrefs.SetString(TARGET_DIR_PREFS_KEY,newDir);

        DoSaveAndMirrorProject ();
    }

    private static void DoSaveAndMirrorProject () {
        // Get target dir
        var targetProjectDir = EditorPrefs.GetString(TARGET_DIR_PREFS_KEY,"");
        targetProjectDir = new DirectoryInfo(targetProjectDir).FullName;
        var targetAssetsDir = Path.Combine(targetProjectDir,"Assets");
        var targetSettingsDir = Path.Combine(targetProjectDir,"ProjectSettings");

        // Do some validation to check we're targeting a Unity project
        if (!Directory.Exists(targetProjectDir))
        {
            UnityEngine.Debug.LogError ("Could not save and push. Directory: "
                + targetProjectDir + " could not be found");
            return;
        }

        if (!Directory.Exists(targetAssetsDir) || !Directory.Exists(targetSettingsDir))
        {
            UnityEngine.Debug.LogError ("Could not save and push. Directory: "
                + targetProjectDir + " needs an Assets and ProjectSettings folder "
                + "(is it a Unity project?)");
            return;
        }

        // Save (should flush all assets to disk so we can copy)
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        // Push (robocopy source destination /MIR)
        var sourceAssetsDir = new DirectoryInfo(Application.dataPath).FullName;
        var sourceProjectDir = Directory.GetParent(sourceAssetsDir).FullName;
        var sourceSettingsDir = Path.Combine(sourceProjectDir,"ProjectSettings");
        var sourcePackagesDir = Path.Combine(sourceProjectDir,"Packages");
        var targetPackagesDir = Path.Combine(targetProjectDir,"Packages");

        // Originally this was async, but there are events in Unity that can
        // cause assembly reloads (e.g., starting play mode) which will silently
        // kill the process. Re-enable here if you're brave enough...
        // new Thread(delegate () {
        //     RunProcess("robocopy",sourceAssetsDir + " " + targetAssetsDir + @" /MIR");
        //     RunProcess("robocopy",sourceSettingsDir + " " + targetSettingsDir + @" /MIR");
        //     RunProcess("robocopy",sourcePackagesDir + " " + targetPackagesDir + @" manifest.json /MIR");
        // }).Start();

        RunAndLogProcess("robocopy",@$"""{sourceAssetsDir}"" ""{targetAssetsDir}"" /MIR");
        RunAndLogProcess("robocopy",@$"""{sourceSettingsDir}"" ""{targetSettingsDir}"" /MIR");
        RunAndLogProcess("robocopy",@$"""{sourcePackagesDir}"" ""{targetPackagesDir}"" manifest.json /MIR");
    }

    static void RunAndLogProcess (string file, string args)
    {
        RunProcess(file,args,out string stdout,out string stderr);
        UnityEngine.Debug.Log(file + " " + args + " " + stdout);
        if (!string.IsNullOrEmpty(stderr))
        {
            UnityEngine.Debug.LogError(file + " " + args + " " + stderr);
        }
    }

    static void RunProcess (string file, string args)
    {
        var process = CreateProcess (file,args,false);
        process.Start();
        process.WaitForExit();
        process.Close();
    }

    static void RunProcess (string file, string args, out string stdout, out string stderr)
    {
        var process = CreateProcess (file,args,true);

        process.Start();

        stdout = process.StandardOutput.ReadToEnd();
        stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();
        process.Close();
    }

    static Process CreateProcess (string file, string args, bool redirectOutput = false)
    {
        var process = new Process();
        var processInfo = new ProcessStartInfo(file,args);
        process.StartInfo.FileName = file;
        process.StartInfo.Arguments = args;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;

        if (redirectOutput)
        {
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
        }
        return process;
    }
}
