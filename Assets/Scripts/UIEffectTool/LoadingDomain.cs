using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LoadingDomain : MonoBehaviour
{
    public static bool Parallel = true;
    public static bool NeverFinish = false;

    public float progress { get; protected set; }

    private List<LoadTask> waitingTasks = new List<LoadTask>();
    private List<LoadTask> processingTasks = new List<LoadTask>();
    private float finishedProgress;
    private float totalProgress;

    private bool parallel;

    public LoadTask addTask(float weight, string name = null)
    {
        var task = new LoadTask();
        task.name = name;
        task.weight = weight;
        waitingTasks.Add(task);
        return task;
    }

    protected IEnumerator doTasksCo()
    {
        parallel = Parallel;
        DebugStopwatch.Reset();
        //如果有条件的话判断条件，如果条件直接不匹配的话，直接短路这个Task,并且移出处理列表
        for (int i = 0; i < waitingTasks.Count; ++i)
        {
            var task = waitingTasks[i];
            if (task.condition != null)
            {
                if(!task.condition()) task.isDone = true;
            }
        }
        waitingTasks.RemoveAll(t => t.isDone);

        //统计全部Task的权重
        for (int i = 0; i < waitingTasks.Count; ++i) totalProgress += waitingTasks[i].weight;

        while (processingTasks.Count >0 || waitingTasks.Count>0)
        {
            bool hasFinishedTask = false;
            float paratialProgress = 0;
            for (int i = processingTasks.Count - 1; i >= 0; --i)
            {
                var task = processingTasks[i];
                if (task.isDone)
                {
                    finishedProgress += task.weight;
                    processingTasks.RemoveAt(i);
                    hasFinishedTask = true;

                    if (!parallel) DebugStopwatch.Lap(task.name ?? "");
                }
                else
                {
                    var p = task.calcProgress == null
                        ? Mathf.Clamp01((Time.time - task._taskStartTime)/task.weight)
                        : task.calcProgress()*task.weight;
                    paratialProgress += P;
                }
            }
            paratialProgress = (finishedProgress + partialProgress)/totalProgress;
            if (parallel)
            {
                if (hasFinishedTask || processingTasks.Count == 0)
                {
                    for (int i = waitingTasks.Count-1; i >=0; --i)
                    {
                        tryDequeueOnWaitingTask(i);
                    }
                }
            }
            else
            {
                if (processingTasks.Count == 0)
                {
                    for (int i = 0; i < waitingTasks.Count; i++)
                    {
                        if(tryDequeueOnWaitingTask(i)) break;
                    }
                }
            }
            yield return null;
        }
        DebugStopwatch.Lap(this.GetType().Name + "Finish");
    }

    private bool tryDequeueOneWaitingTask(int i)
    {
        var task = waitingTask[i];
        if (isAllDependenciesReady(task))
        {
            waitingTask.Remove(i);
            processingTask.Add(task);
            if (task.function != null) task.function();
            if (task.coroutine != null) StartCoroutine(runTask(task));
            else task.isDone = true;
            return true;
        }
        else return false;
    }

    private IEnumerator runTask(LoadTask task)
    {
        task._taskStartTime = Time.time;
        yield return StartCoroutine(task.coroutine);
        task.isDone = true;
    }

    private bool isAllDependenciesReady(LoadTask task)
    {
        for (int i = 0; i < task.dependents.Length; i++)
        {
            if (!task.dependents[i].isDone) return false;
        }
        return true;
    }

    protected virtual void initLoadTasks(){}

    protected virtual IEnumerator postLoadCo() {yield break;}

    public virtual void OnExitDomain(){}

    public virtual IEnumerator loadAndIntializeCoroutine()
    {
        initLoadTasks();
        yield return StartCoroutine(doTasksCo());
        DownloadManager.Instance.StartCoroutine(postLoadCo());
        if (NeverFinish)
        {
            Debug.LogError("Debug: Holding game progress at here forever.");
            do
            {
                yield return null;
            } while (true);
        }
    }

    public class LoadTask
    {
        /// <summary>
        /// 加载任务的名字，用于调试
        /// </summary>
        public string name;

        /// <summary>
        /// 加载时间权重，数字越大代表这个Task在整个加载条件中占据的部分有多大
        /// </summary>
        public float weight = 100;

        /// <summary>
        /// 依赖项目,这个任务会在所有依赖任务都完成之后才会开始执行
        /// </summary>
        public LoadTask[] dependents = new LoadTask[0];

        /// <summary>
        /// 已完成标记，当任务执行完成之后它会自动变成true
        /// </summary>
        public bool isDone = false;

        /// <summary>
        /// 函数形式的加载过程，向这里指定一个函数来决定这个加载任务实际会执行什么行为
        /// 当function和coroutine 同时都是被赋值了时，先执行function后执行corotine.
        /// </summary>
        public System.Action function;

        /// <summary>
        /// 协程形式的加载过程.向这里指定一个函数来决定这个加载任务实际会执行什么行为
        /// 只用当协程退出后，这个任务才被视为完成
        /// 当function和coroutine同时都被赋值了时，先执行function后执行coroutine;
        /// </summary>
        public IEnumerator coroutine;

        /// <summary>
        /// 进度计算函数，如果被赋值，则系统通过来获取此任务的当前进度
        /// </summary>
        public System.Func<float> calcProgress;

        /// <summary>
        /// 条件函数.如果被赋值，则系统根据它执行的结果来决定这个任务是否会被执行或者跳过.
        /// </summary>
        public System.Func<bool> condition;

        public float _taskStartTime;
    }
}
