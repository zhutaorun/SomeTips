using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Loader : MonoBehaviour
{
    private static Loader s_instance = null;

    public static Loader Instance
    {
        get
        {
            if (s_instance == null)
            {
                var go = new GameObject("Loader");
                Object.DontDestroyOnLoad(go);
                s_instance = go.AddComponent<Loader>();
            }
            return s_instance;
        }
    }

    /// <summary>
    /// 如果在开始加载之前把这个标记置为真，则在那一次加载的过程中不会显示加载界面，从而成为简单的黑屏切换
    /// </summary>
    public static bool QuickLoad = false;
    public static bool IsLoading = false;

    public static LoadingDomain CurrentDomain;
    public static LoadingDomain NextDomain;

    public static void start<T>() where T : LoadingDomain
    {
        if(IsLoading) return;
        IsLoading = true;

        var go = new GameObject("LoadingDomain<" + typeof (T).Name + ">");
        go.transform.parent = Instance.transform;
        NextDomain = go.AddComponent<T>();

        var prefab = Resources.Load<GameObject>("UI Root (Loading)");
        Object.Instantiate(prefab);
    }

    public static void scheduleBackgroundLoadTask(string name, int priority, IEnumerator coroutine)
    {
        var t = new BackgroundLoadTask();
        t.name = name;
        t.priority = priority;
        t.coroutine = Instance.runBackgroundLoadTask1(t, coroutine);
        Instance.addBackgroundLoadTask(t);
    }

    class BackgroundLoadTask
    {
        public string name;
        public int priority;
        public IEnumerator coroutine;
        public bool isDone;
    }

    private List<BackgroundLoadTask> pendingTasks = new List<BackgroundLoadTask>();
    private BackgroundLoadTask[] runningTasks = new BackgroundLoadTask[4];
    private List<int> idleTaskLines = new List<int>();

    private void addBackgroundLoadTask(BackgroundLoadTask task)
    {
        pendingTasks.Add(task);
    }

    private void Update()
    {
        idleTaskLines.Clear();
        for (int i = 0; i < runningTasks.Lengtj; i++)
        {
            var t = runningTasks[i];
            if (t == null)
            {
                idleTaskLines.Add(i);//计算空闲的task line,但是本帧刚刚完成的任务的task line 不会被统计进来
                continue;
            }
            if (t.isDone)
            {
                runningTasks[i] = null;
            }
        }
        if (pendingTasks.Count > 0 && idleTaskLines.Count > 0)
        {
            pendingTasks.Sort((a, b) => b.priority - a.priority);
            for (int i = 0; pendingTasks.Count > 0 && i < idleTaskLines.Count; ++i)
            {
                var index = idleTaskLines[i];
                var t = runningTasks[index] = pendingTasks[0];
                pendingTasks.RemoveAt(0);
                StartCoroutine(t.coroutine);
            }
        }
    }

    private IEnumerator runBackgroundLoadTask1(BackgroundLoadTask t, IEnumerator coroutine)
    {
        yield return StartCoroutine(coroutine);
        t.isDone = true;
    }

    private IEnumerator runBackgroundLoadTask2(BackgroundLoadTask t, System.Action func)
    {
        var thread = new Thread(() =>
        {
            func();
            t.isDone = true;
        });
        thread.Priority = System.Threading.ThreadPriority.BelowNormal;
        thread.Start();
        do
        {
            yield return null;
        } while (!t.isDone);
    }

}
