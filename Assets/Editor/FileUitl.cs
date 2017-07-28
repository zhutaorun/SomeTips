using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class FileUitl 
{
    public static void SaveUTF8TextFile(string fn, string txt)
    {
        byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(txt);
        byte[] bom = new byte[]{0xef,0xbb,0xbf};
        byte[] saveData = new byte[data.Length+bom.Length];
        Array.Copy(bom, 0, saveData, 0, bom.Length);
        Array.Copy(data,0,saveData,bom.Length,data.Length);
        SaveFileData(fn,saveData);
    }


    public static void SaveFileData(string fn, byte[] data)
    {
        string dir = Path.GetDirectoryName(fn);
        System.IO.DirectoryInfo dirinfo = new DirectoryInfo(dir);
        if (!dirinfo.Exists)
            dirinfo.Create();
        FileStream fs = new FileStream(fn,FileMode.Create);
        try
        {
            fs.Write(data, 0, data.Length);
        }
        finally
        {
            fs.Close();
        }
    }

    public static Dictionary<string, string> GetDictionaryFromFile(string fn)
    {
        byte[] data = GetFileData(fn);
        if (data != null)
        {
            ByteReader br = new ByteReader(data);
            return br.ReadDictionary();
        }
        return null;
    }

    public static byte[] GetFileData(string fn)
    {
        if (!File.Exists(fn))
            return null;
        FileStream fs = new FileStream(fn,FileMode.Open);
        try
        {
            if (fs.Length > 0)
            {
                byte[] data = new byte[(int)fs.Length];
                fs.Read(data,0,(int)fs.Length);
                return data;
            }
            else
            {
                return null;
            }
        }
        finally
        {
            fs.Close();
        }
    }

    public static void SaveTextFile(string fn, string txt)
    {
        byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(txt);
        SaveFileData(fn,data);
    }
}
