using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;

public class FindChineseTool : MonoBehaviour
{
    private ArrayList csList = new ArrayList();
    private int eachFrameFind = 4;
    private int currentIndex = 0;
    private bool isBeginUpdate = false;
    private string outputText;
    private int counter = 0;
    private int counter0 = 0;
    private void Awake()
    {
        csList.Clear();
        DirectoryInfo d = new DirectoryInfo(Application.dataPath + "/_Script");
        outputText = "开始遍历项目";
        GetAllFIle(d);
        outputText = "游戏内代码文件的数量：" + csList.Count;
        isBeginUpdate = true;
        counter = 0;
        counter0 = 1;
    }

    private void GetAllFIle(DirectoryInfo dir)
    {
        FileInfo[] allFile = dir.GetFiles();
        foreach (FileInfo fi in allFile)
        {
            if (fi.DirectoryName.IndexOf("\\Assets\\_Script") == -1)
                continue;
            if (fi.FullName.IndexOf(".meta") == -1 && fi.FullName.IndexOf(".cs") != -1)
            {
                csList.Add(fi.DirectoryName + "/" + fi.Name);
            }
        }
        DirectoryInfo[] allDir = dir.GetDirectories();
        foreach (DirectoryInfo d in allDir)
        {
            GetAllFIle(d);
        }
    }

    public void OnGUI()
    {
        GUILayout.Label(outputText, EditorStyles.boldLabel);
    }

    void Update()
    {
        if (isBeginUpdate && currentIndex < csList.Count)
        {
            int count = (csList.Count - currentIndex) > eachFrameFind ? eachFrameFind : (csList.Count - currentIndex);
            for (int i = 0; i < count; i++)
            {
                string url = csList[currentIndex].ToString();
                currentIndex = currentIndex + 1;
                url = url.Replace("\\", "/");
                GetScriptLines(url);
            }
            if (currentIndex >= csList.Count)
            {
                isBeginUpdate = false;
                currentIndex = 0;
                outputText = "遍历结束"+"---文件的数量：" + csList.Count;
                Debug.Log(" 总数:" + counter0 + " 计数:" + counter);
            }
        }
    }

    private static bool HasChinese(string str)
    {
        return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
    }
    private static bool WholeLineChinese(string str)
    {
        int idx0 = str.IndexOf("\"");
        int idx1 = str.LastIndexOf("\"") - 1;
        if(idx0 != -1 && idx1 != -1)
        {
            string subStr = str.Substring(idx0 + 1, idx1 - idx0);
            return subStr.IndexOf("\"") == -1;
        }
        else
        {
            return false;
        }
    }
    private static Regex regex = new Regex("\"[^\"]*\"");

    public static List<ScriptLineVo> GetScriptLines(string path)
    {
        List<ScriptLineVo> lines = new List<ScriptLineVo>();
        string[] fileContents = File.ReadAllLines(path, Encoding.Default);
        path = path.Replace(Application.dataPath, "");
        int count = fileContents.Length;
        for (int i = 0; i < count; i++)
        {
            string scriptLine = fileContents[i].Trim();
            if (scriptLine.IndexOf("//") == 0)  //说明是注释
                continue;
            if (scriptLine.IndexOf("/*") == 0)  //说明是注释
                continue;
            if (scriptLine.IndexOf("*") == 0)  //说明是注释
                continue;
            if (scriptLine.IndexOf("Debug.") != -1)  //说明是注释
                continue;
            if (scriptLine.IndexOf("throw new Exception") == 0)
                continue;
            if (scriptLine.IndexOf("debugLog") != -1)
                continue;
            if (scriptLine.IndexOf("debugLog") != -1)
                continue;
            MatchCollection matches = regex.Matches(scriptLine);
            ScriptLineVo line = new ScriptLineVo(path, scriptLine, (i + 1).ToString());
            foreach (Match match in matches)
            {
                if (HasChinese(match.Value))
                {
                    line.orgText.Add(match.Value);
                }
            }
            if (line.orgText.Count > 0)
            {
                line.isStaticText = scriptLine.IndexOf("static ") != -1;
                line.isConstText = scriptLine.IndexOf("const ") != -1;
                line.isCaseText = scriptLine.IndexOf("case ") != -1;
                //Debug.Log(path);
                lines.Add(line);
            }
        }
        fileContents = null;
        return lines;
    }
    public static List<ScriptLineVo> OpenSciptLines(string filePath,ref int lastId)
    {
        List<ScriptLineVo> lines = new List<ScriptLineVo>();
        string[] fileContents = File.ReadAllLines(filePath, Encoding.Default);
        int count = fileContents.Length;
        for (int i = 1; i < count; i++)
        {
            string[] cols = fileContents[i].Split(',');
            ScriptLineVo line = new ScriptLineVo(cols[1], "", cols[2]);
            line.id = int.Parse(cols[0]);
            line.zh_cn = cols[3];
            line.zh_tw = cols[4];
            line.orgText.Add(line.zh_cn);
            lines.Add(line);
            lastId = line.id;
        }
        return lines;
    }
    public static void SaveSciptLines(string filePath, string[] fileContents)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < fileContents.Length; i++)
        {
            sb.Append(fileContents[i]);
            sb.Append("\r\n");
        }
        FileUitl.SaveUTF8TextFile(filePath, sb.ToString());
    }
    public static string replaceWord(string str, string key, string value)
    {
        int idx = -1;
        do
        {
            idx = str.IndexOf(key);
            if (idx != -1)
                str = str.Replace(key, value);
        } while (idx != -1);
        return str;
    }
    //[UnityEditor.Callbacks.OnOpenAssetAttribute(0)]
    public static bool OpenAsset(string path, int line,string devPath)
    {
        if (File.Exists(path))
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = devPath;
            proc.StartInfo.Arguments = string.Format("{0} {1}", "/Edit", path);
            proc.Start();
            return true;
        }
        return true;
        //return UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, line);
    }
}