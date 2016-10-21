using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// 一个支持引用计数的AssetBundle引用对象.
/// 通过该对象你可以加载一个Bundle中的资源,通常在加载过程结束后请及时调用unloadBundle来卸载不必要的内存.
/// 
/// 你可以通过BundleReference.Get获取一个引用对象,并通过Object.Destroy释放该引用对象.
/// 对于一个AssetBundle名称,你取得的所有BundleReference永远指向同一个内部对象.
/// 内部Bundle对象的生存周期遵守引用计数原则,当全部引用它的BundleReference都被释放后,它才会被释放.
/// </summary>

public class BundleReference : ScriptableObject
{
    private static readonly BundleLoadManager mgr = BundleLoadManager.Instance;
    /// <summary>
    /// 取得一个BundleReference
    /// </summary>
    /// <param name="assetBundleName">这个BundleReference所指向的AssetBundle名称</param>
    /// <param name="caller">传入这个Bundle对象的使用者，通常是调用Get函数的脚本对象，用于追踪Bundle的使用和释放情况.</param>
    /// <returns></returns>
    public static BundleReference Get(string assetBundleName, MonoBehaviour caller)
    {
        return mgr.preloadBundle(assetBundleName, caller);
    }

    public string assetBundleName { get { return _bundle.assetBundleName; } }

    public string error { get { return _bundle.error; } }

    public bool isDone { get { return _bundle.isDone; } }

    public bool isSuccess { get { return _bundle.isSuccess; } }

    internal BundleLoadManager.Bundle _bundle;
    internal MonoBehaviour _whoIsReferencingMe;//内部调试使用，让我知道哪个脚本还在引用我，用来追踪内存泄漏

    private BundleLoadManager _mgr;
    internal string _dbgAssetBundleName;

    private BundleReference()
    {
        _mgr = BundleLoadManager.Instance;
    }

    public YieldInstruction waitForFinish()
    {
        return mgr.StartCoroutine(waitForFinishCo());
    }

    public bool contains(string resourceName)
    {
        return _bundle.contains(resourceName);
    }

    public Object load(string resourceName, System.Type type)
    {
        return _bundle.load(resourceName, type);
    }

    public T load<T>(string resourceName) where T : UnityEngine.Object
    {
        return _bundle.load(resourceName, typeof(T)) as T;
    }

    public IEnumerator preloadAsync<T>(string resourceName) where T : UnityEngine.Object
    {
        return _bundle.preloadAsync(resourceName, typeof(T));
    }

    /// 卸载掉内部的AssetBundle.
    /// 在把你想要的资源加载完成之后,请务必调用这个函数卸载AssetBundle所占的内存.
    /// 执行本卸载后,所有这个Bundle中先前从未加载过的资源就无法再加载进来了.
    /// 如果这个Bundle有需求在不可预知的未来可能会加载一些先前没加载过的新资源,请在卸载时使参数guaranteeFutureLoading为True
    /// 
    /// 注意同一个名字的AssetBundle在引用计数归零,被完全销毁之前,管理器总会返回先前的Bundle对象.
    /// 所以如果你的AssetBundle可能会跨越若干个游戏状态,并且无法预测在每个游戏状态有可能会加载其中的哪个资源的话,最好在卸载的时候使参数guaranteeFutureLoading为True
    /// </summary>
    /// <param name="guaranteeFutureLoading">保证未来的加载可以正常运作(会在卸载AssetBundle之前,提前把所有还未加载的对象加载出来)</param>
    public void unloadBundle(bool guaranteeFutureLoading = false)
    {
        _bundle.unloadBundle(guaranteeFutureLoading);
    }

    private IEnumerator waitForFinishCo()
    {
        while (this != null && !isDone) yield return null;
    }

    void OnDestroy()
    {
        if (_bundle != null)
        {
            _dbgAssetBundleName = _bundle.assetBundleName;
            _mgr.notifyRelease(this, _bundle);
            _bundle = null;
        }
        _whoIsReferencingMe = null;
    }

    internal BundleLoadManager.Bundle EDITOR_GetInternalBundle()
    {
        return _bundle;
    }
}
