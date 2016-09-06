using System;
using System.Collections.Generic;
using UnityEngine;

public class PathUtils
{

    public static string GetStreamFilePath(string fileUrl)
    {
        string path = "";
        if (fileUrl.ToLower().IndexOf("http://") == 0)
            return fileUrl;
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
        {
            path = "file://"+Application.streamingAssetsPath+"/"+fileUrl;
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            path = "file://"+Application.streamingAssetsPath+"/"+fileUrl;
        }
        else if(Application.platform ==RuntimePlatform.Android)
        {
            path = "jar:file://"+Application.dataPath+"!/assets/"+fileUrl;
        }
        else
        {
            path = Application.dataPath + "/config/" + fileUrl;
        }
        return path;
    }

    public static string GetPersistebrFilePath(string filename)
    {
        string filepath;
        if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXWebPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer)
            filepath = "file://"+Application.dataPath+"/StreamingAssets/+filename";
        else if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
            filepath = Application.persistentDataPath + "/" + filename;
        else
        {
            filepath = Application.persistentDataPath + "/" + filename;
        }
#if UNITY_IPHONE
            iphone.SetNoBackupFlag(filepath);
#endif
        return filepath;
    }

}
