using UnityEngine;
using System.IO;
using System.Collections;

public class WebPmFileLoader : MonoBehaviour {

    //[HideInInspector]
    //public Stream PMFileStream = null;


    //public IEnumerator LoadFileToStream(string PMFilePath)
    //{

    //    PMFileStream = null;

    //    if (Application.platform == RuntimePlatform.WebGLPlayer ||
    //        Application.platform == RuntimePlatform.WindowsWebPlayer)
    //    {

    //        string FileName = System.IO.Path.GetFileName(PMFilePath);
    //        string FilePath = Application.streamingAssetsPath + "/" + FileName;

    //        if (Application.platform == RuntimePlatform.WindowsWebPlayer)
    //            FilePath = Application.streamingAssetsPath.Replace("Raw", "StreamingAssets") + "/" + FileName;


    //        WWW www = new WWW(FilePath);

    //        yield return www;



    //        if (string.IsNullOrEmpty(www.error))
    //        {
    //            PMFileStream = new System.IO.MemoryStream(www.bytes);
    //            Debug.Log("Loaded bytes length : " + www.bytes.Length);
    //            Debug.Log("Web file loading completed");
    //        }

    //    }

    //}

}
