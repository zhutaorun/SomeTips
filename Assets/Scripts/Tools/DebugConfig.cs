using UnityEngine;
using System.Collections;

public class DebugConfig
{
//    public UITextList logList;
//    public bool ShowDebugInfo;
//    public bool DebugDrawCall;
//    public bool DebugGameObjMgr;
//    public bool DebugGameMonsterLoad;
//    public bool DebugDynAtlasCount;
//    public bool DebugInitTime;
//    public bool DebugMemory;

//    private uint totalMemory;
//    int colorIdx;
//    Color[] cls = new Color[] { Color.green, Color.red, Color.black, Color.white, Color.blue };
//    GameObject wndDebug;
//    public override void Init()
//    {
//        base.Init();
//        //Application.RegisterLogCallback(HandleLog);

//    }

//    void HandleLog(string logString, string stackTrace, LogType type)
//    {
//        logList.Add(type.ToString() + ":" + logString);
//    }
//    void OnGUI()
//    {

//    }
//    void Update()
//    {
//        if (DebugMemory)
//        {
//            //这里把更衣室遮住啦,想要打印的自己把它解开(by:jialongji)
//            //DebugInfo.Print("RunTime",Profiler.get
//            DebugerInfo.Print("Allocated Memory", Profiler.GetTotalAllocatedMemory() / 1024 / 1024 + "MB");
//            DebugerInfo.Print("UnusedReserved Memory", Profiler.GetTotalUnusedReservedMemory() / 1024 / 1024 + " MB");
//            return;
//        }

//        if (ShowDebugInfo)
//        {
//            if (Input.GetKeyDown(KeyCode.Escape))
//            {
//                DebugerInfo.ShowDebugInfo = !DebugerInfo.ShowDebugInfo;
//                //logList.transform.parent.gameObject.SetActive(DebugInfo.ShowDebugInfo);
//                wndDebug = UIManager.GetWindow("WndDebug");
//                wndDebug.SetActive(DebugerInfo.ShowDebugInfo);
//                if (DebugerInfo.ShowDebugInfo)
//                    wndDebug.SendMessage("SetDepth", 99999999);
//                //				if (DebugInfo.ShowDebugInfo){
//                //					colorIdx++;
//                //					if (colorIdx >= cls.Length)
//                //						colorIdx = 0;
//                //					DebugInfo.infoColor = cls[colorIdx];
//                //				}
//            }
//        }
//    }
}

public static class DebugStopwatch
{
    static float _start;
    static float _last;

    public static void Reset()
    {
        _last = _start = Time.realtimeSinceStartup;
    }

    public static void Lap(string message = "")
    {
        var curr = Time.realtimeSinceStartup;
        //GWDebug.Log(string.Format("Stopwatch - Lap:{0:0.000} Total:{1:0.000} {2}", curr - _last, curr - _start, message));
        _last = curr;
    }
}
