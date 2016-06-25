using System.Diagnostics;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class FilesGet : Editor
{

    public static List<string> nameArray = new List<string>();


    /// <summary>

    /// 根据指定的 Assets下的文件路径 返回这个路径下的所有文件名//

    /// </summary>

    /// <returns>文件名数组</returns>

    /// <param name="path">Assets下“一"级路径</param>

    /// <param name="pattern">筛选文件后缀名的条件.</param>

    /// <typeparam name="T">函数模板的类型名t</typeparam>

    private static void GetObjectNameToArray<T>(string path, string pattern)

    {

        string objPath = Application.dataPath + "/" + path;

        string[] directoryEntries;

        try

        {

//返回指定的目录中文件和子目录的名称的数组或空数组

            directoryEntries = System.IO.Directory.GetFileSystemEntries(objPath);


            for (int i = 0; i < directoryEntries.Length; i ++)
            {

                string p = directoryEntries[i];

//得到要求目录下的文件或者文件夹（一级的）//

                string[] tempPaths = StringExtention.SplitWithString(p, "/Assets/" + path + "\\");



//tempPaths 分割后的不可能为空,只要directoryEntries不为空//

                if (tempPaths[1].EndsWith(".meta"))

                    continue;

                string[] pathSplit = StringExtention.SplitWithString(tempPaths[1], ".");

//文件
                if (pathSplit.Length > 1)
                {
                    nameArray.Add(pathSplit[0]);
                }

//遍历子目录下 递归吧！

                else

                {

                    GetObjectNameToArray<T>(path + "/" + pathSplit[0], "pattern");

                    continue;

                }

            }

        }

        catch (System.IO.DirectoryNotFoundException)

        {

            Debug.Log("The path encapsulated in the " + objPath + "Directory object does not exist.");

        }

    }
    [MenuItem("Tools/GetFiles")]
    public static void Start()
    {

//TextAsset[] texts = LoadAsset<TextAsset> ("/CreateScriptDialog/Editor", "cs");

//GetObjectNameToArray<string> ("uSequencer/Example Scenes", "xxx");   //可以实现嵌套遍历

        GetObjectNameToArray<string>("NGUI", "xxx"); //可以实现嵌套遍历

        PrintResults(nameArray);

        foreach (string str in nameArray)
        {

            Debug.Log(str);

        }

    }

    /// 自定义的字符串分割的方法

    /// </summary>

    public class StringExtention
    {

        public static string[] SplitWithString(string sourceString, string splitString)
        {

             string tempSourceString = sourceString;

            List<string> arrayList = new List<string>();

            string s = string.Empty;

            while (sourceString.IndexOf(splitString) > -1) //分割

            {

                s = sourceString.Substring(0, sourceString.IndexOf(splitString));

                sourceString = sourceString.Substring(sourceString.IndexOf(splitString) + splitString.Length);

                arrayList.Add(s);

            }

            arrayList.Add(sourceString);

            return arrayList.ToArray();

        }
    }

    public static void PrintResults(List<string> nameArray)
    {
        string path = Application.dataPath + "/Calculation.txt";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        FileStream fieStream = File.Create(path);
        StreamWriter streamWriter = new StreamWriter(fieStream);
        int count = nameArray.Count;
        for (int i = 0; i < count; i++)
        {
            streamWriter.WriteLine(nameArray[i]);
        }
        streamWriter.Flush();
        streamWriter.Close();
        fieStream.Close();
        Process.Start(path );
    }
}
