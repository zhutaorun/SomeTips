using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


public class ScriptLocalizationWindow : EditorWindow
{
    [MenuItem("Tools/代码国际化")]
    public static void Pack()
    {
        ScriptLocalizationWindow window = EditorWindow.GetWindow<ScriptLocalizationWindow>("代码国际化", true);
        window.Show();
    }
    enum OptStatus
    {
        None,
        Ready,
        Start,
        Doing,
        Complete,
    }
    private string _scritpPath = "/Scripts";
    private static string _csvPath = @"E:\work\branches\doc\GameTable\Config\ScriptLocalization.csv";
    private string _appPath = "/StreamingAssets/config/ScriptLocalization.xml";
    private static string _devPath = @"D:\Program Files\Microsoft Visual Studio 12.0\Common7\IDE\devenv.exe";
    private Queue<string> _fileQueue;
    private int NUM_PER_FRAME = 10;
    private OptStatus state;
    private List<ScriptLineVo> scriptMapLines;
    private List<ScriptLineVo> newScriptLines;
    private int _last_s_id;
    private int _maxFileNum;
    private const string DOT = "|DOT|";
    private Vector2 scrollPos;
    private string tempZh_cn;
    private string temp_id;
    void Awake()
    {
        state = OptStatus.None;
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        _scritpPath = EditorGUILayout.TextField("代码路径", _scritpPath);
        _devPath = EditorGUILayout.TextField("VS2013路径", _devPath);
        _csvPath = EditorGUILayout.TextField("CSV路径", _csvPath);
        if (state == OptStatus.None && GUILayout.Button("加载映射表", GUILayout.Width(200)))
        {
            if (File.Exists(_csvPath))
                scriptMapLines = FindChineseTool.OpenSciptLines(_csvPath, ref _last_s_id);
            else
                scriptMapLines = new List<ScriptLineVo>();
            _last_s_id = _last_s_id == 0 ? 100000 : _last_s_id;
            state = OptStatus.Ready;
        }
        if ((state == OptStatus.Ready || state == OptStatus.Complete) && GUILayout.Button("扫描包含中文的代码", GUILayout.Width(200)))
        {
            state = OptStatus.Start;
            newScriptLines = new List<ScriptLineVo>();
            DoSearch(Application.dataPath + _scritpPath);
        }
        //if (state == OptStatus.Complete && GUILayout.Button("生成代码国际化映射表", GUILayout.Width(200)))
        {
            //SaveMapFile(_csvPath,true);
        }
        //if (scriptMapLines != null && GUILayout.Button("批量替换中文代码", GUILayout.Width(200)))
        {
            //ReplaceScript();
        }
        if (scriptMapLines != null && GUILayout.Button("重新生成正式映射文件", GUILayout.Width(200)))
        {
            reSaveOfficialMapFile(Application.dataPath + _appPath);
        }
        if (scriptMapLines != null)
        {
            temp_id = EditorGUILayout.TextField("ID", temp_id);
            tempZh_cn = EditorGUILayout.TextField("简体", tempZh_cn);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("查看id对应的中文"))
            {
                int id = int.Parse(temp_id);
                if (id > 100000)
                {
                    ScriptLineVo line = GetLine(id);
                    if(line != null)
                    {
                        tempZh_cn = line.orgText[0];
                    }
                    else
                    {
                        Debug.Log("条目找不到");
                    }
                }
            }
            if (GUILayout.Button("保存对应ID的条目"))
            {
                int id = int.Parse(temp_id);
                if (id > 100000 && !string.IsNullOrEmpty(tempZh_cn))
                {
                    ScriptLineVo line = GetLine(id);
                    if (line == null)
                    {
                        ScriptLineVo oldLine = GetOldLine(tempZh_cn);
                        if (oldLine == null)
                        {
                            line = new ScriptLineVo("", "", "");
                            line.id = id;
                            line.orgText.Add(tempZh_cn);
                            line.zh_cn = tempZh_cn;
                            scriptMapLines.Add(line);
                            SaveMapFile(_csvPath, false);
                            Debug.Log("新增条目");
                        }
                        else
                        {
                            Debug.LogError("该条目已经存在 id为：" + oldLine.id);
                            this.ShowNotification(new GUIContent("该条目已经存在 id为：" + oldLine.id));
                        }
                    }
                    else
                    {
                        line.orgText[0] = tempZh_cn;
                        line.zh_tw = null;
                        Debug.Log("修改条目");
                        SaveMapFile(_csvPath, false);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        ShowOtherLines();
    }
    void Update()
    {
        if (_fileQueue != null && _fileQueue.Count > 0)
        {
            for (int i = 0; i < NUM_PER_FRAME && _fileQueue.Count > 0; i++)
            {
                newScriptLines.AddRange(FindChineseTool.GetScriptLines(_fileQueue.Dequeue()));
            }
            if (_fileQueue.Count == 0)
                state = OptStatus.Complete;
            else
                state = OptStatus.Doing;
        }
        if (state == OptStatus.Doing)
            EditorUtility.DisplayProgressBar("Searching..." + (_maxFileNum - _fileQueue.Count) + "/" + _maxFileNum, name, (float)(_maxFileNum - _fileQueue.Count) / (float)_maxFileNum);
        else
            EditorUtility.ClearProgressBar();
    }
    void DoSearch(string scriptPath)
    {
        var files = Directory.GetFiles(scriptPath, "*.cs", SearchOption.AllDirectories);
        _fileQueue = new Queue<string>(files);
        _maxFileNum = files.Length;
    }
    void SaveMapFile(string path,bool merge)
    {
        if(merge && !MergeLines())//合并
        {
            Debug.Log("没有变动无需保存");
            state = OptStatus.None;
            return;
        }
        StringBuilder sb = new StringBuilder();
        sb.Append("id"); sb.Append(",");
        sb.Append("path"); sb.Append(",");
        sb.Append("line"); sb.Append(",");
        sb.Append("zh_cn"); sb.Append(",");
        sb.Append("zh_tw"); sb.Append("\n");
        List<ScriptLineVo> lines = scriptMapLines;
        List<string> texts = new List<string>();
        for (int i = 0; i < lines.Count; i++)
        {
            ScriptLineVo line = lines[i];
            sb.Append(line.id);
            sb.Append(",");
            sb.Append(line.path);
            sb.Append(",");
            sb.Append(line.lineNum);
            sb.Append(",");
            line.orgText[0] = FindChineseTool.replaceWord(line.orgText[0], ",", DOT);
            sb.Append(line.orgText[0]);
            sb.Append(",");
            if (line.zh_tw == null)
                sb.Append(LangguageTools.To_zh_tc(line.orgText[0]));
            else
                sb.Append(line.zh_tw);
            sb.Append("\n");
            
        }
        if (File.Exists(path))
            File.Delete(path);
        FileUitl.SaveUTF8TextFile(path, sb.ToString());
        Debug.Log("保存成功!");
        if (merge)
            state = OptStatus.None;
    }
    bool MergeLines()
    {
        bool needSave = false;
        for (int i = 0; i < newScriptLines.Count; i++)
        {
            ScriptLineVo line = newScriptLines[i];
            if (line.isCanAutoSet())
            {//只处理一行只有一中文的情况
                ScriptLineVo oldLine = GetOldLine(line.orgText[0]);
                if (oldLine == null)
                {
                    _last_s_id++;
                    line.id = _last_s_id;
                    scriptMapLines.Add(line);
                    Debug.Log(string.Format("New file:{0} line:[{1}] \ntext:{2} \nscript:{3}", line.path, line.lineNum, line.orgText[0], line.stript));
                    needSave = true;
                }
                else
                {
                    if(oldLine.path.IndexOf(line.path) == -1)
                    {
                        oldLine.path = oldLine.path + "|" + line.path;
                        oldLine.lineNum = oldLine.lineNum + "|" + line.lineNum;
                        Debug.Log(string.Format("Add file Path:{0} line:[{1}] \ntext:{2} \nscript:{3}", line.path, line.lineNum, line.orgText[0], line.stript));
                        needSave = true;
                    }
                }
            }
        }
        return needSave;
    }
    ScriptLineVo GetOldLine(string text)
    {
        for (int i = 0; i < scriptMapLines.Count; i++)
        {
            text = FindChineseTool.replaceWord(text, ",", DOT);
            if (scriptMapLines[i].orgText.Contains(text))
                return scriptMapLines[i];
        }
        return null;
    }
    ScriptLineVo GetLine(int id)
    {
        for (int i = 0; i < scriptMapLines.Count; i++)
        {
            if (scriptMapLines[i].id == id)
                return scriptMapLines[i];
        }
        return null;
    }
    void ReplaceScript()
    {
        for (int i = 0; i < scriptMapLines.Count; i++)
        {
            ScriptLineVo line = scriptMapLines[i];
            if (line.orgText.Count == 1 && !line.isStaticText)
            {
                string[] paths = line.path.Split('|');
                string[] lineNums = line.lineNum.Split('|');
                for (int j = 0; j < paths.Length; j++)
                {
                    string[] fileContents = File.ReadAllLines(Application.dataPath + paths[j], Encoding.Default);
                    int lineIdx = int.Parse(lineNums[j]) - 1;
                    if(lineIdx == 0 || lineIdx >= fileContents.Length)
                    {
                        Debug.LogError("替换脚本出错");
                        continue;
                    }
                    string script = fileContents[lineIdx];
                    if (script.IndexOf("StringTools.GetLocalization") == -1)
                    {
                        Debug.Log("替换文本 Path:" + paths[j] + "  script:" + script);
                        string text = FindChineseTool.replaceWord(line.orgText[0], DOT, ",");
                        script = script.Replace(text, "StringTools.GetLocalization(" + line.id + ")");
                        fileContents[lineIdx] = script;
                        FindChineseTool.SaveSciptLines(Application.dataPath + paths[j], fileContents);
                    }
                }
            }
        }
    }
    void ShowOtherLines()
    {
        if (newScriptLines == null) return;
        if (newScriptLines.Count > 0)
            EditorGUILayout.LabelField("总计" + newScriptLines.Count + "需要手动修改国际化的代码 记录如下：");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < newScriptLines.Count; i++)
        {
            ScriptLineVo line = newScriptLines[i];
            if(!line.isCanAutoSet())
            {
                EditorGUILayout.BeginHorizontal();
                string error = (line.isStaticText || line.isCaseText || line.isConstText) ? " 不应该出现中文常量" : "";
                error += line.orgText.Count > 1 ? " 需要字符串拼接" : "";
                EditorGUILayout.LabelField(string.Format("{0} line:[{1}] Error:{2}", line.path, line.lineNum, error));
                if (GUILayout.Button("定位脚本", GUILayout.Width(100)))
                {
                    string path = Application.dataPath + line.path;
                    path = FindChineseTool.replaceWord(path, "/", "\\");
                    bool flag = FindChineseTool.OpenAsset(path, int.Parse(line.lineNum) - 1, _devPath);
                    if (!flag)
                        Debug.LogError("无法定位"+ path);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }
    void reSaveOfficialMapFile(string path)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>"); sb.Append("\n");
        sb.Append("<root>"); sb.Append("\n");
        sb.Append("<Scripts>"); sb.Append("\n");
        List<ScriptLineVo> lines = scriptMapLines;
        for (int i = 0; i < lines.Count; i++)
        {
            /*<Script id="" zh_cn="" zh_tw="">*/
            ScriptLineVo line = lines[i];
            sb.Append(string.Format("<Script id=\"{0}\" zh_cn={1} zh_tw={2} />", line.id, line.zh_cn, line.zh_tw));
            sb.Append("\n");
        }
        sb.Append("</Scripts>"); sb.Append("\n");
        sb.Append("</root>");
        if (File.Exists(path))
            File.Delete(path);
        FileUitl.SaveTextFile(path, sb.ToString());
    }
}
/// <summary>
/// 代码行对象
/// </summary>
public class ScriptLineVo
{
    public int id { get; set; }
    public string path { get; set; }
    public string stript { get; private set; }
    public string lineNum { get; set; }
    public List<string> orgText { get; private set; }
    public bool isStaticText { get; set; }
    public bool isConstText { get; set; }
    public bool isCaseText { get; set; }
    public string zh_cn { get; set; }
    public string zh_tw { get; set; }
    public ScriptLineVo(string path, string stript, string lineNum)
    {
        this.lineNum = lineNum;
        this.path   = path;
        this.stript = stript;

        orgText = new List<string>();
    }
    public bool isCanAutoSet()
    {
        return orgText.Count == 1 && !isStaticText && !isConstText && !isCaseText;
    }
}