using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public static class PackageMenuItems
{
    const string TARGET_ZIP_DIR_PREFS_KEY = "Ubiq.Package.ZipDir";
    const string TARGET_GIT_PATH_PREFS_KEY = "Ubiq.Package.GitPath";
    const string DEFAULT_NAME = "ubiq";
    const string ZIP_EXE_WIN_PATH = @"Local\Package\Editor\Win\tar.exe";

    const string TMP_FOLDER_PREFIX = "tmp-";

    private class Manifest
    {
        public string version = "";
    }

    [MenuItem ("Ubiq-dev/Pack for Unity Package Manager")]
    private static void PackForUnityPackageManager ()
    {
        // Check if platform is supported
        Debug.Log("Checking to see if this task is supported on current platform...");
        if (!IsSupported())
        {
            Debug.LogError("Unsupported platform. Currently supports Windows only. Abandoning build");
        return;
        }
        else
        {
            Debug.Log("Platform supported. Continuing...");
        }

        // Get version number
        Debug.Log("Inspecting package.json for version number...");
        var versionString = null as string;
        try
        {
            var packageJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/package.json");
            var manifest = JsonUtility.FromJson<Manifest>(packageJson.text);
            versionString = manifest.version;
        }
        catch
        {
            Debug.LogError("Manifest package.json could not be found or read. Abandoning build");
            return;
        }
        Debug.Log("Version number read from package.json: " + versionString);

        // Check if git status is clean
        Debug.Log("Finding appropriate git exe...");

        var gitPath = EditorPrefs.GetString(TARGET_GIT_PATH_PREFS_KEY,"NOT FOUND");
        var response = EditorUtility.DisplayDialogComplex("Choose git Executable",
                "Your current git path is:\n\n" + gitPath + "\n\nWould you like to use this exe or browse for another?\n\nNote: This path should be to your git executable, not git-cmd or git-bash",
                "Use Current Git Path",
                "Cancel",
                "Browse");
        switch (response)
        {
            case 0:
            {
                break;
            }
            case 1:
            {
                Debug.Log("Cancelling...");
                return;
            }
            case 2:
            {
                gitPath = EditorUtility.OpenFilePanel("Select git","","");
                if (string.IsNullOrEmpty(gitPath))
                {
                    Debug.Log("Cancelling...");
                    return;
                }

                EditorPrefs.SetString(TARGET_GIT_PATH_PREFS_KEY,gitPath);
                break;
            }
        }


        // Check if git status is clean
        Debug.Log("Checking git status to see if we're modified...");
        RunProcess(gitPath,"status --porcelain",out string stdout, out string stderr);
        var commitHash = "unknown";
        if (!string.IsNullOrEmpty(stderr))
        {
            Debug.LogError("Error found while checking git status: " + stderr);
            if(!EditorUtility.DisplayDialog("Pack for Unity Package Manager",
                "Error while checking git status:\n\n" + stderr + "\n\nContinue packing anyway?",
                "OK","Cancel"))
            {
                return;
            }
            Debug.LogWarning("Continuing despite git status error. Commit hash unknown");
        }
        else if (!string.IsNullOrEmpty(stdout))
        {
            Debug.LogWarning("Git status: " + stdout);
            if(!EditorUtility.DisplayDialog("Pack for Unity Package Manager",
                "Git status reports modified files:\n\n" + stdout + "\nContinue packing anyway?",
                "OK","Cancel"))
            {
                return;
            }
            Debug.LogWarning("Continuing despite git status reporting modified. Commit hash unknown");
        }
        else
        {
            RunProcess(gitPath, "rev-parse --short HEAD",out stdout, out string _);
            commitHash = stdout;
            Debug.Log("Git status reports up-to-date. Commit hash " + commitHash);
        }

        // Get output path
        var prefKeyZipDir = EditorPrefs.GetString(TARGET_ZIP_DIR_PREFS_KEY,"");
        var defaultName = DEFAULT_NAME + "-" + versionString;
        var outputPath = EditorUtility.SaveFilePanel("Destination zip",prefKeyZipDir,defaultName,"zip");

        if (string.IsNullOrEmpty(outputPath))
        {
            // Assume user abandoned
            return;
        }
        outputPath = new DirectoryInfo(outputPath).FullName;
        var sourceAssetsDir = new DirectoryInfo(Application.dataPath).FullName;
        var tmpFolderName = TMP_FOLDER_PREFIX + System.Guid.NewGuid().ToString().Substring(0,8);
        var tmpAssetsDir = Path.Combine(Directory.GetParent(outputPath).FullName,tmpFolderName);

        EditorPrefs.SetString(TARGET_ZIP_DIR_PREFS_KEY,Directory.GetParent(outputPath).FullName);

        // Copy to tmp
        Debug.Log("Copying tmp files...");
        // Note 'Samples~' with tilde - makes samples hidden in package but visible to sample importer
        CopyDir(Path.Combine(sourceAssetsDir,"Samples"),Path.Combine(tmpAssetsDir,"Samples~"));
        CopyDir(Path.Combine(sourceAssetsDir,"Editor"),Path.Combine(tmpAssetsDir,"Editor"));
        CopyFile(Path.Combine(sourceAssetsDir,"Editor.meta"),Path.Combine(tmpAssetsDir,"Editor.meta"));
        CopyDir(Path.Combine(sourceAssetsDir,"Runtime"),Path.Combine(tmpAssetsDir,"Runtime"));
        CopyFile(Path.Combine(sourceAssetsDir,"Runtime.meta"),Path.Combine(tmpAssetsDir,"Runtime.meta"));
        CopyFile(Path.Combine(sourceAssetsDir,"package.json"),Path.Combine(tmpAssetsDir,"package.json"));
        CopyFile(Path.Combine(sourceAssetsDir,"package.json.meta"),Path.Combine(tmpAssetsDir,"package.json.meta"));

        // Zip
        Debug.Log("Zipping...");
        Zip(tmpAssetsDir,outputPath,log:false,shell:true);

        // Cleanup
        Debug.Log("Cleanup up tmp files...");
        Directory.Delete(tmpAssetsDir,true);

        if (commitHash != "unknown")
        {
            EditorUtility.DisplayDialog("Success!",
                "Package built at path: " + outputPath +
                "\n\n" +
                "IS THIS A RELEASE BUILD? THEN:\n" +
                "Tag the commit: " + commitHash +
                "With version: " + versionString,
                "Ok");
        } else
        {
            EditorUtility.DisplayDialog("Success!",
                "Package built at path: " + outputPath +
                "\n\n" +
                "DO NOT USE AS A RELEASE BUILD.\n" +
                "Reason: no accompanying git commit.",
                "Ok");
        }
        Debug.Log("Success! Package built at path: " + outputPath);
    }

    static bool IsSupported ()
    {
#if UNITY_EDITOR_WIN
        return true;
# elif UNITY_EDITOR_OSX
        return false;
# elif UNITY_EDITOR_LINUX
        return false;
# else
        return false;
#endif
    }

    static void CopyDir (string src, string dst)
    {
        CopyAll(new DirectoryInfo(src),new DirectoryInfo(dst));
    }

    static void CopyFile (string src, string dst)
    {
        var sourceFileInfo = new FileInfo(src);
        sourceFileInfo.CopyTo(dst,true);
    }

    // Example directory copy code from MSDN
    // Looks like it could overflow stack if directory structure is weird enough
    private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        if (source.FullName.ToLower() == target.FullName.ToLower())
        {
            return;
        }

        // Check if the target directory exists, if not, create it.
        if (Directory.Exists(target.FullName) == false)
        {
            Directory.CreateDirectory(target.FullName);
        }

        // Copy each file into it's new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    static void Zip (string srcDir, string dst, bool log = false, bool shell = false)
    {
        var exePath = Path.Combine(Application.dataPath.Replace('/','\\'),ZIP_EXE_WIN_PATH);
#if UNITY_EDITOR_WIN
        if (log)
        {
            RunProcessAndLog(exePath,"-cvaf \"" + dst + "\" *",srcDir);
        }
        else
        {
            RunProcess(exePath,"-cvaf \"" + dst + "\" *",srcDir,shell);
        }
# elif UNITY_EDITOR_OSX
# elif UNITY_EDITOR_LINUX
# else
# endif
    }

    static void RunProcessAndLog (string file, string args, string workingDir = "")
    {
        RunProcess(file,args,out string stdout,out string stderr,workingDir);
        UnityEngine.Debug.Log(file + " " + args + " " + stdout);
        if (!string.IsNullOrEmpty(stderr))
        {
            UnityEngine.Debug.LogError(file + " " + args + " " + stderr);
        }
    }

    static void RunProcess (string file, string args, string workingDir = "", bool shell = false)
    {
        var process = CreateProcess (file,args,workingDir:workingDir,shell:shell);
        process.Start();
        process.WaitForExit();
        process.Close();
    }

    static void RunProcess (string file, string args, out string stdout, out string stderr, string workingDir = "")
    {
        var process = CreateProcess (file,args,workingDir:workingDir,shell:false);
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        // Add stderr handler - need one handler to be async to prevent deadlock
        string stderrout = null;
        process.ErrorDataReceived +=
            new System.Diagnostics.DataReceivedEventHandler((sender, e) =>
                                 { stderrout += e.Data; });

        process.Start();

        // Begin stderr async handler
        process.BeginErrorReadLine();

        stdout = process.StandardOutput.ReadToEnd();

        process.WaitForExit();
        process.Close();

        stderr = stderrout;
    }

    static System.Diagnostics.Process CreateProcess (string file, string args, string workingDir = "", bool shell = false)
    {
        var process = new System.Diagnostics.Process();
        var processInfo = new System.Diagnostics.ProcessStartInfo(file,args);
        process.StartInfo.FileName = file;
        process.StartInfo.Arguments = args;
        if (shell)
        {
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = true;
        }
        else
        {
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
        }

        if (!string.IsNullOrEmpty(workingDir))
        {
            process.StartInfo.WorkingDirectory = workingDir;
        }

        return process;
    }
}
